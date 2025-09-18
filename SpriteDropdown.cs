using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;
using TMPro;

[DisallowMultipleComponent]
public class SpriteDropdown : MonoBehaviour
{
    // DIBIARKAN untuk opsi nanti; default kita nonaktifkan auto-positioning.
    public enum XAlign { Left, Center, Right }

    [Header("Wiring")]
    public Button headerButton;

    // Header
    public RectTransform captionSlot;
    public Image captionIcon;

    // Panel & list
    public RectTransform panel;            // popup container (tetap child Dropdown_Font)
    public RectTransform content;          // parent item
    public GameObject optionButtonPrefab;  // prefab item ("Slot"(RectTransform), "Icon"(Image))

    [Header("Behaviour")]
    public bool closeOnSelect = true;
    public bool rebuildOnAwake = true;
    public bool hideSelectedInList = true;

    [Header("Options")]
    public List<Option> options = new List<Option>();
    [Serializable]
    public class Option
    {
        public string id;
        public Sprite sprite;
        public TMP_FontAsset tmpFontAsset; // tetap ada jika dipakai untuk preview di header
    }

    [Header("Apply Selection (optional)")]
    public TMP_Text sampleTMP;
    public Image previewImage;

    [Header("Slot Fit")]
    public float slotPadding = 6f;
    public bool allowUpscale = true;

    [Serializable] public class DropdownEvent : UnityEvent<int, Option> { }
    public DropdownEvent onValueChanged = new DropdownEvent();

    int _value = -1;
    GameObject _blocker;
    Canvas _rootCanvas;
    readonly List<GameObject> _spawnedItems = new();

    // Outside click
    [Header("Outside Click")]
    public bool closeOnOutsideClick = true;
    public bool closeOnEscape = true;
    public bool useBlocker = false;
    public List<RectTransform> insideRects = new();

    // ====== Popup Placement (Togglable) ======
    [Header("Popup Placement")]
    [Tooltip("Jika ON: panel diposisikan otomatis di bawah header. Jika OFF: posisi mengikuti prefab (TIDAK loncat).")]
    public bool autoPositionUnderHeader = false;      // ← default OFF, menonaktifkan logika 'loncat'
    public XAlign panelAlign = XAlign.Left;
    public Vector2 popupOffset = new Vector2(0f, -4f);

    [Tooltip("Jika ON: panel dibawa ke urutan sibling paling depan. OFF: biarkan layer/sibling apa adanya.")]
    public bool bringPanelToFront = false;            // ← default OFF, layer dibiarkan seperti sekarang
    [Tooltip("Dipakai hanya jika bringPanelToFront = ON.")]
    public int panelSortingOrder = 1000;

    // (Legacy—dimatikan supaya tak geser toolbar)
    [Header("Legacy Z-Order (ignored)")]
    public bool keepHeaderOnTop = false;
    public bool raiseDropdownAboveSiblings = false;

    // cache parent asli (jaga-jaga)
    Transform _panelOriginalParent;
    int _panelOriginalSibling;
    Vector3 _panelOriginalScale;

    // ====== Integrasi Variant tanpa VariantBridge ======
    [Header("Variant Integration")]
    [Tooltip("Dropdown otomatis membaca ID dari objek aktif & selection ketika berubah.")]
    public bool autoSyncFromSelection = true;

    [Tooltip("Dropdown otomatis mengikuti bila ID objek aktif berubah dari luar (polling ringan).")]
    public bool autoTrackActiveId = true;

    [Tooltip("Interval pengecekan perubahan ID (detik) saat autoTrackActiveId aktif).")]
    public float trackInterval = 0.15f;

#if UNITY_EDITOR
    [ContextMenu("Rebuild Now")] void RebuildNow() => Rebuild();
#endif

