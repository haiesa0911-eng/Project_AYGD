using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class SlideItem : MonoBehaviour
{
    [Header("Neighbors")]
    public SlideItem leftNeighbor;
    public SlideItem rightNeighbor;

    private RectTransform rt;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
    }

    public void ShiftX(float deltaX)
    {
        if (!rt) return;
        var p = rt.anchoredPosition;
        p.x += deltaX;
        rt.anchoredPosition = p;
    }

    public IEnumerator ShiftXAnimated(float deltaX, float duration, AnimationCurve ease = null)
    {
        if (!rt || duration <= 0f) { ShiftX(deltaX); yield break; }

        Vector2 start = rt.anchoredPosition;
        Vector2 target = start + new Vector2(deltaX, 0f);

        float t = 0f;
        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / duration;
            float k = Mathf.Clamp01(t);
            if (ease != null) k = ease.Evaluate(k);
            rt.anchoredPosition = Vector2.LerpUnclamped(start, target, k);
            yield return null;
        }
        rt.anchoredPosition = target;
    }

    // Opsional helper untuk “snap” nilai X absolut (mis. 0, ±offset)
    public void SetX(float x)
    {
        if (!rt) return;
        var p = rt.anchoredPosition;
        p.x = x;
        rt.anchoredPosition = p;
    }
}
