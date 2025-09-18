using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Events;

[DisallowMultipleComponent]
public class ColorDropdown : MonoBehaviour
{
    public enum XAlign { Left, Center, Right }

    [Header("Wiring")]
    public Button headerButton;
    public RectTransform panel;       // popup (tetap child dropdown)
    public RectTransform content;     // parent item
    [Tooltip("Prefab opsi (root = Button).")]
    public GameObject optionButtonPrefab;

    [Header("Caption - Pakai Prefab (opsional)")]
    [Tooltip("Container tempat meletakkan prefab caption (mis. slot kecil di toolbar).")]
    public RectTransform captionContainer;
    [Tooltip("Prefab yang sama/serupa dengan item agar header punya frame juga.")]
    public GameObject captionPrefab;

    [Header("Caption - Fallback Icon (opsional)")]
    [Tooltip("Kalau tidak pakai captionPrefab, bisa tint 1 Image ini saja.")]
    public Image captionIcon;

    [Header("Apply Selection")]
    [Tooltip("Image target yang diwarnai saat user memilih opsi (White Box di kanvas kerja).")]
    public Image targetWhiteBox;

    public enum FitMode { UsePrefabLayout, StretchToFrame }

    [Header("Behaviour")]
    public bool closeOnSelect = true;
    public bool hideSelectedInList = true;
    public bool rebuildOnAwake = true;
    public FitMode fitMode = FitMode.StretchToFrame;

    // ===== Popup placement (baru; aman default OFF) =====
    [Header("Popup Placement")]
    [Tooltip("Jika ON: panel diposisikan otomatis tepat di bawah header. Jika OFF: posisi mengikuti prefab (tidak loncat).")]
    public bool autoPositionUnderHeader = false;
    public XAlign panelAlign = XAlign.Left;
    public Vector2 popupOffset = new Vector2(0f, -4f);

    [Tooltip("Jika ON: panel dibawa ke urutan sibling paling depan. OFF: biarkan layer/sibling apa adanya.")]
    public bool bringPanelToFront = false;
    [Tooltip("SortingOrder Canvas panel (dipakai hanya jika bringPanelToFront = ON).")]
    public int panelSortingOrder = 1000;

    // ===== Prefab hook =====
    [Header("Prefab Hook")]
    [Tooltip("Path Image yang ditint di item. Kosongkan bila pakai marker ColorTarget.")]
    public string optionColorImagePath = "";
    [Tooltip("Path RectTransform 'Frame' untuk autofit (StretchToFrame). Kosong = pakai parent dari target.")]
    public string optionFrameRectPath = "Frame";
    [Tooltip("Padding (L,T,R,B) saat fill ke frame (StretchToFrame).")]
    public Vector4 fitPadding = new Vector4(4, 4, 4, 4);
    [Tooltip("Bersihkan preferred size LayoutElement agar ukuran tidak terkunci (StretchToFrame).")]
    public bool clearLayoutPreferred = true;

    [Header("Caption Hook (jika pakai captionPrefab)")]
    [Tooltip("Path Image yang ditint di caption. Kosongkan bila pakai ColorTarget di prefab caption.")]
    public string captionColorImagePath = "";
    [Tooltip("Path 'Frame' di caption untuk autofit (StretchToFrame).")]
    public string captionFrameRectPath = "Frame";

    [Serializable]
    public class Option
    {
        public string id;
        public Color color = Color.white;
        [Tooltip("Override hex (#RRGGBB atau #RRGGBBAA). Jika diisi, menggantikan 'color'.")]
        public string hexOverride;
    }

    [Header("Options")]
    public List<Option> options = new List<Option>();

    [Serializable] public class DropdownEvent : UnityEvent<int, Option> { }
    public DropdownEvent onValueChanged = new DropdownEvent();

    // runtime
    int _value = -1;
    GameObject _blocker;
    readonly List<GameObject> _spawned = new List<GameObject>();
    Canvas _rootCanvas;

