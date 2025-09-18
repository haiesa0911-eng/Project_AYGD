using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class PieceSnapper : MonoBehaviour
{
    [Header("Board Ref")]
    public BoardGridRef board;

    [Header("Piece Size (dalam cell)")]
    public int sizeRows = 7;
    public int sizeCols = 7;

    public enum PivotAnchor { Center, TopLeft, TopRight, BottomLeft, BottomRight }
    [Header("Anchor")]
    public PivotAnchor pivot = PivotAnchor.Center;

    [Header("Motion")]
    public float moveSnapDuration = 0.08f;

    [Header("Alignment")]
    [Tooltip("Jika diisi, posisi handle ini yang dipakai sebagai pusat footprint (bukan transform root).")]
    public Transform handle;
    [Tooltip("Geser footprint dalam SATUAN CELL. X = kolom (+kanan), Y = baris (+bawah).")]
    public Vector2 footprintOffsetCells = Vector2.zero;
    [Tooltip("Gambar gizmo batas footprint untuk debug alignment.")]
    public bool debugFootprintGizmo = true;

    [Header("Board Area (visual truth)")]
    [Tooltip("Collider area papan (mis. BoxCollider2D pada frame hitam). Bila diisi, dipakai untuk cek 'totally di luar'.")]
    public Collider2D boardAreaCollider;
    [Tooltip("Kurangi AABB papan saat cek overlap agar tepi tidak 'nempel'. Satuan world (contoh 0.02).")]
    public float boardOutsideShrink = 0.02f;

    [Header("Debug")]
    public bool debugDecisions = false;

    // ----- runtime state -----
    private readonly List<SlotCell> occupiedCells = new List<SlotCell>();   // sel yang sedang ditempati SETELAH snap
    private readonly List<SlotCell> tempCoverage = new List<SlotCell>();
    private readonly List<SlotCell> tempOverlapOccupied = new List<SlotCell>();

    // PATCH: backup sel yang ditempati SEBELUM drag untuk keperluan revert
    private readonly List<SlotCell> prevOccupiedAtDragStart = new List<SlotCell>();

    private bool dragging;
    private bool externalControl;
    private Vector3 offset;
    private Vector3 startPos;

    // true jika saat BeginDrag piece sedang terpasang di slot (pernah tersnap)
    private bool snappedAtDragStart;

    void Awake()
    {
        if (!board)
        {
            var go = GameObject.Find("Board_3x4");
            if (go) board = go.GetComponent<BoardGridRef>();
        }
        EnsureHandle();
    }

    // ========================= EVENT INPUT =========================
    void OnMouseDown()
    {
        Vector3 mouse = Camera.main
            ? Camera.main.ScreenToWorldPoint(Input.mousePosition)
            : new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f);
        mouse.z = GetAnchorWorldPos().z;
        StartDragAt(mouse);
    }

    void OnMouseDrag()
    {
        if (!dragging) return;
        Vector3 mouse = Camera.main
            ? Camera.main.ScreenToWorldPoint(Input.mousePosition)
            : new Vector3(Input.mousePosition.x, Input.mousePosition.y, 0f);
        mouse.z = GetAnchorWorldPos().z;
        DragTo(mouse);
    }

    void OnMouseUp()
    {
        if (!dragging) return;
        CommitOrRevert();
    }

    // ========================= API EKSTERNAL =========================
    public void BeginExternalDrag(Vector3 worldAnchor)
    {
        externalControl = true;
        StartDragAt(worldAnchor);
    }

    public void UpdateExternalDrag(Vector3 worldAnchor)
    {
        if (!externalControl || !dragging) return;
        DragTo(worldAnchor);
    }

    public bool EndExternalDrag()
    {
        if (!externalControl || !dragging) return false;
        bool result = CommitOrRevert();
        externalControl = false;
        return result;
    }

    // ========================= LOGIKA =========================
    private void StartDragAt(Vector3 worldAnchor)
    {
        dragging = true;
        startPos = transform.position;
        offset = GetAnchorWorldPos() - worldAnchor;

        // BACKUP state slot saat mulai drag
        prevOccupiedAtDragStart.Clear();
        prevOccupiedAtDragStart.AddRange(occupiedCells);
        snappedAtDragStart = prevOccupiedAtDragStart.Count > 0;

        // Lepaskan klaim sel saat sedang di-drag
        ReleaseCells();

        if (!board) return;
        board.SetAllAlpha(100);
        board.HighlightOccupied(200);
        ApplyFootprintVisuals();
    }

    private void DragTo(Vector3 worldAnchor)
    {
        var targetAnchor = worldAnchor + offset;
        var delta = targetAnchor - GetAnchorWorldPos();
        transform.position += delta;
        if (!board) return;
        ApplyFootprintVisuals();
    }

    private bool CommitOrRevert()
    {
        dragging = false;

        if (board)
        {
            board.ClearHighlights();
            board.RefreshRulerView();
        }

        if (!board)
        {
            ReturnToStart(false); // tidak re-occupy karena tidak tahu grid
            return false;
        }

        FootprintFromPosition(GetAnchorWorldPos(), out int r0, out int c0, out int r1, out int c1);

        var cells = new List<SlotCell>();
        board.CollectCellsClamped(r0, c0, r1, c1, cells);

        bool fullyInside = board.IsRectInside(r0, c0, r1, c1);
        bool allAvailable = fullyInside && board.AreAllAvailable(cells);

        bool outside = IsTotallyOutside();

        if (outside)
        {
            if (snappedAtDragStart)
            {
                // Benar-benar di luar dan sebelumnya tersnap → HANCUR
                ReleaseCells();
                if (debugDecisions) Debug.Log("[PieceSnapper] Outside board → DESTROY");
                Destroy(gameObject);
            }
            else
            {
                // Benar-benar di luar tapi belum pernah snap → kembali ke asal (tanpa re-occupy)
                if (debugDecisions) Debug.Log("[PieceSnapper] Outside board (new) → RETURN");
                ReturnToStart(false);
            }
            board.RefreshRulerView();
            return false;
        }

        if (!allAvailable)
        {
            // Masih menyentuh papan tapi placement tidak valid → Revert dan RE-OCCUPY sel lama
            if (debugDecisions) Debug.Log("[PieceSnapper] Partial/invalid overlap → RETURN & RE-OCCUPY");
            ReturnToStart(true);   // <<=== INI KUNCI: re-occupy agar drag berikutnya dianggap 'dari slot'
            board.RefreshRulerView();
            return false;
        }

        // Valid → SNAP
        Vector3 target = WorldPosForRect(r0, c0, r1, c1);
        StopAllCoroutines();
        StartCoroutine(SnapTo(target));

        foreach (var cell in cells) cell.AddOccupant(this);
        occupiedCells.Clear();
        occupiedCells.AddRange(cells);

        if (debugDecisions) Debug.Log("[PieceSnapper] Valid snap");
        board.RefreshRulerView();
        return true;
    }

    // ========================= HELPERS =========================
    bool IsTotallyOutside()
    {
        if (boardAreaCollider != null)
        {
            GetPieceWorldAABB(out var pieceMin, out var pieceMax, 0f);
            var b = boardAreaCollider.bounds;
            float s = Mathf.Max(0f, boardOutsideShrink);
            Vector2 boardMin = new Vector2(b.min.x + s, b.min.y + s);
            Vector2 boardMax = new Vector2(b.max.x - s, b.max.y - s);
            return !AABBOverlap(pieceMin, pieceMax, boardMin, boardMax);
        }
        else
        {
            // fallback ke grid index
            FootprintFromPosition(GetAnchorWorldPos(), out int r0, out int c0, out int r1, out int c1);
            return !RectOverlapsBoard(r0, c0, r1, c1);
        }
    }

    static bool AABBOverlap(Vector2 aMin, Vector2 aMax, Vector2 bMin, Vector2 bMax)
    {
        if (aMax.x < bMin.x || aMin.x > bMax.x) return false;
        if (aMax.y < bMin.y || aMin.y > bMax.y) return false;
        return true;
    }

    void GetPieceWorldAABB(out Vector2 min, out Vector2 max, float inflate = 0f)
    {
        var col = GetComponent<Collider2D>();
        Bounds b;
        if (col) b = col.bounds;
        else
        {
            var sr = GetComponentInChildren<SpriteRenderer>();
            b = sr ? sr.bounds : new Bounds(transform.position, Vector3.zero);
        }
        min = new Vector2(b.min.x - inflate, b.min.y - inflate);
        max = new Vector2(b.max.x + inflate, b.max.y + inflate);
    }

    bool RectOverlapsBoard(int r0, int c0, int r1, int c1)
    {
        if (!board) return false;
        int maxRow = board.rows - 1;
        int maxCol = board.cols - 1;
        if (c1 < 0) return false;
        if (c0 > maxCol) return false;
        if (r1 < 0) return false;
        if (r0 > maxRow) return false;
        return true;
    }

    void ApplyFootprintVisuals()
    {
        if (!board) return;
        board.SetAllAlpha(100);
        board.HighlightOccupied(200);

        FootprintFromPosition(GetAnchorWorldPos(), out int r0, out int c0, out int r1, out int c1);
        tempCoverage.Clear();
        board.CollectCellsClamped(r0, c0, r1, c1, tempCoverage);
        if (tempCoverage.Count == 0) return;
        SetCellsAlpha(tempCoverage, 200);

        tempOverlapOccupied.Clear();
        for (int i = 0; i < tempCoverage.Count; i++)
        {
            var cell = tempCoverage[i];
            if (cell == null) continue;
            bool occupied =
                (cell.occupants != null && cell.occupants.Count > 0) ||
                (cell.occupiedBy != null);
            if (occupied) tempOverlapOccupied.Add(cell);
        }
        SetCellsAlpha(tempOverlapOccupied, 255);
    }

    void SetCellsAlpha(List<SlotCell> cells, byte alpha)
    {
        if (cells == null) return;
        for (int i = 0; i < cells.Count; i++)
        {
            var v = cells[i] ? cells[i].GetComponent<SlotVisual>() : null;
            if (v) v.SetAlpha(alpha);
        }
    }

    void EnsureHandle()
    {
        if (handle != null) return;
        var sr = GetComponentInChildren<SpriteRenderer>();
        var go = new GameObject("Handle");
        go.transform.SetParent(transform, false);
        if (sr != null)
        {
            Vector3 centerLocal = transform.InverseTransformPoint(sr.bounds.center);
            go.transform.localPosition = centerLocal;
        }
        else go.transform.localPosition = Vector3.zero;
        handle = go.transform;
    }

    Vector3 GetAnchorWorldPos() => handle ? handle.position : transform.position;

    void ReleaseCells()
    {
        if (occupiedCells.Count == 0) return;
        for (int i = 0; i < occupiedCells.Count; i++)
            if (occupiedCells[i]) occupiedCells[i].RemoveOccupant(this);
        occupiedCells.Clear();
    }

    void OnDestroy() => ReleaseCells();

    void FootprintFromPosition(Vector3 worldPos, out int r0, out int c0, out int r1, out int c1)
    {
        board.GetFractionalIndex(worldPos, out float fRow, out float fCol);
        fCol += footprintOffsetCells.x;
        fRow += footprintOffsetCells.y;

        switch (pivot)
        {
            case PivotAnchor.Center:
                {
                    float halfH = (sizeRows - 1) * 0.5f;
                    float halfW = (sizeCols - 1) * 0.5f;
                    r0 = Mathf.RoundToInt(fRow - halfH);
                    r1 = Mathf.RoundToInt(fRow + halfH);
                    c0 = Mathf.RoundToInt(fCol - halfW);
                    c1 = Mathf.RoundToInt(fCol + halfW);
                    break;
                }
            case PivotAnchor.TopLeft:
                {
                    r0 = Mathf.RoundToInt(fRow);
                    c0 = Mathf.RoundToInt(fCol);
                    r1 = r0 + sizeRows - 1;
                    c1 = c0 + sizeCols - 1;
                    break;
                }
            case PivotAnchor.TopRight:
                {
                    r0 = Mathf.RoundToInt(fRow);
                    c1 = Mathf.RoundToInt(fCol);
                    r1 = r0 + sizeRows - 1;
                    c0 = c1 - sizeCols + 1;
                    break;
                }
            case PivotAnchor.BottomLeft:
                {
                    r1 = Mathf.RoundToInt(fRow);
                    c0 = Mathf.RoundToInt(fCol);
                    r0 = r1 - sizeRows + 1;
                    c1 = c0 + sizeCols - 1;
                    break;
                }
            default: // BottomRight
                {
                    r1 = Mathf.RoundToInt(fRow);
                    c1 = Mathf.RoundToInt(fCol);
                    r0 = r1 - sizeRows + 1;
                    c0 = c1 - sizeCols + 1;
                    break;
                }
        }
    }

    Vector3 WorldPosForRect(int r0, int c0, int r1, int c1)
    {
        Vector3 p00 = board.WorldPosOf(r0, c0);
        Vector3 p11 = board.WorldPosOf(r1, c1);
        return (p00 + p11) * 0.5f;
    }

    IEnumerator SnapTo(Vector3 target)
    {
        Vector3 start = transform.position;
        float t = 0f;
        while (t < moveSnapDuration)
        {
            t += Time.deltaTime;
            float k = Mathf.Clamp01(t / moveSnapDuration);
            transform.position = Vector3.Lerp(start, target, k);
            yield return null;
        }
        transform.position = target;
    }

    // Return ke start, dengan opsi RE-OCCUPY sel sebelum drag
    void ReturnToStart(bool reoccupy)
    {
        StopAllCoroutines();
        StartCoroutine(Co_ReturnAndMaybeReoccupy(reoccupy));
    }

    IEnumerator Co_ReturnAndMaybeReoccupy(bool reoccupy)
    {
        yield return StartCoroutine(SnapTo(startPos));

        if (reoccupy && prevOccupiedAtDragStart.Count > 0)
        {
            foreach (var cell in prevOccupiedAtDragStart)
                if (cell != null) cell.AddOccupant(this);

            occupiedCells.Clear();
            occupiedCells.AddRange(prevOccupiedAtDragStart);
        }
    }

    // ========== PATCH: ALIGNMENT API (PUBLIC) ==========
    // Disisipkan sebelum bagian Gizmo agar mudah ditemukan & tidak mengganggu logika lain.

    /// <summary>True jika piece saat ini sedang tersnap (menempati ≥1 slot).</summary>
    public bool IsSnapped => occupiedCells != null && occupiedCells.Count > 0;

    /// <summary>Ambil rect indeks footprint saat ini (wajib sudah snapped).</summary>
    public bool TryGetCurrentRect(out int r0, out int c0, out int r1, out int c1)
    {
        r0 = c0 = r1 = c1 = 0;
        if (!IsSnapped || board == null) return false;

        int minR = int.MaxValue, maxR = int.MinValue;
        int minC = int.MaxValue, maxC = int.MinValue;

        for (int i = 0; i < occupiedCells.Count; i++)
        {
            var cell = occupiedCells[i];
            if (cell == null) continue;
            if (cell.row < minR) minR = cell.row;
            if (cell.row > maxR) maxR = cell.row;
            if (cell.col < minC) minC = cell.col;
            if (cell.col > maxC) maxC = cell.col;
        }

        if (minR == int.MaxValue) return false;
        r0 = minR; r1 = maxR; c0 = minC; c1 = maxC;
        return true;
    }

    /// <summary>Snap paksa ke rect target jika semua cell tersedia (inside + tidak occupied).</summary>
    public bool TrySnapToRect(int r0, int c0, int r1, int c1, bool animate = true)
    {
        if (board == null) return false;
        if (!board.IsRectInside(r0, c0, r1, c1)) return false;

        var cells = new List<SlotCell>();
        board.CollectCellsClamped(r0, c0, r1, c1, cells);
        if (!board.AreAllAvailable(cells)) return false;

        // Lepas klaim lama, lalu klaim baru
        ReleaseCells();

        Vector3 target = WorldPosForRect(r0, c0, r1, c1);
        StopAllCoroutines();
        if (animate) StartCoroutine(SnapTo(target));
        else transform.position = target;

        for (int i = 0; i < cells.Count; i++) cells[i].AddOccupant(this);
        occupiedCells.Clear();
        occupiedCells.AddRange(cells);

        board.RefreshRulerView();
        return true;
    }

    public bool AlignHorizontalCenter(bool animate = true)
    {
        if (!IsSnapped || board == null) return false;
        if (!TryGetCurrentRect(out int r0, out int _, out int r1, out int _c1)) return false;

        int height = r1 - r0 + 1;
        int width = sizeCols; // pakai footprint deklaratif agar konsisten

        float boardCenter = (board.cols - 1) * 0.5f;
        int newC0 = Mathf.RoundToInt(boardCenter - (width - 1) * 0.5f);
        int newC1 = newC0 + width - 1;

        // Clamp ke dalam board
        if (newC0 < 0) { newC1 -= newC0; newC0 = 0; }
        if (newC1 > board.cols - 1) { int diff = newC1 - (board.cols - 1); newC1 -= diff; newC0 -= diff; }

        return TrySnapToRect(r0, newC0, r0 + height - 1, newC1, animate);
    }

    public bool AlignVerticalCenter(bool animate = true)
    {
        if (!IsSnapped || board == null) return false;
        if (!TryGetCurrentRect(out int r0, out int c0, out int r1, out int c1)) return false;

        int width = c1 - c0 + 1;   // pakai lebar aktual
        int height = sizeRows;      // pakai footprint deklaratif

        float boardMid = (board.rows - 1) * 0.5f;
        int newR0 = Mathf.RoundToInt(boardMid - (height - 1) * 0.5f);
        int newR1 = newR0 + height - 1;

        if (newR0 < 0) { newR1 -= newR0; newR0 = 0; }
        if (newR1 > board.rows - 1) { int diff = newR1 - (board.rows - 1); newR1 -= diff; newR0 -= diff; }

        return TrySnapToRect(newR0, c0, newR1, c0 + width - 1, animate);
    }

    public bool AlignLeft(bool animate = true)
    {
        if (!IsSnapped || board == null) return false;
        if (!TryGetCurrentRect(out int r0, out int c0, out int r1, out int c1)) return false;

        int width = c1 - c0 + 1;
        int newC0 = 0;
        int newC1 = newC0 + width - 1;

        return TrySnapToRect(r0, newC0, r1, newC1, animate);
    }

    public bool AlignRight(bool animate = true)
    {
        if (!IsSnapped || board == null) return false;
        if (!TryGetCurrentRect(out int r0, out int c0, out int r1, out int c1)) return false;

        int width = c1 - c0 + 1;
        int newC1 = board.cols - 1;
        int newC0 = newC1 - width + 1;

        return TrySnapToRect(r0, newC0, r1, newC1, animate);
    }

    public bool AlignTop(bool animate = true)
    {
        if (!IsSnapped || board == null) return false;
        if (!TryGetCurrentRect(out int r0, out int c0, out int r1, out int c1)) return false;

        int height = r1 - r0 + 1;
        int newR0 = 0;
        int newR1 = newR0 + height - 1;

        return TrySnapToRect(newR0, c0, newR1, c1, animate);
    }

    public bool AlignBottom(bool animate = true)
    {
        if (!IsSnapped || board == null) return false;
        if (!TryGetCurrentRect(out int r0, out int c0, out int r1, out int c1)) return false;

        int height = r1 - r0 + 1;
        int newR1 = board.rows - 1;
        int newR0 = newR1 - height + 1;

        return TrySnapToRect(newR0, c0, newR1, c1, animate);
    }

    // ----------------- Gizmo -----------------
    void OnDrawGizmosSelected()
    {
        if (!debugFootprintGizmo || board == null) return;

        FootprintFromPosition(GetAnchorWorldPos(), out int r0, out int c0, out int r1, out int c1);

        Gizmos.color = new Color(1f, 0.85f, 0f, 0.35f);
        for (int r = r0; r <= r1; r++)
            for (int c = c0; c <= c1; c++)
            {
                if (!board.IsRectInside(r, c, r, c)) continue;
                var center = board.WorldPosOf(r, c);

                if (boardAreaCollider != null)
                {
                    var b = boardAreaCollider.bounds;
                    float s = Mathf.Max(0f, boardOutsideShrink);
                    if (center.x < b.min.x + s || center.x > b.max.x - s ||
                        center.y < b.min.y + s || center.y > b.max.y - s)
                        continue;
                }

                Gizmos.DrawWireCube(center, new Vector3(1f, 1f, 0.01f));
            }

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(GetAnchorWorldPos(), 0.05f);
    }
}
