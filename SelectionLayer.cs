using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// UI Layer (visual only), sinkron dengan SelectionManager melalui Link.
/// - Single-click/multi-click diputuskan oleh SelectionLayerLinkToManager + SelectionManager.
/// - Layer boleh aktif meski worldBox belum ada (Photoshop-like),
///   tapi akan padam otomatis saat ada seleksi world single di tempat lain.
/// </summary>
[DisallowMultipleComponent]
public class SelectionLayer : MonoBehaviour,
    IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [Header("Target Box UI")]
    [SerializeField] private Image targetBox;

    [Header("Behavior")]
    [Tooltip("Klik UI kosong (non-interaktif) akan Clear() lewat SelectionManager.")]
    [SerializeField] private bool enableGlobalClickClear = true;

    [Header("Alpha Settings")]
    [Range(0, 255)] public int alphaInactive = 0;
    [Range(0, 255)] public int alphaHighlight = 155;
    [Range(0, 255)] public int alphaActive = 255;

    private static SelectionLayer s_leader;

    private static readonly System.Type[] s_interactiveTypes = new System.Type[] {
        typeof(Button), typeof(Toggle), typeof(Scrollbar), typeof(Slider),
        typeof(Dropdown), typeof(ScrollRect), typeof(InputField),
#if TMP_PRESENT
        typeof(TMPro.TMP_InputField),
#endif
        typeof(SelectionLayer)
    };

    // visual state
    private bool _isActive;
    public bool IsSelected => _isActive;   // <— dipakai Link

    // ---------- Unity lifecycle ----------
    private void Reset()
    {
        if (!targetBox) targetBox = GetComponent<Image>();
    }

    private void Awake()
    {
        if (!targetBox) targetBox = GetComponent<Image>();
        if (s_leader == null) s_leader = this;
    }

    private void OnEnable() => SetAlphaInternal(_isActive ? alphaActive : alphaInactive);

    private void OnDestroy()
    {
        if (s_leader == this) s_leader = null;
    }

    private void Update()
    {
        if (!enableGlobalClickClear) return;
        if (s_leader != this) return;

        if (Input.GetMouseButtonDown(0)) TryGlobalDeselect(Input.mousePosition);
        if (Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Began)
            TryGlobalDeselect(Input.GetTouch(0).position);
    }

    // ---------- Pointer (hover only). Klik diproses di Link ----------
    public void OnPointerEnter(PointerEventData _) { if (!_isActive) SetAlphaInternal(alphaHighlight); }
    public void OnPointerExit(PointerEventData _) { if (!_isActive) SetAlphaInternal(alphaInactive); }
    public void OnPointerClick(PointerEventData _) { /* no-op, Link yang handle */ }

    // ---------- Public API (dipanggil Link/Manager) ----------
    public void SetSelected(bool value)
    {
        _isActive = value;
        SetAlphaInternal(value ? alphaActive : alphaInactive);
    }

    public void SetHighlight(bool value)
    {
        if (_isActive) { SetAlphaInternal(alphaActive); return; }
        SetAlphaInternal(value ? alphaHighlight : alphaInactive);
    }

    public static void ClearGlobal()
    {
#if UNITY_2023_1_OR_NEWER
        var layers = Object.FindObjectsByType<SelectionLayer>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        var layers = Object.FindObjectsOfType<SelectionLayer>(true);
#endif
        foreach (var l in layers) if (l) l.SetSelected(false);
    }

    // ---------- Internal ----------
    private void SetAlphaInternal(int a255)
    {
        if (!targetBox) return;
        var c = targetBox.color; c.a = a255 / 255f; targetBox.color = c;
    }

    private static void TryGlobalDeselect(Vector2 screenPos)
    {
        if (IsPointerOverInteractive(screenPos)) return;

        // Bila ada SelectionManager, clear di sana (UI akan ikut via event)
        if (SelectionManager.I != null) { SelectionManager.I.Clear(); return; }

        // Fallback tanpa manager
        ClearGlobal();
    }

    private static bool IsPointerOverInteractive(Vector2 screenPos)
    {
        if (EventSystem.current == null) return false;
        var ped = new PointerEventData(EventSystem.current) { position = screenPos };
        var results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(ped, results);

        foreach (var r in results)
        {
            Transform t = r.gameObject.transform;
            while (t)
            {
                var go = t.gameObject;
                for (int i = 0; i < s_interactiveTypes.Length; i++)
                    if (go.GetComponent(s_interactiveTypes[i]) != null) { results.Clear(); return true; }
                t = t.parent;
            }
        }
        results.Clear();
        return false;
    }
}