    void Awake()
    {
        _rootCanvas = GetComponentInParent<Canvas>();

        if (rebuildOnAwake) Rebuild();
        SafeHidePanel();

        if (headerButton) headerButton.onClick.AddListener(Toggle);

        var headerRT = headerButton ? headerButton.GetComponent<RectTransform>() : null;
        if (headerRT && !insideRects.Contains(headerRT)) insideRects.Add(headerRT);
        if (panel && !insideRects.Contains(panel)) insideRects.Add(panel);

        // Pastikan PANEL tidak ikut layout; layer dibiarkan apa adanya.
        if (panel)
        {
            var le = panel.GetComponent<LayoutElement>();
            if (!le) le = panel.gameObject.AddComponent<LayoutElement>();
            le.ignoreLayout = true;

            // Hanya pasang Canvas override jika kamu ingin 'bringPanelToFront'
            if (bringPanelToFront)
            {
                var canv = panel.GetComponent<Canvas>() ?? panel.gameObject.AddComponent<Canvas>();
                canv.overrideSorting = true;
                canv.sortingOrder = panelSortingOrder;

                if (!panel.GetComponent<GraphicRaycaster>())
                    panel.gameObject.AddComponent<GraphicRaycaster>();
            }

            _panelOriginalParent = panel.parent;
            _panelOriginalSibling = panel.GetSiblingIndex();
            _panelOriginalScale = panel.localScale;
        }

        // === Hook selection change (tanpa VariantBridge)
        if (autoSyncFromSelection && SelectionManager.I != null)
            SelectionManager.I.OnSelectionChanged += _ => SyncFromSelection();
    }

    void Start()
    {
        SafeHidePanel(); // tetap tertutup saat start

        if (autoSyncFromSelection) SyncFromSelection();
        if (autoTrackActiveId) StartCoroutine(TrackActiveIdLoop());
    }

    void OnDestroy()
    {
        if (headerButton) headerButton.onClick.RemoveListener(Toggle);

        if (SelectionManager.I != null && autoSyncFromSelection)
            SelectionManager.I.OnSelectionChanged -= _ => SyncFromSelection();
    }

    // ================== BUILD ==================
    public void Rebuild()
    {
        if (!content || !optionButtonPrefab) return;

        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
        _spawnedItems.Clear();

        for (int i = 0; i < options.Count; i++)
        {
            var opt = options[i];
            var go = Instantiate(optionButtonPrefab, content);
            go.name = $"Option_{i}_{opt.id}";
            _spawnedItems.Add(go);

            var btn = go.GetComponent<Button>();
            var slot = go.transform.Find("Slot") as RectTransform;
            var icon = go.transform.Find("Icon")?.GetComponent<Image>();
            if (icon) icon.sprite = opt.sprite;

            FitSpriteIntoSlot(slot ? slot : go.transform as RectTransform, icon, opt.sprite);

            int idx = i;
            if (btn) btn.onClick.AddListener(() => Select(idx));
        }

        if (options.Count > 0)
            Select(Mathf.Clamp(_value, 0, options.Count - 1), invoke: false);

        UpdateVisibleOptions();
    }

    // ================== OPEN/CLOSE ==================
    void Toggle()
    {
        if (!panel) return;
        if (panel.gameObject.activeSelf) Close();
        else Open();
    }

    public void Open()
    {
        if (!panel) return;

        panel.gameObject.SetActive(true);

        if (autoPositionUnderHeader)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
            PositionPanelUnderHeader();   // hanya jika toggle ON
        }