    // caption instance
    GameObject _captionInstance;
    Image _captionTargetImg;

    // === Outside Click ===
    [Header("Outside Click")]
    [Tooltip("Kalau true, panel akan menutup saat klik di luar area dropdown/header.")]
    public bool closeOnOutsideClick = true;

    [Tooltip("Kalau true, tekan ESC menutup panel.")]
    public bool closeOnEscape = true;

    [Tooltip("Kalau true, tetap pakai blocker transparan (menahan klik di belakang). " +
             "Set false untuk non-blocking: klik luar menutup dan tetap tembus ke UI lain.")]
    public bool useBlocker = false; // default non-blocking

    [Tooltip("Area tambahan yang dianggap 'di dalam' (opsional). " +
             "Misal: container caption di toolbar, tombol lain yang terkait).")]
    public List<RectTransform> insideRects = new List<RectTransform>();

#if UNITY_EDITOR
    [ContextMenu("Rebuild Now")] void RebuildNow() => Rebuild();
#endif

    void Awake()
    {
        _rootCanvas = GetComponentInParent<Canvas>();
        if (rebuildOnAwake) Rebuild();
        SafeHidePanel();
        if (headerButton) headerButton.onClick.AddListener(Toggle);
        PrepareCaptionInstance(); // siapkan header kalau pakai prefab

        // daftarkan area "di dalam" agar klik padanya tidak menutup
        var headerRT = headerButton ? headerButton.GetComponent<RectTransform>() : null;
        if (headerRT && !insideRects.Contains(headerRT)) insideRects.Add(headerRT);
        if (captionContainer && !insideRects.Contains(captionContainer)) insideRects.Add(captionContainer);
        if (panel && !insideRects.Contains(panel)) insideRects.Add(panel);

        // --- Penting: panel tidak ikut layout & (opsional) berada di atas ---
        if (panel)
        {
            var le = panel.GetComponent<LayoutElement>();
            if (!le) le = panel.gameObject.AddComponent<LayoutElement>();
            le.ignoreLayout = true;

            if (bringPanelToFront)
            {
                var canv = panel.GetComponent<Canvas>() ?? panel.gameObject.AddComponent<Canvas>();
                canv.overrideSorting = true;
                canv.sortingOrder = panelSortingOrder;

                if (!panel.GetComponent<GraphicRaycaster>())
                    panel.gameObject.AddComponent<GraphicRaycaster>();
            }
        }
    }

    void PrepareCaptionInstance()
    {
        if (!captionContainer || !captionPrefab) return;
        if (_captionInstance == null)
        {
            _captionInstance = Instantiate(captionPrefab, captionContainer);
            _captionInstance.name = "CaptionPrefabInstance";
            _captionTargetImg = FindColorImage(_captionInstance.transform, captionColorImagePath, "White Box", captionFrameRectPath, false);
            if (fitMode == FitMode.StretchToFrame && _captionTargetImg)
                AutoFitToFrame(_captionTargetImg.rectTransform, _captionInstance.transform, captionFrameRectPath);
        }
    }

    // ---------- Build ----------
    public void Rebuild()
    {
        if (!content || !optionButtonPrefab) return;

        for (int i = content.childCount - 1; i >= 0; i--)
            Destroy(content.GetChild(i).gameObject);
        _spawned.Clear();

        for (int i = 0; i < options.Count; i++)
        {
            var opt = options[i];
            var go = Instantiate(optionButtonPrefab, content);
            go.name = $"Option_{i}_{opt.id}";
            _spawned.Add(go);

            // target warna per item
            var itemTarget = FindColorImage(go.transform, optionColorImagePath, "White Box", optionFrameRectPath, false);
            if (itemTarget)
            {
                itemTarget.color = ResolveColor(opt);
                if (fitMode == FitMode.StretchToFrame)
                    AutoFitToFrame(itemTarget.rectTransform, go.transform, optionFrameRectPath);
            }

            var btn = go.GetComponent<Button>();
            int idx = i;
            if (btn) btn.onClick.AddListener(() => Select(idx));
        }

        if (options.Count > 0)
            Select(Mathf.Clamp(_value, 0, options.Count - 1), invoke: false);

        UpdateVisibleOptions();
    }

