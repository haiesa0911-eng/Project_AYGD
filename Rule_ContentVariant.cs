using UnityEngine;

public enum VariantSource { PieceVariant, TypeVariant }

[CreateAssetMenu(menuName = "GD/Rules/VariantChoice")]
public sealed class Rule_ContentVariant : RuleBase
{
    public PieceId target = PieceId.Title;
    public VariantSource source = VariantSource.PieceVariant;

    [Header("ID Sets & Scores")]
    public int[] bestIds; [Range(0, 1)] public float bestScore = 1.0f;
    public int[] goodIds; [Range(0, 1)] public float goodScore = 0.8f;
    public int[] badIds; [Range(0, 1)] public float badScore = 0.3f;
    [Range(0, 1)] public float unknownScore = 0.5f;

    public override RuleResult Evaluate(GridState s)
    {
        if (!s.pieces.TryGetValue(target, out var p) || !p.snapped) return Fail($"{target} belum ditempatkan");
        var go = p.snap.gameObject;

        int id;
        bool ok = (source == VariantSource.PieceVariant)
            ? Util_StyleAccess.TryGetPieceVariantId(go, out id)
            : Util_StyleAccess.TryGetTypeVariantId(go, out id);

        if (!ok) return new RuleResult { pass = true, score01 = unknownScore, reason = "Variant ID tidak terbaca" };

        float sc =
            Contains(bestIds, id) ? bestScore :
            Contains(goodIds, id) ? goodScore :
            Contains(badIds, id) ? badScore :
                                    unknownScore;

        return new RuleResult { pass = true, score01 = sc, reason = $"{target} variantId={id} score={sc:0.00}" };
    }

    bool Contains(int[] arr, int v) => arr != null && System.Array.IndexOf(arr, v) >= 0;
    RuleResult Fail(string m) => new RuleResult { pass = false, score01 = 0f, reason = m };
}
