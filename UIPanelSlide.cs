using System.Collections;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class UIPanelSlide : MonoBehaviour
{
    [Header("Posisi Panel")]
    public Vector2 startAnchoredPos = new Vector2(-1600f, 0f);  // posisi awal (misal di luar layar)
    public Vector2 endAnchoredPos = Vector2.zero;             // posisi akhir (biasanya (0,0) di viewport)

    [Header("Animasi")]
    [Range(0.05f, 2f)] public float duration = 0.35f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public bool disableOnHide = true;
    public bool useUnscaledTime = true;

    RectTransform rt;
    Coroutine anim;

    void Awake()
    {
        rt = GetComponent<RectTransform>();
    }

    public void Open()
    {
        gameObject.SetActive(true);
        // mulai dari posisi start lalu animasi ke end
        rt.anchoredPosition = startAnchoredPos;
        PlayTo(endAnchoredPos, null);
    }

    public void Close()
    {
        PlayTo(startAnchoredPos, () => { if (disableOnHide) gameObject.SetActive(false); });
    }

    public void SnapToStart()
    {
        rt.anchoredPosition = startAnchoredPos;
    }

    public void SnapToEnd()
    {
        rt.anchoredPosition = endAnchoredPos;
    }

    void PlayTo(Vector2 target, System.Action onComplete)
    {
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(AnimateTo(target, onComplete));
    }

    IEnumerator AnimateTo(Vector2 target, System.Action onComplete)
    {
        Vector2 start = rt.anchoredPosition;
        float t = 0f;
        while (t < 1f)
        {
            t += (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime) / duration;
            float k = ease.Evaluate(Mathf.Clamp01(t));
            rt.anchoredPosition = Vector2.LerpUnclamped(start, target, k);
            yield return null;
        }
        rt.anchoredPosition = target;
        onComplete?.Invoke();
    }
}
