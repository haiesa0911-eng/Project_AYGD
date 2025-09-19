using UnityEngine;

public class BackgroundColorTag : MonoBehaviour
{
    [Tooltip("Hex seperti #000000 / #FFFFFF / #FF0000")]
    public string backgroundHex = "#000000";

    public Color GetColorOr(Color fallback)
    {
        return ColorUtility.TryParseHtmlString(backgroundHex, out var c) ? c : fallback;
    }
}
