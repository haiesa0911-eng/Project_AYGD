using UnityEngine;
using System.Collections.Generic;

public class BoardGridRef : MonoBehaviour
{
    [Header("Samakan dengan BoardGridMaker")]
    public int rows;
    public int cols;
    public float gutter = 0.1f;          // hanya fallback
    public string slotsParentName = "_Slots";

    [Header("Force Visible (untuk Button Ruler)")]
    public bool forceVisible = false;
    [Range(0, 255)] public byte forcedAlpha = 100;

    // Index data
    private Dictionary<(int r, int c), SlotCell> map;
    private readonly List<SlotVisual> visuals = new List<SlotVisual>();
    private Transform slotsParent;

    // Kalibrasi berbasis vektor
    private bool calibrated;
    private Vector3 originWorld;     // pusat cell (0,0)
    private Vector3 ex;              // vektor 1 kolom ke kanan: (0,1) - (0,0)
    private Vector3 er;              // vektor 1 baris ke bawah: (1,0) - (0,0)
    private float ex2;               // dot(ex, ex)
    private float er2;               // dot(er, er)

    void Awake() => BuildIndex();

    public void BuildIndex()
    {
        map = new Dictionary<(int, int), SlotCell>();
        visuals.Clear();
        calibrated = false;

        slotsParent = transform.Find(slotsParentName);
        if (!slotsParent)
        {
            Debug.LogError($"[BoardGridRef] Parent '{slotsParentName}' tidak ditemukan.");
            return;
        }

        foreach (Transform t in slotsParent)
        {
            var cell = t.GetComponent<SlotCell>();
            if (!cell) continue;
            map[(cell.row, cell.col)] = cell;

            var vis = t.GetComponent<SlotVisual>();
            if (vis) visuals.Add(vis);
        }

        CalibrateFromScene();
    }

    void CalibrateFromScene()
    {
        if (!TryGetCell(0, 0, out var c00))
        {
            Debug.LogWarning("[BoardGridRef] Cell (0,0) tidak ditemukan. Pakai fallback.");
            FallbackCalibrate();
            return;
        }

        originWorld = c00.transform.position;

        bool got01 = TryGetCell(0, 1, out var c01);
        bool got10 = TryGetCell(1, 0, out var c10);

        if (!got01 || !got10)
        {
            Debug.LogWarning("[BoardGridRef] Tidak menemukan (0,1) atau (1,0). Pakai fallback.");
            FallbackCalibrate();
            return;
        }

        ex = c01.transform.position - originWorld;   // ke kanan (kolom +1)
        er = c10.transform.position - originWorld;   // ke bawah (baris +1)

        ex2 = Vector3.Dot(ex, ex);
        er2 = Vector3.Dot(er, er);

        // keamanan
        if (ex2 < 1e-8f || er2 < 1e-8f)
        {
            Debug.LogWarning("[BoardGridRef] Vektor basis terlalu kecil. Pakai fallback.");
            FallbackCalibrate();
            return;
        }

        calibrated = true;
    }

    void FallbackCalibrate()
    {
        // fallback asumsi grid terpusat (tanpa rotasi)
        float totalW = cols + (cols - 1) * gutter;
        float totalH = rows + (rows - 1) * gutter;
        float startX = -totalW / 2f + 0.5f;
        float startY = totalH / 2f - 0.5f;

        originWorld = transform.TransformPoint(new Vector3(startX, startY, 0f));
        var step = 1f + gutter;
        ex = transform.TransformVector(new Vector3(step, 0f, 0f));
        er = transform.TransformVector(new Vector3(0f, -step, 0f));

        ex2 = Vector3.Dot(ex, ex);
        er2 = Vector3.Dot(er, er);
        calibrated = true;
    }

    // ----------------- API lookup -----------------
    public bool TryGetCell(int r, int c, out SlotCell cell)
    {
        cell = null;
        return map != null && map.TryGetValue((r, c), out cell);
    }

    public Vector3 WorldPosOf(int r, int c)
    {
        if (!calibrated) CalibrateFromScene();
        return originWorld + ex * c + er * r;
    }

    // fr = baris fraksional; fc = kolom fraksional
    public void GetFractionalIndex(Vector3 worldPos, out float fr, out float fc)
    {
        if (!calibrated) CalibrateFromScene();

        Vector3 d = worldPos - originWorld;
        // proyeksikan ke basis
        fc = Vector3.Dot(d, ex) / ex2;   // kolom
        fr = Vector3.Dot(d, er) / er2;   // baris (ke bawah positif)
    }

