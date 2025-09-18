using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;

public class UIButtonHighlightActive : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, ISubmitHandler
{
    [Header("Target Box UI")]
    [SerializeField] private Image targetBox;

    [Header("Alpha Settings (0–255)")]
    [Range(0, 255)] public int alphaInactive = 0;    // saat tidak di-hover
    [Range(0, 255)] public int alphaHighlight = 155; // saat di-hover
    [Range(0, 255)] public int alphaActive = 255;    // saat click (blink)

    [Header("Blink Timing")]
    [Tooltip("Berapa lama box bertahan di alphaActive setelah klik.")]
    [SerializeField] private float flashHold = 0.12f;

    [Tooltip("Kembalinya bisa langsung (false) atau fade singkat (true).")]
    [SerializeField] private bool smoothReturn = false;

    [Tooltip("Durasi fade saat kembali (hanya jika smoothReturn = true).")]
    [SerializeField] private float returnDuration = 0.08f;

    private bool isHovering;
    private bool isFlashing;
    private Coroutine flashRoutine;

    private void Reset()
    {
        // Auto-isi jika dipasang di Image yang sama
        if (!targetBox) targetBox = GetComponent<Image>();
    }

    private void Awake()
    {
        if (!targetBox) targetBox = GetComponent<Image>();
    }

    private void Start()
    {
        SetAlpha(alphaInactive);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isHovering = true;
        if (!isFlashing) SetAlpha(alphaHighlight);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isHovering = false;
        if (!isFlashing) SetAlpha(alphaInactive);
    }

    // Gunakan PointerDown agar feedback terasa instan saat tombol ditekan.
    public void OnPointerDown(PointerEventData eventData)
    {
        TriggerFlash();
    }

    // Dukung keyboard/controller (Submit)
    public void OnSubmit(BaseEventData eventData)
    {
        TriggerFlash();
    }

    private void TriggerFlash()
    {
        if (!targetBox) return;

        if (flashRoutine != null) StopCoroutine(flashRoutine);
        flashRoutine = StartCoroutine(FlashCo());
    }

    private IEnumerator FlashCo()
    {
        isFlashing = true;

        // Step 1: naik ke alphaActive
        SetAlpha(alphaActive);

        // Tahan sejenak
        if (flashHold > 0f)
            yield return new WaitForSeconds(flashHold);

        // Step 2: kembali ke state semula (hover atau idle)
        int target = isHovering ? alphaHighlight : alphaInactive;

        if (smoothReturn && returnDuration > 0f)
        {
            float t = 0f;
            float startA = targetBox.color.a;
            float endA = target / 255f;

            while (t < returnDuration)
            {
                t += Time.unscaledDeltaTime; // pakai unscaled agar tidak terpengaruh timeScale
                float k = Mathf.Clamp01(t / returnDuration);
                SetAlphaFloat(Mathf.Lerp(startA, endA, k));
                yield return null;
            }
        }

        SetAlpha(target);
        isFlashing = false;
        flashRoutine = null;
    }

    private void SetAlpha(int alpha255)
    {
        SetAlphaFloat(Mathf.Clamp01(alpha255 / 255f));
    }

    private void SetAlphaFloat(float a01)
    {
        if (!targetBox) return;
        var c = targetBox.color;
        c.a = a01;
        targetBox.color = c;
    }
}
