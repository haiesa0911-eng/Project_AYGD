using UnityEngine;
using UnityEngine.UI;

public class UISlotGridController : MonoBehaviour
{
    [Header("Parent yang berisi semua slot")]
    [SerializeField] private Transform slotsParent;

    [Header("Alpha Settings")]
    [Range(0, 255)] public int alphaInactive = 0;
    [Range(0, 255)] public int alphaActive = 100;

    private Image[] slotImages;

    private void Awake()
    {
        if (slotsParent != null)
            slotImages = slotsParent.GetComponentsInChildren<Image>(true);

        SetSlotsAlpha(alphaInactive);
    }

    public void ToggleSlots(bool active)
    {
        SetSlotsAlpha(active ? alphaActive : alphaInactive);
    }

    private void SetSlotsAlpha(int alpha)
    {
        if (slotImages == null) return;

        float a = alpha / 255f;
        foreach (var img in slotImages)
        {
            Color c = img.color;
            c.a = a;
            img.color = c;
        }
    }
}