    // ---------- Open/Close ----------
    public void Toggle()
    {
        if (panel && panel.gameObject.activeSelf) Close();
        else Open();
    }

    public void Open()
    {
        if (!panel) return;

        panel.gameObject.SetActive(true);

        if (autoPositionUnderHeader)
        {
            LayoutRebuilder.ForceRebuildLayoutImmediate(panel);
            PositionPanelUnderHeader(); // hanya jika ON
        }

        if (bringPanelToFront)
            panel.SetAsLastSibling(); // kalau OFF, biarkan layer/sibling sesuai prefab

        if (useBlocker) SetupBlockerUnderDropdown(); // hanya jika ingin blok klik di belakang
        else if (_blocker) _blocker.SetActive(false);
    }

    void SetupBlockerUnderDropdown()
    {
        if (_blocker == null)
        {
            _blocker = new GameObject("DropdownBlocker",
                typeof(RectTransform), typeof(Image), typeof(Button));
            Transform parentForBlocker = transform.parent
                ? transform.parent
                : (_rootCanvas ? _rootCanvas.transform : transform.root);
            _blocker.transform.SetParent(parentForBlocker, false);

            var rt = (RectTransform)_blocker.transform;
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero; rt.offsetMax = Vector2.zero;

            var img = _blocker.GetComponent<Image>();
            img.color = new Color(0, 0, 0, 0); // transparan tapi raycast ON
            _blocker.GetComponent<Button>().onClick.AddListener(Close);
        }

        _blocker.SetActive(true);

        // Pastikan blocker tepat di bawah dropdown dalam urutan sibling
        int ddIndex = transform.GetSiblingIndex();
        _blocker.transform.SetSiblingIndex(Mathf.Max(0, ddIndex - 1));
    }

    public void Close()
    {
        SafeHidePanel();
        if (_blocker) _blocker.SetActive(false);
    }

    void SafeHidePanel()
    {
        if (panel) panel.gameObject.SetActive(false);
    }

    // ---------- Select ----------
    public void Select(int index, bool invoke = true)
    {
        if (index < 0 || index >= options.Count) return;
        _value = index;

        var opt = options[index];
        var col = ResolveColor(opt);

        // caption pakai prefab
        if (_captionInstance && _captionTargetImg)
            _captionTargetImg.color = col;

        // fallback caption icon
        if (captionIcon)
            captionIcon.color = col;

        if (targetWhiteBox)
            targetWhiteBox.color = col;

        UpdateVisibleOptions();

        if (invoke) onValueChanged.Invoke(index, opt);
        if (closeOnSelect) Close();
    }

    void UpdateVisibleOptions()
    {
        if (!hideSelectedInList) return;
        for (int i = 0; i < _spawned.Count; i++)
            if (_spawned[i]) _spawned[i].SetActive(i != _value);

        if (content) LayoutRebuilder.ForceRebuildLayoutImmediate(content);
    }

    // ---------- Outside click / ESC ----------
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

