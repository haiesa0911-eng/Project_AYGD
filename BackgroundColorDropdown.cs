using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class BackgroundColorDropdown : MonoBehaviour
{
    [Header("Wiring")]
    public Button headerButton;
    public RectTransform panel;
    public RectTransform content;
    [Tooltip("Prefab opsi (idealnya root = Button).")]
    public GameObject optionButtonPrefab;

    [Header("Target Background (World)")]
    [Tooltip("SpriteRenderer background dunia yang akan diwarnai.")]
    public SpriteRenderer targetBackground;

    [Header("Header Preview (UI)")]
    [Tooltip("Ikon tunggal di header yang harus ikut berubah warna (opsional).")]
    public Image headerPreviewIcon;
    [Tooltip("Jika ada beberapa ikon yang juga harus ikut berubah (opsional).")]
    public List<Image> extraHeaderPreviewIcons = new List<Image>();

    [Header("Behaviour")]
    public bool closeOnSelect = true;
    public bool closeOnOutsideClick = true;
    public bool closeOnEscape = true;

    [Header("Options")]
    public List<Option> options = new List<Option>();
    [Serializable]
    public class Option
    {
        public string id = "color";
        public Color color = Color.white;
        [Tooltip("Opsional: override hex, contoh #FFAA00 atau #FFAA00CC")]
        public string hexOverride = "";
        [Tooltip("Opsional: path Image di prefab opsi yang akan diwarnai (kosongkan untuk cari Image pertama).")]
        public string previewImagePath = "";
    }

    // runtime
    private Canvas _rootCanvas;
    private readonly List<GameObject> _spawned = new();
    private int _value = -1;
    private readonly List<RectTransform> _inside = new();

    void Awake()
    {
        _rootCanvas = GetComponentInParent<Canvas>();
        if (panel) panel.gameObject.SetActive(false);
        if (headerButton) headerButton.onClick.AddListener(Toggle);

        Rebuild();

        // area yang dianggap "di dalam"
        if (headerButton) _inside.Add(headerButton.GetComponent<RectTransform>());
        if (panel) _inside.Add(panel);
    }

    [ContextMenu("Rebuild")]
    public void Rebuild()
    {
        if (!content || !optionButtonPrefab) return;

        // --- HAPUS ANAK2 CONTENT, TAPI JANGAN HAPUS TEMPLATE JIKA DIA ADA DI DALAM CONTENT ---
        for (int i = content.childCount - 1; i >= 0; i--)
        {
            var child = content.GetChild(i);
            // jika template berada di dalam content, jangan dihancurkan
            if (optionButtonPrefab && child == optionButtonPrefab.transform)
                continue;
            Destroy(child.gameObject);
        }
        _spawned.Clear();

        for (int i = 0; i < options.Count; i++)
        {
            var opt = options[i];

            // --- INSTANTIATE DARI TEMPLATE (BOLEH TEMPLATE DI HIERARCHY) ---
            var go = Instantiate(optionButtonPrefab, content);

            // Penting: jika template nonaktif, pastikan hasil spawn diaktifkan
            if (!go.activeSelf) go.SetActive(true);

            go.name = $"Option_{i}_{opt.id}";
            _spawned.Add(go);

            // set warna preview pada Image (kalau ada)
            var img = FindImage(go.transform, opt.previewImagePath);
            if (img) img.color = ResolveColor(opt);

            // pastikan ada Button dan pasang listener
            var btn = SafeGetButton(go);
            int idx = i;
            btn.onClick.AddListener(() => Select(idx));
        }

        if (options.Count > 0)
            Select(Mathf.Clamp(_value, 0, options.Count - 1), invokeColor: false);
    }

    public void Toggle()
    {
        if (!panel) return;
        panel.gameObject.SetActive(!panel.gameObject.activeSelf);
    }

    public void Close()
    {
        if (panel) panel.gameObject.SetActive(false);
    }

    public void Open()
    {
        if (panel) panel.gameObject.SetActive(true);
    }

    public void Select(int index, bool invokeColor = true)
    {
        if (index < 0 || index >= options.Count) return;
        _value = index;

        var c = ResolveColor(options[index]);

        if (invokeColor)
            ApplyBackgroundColor(c);

        ApplyHeaderPreview(c);   // <-- penting: tint ikon header juga

        if (closeOnSelect) Close();
    }

    void Update()
    {
        if (!panel || !panel.gameObject.activeSelf) return;

        if (closeOnEscape && Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
            return;
        }

        if (closeOnOutsideClick)
        {
            if (Input.GetMouseButtonDown(0) && ShouldCloseOnPointer(Input.mousePosition))
                Close();

            if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
                if (ShouldCloseOnPointer(Input.GetTouch(0).position)) Close();
        }
    }

    // -------- helpers --------
    void ApplyBackgroundColor(Color c)
    {
        if (targetBackground) targetBackground.color = c;
    }

    void ApplyHeaderPreview(Color c)
    {
        if (headerPreviewIcon) headerPreviewIcon.color = c;
        if (extraHeaderPreviewIcons != null)
        {
            for (int i = 0; i < extraHeaderPreviewIcons.Count; i++)
                if (extraHeaderPreviewIcons[i]) extraHeaderPreviewIcons[i].color = c;
        }
    }

    bool ShouldCloseOnPointer(Vector2 screenPos)
    {
        for (int i = 0; i < _inside.Count; i++)
        {
            var rt = _inside[i];
            if (!rt) continue;
            if (RectTransformUtility.RectangleContainsScreenPoint(rt, screenPos, CanvasCamera()))
                return false;
        }
        return true;
    }

    Camera CanvasCamera()
    {
        if (_rootCanvas && _rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            return _rootCanvas.worldCamera;
        return null;
    }

    static Image FindImage(Transform root, string path)
    {
        if (!string.IsNullOrEmpty(path))
        {
            var t = root.Find(path);
            if (t) return t.GetComponent<Image>();
        }
        return root.GetComponentInChildren<Image>(true);
    }

    static Button SafeGetButton(GameObject go)
    {
        var btn = go.GetComponent<Button>();
        if (!btn) btn = go.GetComponentInChildren<Button>(true);
        if (!btn) btn = go.AddComponent<Button>(); // fallback
        return btn;
    }

    static bool TryParseHex(string hex, out Color color)
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

    Color ResolveColor(Option opt)
    {
        if (!string.IsNullOrEmpty(opt.hexOverride) && TryParseHex(opt.hexOverride, out var parsed))
            return parsed;
        return opt.color;
    }
}