    public SlotCell NearestCell(Vector3 worldPos)
    {
        GetFractionalIndex(worldPos, out float fr, out float fc);
        int r = Mathf.Clamp(Mathf.RoundToInt(fr), 0, rows - 1);
        int c = Mathf.Clamp(Mathf.RoundToInt(fc), 0, cols - 1);
        TryGetCell(r, c, out var cell);
        return cell;
    }

    // ----------------- Highlight helpers -----------------
    public void ClearHighlights()
    {
        if (forceVisible)
        {
            SetAllAlpha(forcedAlpha);
            return;
        }
        for (int i = 0; i < visuals.Count; i++) visuals[i].SetAlpha(0);
    }

    public void SetAllAlpha(byte a)
    {
        if (forceVisible) a = forcedAlpha;
        for (int i = 0; i < visuals.Count; i++) visuals[i].SetAlpha(a);
    }

    public void HighlightOnly(ICollection<SlotCell> selected, byte selectedAlpha, byte othersAlpha)
    {
        if (forceVisible)
        {
            SetAllAlpha(forcedAlpha);
            return;
        }

        for (int i = 0; i < visuals.Count; i++) visuals[i].SetAlpha(othersAlpha);
        foreach (var s in selected)
        {
            if (!s) continue;
            var v = s.GetComponent<SlotVisual>();
            if (v) v.SetAlpha(selectedAlpha);
        }
    }

    // Tampilkan semua cell yang SUDAH TERISI (1+ occupant) dengan alpha tertentu.
    // Tidak mengubah cell lain (biarkan baseline dari luar).
    public void HighlightOccupied(byte alpha)
    {
        if (map == null || map.Count == 0) return;

        foreach (var kv in map)
        {
            var cell = kv.Value;
            if (cell == null) continue;

            bool occupied =
                (cell.occupants != null && cell.occupants.Count > 0) ||
                (cell.occupiedBy != null); // back-compat

            if (!occupied) continue;

            var v = cell.GetComponent<SlotVisual>();
            if (v) v.SetAlpha(alpha);
        }
    }

    // Helper agar mode Ruler langsung menampilkan baseline + occupied
    public void RefreshRulerView()
    {
        if (!forceVisible) return;
        SetAllAlpha(forcedAlpha); // baseline (mis. 100)
        HighlightOccupied(200);   // occupied
    }

    // ----------------- Rect helpers -----------------
    public bool IsRectInside(int r0, int c0, int r1, int c1)
    {
        return r0 >= 0 && c0 >= 0 && r1 < rows && c1 < cols;
    }

    public void CollectCellsClamped(int r0, int c0, int r1, int c1, List<SlotCell> outList)
    {
        outList.Clear();
        int rr0 = Mathf.Max(0, r0);
        int cc0 = Mathf.Max(0, c0);
        int rr1 = Mathf.Min(rows - 1, r1);
        int cc1 = Mathf.Min(cols - 1, c1);

        for (int r = rr0; r <= rr1; r++)
            for (int c = cc0; c <= cc1; c++)
                if (TryGetCell(r, c, out var cell)) outList.Add(cell);
    }

    public bool AreAllAvailable(IList<SlotCell> cells)
    {
        if (cells == null || cells.Count == 0) return false;
        for (int i = 0; i < cells.Count; i++)
        {
            var c = cells[i];
            if (c == null) return false;
            if (c.IsFull) return false;
        }
        return true;
    }

    // semua cell kosong?
    public bool AreAllFree(IList<SlotCell> cells)
    {
        if (cells == null) return false;
        for (int i = 0; i < cells.Count; i++)
        {
            var cell = cells[i];
            if (cell == null) return false;
            if (cell.occupiedBy != null) return false;
        }
        return true;
    }

#if UNITY_EDITOR
    // Debug kalibrasi
    void OnDrawGizmosSelected()
    {
        if (!slotsParent) slotsParent = transform.Find(slotsParentName);
        if (!slotsParent) return;

        CalibrateFromScene();

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(originWorld, 0.05f);
        Gizmos.color = Color.green;   // ex (kolom)
        Gizmos.DrawLine(originWorld, originWorld + ex);
        Gizmos.color = Color.magenta; // er (baris)
        Gizmos.DrawLine(originWorld, originWorld + er);
    }
#endif
}
