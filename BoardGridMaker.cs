using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System;

/// Pasang di GameObject "Board".
/// Pilih Board Source sesuai konteks (UI: RectTransform; World: SpriteRenderer/BoxCollider2D/Manual).
/// Klik kanan komponen -> "Generate Grid" untuk membuat slot yang ukurannya menyesuaikan board.
public class BoardGridMakerFlexible : MonoBehaviour
{
    public enum BoardSource
    {
        Auto,           // coba RectTransform -> SpriteRenderer -> BoxCollider2D; fallback ke Manual
        RectTransform,  // UI / Canvas
        SpriteRenderer, // World 2D pakai SpriteRenderer (mis. kotak putih)
        BoxCollider2D,  // World 2D pakai BoxCollider2D
        Manual          // Masukkan width/height sendiri (world units atau pixels untuk UI)
    }

    [Header("Board")]
    public BoardSource boardSource = BoardSource.Auto;
    [Tooltip("Untuk Manual, satuan mengikuti konteks: UI=px, World=unit dunia")]
    public Vector2 manualBoardSize = new Vector2(4, 4);

    [Header("Grid Settings")]
    [Min(1)] public int rows = 4;
    [Min(1)] public int cols = 4;
    [Min(0f)] public float gutter = 0.1f;  // UI: pixel | World: unit dunia

    [Header("Slot Prefab")]
    [Tooltip("Prefab slot (WAJIB). Akan diatur ukurannya agar pas cell.")]
    public GameObject slotPrefab;

    [Tooltip("Nama parent otomatis untuk menampung slot")]
    public string slotsParentName = "_Slots";

    [Header("Mode")]
    [Tooltip("Centang bila board berada dalam UI/Canvas (RectTransform). " +
             "Jika BoardSource=Auto, ini memperkuat deteksi ke UI.")]
    public bool isUI = false;

    [Header("Gizmo (World mode)")]
    public bool drawGizmos = true;
    public Color gizmoColor = new Color(0f, 0.7f, 1f, 0.25f);

    Transform slotsParent;

