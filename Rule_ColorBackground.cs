using UnityEngine;

[CreateAssetMenu(menuName = "GD/Rules/Color/BackgroundPreference")]
public sealed class Rule_ColorBackground : RuleBase
{
    public BackgroundColorTag background;   // drag GO background yg punya tag

    [Header("Preferensi Klien (Hex)")]
    public string black = "#000000";
    public string white = "#FFFFFF";
    public string red = "#FF0000";

    [Header("Skor")]
    [Range(0f, 1f)] public float blackScore = 1.0f; // pilihan utama
    [Range(0f, 1f)] public float whiteScore = 0.7f; // aman
    [Range(0f, 1f)] public float redScore = 0.1f; // sangat tidak disukai

    public override RuleResult Evaluate(GridState s)
    {
        Color bg = (background != null) ? background.GetColorOr(Color.black) : Color.black;

        Color cBlack = Color.black, cWhite = Color.white, cRed = Color.red;
        ColorUtility.TryParseHtmlString(black, out cBlack);
        ColorUtility.TryParseHtmlString(white, out cWhite);
        ColorUtility.TryParseHtmlString(red, out cRed);

        float score =
              SameColor(bg, cBlack) ? blackScore
            : SameColor(bg, cWhite) ? whiteScore
            : SameColor(bg, cRed) ? redScore
            : whiteScore; // default netral/aman

        return new RuleResult { pass = true, score01 = score, reason = $"BG score={score:0.00}" };
    }

    // compare per-channel (tanpa .sqrMagnitude)
    static bool SameColor(Color a, Color b, float eps = 1e-4f)
    {
        return Mathf.Abs(a.r - b.r) <= eps &&
               Mathf.Abs(a.g - b.g) <= eps &&
               Mathf.Abs(a.b - b.b) <= eps;
    }
}
