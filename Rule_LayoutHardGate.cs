using UnityEngine;

[CreateAssetMenu(menuName = "GD/Rules/Hard/LayoutHardGate")]
public sealed class Rule_LayoutHardGate : RuleBase
{
    [Header("Targets")]
    public PieceId illustrationId = PieceId.Asset1;
    public PieceId titleId = PieceId.Title;
    public PieceId infoId = PieceId.Tagline;
    public PieceId dateId = PieceId.Date;

    [Header("Whitespace (cells)")]
    [Tooltip("Minimal jarak ke tepi papan.")]
    public int minEdgeMargin = 1;
    [Tooltip("Minimal jarak antar piece (selain tidak boleh overlap).")]
    public int minInterPieceGap = 1;

    public Rule_LayoutHardGate()
    {
        isHardGate = true;
        weight = 1f;
    }

    public override RuleResult Evaluate(GridState s)
    {
        // Cek eksistensi 4 piece (PiecesCompletionRule juga cek snap, tapi kita validasi cepat di sini)
        if (!TryGetPI(s, illustrationId, out var ill) || !ill.snapped)
            return Fail($"{illustrationId} belum ditempatkan / belum snapped");
        if (!TryGetPI(s, titleId, out var ttl) || !ttl.snapped)
            return Fail($"{titleId} belum ditempatkan / belum snapped");
        if (!TryGetPI(s, infoId, out var inf) || !inf.snapped)
            return Fail($"{infoId} belum ditempatkan / belum snapped");
        if (!TryGetPI(s, dateId, out var dat) || !dat.snapped)
            return Fail($"{dateId} belum ditempatkan / belum snapped");

        if (!GridEval.TryGetRect(ill, out var rIll) ||
            !GridEval.TryGetRect(ttl, out var rTtl) ||
            !GridEval.TryGetRect(inf, out var rInf) ||
            !GridEval.TryGetRect(dat, out var rDat))
            return Fail("Gagal membaca rect grid");

        if (!GridEval.TryGetBoardSize(s.board, out int rows, out int cols))
            return Fail("Ukuran grid papan tidak diketahui (set GridEval.TryGetBoardSize)");

        // 1) Whitespace tiap piece (jarak ke tepi)
        if (!HasEdgeMargin(rIll, rows, cols, minEdgeMargin)) return Fail("Ilustrasi terlalu mepet tepi");
        if (!HasEdgeMargin(rTtl, rows, cols, minEdgeMargin)) return Fail("Headline terlalu mepet tepi");
        if (!HasEdgeMargin(rInf, rows, cols, minEdgeMargin)) return Fail("Informasi terlalu mepet tepi");
        if (!HasEdgeMargin(rDat, rows, cols, minEdgeMargin)) return Fail("Tanggal terlalu mepet tepi");

        // 2) Info & Date tidak overlap dengan objek lain
        foreach (var kv in s.pieces)
        {
            var p = kv.Value;
            if (!p.snapped) continue;
            if (!GridEval.TryGetRect(p, out var r)) continue;

            if (kv.Key != infoId && GridEval.Intersects(rInf, r))
                return Fail("Informasi overlap dengan objek lain");
            if (kv.Key != dateId && GridEval.Intersects(rDat, r))
                return Fail("Tanggal overlap dengan objek lain");
        }

        // 3) Minimal gap antar piece (biar tidak “sesak”)
        if (!MinGapOK(rIll, rTtl)) return Fail("Ilustrasi & Headline terlalu rapat");
        if (!MinGapOK(rIll, rInf)) return Fail("Ilustrasi & Informasi terlalu rapat");
        if (!MinGapOK(rIll, rDat)) return Fail("Ilustrasi & Tanggal terlalu rapat");
        if (!MinGapOK(rTtl, rInf)) return Fail("Headline & Informasi terlalu rapat");
        if (!MinGapOK(rTtl, rDat)) return Fail("Headline & Tanggal terlalu rapat");
        if (!MinGapOK(rInf, rDat)) return Fail("Informasi & Tanggal terlalu rapat");

        // 4) Headline harus berada di atas ilustrasi (vertically above)
        if (!(rTtl.r1 < rIll.r0))
            return Fail("Headline harus ditempatkan di atas Ilustrasi");

        return new RuleResult
        {
            pass = true,
            score01 = 1f,
            reason = "Layout aman: whitespace OK, tidak overlap, Headline di atas Ilustrasi"
        };

        // ===== local helpers =====
        bool MinGapOK(in GridEval.RectI a, in GridEval.RectI b)
        {
            if (GridEval.Intersects(a, b)) return false; // overlap jelas gagal
            return GridEval.MinGapCells(a, b) >= minInterPieceGap;
        }

        bool HasEdgeMargin(in GridEval.RectI r, int rr, int cc, int min)
        {
            var m = GridEval.EdgeMargins(r, rr, cc);
            return (m.x >= min && m.y >= min && m.z >= min && m.w >= min);
        }

        // ✅ Assign 'pi' di semua jalur agar tidak memicu CS0177
        bool TryGetPI(GridState st, PieceId id, out PieceInfo pi)
        {
            pi = default;
            return st.pieces != null && st.pieces.TryGetValue(id, out pi);
        }

        RuleResult Fail(string msg) => new RuleResult { pass = false, score01 = 0f, reason = msg };
    }
}
