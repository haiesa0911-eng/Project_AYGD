using UnityEngine;

/// <summary>
/// Menampilkan kotak seleksi (outline biru) saat objek diseleksi.
/// Bisa diaktifkan via klik, Spawner, atau SelectionManager.
/// Kompatibel SpriteRenderer (2D) & MeshRenderer (mis. TMP 3D/world-space).
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class SelectionBox : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Jika null, otomatis cari SpriteRenderer di object/child.")]
    public SpriteRenderer spriteRef;

    [Tooltip("Jika null & tidak ada SpriteRenderer, akan cari MeshRenderer/Renderer di object/child.")]
    public Renderer targetRenderer;

    [Header("Style")]
    public Color boxColor = new Color(0.22f, 0.6f, 1f, 1f);
    [Tooltip("Ketebalan garis dalam world units.")]
    public float lineWidth = 0.035f;
    [Tooltip("Padding ekstra (world units) di X/Y untuk kotak seleksi.")]
    public Vector2 padding = new Vector2(0.06f, 0.06f);
    [Tooltip("Sorting Order offset agar garis di atas target.")]
    public int sortingOrderOffset = 10;
    [Tooltip("Offset Z agar garis tidak z-fighting dengan target.")]
    public float zOffset = -0.01f;

    [Header("Behavior")]
    [Tooltip("Jika true, garis akan terlihat saat pointer hover (tanpa klik).")]
    public bool showOnHover = false;

    // --- runtime ---
    private LineRenderer lr;
    private bool selected;
    private bool hovered;

    public bool IsSelected => selected;

    void Awake()
    {
        // Cari target SpriteRenderer lebih dulu
        if (!spriteRef) spriteRef = GetComponentInChildren<SpriteRenderer>();

        // Tentukan targetRenderer (prioritas: spriteRef -> MeshRenderer -> Renderer umum)
        if (!targetRenderer)
        {
            if (spriteRef) targetRenderer = spriteRef;
            else
            {
                targetRenderer = GetComponentInChildren<MeshRenderer>();
                if (!targetRenderer) targetRenderer = GetComponentInChildren<Renderer>();
            }
        }

        // Siapkan LineRenderer
        lr = GetComponent<LineRenderer>();
        if (!lr) lr = gameObject.AddComponent<LineRenderer>();

        lr.useWorldSpace = true;
        lr.loop = true;
        lr.numCornerVertices = 0;
        lr.numCapVertices = 0;
        lr.positionCount = 4;

        // Material default yang mendukung sorting layer
        var mat = new Material(Shader.Find("Sprites/Default"));
        lr.material = mat;

        ApplyStyleToLineRenderer();
        SyncSortingToTarget();

        lr.enabled = false; // awalnya tidak terlihat
    }

    void LateUpdate()
    {
        // Sinkronkan style bila diubah dari Inspector saat play
        ApplyStyleToLineRenderer();
        SyncSortingToTarget();

        bool shouldShow = selected || (showOnHover && hovered);
        if (shouldShow)
        {
            UpdateBox();
            lr.enabled = true;
        }
        else
        {
            lr.enabled = false;
        }
    }

    private void ApplyStyleToLineRenderer()
    {
        if (!lr) return;
        lr.startColor = lr.endColor = boxColor;
        lr.widthMultiplier = lineWidth;
    }

    private void SyncSortingToTarget()
    {
        if (!lr) return;

        // Jika ada SpriteRenderer, pakai sorting-nya
        if (spriteRef)
        {
            lr.sortingLayerID = spriteRef.sortingLayerID;
            lr.sortingOrder = spriteRef.sortingOrder + sortingOrderOffset;
            return;
        }

        // Jika tidak ada, tapi ada Renderer umum (MeshRenderer, dll.)
        if (targetRenderer)
        {
            lr.sortingLayerID = targetRenderer.sortingLayerID;
            lr.sortingOrder = targetRenderer.sortingOrder + sortingOrderOffset;
        }
    }

    private void UpdateBox()
    {
        // --- 1) Prioritas: SpriteRenderer (2D sprite) ---
        if (spriteRef && spriteRef.sprite)
        {
            // Bounds lokal sprite (menghormati pivot/PPU)
            Bounds lb = spriteRef.sprite.bounds;

            // Padding world → konversi kira-kira ke lokal agar konsisten
            Vector3 padLocal = new Vector3(
                padding.x / Mathf.Max(0.0001f, Mathf.Abs(spriteRef.transform.lossyScale.x)),
                padding.y / Mathf.Max(0.0001f, Mathf.Abs(spriteRef.transform.lossyScale.y)),
                0f);

            Vector3 minL = lb.min - padLocal;
            Vector3 maxL = lb.max + padLocal;

            // 4 sudut lokal
            Vector3[] localCorners =
            {
                new Vector3(minL.x, minL.y, 0f),
                new Vector3(maxL.x, minL.y, 0f),
                new Vector3(maxL.x, maxL.y, 0f),
                new Vector3(minL.x, maxL.y, 0f)
            };

            // Transform ke world mengikuti transform sprite
            var m = spriteRef.transform.localToWorldMatrix;
            Vector3[] worldCorners = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                var w = m.MultiplyPoint3x4(localCorners[i]);
                w.z += zOffset;
                worldCorners[i] = w;
            }

            lr.SetPositions(worldCorners);
            return;
        }

        // --- 2) Prioritas berikut: MeshRenderer/Renderer umum (contoh: TMP world-space) ---
        if (targetRenderer)
        {
            // Gunakan localBounds agar rotasi/scale dihormati
            Bounds lb = targetRenderer.localBounds;
            Transform t = targetRenderer.transform;

            // Padding dihitung relatif skala agar konsisten
            var sx = Mathf.Max(0.0001f, Mathf.Abs(t.lossyScale.x));
            var sy = Mathf.Max(0.0001f, Mathf.Abs(t.lossyScale.y));
            Vector3 padLocal = new Vector3(padding.x / sx, padding.y / sy, 0f);

            Vector3 minL = lb.min - padLocal;
            Vector3 maxL = lb.max + padLocal;

            Vector3[] localCorners =
            {
                new Vector3(minL.x, minL.y, 0f),
                new Vector3(maxL.x, minL.y, 0f),
                new Vector3(maxL.x, maxL.y, 0f),
                new Vector3(minL.x, maxL.y, 0f)
            };

            var m = t.localToWorldMatrix;
            Vector3[] worldCorners = new Vector3[4];
            for (int i = 0; i < 4; i++)
            {
                var w = m.MultiplyPoint3x4(localCorners[i]);
                w.z += zOffset;
                worldCorners[i] = w;
            }

            lr.SetPositions(worldCorners);
            return;
        }

        // --- 3) Fallback terakhir: Collider2D AABB (world) ---
        var col = GetComponent<Collider2D>();
        if (!col) return;

        var b = col.bounds;
        // Catatan: zOffset hanya ditambahkan SEKALI di SetRectFromWorldAABB
        var pmin = new Vector3(b.min.x - padding.x, b.min.y - padding.y, transform.position.z);
        var pmax = new Vector3(b.max.x + padding.x, b.max.y + padding.y, transform.position.z);
        SetRectFromWorldAABB(pmin, pmax);
    }

    private void SetRectFromWorldAABB(Vector3 min, Vector3 max)
    {
        // Tambahkan zOffset di sini saja (hindari double offset)
        float z = min.z + zOffset;

        Vector3[] pts = new Vector3[4];
        pts[0] = new Vector3(min.x, min.y, z);
        pts[1] = new Vector3(max.x, min.y, z);
        pts[2] = new Vector3(max.x, max.y, z);
        pts[3] = new Vector3(min.x, max.y, z);
        lr.SetPositions(pts);
    }

    // ---- Input mouse langsung di world (butuh Collider2D) ----
    void OnMouseDown()
    {
        bool additive =
            Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift) ||
            Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);

        if (SelectionManager.I != null)
            SelectionManager.I.Select(this, additive);
        else
            SetSelected(true);
    }

    void OnMouseEnter() { hovered = true; }
    void OnMouseExit() { hovered = false; }

    // ===================== PUBLIC API =====================
    /// <summary>Set status selected (dipanggil oleh SelectionManager/Spawner).</summary>
    public void SetSelected(bool v) { selected = v; }

    /// <summary>Nyalakan selection (alias SetSelected(true)).</summary>
    public void Show() => SetSelected(true);

    /// <summary>Matikan selection (alias SetSelected(false)).</summary>
    public void Hide() => SetSelected(false);
    void OnDisable() { if (SelectionManager.I) SelectionManager.I.Deselect(this); }
    void OnDestroy() { if (SelectionManager.I) SelectionManager.I.Deselect(this); }

}
