using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class ToolbarWidget : MonoBehaviour
{
    [Tooltip("Paksa widget dalam keadaan tersembunyi saat start/play.")]
    public bool startHidden = true;

    [Tooltip("Jika layout dihitung di child lain, drag LayoutElement target ke sini.")]
    public LayoutElement layoutTargetOverride;

    LayoutElement le;
    CanvasGroup cg;

    LayoutElement LE
    {
        get
        {
            if (layoutTargetOverride) return layoutTargetOverride;
            if (!le && !TryGetComponent(out le)) le = gameObject.AddComponent<LayoutElement>();
            return le;
        }
    }

    void Awake()
    {
        if (!cg && !TryGetComponent(out cg)) cg = gameObject.AddComponent<CanvasGroup>();
        if (startHidden) SetVisible(false);      // <-- kunci awal
    }

    public void SetVisible(bool on)
    {
        var target = LE;
        if (target)
        {
            target.ignoreLayout = !on;
            if (!on)
            {
                target.minWidth = 0;
                target.preferredWidth = 0;
                target.flexibleWidth = 0;
            }
        }

        if (cg)
        {
            cg.alpha = on ? 1f : 0f;
            cg.interactable = on;
            cg.blocksRaycasts = on;
        }
    }
}