    // ---------- Utils ----------
    static Image FindColorImage(Transform itemRoot, string path, string nameHint, string framePath, bool debugLog = false)
    {
        // 1) Marker ColorTarget (paling aman)
        var marker = itemRoot.GetComponentInChildren<ColorTarget>(true);
        if (marker)
        {
            var img = marker.GetComponent<Image>();
            if (debugLog) Debug.Log($"[ColorDropdown] Found ColorTarget -> {img?.name}");
            if (img) return img;
        }

        // 2) Path eksplisit
        if (!string.IsNullOrEmpty(path))
        {
            var t = itemRoot.Find(path);
            if (t)
            {
                var img = t.GetComponent<Image>();
                if (debugLog) Debug.Log($"[ColorDropdown] Found by path '{path}' -> {img?.name}");
                if (img) return img;
            }
        }

        // 3) Berdasarkan nama (case-insensitive, ignore spasi)
        if (!string.IsNullOrEmpty(nameHint))
        {
            string norm(string s) => new string(Array.FindAll(s.ToCharArray(), c => !char.IsWhiteSpace(c))).ToLowerInvariant();
            string goal = norm(nameHint);

            foreach (var img in itemRoot.GetComponentsInChildren<Image>(true))
            {
                if (norm(img.name) == goal) return img;
            }
        }

        // 4) Fallback: Image pertama di bawah Frame (kalau ada) atau root
        Transform frame = null;
        if (!string.IsNullOrEmpty(framePath)) frame = itemRoot.Find(framePath);
        var searchRoot = frame ? frame : itemRoot;
        return searchRoot.GetComponentInChildren<Image>(true);
    }

    void AutoFitToFrame(RectTransform target, Transform itemRoot, string framePath)
    {
        if (fitMode == FitMode.UsePrefabLayout || !target) return;

        RectTransform frame = null;
        if (!string.IsNullOrEmpty(framePath))
        {
            var t = itemRoot.Find(framePath);
            if (t) frame = t as RectTransform;
        }
        if (!frame) frame = target.parent as RectTransform;
        if (!frame) return;

        // optional: bersihkan layout lock
        if (clearLayoutPreferred)
        {
            var le = target.GetComponent<LayoutElement>();
            if (le)
            {
                le.preferredWidth = -1; le.preferredHeight = -1;
                le.minWidth = -1; le.minHeight = -1;
            }
        }

        target.anchorMin = Vector2.zero;
        target.anchorMax = Vector2.one;
        target.pivot = new Vector2(0.5f, 0.5f);
        target.anchoredPosition = Vector2.zero;
        target.offsetMin = new Vector2(fitPadding.x, fitPadding.w); // L, B
        target.offsetMax = new Vector2(-fitPadding.z, -fitPadding.y); // -R, -T

        var img = target.GetComponent<Image>();
        if (img)
        {
            img.type = Image.Type.Simple;
            img.preserveAspect = false; // benar-benar fill
        }
    }

    Color ResolveColor(Option opt)
    {
        if (!string.IsNullOrEmpty(opt.hexOverride) && TryParseHexColor(opt.hexOverride, out var parsed))
            return parsed;
        return opt.color;
    }

    public static bool TryParseHexColor(string hex, out Color color)
    {
        color = Color.white;
        if (string.IsNullOrEmpty(hex)) return false;
        string h = hex.Trim();
        if (h[0] == '#') h = h.Substring(1);

        if (h.Length == 6 && uint.TryParse(h, System.Globalization.NumberStyles.HexNumber, null, out var rgb))
        {
            byte r = (byte)((rgb >> 16) & 0xFF);
            byte g = (byte)((rgb >> 8) & 0xFF);
            byte b = (byte)(rgb & 0xFF);
            color = new Color32(r, g, b, 255);
            return true;
        }
        if (h.Length == 8 && uint.TryParse(h, System.Globalization.NumberStyles.HexNumber, null, out var rgba))
        {
            byte r = (byte)((rgba >> 24) & 0xFF);
            byte g = (byte)((rgba >> 16) & 0xFF);
            byte b = (byte)((rgba >> 8) & 0xFF);
            byte a = (byte)(rgba & 0xFF);
            color = new Color32(r, g, b, a);
            return true;
        }
        return false;
    }

    // ===== Posisi otomatis (opsional) =====
    void PositionPanelUnderHeader()
    {
        if (!autoPositionUnderHeader || !headerButton || !panel) return;

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
}
