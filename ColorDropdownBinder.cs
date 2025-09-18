using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class ColorDropdownBinder : MonoBehaviour
{
    [Header("Link")]
    public ColorDropdown dropdown;

    [Header("Apply")]
    [Tooltip("Kalau ON semua object terseleksi diganti, kalau OFF hanya object pertama.")]
    public bool applyToAllSelected = true;

    [Header("Auto Sync")]
    [Tooltip("Binder akan otomatis mengikuti selection aktif setiap frame.")]
    public bool autoSyncFromSelection = true;

    SelectionBox _lastActive;       // cache untuk deteksi perubahan selection
    string _lastActiveColorId;      // cache untuk deteksi perubahan id pada object yg sama

    void Awake()
    {
        if (dropdown) dropdown.onValueChanged.AddListener(OnDropdownChanged);
    }

    void OnDestroy()
    {
        if (dropdown) dropdown.onValueChanged.RemoveListener(OnDropdownChanged);
    }

    void OnEnable()
    {
        // sync awal supaya header langsung cocok saat scene play/enable
        SyncDropdownToSelectionById(fallbackToColor: true);
        CacheActive();
    }

    void LateUpdate()
    {
        if (!autoSyncFromSelection) return;

        var cur = (SelectionManager.I ? SelectionManager.I.Active : null);
        // kalau Active berganti object → sync
        if (cur != _lastActive)
        {
            SyncDropdownToSelectionById(fallbackToColor: true);
            CacheActive();
            return;
        }

        // kalau object sama tapi colorId-nya berubah (mis. diubah script lain) → sync
        if (_lastActive != null)
        {
            var recv = _lastActive.GetComponent<IColorReceiver>() ?? _lastActive.GetComponentInParent<IColorReceiver>();
            var curId = NormalizeId(recv?.GetColorId());
            if (curId != _lastActiveColorId)
            {
                SyncDropdownToSelectionById(fallbackToColor: true);
                CacheActive();
            }
        }
    }

    void CacheActive()
    {
        _lastActive = (SelectionManager.I ? SelectionManager.I.Active : null);
        if (_lastActive != null)
        {
            var recv = _lastActive.GetComponent<IColorReceiver>() ?? _lastActive.GetComponentInParent<IColorReceiver>();
            _lastActiveColorId = NormalizeId(recv?.GetColorId());
        }
        else
        {
            _lastActiveColorId = null;
        }
    }

    // === USER pilih warna di dropdown → apply ke selection ===
    void OnDropdownChanged(int idx, ColorDropdown.Option opt)
    {
        var selected = GetSelectedBoxes();
        if (selected.Count == 0) return;

        if (!applyToAllSelected)
            selected = new List<SelectionBox> { selected[0] };

        var col = ResolveColor(opt);

        foreach (var sb in selected)
        {
            if (!sb) continue;
            var recv = sb.GetComponent<IColorReceiver>() ?? sb.GetComponentInParent<IColorReceiver>();
            if (recv == null) continue;

            recv.SetColor(col);
            recv.SetColorId(opt.id); // update ID agar klik berikutnya mudah disinkronkan
        }

        // setelah apply, update cache id (agar LateUpdate tidak langsung menganggap “berubah”)
        CacheActive();
    }

    // === Sinkronkan header ke object terseleksi (prioritas: ID, fallback warna) ===
    public void SyncDropdownToSelectionById(bool fallbackToColor = false)
    {
        if (!dropdown) return;

        // gunakan Active kalau ada, kalau tidak cari yang selected pertama
        SelectionBox sourceBox = SelectionManager.I ? SelectionManager.I.Active : null;
        if (sourceBox == null)
        {
#if UNITY_2023_1_OR_NEWER
            var all = Object.FindObjectsByType<SelectionBox>(FindObjectsSortMode.None);
#else
            var all = Object.FindObjectsOfType<SelectionBox>();
#endif
            foreach (var sb in all) { if (sb && sb.IsSelected) { sourceBox = sb; break; } }
        }
        if (sourceBox == null) return;

        var recv = sourceBox.GetComponent<IColorReceiver>() ?? sourceBox.GetComponentInParent<IColorReceiver>();
        if (recv == null) return;

        int idx = -1;

        // 1) Cocokkan berdasar ID (case-insensitive + trim)
        string id = NormalizeId(recv.GetColorId());
        if (!string.IsNullOrEmpty(id))
        {
            for (int i = 0; i < dropdown.options.Count; i++)
                if (NormalizeId(dropdown.options[i].id) == id) { idx = i; break; }
        }

        // 2) (opsional) kalau ID kosong/tak ketemu → cocokkan warna persis
        if (idx < 0 && fallbackToColor)
        {
            var cur = recv.GetColor();
            for (int i = 0; i < dropdown.options.Count; i++)
                if (ApproximatelyEqualColor(cur, ResolveColor(dropdown.options[i]))) { idx = i; break; }
        }

        // apply ke header tanpa invoke (tidak memodifikasi object)
        if (idx >= 0) dropdown.Select(idx, invoke: false);  // method ini memang update caption. :contentReference[oaicite:3]{index=3}
    }

    // ===== Helpers =====
    static List<SelectionBox> GetSelectedBoxes()
    {
#if UNITY_2023_1_OR_NEWER
        var arr = Object.FindObjectsByType<SelectionBox>(FindObjectsSortMode.None);
#else
        var arr = Object.FindObjectsOfType<SelectionBox>();
#endif
        var list = new List<SelectionBox>(arr.Length);
        foreach (var sb in arr) if (sb && sb.IsSelected) list.Add(sb);
        return list;
    }

    static Color ResolveColor(ColorDropdown.Option opt)
    {
        if (!string.IsNullOrEmpty(opt.hexOverride) &&
            ColorDropdown.TryParseHexColor(opt.hexOverride, out var parsed))
            return parsed;
        return opt.color;
    }

    static string NormalizeId(string s) => string.IsNullOrEmpty(s) ? "" : s.Trim().ToLowerInvariant();

    static bool ApproximatelyEqualColor(Color a, Color b)
    {
        const float eps = 1e-4f;
        return Mathf.Abs(a.r - b.r) < eps &&
               Mathf.Abs(a.g - b.g) < eps &&
               Mathf.Abs(a.b - b.b) < eps &&
               Mathf.Abs(a.a - b.a) < eps;
    }
}
