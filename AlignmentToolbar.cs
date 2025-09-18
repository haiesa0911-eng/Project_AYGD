using UnityEngine;

public class AlignmentToolbar : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private SelectionManager selectionManager;

    [Header("Animasi Snap")]
    [Tooltip("Jika true, align menggunakan animasi (Lerp) sesuai moveSnapDuration pada PieceSnapper.")]
    [SerializeField] private bool animate = true;

    private SelectionManager Mgr => selectionManager ? selectionManager : SelectionManager.I;

    private void Reset()
    {
        if (!selectionManager) selectionManager = SelectionManager.I;
    }

    // ====== Hook ke Button OnClick() ======
    public void OnAlignLeft() => Apply("AlignLeft");
    public void OnAlignRight() => Apply("AlignRight");
    public void OnAlignTop() => Apply("AlignTop");
    public void OnAlignBottom() => Apply("AlignBottom");
    public void OnAlignHorizontalCenter() => Apply("AlignHorizontalCenter");
    public void OnAlignVerticalCenter() => Apply("AlignVerticalCenter");

    // ====== Inti ======
    private void Apply(string opName)
    {
        var mgr = Mgr;
        if (mgr == null || mgr.Current == null) return;

        // Hitung jumlah terpilih
        int count = 0; foreach (var _ in mgr.Current) count++;

        if (count <= 1)
        {
            // SINGLE → terhadap Board
            foreach (var box in mgr.Current)
            {
                if (!box) continue;
                var snap = box.GetComponent<PieceSnapper>();
                if (snap == null || !snap.IsSnapped)
                {
                    Debug.Log($"[{opName}] Abaikan: piece belum tersnap.");
                    continue;
                }
                bool ok = InvokeSingle(snap, opName);
                if (!ok) Debug.Log($"[{opName}] Gagal: area target tidak tersedia/terblok.");
            }
            return;
        }

        // MULTI → terhadap Active (object referensi)
        var refBox = mgr.Active ?? GetAny(mgr);
        if (!refBox) return;

        var refSnap = refBox.GetComponent<PieceSnapper>();
        if (refSnap == null || !refSnap.IsSnapped || !refSnap.TryGetCurrentRect(out int rr0, out int rc0, out int rr1, out int rc1))
        {
            Debug.Log($"[{opName}] Referensi tidak valid (belum tersnap).");
            return;
        }

        int RefColsCenter() => rc0 + (rc1 - rc0) / 2;
        int RefRowsCenter() => rr0 + (rr1 - rr0) / 2;

        foreach (var box in mgr.Current)
        {
            if (!box || box == refBox) continue;

            var snap = box.GetComponent<PieceSnapper>();
            if (snap == null || !snap.IsSnapped) { Debug.Log($"[{opName}] Abaikan: satu object belum tersnap."); continue; }
            if (!snap.TryGetCurrentRect(out int r0, out int c0, out int r1, out int c1)) continue;

            int newR0 = r0, newR1 = r1, newC0 = c0, newC1 = c1;

            switch (opName)
            {
                case "AlignLeft":
                    newC0 = rc0; newC1 = newC0 + (c1 - c0);
                    break;
                case "AlignRight":
                    newC1 = rc1; newC0 = newC1 - (c1 - c0);
                    break;
                case "AlignTop":
                    newR0 = rr0; newR1 = newR0 + (r1 - r0);
                    break;
                case "AlignBottom":
                    newR1 = rr1; newR0 = newR1 - (r1 - r0);
                    break;
                case "AlignHorizontalCenter":
                    {
                        int w = (c1 - c0 + 1);
                        int targetCenter = RefColsCenter();
                        newC0 = Mathf.RoundToInt(targetCenter - (w - 1) * 0.5f);
                        newC1 = newC0 + w - 1;
                        break;
                    }
                case "AlignVerticalCenter":
                    {
                        int h = (r1 - r0 + 1);
                        int targetCenter = RefRowsCenter();
                        newR0 = Mathf.RoundToInt(targetCenter - (h - 1) * 0.5f);
                        newR1 = newR0 + h - 1;
                        break;
                    }
            }

            // Clamp terhadap board referensi (asumsi semua piece pada board yang sama)
            var b = refSnap.board;
            if (b == null) continue;
            if (newC0 < 0) { newC1 -= newC0; newC0 = 0; }
            if (newC1 > b.cols - 1) { int diff = newC1 - (b.cols - 1); newC1 -= diff; newC0 -= diff; }
            if (newR0 < 0) { newR1 -= newR0; newR0 = 0; }
            if (newR1 > b.rows - 1) { int diff = newR1 - (b.rows - 1); newR1 -= diff; newR0 -= diff; }

            bool ok2 = snap.TrySnapToRect(newR0, newC0, newR1, newC1, animate);
            if (!ok2) Debug.Log($"[{opName}] Gagal untuk 1 object: target blocked/di luar board.");
        }
    }

    private bool InvokeSingle(PieceSnapper s, string opName)
    {
        switch (opName)
        {
            case "AlignLeft": return s.AlignLeft(animate);
            case "AlignRight": return s.AlignRight(animate);
            case "AlignTop": return s.AlignTop(animate);
            case "AlignBottom": return s.AlignBottom(animate);
            case "AlignHorizontalCenter": return s.AlignHorizontalCenter(animate);
            case "AlignVerticalCenter": return s.AlignVerticalCenter(animate);
        }
        return false;
    }

    private SelectionBox GetAny(SelectionManager mgr)
    {
        foreach (var b in mgr.Current) return b; // ambil satu saja sebagai fallback
        return null;
    }
}
