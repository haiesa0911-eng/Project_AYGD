using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

[RequireComponent(typeof(Button))]
public class ButtonPressHighlight : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler,
    IPointerDownHandler, IPointerUpHandler
{
    [Header("Press Effect")]
    [SerializeField] private float pressedScale = 0.9f;
    [SerializeField] private bool highlightWhileHeld = true;

    private Button btn;
    private Image img;                // targetGraphic (biasanya Image)
    private Sprite normalSprite;
    private Sprite highlightedSprite;
    private Sprite pressedSprite;     // opsional, jika suatu saat ingin pakai
    private Vector3 originalScale;

    private bool isPointerInside;
    private bool isHeld;

    void Awake()
    {
        btn = GetComponent<Button>();
        img = btn.targetGraphic as Image;

        if (img == null)
        {
            Debug.LogWarning($"[{name}] Button targetGraphic bukan Image. Harap pakai Image.");
            enabled = false;
            return;
        }

        // Ambil SpriteState dari Button
        SpriteState st = btn.spriteState;
        highlightedSprite = st.highlightedSprite;
        pressedSprite = st.pressedSprite; // tidak wajib dipakai, tapi disimpan
    }

    void Start()
    {
        originalScale = transform.localScale;
        normalSprite = img.sprite; // sprite default saat start
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        isPointerInside = true;
        if (!isHeld)
            SetSprite(highlightedSprite ? highlightedSprite : normalSprite);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        isPointerInside = false;
        if (!isHeld)
            SetSprite(normalSprite);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!btn.interactable) return;

        isHeld = true;
        // Efek “dipencet”
        transform.localScale = originalScale * Mathf.Clamp(pressedScale, 0.5f, 1f);

        // Inti permintaanmu: saat ditahan tetap tampil Highlight (bukan Pressed)
        if (highlightWhileHeld && highlightedSprite != null)
        {
            SetSprite(highlightedSprite);
        }
        else
        {
            // fallback: pakai pressedSprite jika memang ingin efek berbeda
            SetSprite(pressedSprite ? pressedSprite : normalSprite);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        isHeld = false;
        // Kembalikan scale
        transform.localScale = originalScale;

        // Setelah dilepas: jika masih di atas tombol → Highlight, kalau tidak → Normal
        SetSprite(isPointerInside && highlightedSprite != null ? highlightedSprite : normalSprite);
    }

    void OnDisable()
    {
        // Reset aman saat object disable
        isHeld = false;
        isPointerInside = false;
        if (img) SetSprite(normalSprite);
        transform.localScale = originalScale;
    }

    private void SetSprite(Sprite s)
    {
        if (img && s != null)
            img.sprite = s;
    }
}
