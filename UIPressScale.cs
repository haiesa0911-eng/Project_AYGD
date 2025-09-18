using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class UIPressScale : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    [Header("Target")]
    public RectTransform target;      // kosong = pakai RectTransform sendiri

    [Header("Scales")]
    public float normalScale = 1.00f;  // keadaan biasa
    public float hoverScale = 0.96f;  // saat mouse hover -> sedikit mengecil
    public float pressedScale = 1.00f;  // saat ditekan -> kembali ke normal (membesar dari hover)

    [Header("Timing")]
    public float enterDuration = 0.12f; // ke hover
    public float exitDuration = 0.10f; // ke normal (keluar)
    public float pressDuration = 0.06f; // ke pressed
    public float releaseDuration = 0.10f; // dari pressed -> hover/normal

    [Header("FX")]
    public bool useOvershootOnRelease = true;
    public float overshootScale = 1.02f; // pop kecil sebelum settle
    public bool useUnscaledTime = true;

    bool _inside;
    bool _pressed;
    Coroutine _anim;

    void Reset() { target = transform as RectTransform; }
    void OnEnable()
    {
        if (!target) target = transform as RectTransform;
        SetScale(normalScale);
        _inside = _pressed = false;
    }
    void OnDisable()
    {
        StopAnim();
        if (target) target.localScale = Vector3.one * normalScale;
        _inside = _pressed = false;
    }

    // ---------- Pointer events ----------
    public void OnPointerEnter(PointerEventData e)
    {
        _inside = true;
        if (!_pressed) TweenTo(hoverScale, enterDuration);
    }

    public void OnPointerExit(PointerEventData e)
    {
        _inside = false;
        if (!_pressed) TweenTo(normalScale, exitDuration);
    }

    public void OnPointerDown(PointerEventData e)
    {
        _pressed = true;
        // tekan = membesar ke normal (dari hover yg kecil)
        TweenTo(pressedScale, pressDuration);
    }

    public void OnPointerUp(PointerEventData e)
    {
        _pressed = false;

        // Jika masih di atas tombol, kembali ke hover (dengan pop kecil bila diaktifkan)
        float targetScale = _inside ? hoverScale : normalScale;

        if (useOvershootOnRelease && !_inside)
        {
            // kalau lepas dan pointer keluar, bisa langsung ke normal tanpa pop
            TweenTo(targetScale, releaseDuration);
            return;
        }

        if (useOvershootOnRelease && overshootScale > targetScale)
            StartCoroutine(Sequence(new[] { overshootScale, targetScale }, releaseDuration * 0.6f));
        else
            TweenTo(targetScale, releaseDuration);
    }

    // ---------- Tween helpers ----------
    void SetScale(float s)
    {
        if (target) target.localScale = new Vector3(s, s, 1f);
    }

    void StopAnim()
    {
        if (_anim != null) { StopCoroutine(_anim); _anim = null; }
    }

    void TweenTo(float to, float dur)
    {
        if (!target) target = transform as RectTransform;
        StopAnim();
        _anim = StartCoroutine(TweenCR(to, dur));
    }

    System.Collections.IEnumerator Sequence(float[] seq, float durEach)
    {
        foreach (var val in seq)
        {
            yield return TweenCR(val, durEach);
        }
        _anim = null;
    }

    System.Collections.IEnumerator TweenCR(float to, float dur)
    {
        float from = target ? target.localScale.x : 1f;
        float t = 0f;
        while (t < dur)
        {
            t += useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;
            float k = Mathf.Clamp01(t / dur);
            float s = Mathf.SmoothStep(from, to, k);
            SetScale(s);
            yield return null;
        }
        SetScale(to);
    }
}
