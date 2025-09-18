using UnityEngine;
using TMPro;
using System.Collections;

public class ChatBubbleUI : MonoBehaviour
{
    [Header("Refs")]
    public RectTransform bubble;   // biasanya = (RectTransform)transform
    public TMP_Text text;

    [Header("Layout")]
    public float minWidth = 220f;
    public float maxWidth = 420f;
    // Padding: Left, Top, Right, Bottom
    public Vector4 padding = new Vector4(24, 16, 24, 16);

    Coroutine typingCo;

    void Awake()
    {
        if (!bubble) bubble = (RectTransform)transform;
        text.textWrappingMode = TextWrappingModes.Normal;
        text.overflowMode = TextOverflowModes.Overflow;
    }

    public void ShowLine(string line, float charsPerSecond = 40f)
    {
        // Ukuran bubble dihitung berdasarkan TEKS PENUH,
        // sementara yang ditampilkan diketik bertahap.
        FitToText(line);

        if (typingCo != null) StopCoroutine(typingCo);
        typingCo = StartCoroutine(TypeRoutine(line, charsPerSecond));
    }

    public bool IsTyping => typingCo != null;

    public void SkipTyping()
    {
        if (typingCo != null)
        {
            StopCoroutine(typingCo);
            typingCo = null;
            text.maxVisibleCharacters = int.MaxValue; // tampilkan semua
        }
    }

    IEnumerator TypeRoutine(string line, float cps)
    {
        text.text = line;
        text.ForceMeshUpdate();
        int total = text.textInfo.characterCount;
        text.maxVisibleCharacters = 0;

        if (cps <= 0f) cps = 60f;
        float t = 0f;
        while (text.maxVisibleCharacters < total)
        {
            t += Time.unscaledDeltaTime * cps;
            text.maxVisibleCharacters = Mathf.Min(total, Mathf.FloorToInt(t));
            yield return null;
        }
        typingCo = null;
    }

    void FitToText(string fullString)
    {
        float innerMaxW = Mathf.Max(1f, maxWidth - padding.x - padding.z);

        // 1) Estimasi ukuran teks pada batas lebar
        Vector2 pref = text.GetPreferredValues(fullString, innerMaxW, 0f);
        float w = Mathf.Clamp(pref.x + padding.x + padding.z, minWidth, maxWidth);

        // 2) Hitung ulang tinggi dengan lebar final untuk wrapping yang akurat
        pref = text.GetPreferredValues(fullString, w - padding.x - padding.z, 0f);
        float h = pref.y + padding.y + padding.w;

        // Terapkan ke bubble dan ke area teks
        bubble.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w);
        bubble.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, h);

        var tr = text.rectTransform;
        tr.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, w - padding.x - padding.z);
        tr.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, pref.y);
        tr.anchoredPosition = new Vector2(padding.x, -padding.y); // offset dari sudut kiri-atas bubble
    }
}
