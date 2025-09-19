using UnityEngine;

public enum RelationMode { Above, Below, LeftOf, RightOf, Centered }

[CreateAssetMenu(menuName = "GD/Rules/Grid/Relation")]
public sealed class Rule_PlacementRelation : RuleBase
{
    public RelationMode mode = RelationMode.Centered;
    public PieceId a = PieceId.Asset1;
    public PieceId b = PieceId.Title;     // untuk relasi dua piece
    public int toleranceCells = 1;        // untuk Centered

    public override RuleResult Evaluate(GridState s)
    {
        if (!s.pieces.TryGetValue(a, out var pa) || !pa.snapped) return Fail($"{a} belum ditempatkan");
        if (!GridEval.TryGetRect(pa, out var rA)) return Fail($"Rect {a} tidak valid");

        if (mode == RelationMode.Centered)
        {
            if (!GridEval.TryGetBoardSize(s.board, out int rows, out int cols)) return Fail("Grid size tidak diketahui");
            float center = GridEval.CenterScore(rA, rows, cols, toleranceCells);
            return new RuleResult { pass = true, score01 = center, reason = $"Centered score={center:0.00}" };
        }

        if (!s.pieces.TryGetValue(b, out var pb) || !pb.snapped) return Fail($"{b} belum ditempatkan");
        if (!GridEval.TryGetRect(pb, out var rB)) return Fail($"Rect {b} tidak valid");

        bool ok =
            (mode == RelationMode.Above && rA.r1 < rB.r0) ||
            (mode == RelationMode.Below && rA.r0 > rB.r1) ||
            (mode == RelationMode.LeftOf && rA.c1 < rB.c0) ||
            (mode == RelationMode.RightOf && rA.c0 > rB.c1);

        return new RuleResult { pass = ok, score01 = ok ? 1f : 0f, reason = $"{a} {mode} {b} = {ok}" };
    }

    RuleResult Fail(string m) => new RuleResult { pass = false, score01 = 0f, reason = m };
}
