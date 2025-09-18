using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ToolbarContextController : MonoBehaviour
{
    public RectTransform toolbarRoot;
    public ToolbarWidget wDropdownFont, wDropdownFill, wBtnBoldItalic, wBtnAlign;

    SelectionBox _lastActive;

    void Awake()
    {
        HideAll();              // sebelum frame 1
    }

    void OnEnable()
    {
        HideAll();
        ForceRebuild();
        StartCoroutine(RebuildNextFrame());
    }

    IEnumerator RebuildNextFrame()
    {
        yield return null;      // tunggu layout pass pertama
        ForceRebuild();
    }

    void LateUpdate()
    {
        var cur = SelectionManager.I ? SelectionManager.I.Active : null;
        if (cur != _lastActive)
        {
            _lastActive = cur;
            if (!cur) { HideAll(); ForceRebuild(); return; }

            ApplyCapabilities(cur);
            ForceRebuild();
        }
    }

    void ApplyCapabilities(SelectionBox box)
    {
        var caps = box ? (box.GetComponent<ToolCapabilities>() ?? box.GetComponentInParent<ToolCapabilities>()) : null;

        bool showFont = caps && caps.DropdownFont;
        bool showFill = caps && caps.DropdownFill;
        bool showBI = caps && caps.BtnBoldItalic;
        bool showAlign = caps && caps.BtnAlign;

        if (wDropdownFont) wDropdownFont.SetVisible(showFont);
        if (wDropdownFill) wDropdownFill.SetVisible(showFill);
        if (wBtnBoldItalic) wBtnBoldItalic.SetVisible(showBI);
        if (wBtnAlign) wBtnAlign.SetVisible(showAlign);
    }

    void HideAll()
    {
        if (wDropdownFont) wDropdownFont.SetVisible(false);
        if (wDropdownFill) wDropdownFill.SetVisible(false);
        if (wBtnBoldItalic) wBtnBoldItalic.SetVisible(false);
        if (wBtnAlign) wBtnAlign.SetVisible(false);
    }

    void ForceRebuild()
    {
        if (toolbarRoot) LayoutRebuilder.ForceRebuildLayoutImmediate(toolbarRoot);
    }
}