        if (bringPanelToFront)
            panel.SetAsLastSibling();     // kalau OFF, biarkan sibling/layer sesuai prefab
    }

    public void Close()
    {
        SafeHidePanel();
        if (_blocker) _blocker.SetActive(false);

        // kembalikan sibling/scale (jika sempat berubah)
        if (panel && _panelOriginalParent && panel.parent == _panelOriginalParent)
        {
            panel.SetSiblingIndex(_panelOriginalSibling);
            panel.localScale = _panelOriginalScale;
        }
    }

    void SafeHidePanel()
    {
        if (panel && panel.gameObject.activeSelf)
            panel.gameObject.SetActive(false);
    }

    // ================== POSITIONING (opsional) ==================
    void PositionPanelUnderHeader()
    {
        if (!headerButton || !panel) return;

        var headerRT = headerButton.GetComponent<RectTransform>();
        if (!headerRT) return;

        var cam = (_rootCanvas && _rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                  ? _rootCanvas.worldCamera : null;

        Vector3[] wc = new Vector3[4];
        headerRT.GetWorldCorners(wc); // 0=BL,1=TL,2=TR,3=BR

        Vector3 worldAnchor;
        switch (panelAlign)
        {
            case XAlign.Center: worldAnchor = (wc[0] + wc[3]) * 0.5f; break;
            case XAlign.Right: worldAnchor = wc[3]; break;
            default: worldAnchor = wc[0]; break;
        }

        var target = panel.parent as RectTransform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            target,
            RectTransformUtility.WorldToScreenPoint(cam, worldAnchor),
            cam,
            out var localPt
        );

        float px = (panelAlign == XAlign.Left) ? 0f : (panelAlign == XAlign.Center ? 0.5f : 1f);
        panel.anchorMin = panel.anchorMax = new Vector2(0f, 1f);
        panel.pivot = new Vector2(px, 1f);
        panel.anchoredPosition = localPt + popupOffset;
    }

    // ================== SELECTION (DARI USER) ==================
    public void Select(int index, bool invoke = true)
    {
        if (index < 0 || index >= options.Count) return;

        _value = index;
        var opt = options[index];

        if (captionIcon && captionSlot && opt.sprite)
        {
            captionIcon.sprite = opt.sprite;
            FitSpriteIntoSlot(captionSlot, captionIcon, opt.sprite);
        }

        if (sampleTMP && opt.tmpFontAsset) sampleTMP.font = opt.tmpFontAsset;
        if (previewImage && opt.sprite) previewImage.sprite = opt.sprite;

        UpdateVisibleOptions();

        if (invoke)
        {
            onValueChanged.Invoke(index, opt);
            // Push ID ke object terpilih (PieceVariant / TypeVariant)
            ApplyIdToSelection(opt?.id);
        }

        if (closeOnSelect) Close();
    }

    // === API tambahan: pilih berdasarkan ID (untuk sinkronisasi tanpa memicu Apply ulang)
    public void SelectById(string id, bool invoke = false)
    {
        if (string.IsNullOrEmpty(id)) return;
        int idx = options.FindIndex(o => o != null && o.id == id);
        if (idx >= 0) Select(idx, invoke);
    }

    // ================== UPDATE LOOP ==================
    void Update()
    {
        if (!panel || !panel.gameObject.activeSelf) return;

        if (closeOnOutsideClick)
        {
            if (Input.GetMouseButtonDown(0)) TryCloseOnPointer(Input.mousePosition);
            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
                TryCloseOnPointer(Input.GetTouch(0).position);
        }

        if (closeOnEscape && Input.GetKeyDown(KeyCode.Escape))
            Close();
    }

    void TryCloseOnPointer(Vector2 screenPos)
    {
        if (IsInside(panel, screenPos)) return;

        for (int i = 0; i < insideRects.Count; i++)
            if (insideRects[i] && IsInside(insideRects[i], screenPos))
                return;

        Close();
    }

    bool IsInside(RectTransform rt, Vector2 screenPos)
    {
        if (!rt) return false;
        Camera cam = (_rootCanvas && _rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                     ? _rootCanvas.worldCamera : null;
        return RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, cam);
    }

    // ================== HELPERS ==================
    void FitSpriteIntoSlot(RectTransform slot, Image icon, Sprite sprite)
    {
        if (!slot || !icon || !sprite) return;

        LayoutRebuilder.ForceRebuildLayoutImmediate(slot);

        float maxW = Mathf.Max(0, slot.rect.width - 2f * slotPadding);
        float maxH = Mathf.Max(0, slot.rect.height - 2f * slotPadding);
        if (maxW <= 0 || maxH <= 0) return;

        float natW = sprite.rect.width;
        float natH = sprite.rect.height;

        float scale = Mathf.Min(maxW / natW, maxH / natH);
        if (!allowUpscale) scale = Mathf.Min(scale, 1f);

        float fitW = natW * scale;
        float fitH = natH * scale;

        var rt = icon.rectTransform;
        rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;

        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Horizontal, fitW);
        rt.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, fitH);

        icon.preserveAspect = true;
        icon.raycastTarget = false;
    }

    void UpdateVisibleOptions()
    {
        if (!hideSelectedInList) return;
        if (_spawnedItems == null || _spawnedItems.Count == 0) return;

        for (int i = 0; i < _spawnedItems.Count; i++)
        {
            var go = _spawnedItems[i];
            if (!go) continue;
            bool shouldShow = (i != _value);
            if (go.activeSelf != shouldShow) go.SetActive(shouldShow);
        }

        if (content) LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }

    public int GetValue() => _value;
    public Option GetOption() =>
        (_value >= 0 && _value < options.Count) ? options[_value] : null;

    // ================== VARIANT INTEGRATION ==================

    // Tarik ID dari object aktif/selection, lalu set tampilan dropdown
    public void SyncFromSelection()
    {
        if (options == null || options.Count == 0) return;
        if (SelectionManager.I == null) return;

        // 1) Active dulu
        GameObject go = SelectionManager.I.Active ? SelectionManager.I.Active.gameObject : null;
        string id = ReadId(go);

        // 2) Jika belum ada, cari dari anggota selection lain
        if (string.IsNullOrEmpty(id) && SelectionManager.I.Current != null)
        {
            foreach (var box in SelectionManager.I.Current)
            {
                if (!box) continue;
                id = ReadId(box.gameObject);
                if (!string.IsNullOrEmpty(id)) break;
            }
        }

        // 3) Sinkron (tanpa invoke agar tidak memicu Apply kembali)
        if (!string.IsNullOrEmpty(id))
            SelectById(id, invoke: false);
    }

    // Dorong ID yang dipilih ke semua objek dalam selection
    void ApplyIdToSelection(string id)
    {
        if (string.IsNullOrEmpty(id) || SelectionManager.I == null) return;
        var sel = SelectionManager.I.Current;
        if (sel == null) return;

        foreach (var box in sel)
        {
            if (!box) continue;
            var go = box.gameObject;

            // PieceVariant
            var pv = go.GetComponentInChildren<PieceVariant>(true);
            if (pv && pv.HasVariantId(id))
                pv.TrySetVariantById(id, tryKeepSnap: true);

            // TypeVariant
            var tv = go.GetComponentInChildren<TypeVariant>(true);
            if (tv && tv.HasVariantId(id))
                tv.TrySetById(id);
        }
    }

    // Baca ID dengan prioritas TypeVariant → PieceVariant
    string ReadId(GameObject g)
    {
        if (!g) return null;

        var tv = g.GetComponentInChildren<TypeVariant>(true);
        if (tv != null)
        {
            var id = tv.GetCurrentId();
            if (!string.IsNullOrEmpty(id)) return id;
        }

        var pv = g.GetComponentInChildren<PieceVariant>(true);
        if (pv != null) return pv.CurrentId;

        return null;
    }

    // Poll ringan agar dropdown mengikuti jika ID objek aktif berubah dari luar
    IEnumerator TrackActiveIdLoop()
    {
        var wait = new WaitForSeconds(trackInterval);

        while (true)
        {
            yield return wait;

            if (!autoTrackActiveId || SelectionManager.I == null) continue;

            string currentId = null;
            var active = SelectionManager.I.Active;
            if (active) currentId = ReadId(active.gameObject);

            if (!string.IsNullOrEmpty(currentId))
            {
                var opt = GetOption();
                var shownId = (opt != null) ? opt.id : null;

                if (currentId != shownId)
                    SelectById(currentId, invoke: false);
            }
        }
    }
}
