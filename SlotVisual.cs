using UnityEngine;

public class SlotVisual : MonoBehaviour
{
    [Header("Target Visual (pilih salah satu, otomatis cari jika kosong)")]
    public SpriteRenderer targetSprite;
    public CanvasGroup targetCanvasGroup;

    [Range(0, 255)] public byte currentAlpha = 0;

    void Awake()
    {
        if (!targetSprite) targetSprite = GetComponentInChildren<SpriteRenderer>(true);
        if (!targetCanvasGroup) targetCanvasGroup = GetComponentInChildren<CanvasGroup>(true);
        ApplyAlpha(currentAlpha);
    }

    public void SetAlpha(byte a)
    {
        currentAlpha = a;
        ApplyAlpha(a);
    }

    void ApplyAlpha(byte a)
    {
        if (targetSprite)
        {
            var c = targetSprite.color;
            c.a = a / 255f;
            targetSprite.color = c;
        }
        if (targetCanvasGroup)
        {
            targetCanvasGroup.alpha = a / 255f;
        }
    }
}
