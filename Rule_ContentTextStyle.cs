using UnityEngine;
using TMPro;

public enum StyleCheck { RequireBold, PreferBold, PreferItalic }

[CreateAssetMenu(menuName = "GD/Rules/TypeTextStyle")]
public sealed class Rule_ContentTextStyle : RuleBase
{
    public PieceId target = PieceId.Tagline;
    public StyleCheck check = StyleCheck.RequireBold;
    [Range(0, 1)] public float passScore = 1.0f;
    [Range(0, 1)] public float failScore = 0.3f;

    public override RuleResult Evaluate(GridState s)
    {
        if (!s.pieces.TryGetValue(target, out var p) || !p.snapped) return Fail($"{target} belum ditempatkan");
        var go = p.snap.gameObject;

        if (!Util_StyleAccess.TryGetTMP(go, out TMP_Text tmp))
            return new RuleResult { pass = true, score01 = failScore, reason = "TMP tidak ditemukan" };

        bool bold = Util_StyleAccess.IsBold(tmp);
        bool ital = Util_StyleAccess.IsItalic(tmp);

        bool pass = check switch
        {
            StyleCheck.RequireBold => bold,
            StyleCheck.PreferBold => bold || ital == false, // longgar
            StyleCheck.PreferItalic => ital || bold == false,
            _ => true
        };

        return new RuleResult { pass = pass, score01 = pass ? passScore : failScore, reason = $"bold={bold} italic={ital}" };
    }

    RuleResult Fail(string m) => new RuleResult { pass = false, score01 = 0f, reason = m };
}