    void EnsureParent()
    {
        if (slotsParent != null) return;
        var t = transform.Find(slotsParentName);
        if (t == null)
        {
            var go = new GameObject(slotsParentName);
            go.transform.SetParent(transform, false);
            slotsParent = go.transform;

            if (isUI && TryGetComponent<RectTransform>(out _))
            {
                // Pastikan parent slots juga RectTransform utk UI
                var rt = go.AddComponent<RectTransform>();
                var parentRT = GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0.5f, 0.5f);
                rt.anchorMax = new Vector2(0.5f, 0.5f);
                rt.pivot = new Vector2(0.5f, 0.5f);
                rt.anchoredPosition = Vector2.zero;
                rt.sizeDelta = parentRT.rect.size;
            }
        }
        else slotsParent = t;
    }

    bool TryGetBoardSize(out Vector2 size, out bool uiMode)
    {
        uiMode = isUI;

        // Auto-detect
        if (boardSource == BoardSource.Auto)
        {
            if (TryGetComponent<RectTransform>(out RectTransform rt))
            {
                size = rt.rect.size;
                uiMode = true; // UI mode
                return true;
            }

            var sr = GetComponent<SpriteRenderer>();
            if (sr && sr.sprite != null)
            {
                size = sr.bounds.size; // world units
                uiMode = false;
                return true;
            }

            var bc = GetComponent<BoxCollider2D>();
            if (bc)
            {
                // bounds.size sudah termasuk lossyScale
                size = bc.bounds.size; // world units
                uiMode = false;
                return true;
            }

            // fallback ke Manual
            size = manualBoardSize;
            // Jika tidak ada RectTransform, anggap world
            uiMode = isUI && TryGetComponent<RectTransform>(out _);
            return true;
        }

        // Specific source
        switch (boardSource)
        {
            case BoardSource.RectTransform:
                {
                    if (TryGetComponent<RectTransform>(out RectTransform rt))
                    {
                        size = rt.rect.size; // pixels
                        uiMode = true;
                        return true;
                    }
                    break;
                }
            case BoardSource.SpriteRenderer:
                {
                    var sr = GetComponent<SpriteRenderer>();
                    if (sr && sr.sprite != null)
                    {
                        size = sr.bounds.size; // world
                        uiMode = false;
                        return true;
                    }
                    break;
                }
            case BoardSource.BoxCollider2D:
                {
                    var bc = GetComponent<BoxCollider2D>();
                    if (bc)
                    {
                        size = bc.bounds.size; // world
                        uiMode = false;
                        return true;
                    }
                    break;
                }
            case BoardSource.Manual:
                {
                    size = manualBoardSize;
                    // UI ditentukan oleh flag isUI
                    uiMode = isUI && TryGetComponent<RectTransform>(out _);
                    return true;
                }
        }

        size = Vector2.one * 4f;
        uiMode = isUI;
        return false;
    }

    // Hitung ukuran cell dan origin (titik start kiri-atas ke kanan-bawah)
    void ComputeLayout(Vector2 boardSize, bool uiMode, out Vector2 cellSize, out Vector2 start)
    {
        float totalGutterW = (cols - 1) * gutter;
        float totalGutterH = (rows - 1) * gutter;

        float cw = (boardSize.x - totalGutterW) / cols;
        float ch = (boardSize.y - totalGutterH) / rows;
        cellSize = new Vector2(cw, ch);

        // Di ruang lokal board, 0,0 = pusat (baik UI maupun World kita treat simetris)
        // start = center-top-left corner of first cell
        start = new Vector2(-boardSize.x * 0.5f + cw * 0.5f,
                             boardSize.y * 0.5f - ch * 0.5f);
    }

    Vector3 LocalPosFromRC(int r, int c, Vector2 cellSize, Vector2 start, bool uiMode)
    {
        float x = start.x + c * (cellSize.x + gutter);
        float y = start.y - r * (cellSize.y + gutter);
        return new Vector3(x, y, 0f);
    }

    void FitSlotToCell(GameObject go, Vector2 cellSize, bool uiMode)
    {
        if (uiMode)
        {
            // UI: pakai RectTransform & sizeDelta
            var rt = go.GetComponent<RectTransform>();
            if (rt == null) rt = go.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(0.5f, 0.5f);
            rt.anchorMax = new Vector2(0.5f, 0.5f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.sizeDelta = cellSize;
            rt.localScale = Vector3.one;
        }
        else
        {
            // World: skala berdasarkan Renderer bounds (fallback 1x1 bila tidak ada)
            // Ambil ukuran asli prefab (approx)
            Vector2 prefabSize = new Vector2(1f, 1f);
            var rend = go.GetComponentInChildren<Renderer>();
            if (rend != null)
            {
                var b = rend.bounds.size;
                // Konversi ke local space (hilangkan lossyScale) agar akurat
                Vector3 lossy = go.transform.lossyScale;
                float w = (lossy.x != 0f) ? (b.x / lossy.x) : b.x;
                float h = (lossy.y != 0f) ? (b.y / lossy.y) : b.y;
                prefabSize = new Vector2(Mathf.Max(0.0001f, w), Mathf.Max(0.0001f, h));
            }

            float sx = cellSize.x / prefabSize.x;
            float sy = cellSize.y / prefabSize.y;
            // jaga z=1
            go.transform.localScale = new Vector3(sx, sy, 1f);
        }
    }

    void ClearOld()
    {
        if (slotsParent == null) return;
        for (int i = slotsParent.childCount - 1; i >= 0; i--)
        {
#if UNITY_EDITOR
            if (!Application.isPlaying) DestroyImmediate(slotsParent.GetChild(i).gameObject);
            else Destroy(slotsParent.GetChild(i).gameObject);
#else
            DestroyImmediate(slotsParent.GetChild(i).gameObject);
#endif
        }
    }

    [ContextMenu("Generate Grid")]
    public void GenerateGrid()
    {
        if (slotPrefab == null)
        {
            Debug.LogError("[BoardGridMakerFlexible] slotPrefab belum diisi.");
            return;
        }

        EnsureParent();

        if (!TryGetBoardSize(out Vector2 boardSize, out bool uiMode))
        {
            Debug.LogWarning("[BoardGridMakerFlexible] Gagal deteksi size board. Memakai Manual/Default.");
        }

        ClearOld();

        // Bila UI, pastikan parent slots memiliki RectTransform agar anchoring bekerja
        if (uiMode && slotsParent.GetComponent<RectTransform>() == null)
            slotsParent.gameObject.AddComponent<RectTransform>();

        ComputeLayout(boardSize, uiMode, out Vector2 cellSize, out Vector2 start);

        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                var go = Instantiate(slotPrefab, slotsParent);
                go.name = $"Slot_{r}_{c}";

                Vector3 localPos = LocalPosFromRC(r, c, cellSize, start, uiMode);

                if (uiMode)
                {
                    var rt = go.GetComponent<RectTransform>();
                    if (rt == null) rt = go.AddComponent<RectTransform>();
                    rt.anchorMin = new Vector2(0.5f, 0.5f);
                    rt.anchorMax = new Vector2(0.5f, 0.5f);
                    rt.pivot = new Vector2(0.5f, 0.5f);
                    rt.anchoredPosition = localPos;
                    rt.localRotation = Quaternion.identity;
                }
                else
                {
                    go.transform.localPosition = localPos;
                    go.transform.localRotation = Quaternion.identity;
                }

                FitSlotToCell(go, cellSize, uiMode);

                // contoh pengisian koordinat jika Anda punya komponen SlotCell
                var cell = go.GetComponent<SlotCell>();
                if (cell == null) cell = go.AddComponent<SlotCell>();
                cell.row = r; cell.col = c;
            }

#if UNITY_EDITOR
        // pilih parent agar terlihat di hierarchy setelah generate
        Selection.activeObject = slotsParent.gameObject;
#endif
    }

    void OnDrawGizmos()
    {
        if (!drawGizmos || slotPrefab == null) return;

        // Gambar hanya untuk World mode agar tidak mengganggu UI
        if (!TryGetBoardSize(out Vector2 boardSize, out bool uiMode) || uiMode) return;

        ComputeLayout(boardSize, false, out Vector2 cellSize, out Vector2 start);

        Gizmos.color = gizmoColor;
        for (int r = 0; r < rows; r++)
            for (int c = 0; c < cols; c++)
            {
                var local = LocalPosFromRC(r, c, cellSize, start, false);
                var worldCenter = transform.TransformPoint(local);
                Gizmos.DrawWireCube(worldCenter, new Vector3(cellSize.x, cellSize.y, 0.01f));
            }
    }
}
