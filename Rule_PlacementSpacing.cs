using UnityEngine;

public enum SpacingMode { EdgeMargin, InterPieceGap }

[CreateAssetMenu(menuName = "GD/Rules/Grid/Spacing")]
public sealed class Rule_PlacementSpacing : RuleBase
{
    public SpacingMode mode = SpacingMode.EdgeMargin;

    [Header("Targets")]
    public PieceId targetA = PieceId.Asset1;
    public PieceId targetB = PieceId.Title; // untuk InterPieceGap

    [Header("Params (cells)")]
    [Tooltip("Nilai minimum agar tidak dianggap 'sesak'")]
    public int minCells = 1;
    [Tooltip("Di atas nilai ini skor akan 1.0 (full)")]
    public int fullAtCells = 2;

    public override RuleResult Evaluate(GridState s)
    {
        // Ambil rect & ukuran papan
        if (!s.pieces.TryGetValue(targetA, out var a) || !a.snapped) return Fail($"{targetA} belum ditempatkan");
        if (!GridEval.TryGetRect(a, out var rA)) return Fail($"Rect {targetA} tidak valid");
        if (!GridEval.TryGetBoardSize(s.board, out int rows, out int cols)) return Fail("Grid size tidak diketahui");

        float score = 0f;
        string reason;

        if (mode == SpacingMode.EdgeMargin)
        {
            var m = GridEval.EdgeMargins(rA, rows, cols); // (top,right,bottom,left)
            int minEdge = Mathf.Min((int)m.x, (int)m.y, (int)m.z, (int)m.w);
            score = RemapClamped(minEdge, minCells, fullAtCells);
            reason = $"Edge min={minEdge} (min {minCells}, full {fullAtCells})";
        }
        else // InterPieceGap
        {
            if (!s.pieces.TryGetValue(targetB, out var b) || !b.snapped) return Fail($"{targetB} belum ditempatkan");
            if (!GridEval.TryGetRect(b, out var rB)) return Fail($"Rect {targetB} tidak valid");
            if (GridEval.Intersects(rA, rB)) return Fail($"{targetA} overlap {targetB}");

            int gap = GridEval.MinGapCells(rA, rB); // 0=bersinggungan/menempel
            score = RemapClamped(gap, minCells, fullAtCells);
            reason = $"Gap {targetA}-{targetB}={gap} (min {minCells}, full {fullAtCells})";
        }

        return new RuleResult { pass = true, score01 = score, reason = reason };
    }

    float RemapClamped(int v, int lo, int hi)
    {
        if (hi <= lo) return v >= lo ? 1f : 0f;
        return Mathf.Clamp01((v - lo) / (float)(hi - lo));
    }

    RuleResult Fail(string m) => new RuleResult { pass = false, score01 = 0f, reason = m };
}
