using UnityEngine;

[CreateAssetMenu(menuName = "GD/Rules/IllustrationCenter")]
public sealed class Rule_PlacementCenter : RuleBase
{
    public PieceId illustrationId = PieceId.Asset1;

    [Header("Center Tolerance (cells)")]
    public int centerTolerance = 1;

    [Header("Edge Margin Target (cells)")]
    public int desiredEdgeMargin = 2; // makin besar → prefer lebih ke tengah

    public Rule_PlacementCenter()
    {
        isHardGate = false;
        weight = 1f; // atur di LevelRubric
    }

    public override RuleResult Evaluate(GridState s)
    {
        if (!s.pieces.TryGetValue(illustrationId, out var pi) || !pi.snapped)
            return new RuleResult { pass = false, score01 = 0f, reason = "Ilustrasi belum ditempatkan" };

        if (!GridEval.TryGetRect(pi, out var r))
            return new RuleResult { pass = false, score01 = 0f, reason = "Rect ilustrasi tidak valid" };

        if (!GridEval.TryGetBoardSize(s.board, out int rows, out int cols))
            return new RuleResult { pass = false, score01 = 0f, reason = "Ukuran grid papan tidak diketahui" };

        // Skor gabungan: center + balance + bonus napas dari tepi
        float center = GridEval.CenterScore(r, rows, cols, centerTolerance);
        float balance = GridEval.BalanceScore(r, rows, cols);

        var m = GridEval.EdgeMargins(r, rows, cols);
        float edge = 0f;
        // “Semakin >= desiredEdgeMargin” semakin nyaman
        edge += Mathf.Clamp01(m.x / (float)desiredEdgeMargin);
        edge += Mathf.Clamp01(m.y / (float)desiredEdgeMargin);
        edge += Mathf.Clamp01(m.z / (float)desiredEdgeMargin);
        edge += Mathf.Clamp01(m.w / (float)desiredEdgeMargin);
        edge *= 0.25f;

        // Bobot: center 50%, balance 30%, edge 20%
        float score = Mathf.Clamp01(0.5f * center + 0.3f * balance + 0.2f * edge);

        return new RuleResult
        {
            pass = true,
            score01 = score,
            reason = $"Center:{center:0.00} Balance:{balance:0.00} Edge:{edge:0.00}"
        };
    }
}
