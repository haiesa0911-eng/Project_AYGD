using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UIButtonHighlight : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Target Box UI")]
    [SerializeField] private Image targetBox;

    [Header("Alpha Settings")]
    [Range(0, 255)] public int alphaInactive = 0;
    [Range(0, 255)] public int alphaHighlight = 155;
    [Range(0, 255)] public int alphaActive = 255;

    private bool isActive = false;

    private void Start()
    {
        SetAlpha(alphaInactive); // default awal
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (!isActive)
            SetAlpha(alphaHighlight);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        if (!isActive)
            SetAlpha(alphaInactive);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        isActive = !isActive;
        SetAlpha(isActive ? alphaActive : alphaInactive);
    }

    private void SetAlpha(int alpha)
    {
        if (targetBox == null) return;
        Color c = targetBox.color;
        c.a = alpha / 255f;
        targetBox.color = c;
    }
}
