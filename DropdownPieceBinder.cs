using System.Collections.Generic;
using UnityEngine;

[DisallowMultipleComponent]
public class DropdownPieceBinder : MonoBehaviour
{
    public SpriteDropdown dropdown;          // drag dari Inspector
    [Tooltip("Jika ON, semua objek yang terseleksi akan diganti. Jika OFF, hanya objek ter-atas (pertama).")]
    public bool applyToAllSelected = true;

    [Header("Auto Sync")]
    [Tooltip("Binder akan otomatis mengikuti selection aktif setiap frame, termasuk saat ID varian berubah.")]
    public bool autoSyncFromSelection = true;

    private SelectionBox _lastActive;
    private string _lastActiveVariantIdNorm;

    void Awake()
    {
        if (dropdown) dropdown.onValueChanged.AddListener(OnDropdownChangedById);
    }

    void OnDestroy()
    {
        if (dropdown) dropdown.onValueChanged.RemoveListener(OnDropdownChangedById);
    }

    void OnEnable()
    {
        // sync awal saat komponen aktif
        SyncDropdownToSelectionById();
        CacheActive();
    }

    void LateUpdate()
    {
        if (!autoSyncFromSelection) return;

        var cur = SelectionManager.I ? SelectionManager.I.Active : null;

        // 1) Jika Active ganti object → sync
        if (cur != _lastActive)
        {
            SyncDropdownToSelectionById();
            CacheActive();
            return;
        }

        // 2) Jika object sama tapi CurrentId berubah (mis. lewat kode lain) → sync
        if (_lastActive != null)
        {
            var pv = _lastActive.GetComponent<PieceVariant>() ?? _lastActive.GetComponentInParent<PieceVariant>();
            var curIdNorm = NormalizeId(pv ? pv.CurrentId : null);
            if (curIdNorm != _lastActiveVariantIdNorm)
            {
                SyncDropdownToSelectionById();
                CacheActive();
            }
        }
    }

    void CacheActive()
    {
        _lastActive = SelectionManager.I ? SelectionManager.I.Active : null;
        if (_lastActive != null)
        {
            var pv = _lastActive.GetComponent<PieceVariant>() ?? _lastActive.GetComponentInParent<PieceVariant>();
            _lastActiveVariantIdNorm = NormalizeId(pv ? pv.CurrentId : null);
        }
        else
        {
            _lastActiveVariantIdNorm = null;
        }
    }

    // === Handler utama: user memilih ID dari dropdown → apply ke selection ===
    void OnDropdownChangedById(int index, SpriteDropdown.Option opt)
    {
        var selected = GetSelectedBoxes();
        if (selected.Count == 0) return;

        if (!applyToAllSelected)
            selected = new List<SelectionBox> { selected[0] };

        foreach (var sb in selected)
        {
            if (!sb) continue;
            var pv = sb.GetComponent<PieceVariant>() ?? sb.GetComponentInParent<PieceVariant>();
            if (!pv) continue;

            // Apply berdasarkan ID; PieceVariant sudah sediakan TrySetVariantById
            bool ok = pv.TrySetVariantById(opt.id);   // no-op jika ID tidak ada
            if (!ok) Debug.LogWarning($"[Variant] ID '{opt.id}' tidak ditemukan pada piece '{pv.name}'.", pv);
        }

        // Update cache agar LateUpdate tidak langsung mengira berubah lagi
        CacheActive();
    }

    // === Sinkronkan caption dropdown ke varian CURRENT pada selection aktif ===
    public void SyncDropdownToSelectionById()
    {
        if (!dropdown) return;

        var src = SelectionManager.I ? SelectionManager.I.Active : null;
        if (src == null)
        {
            // fallback: pakai selection pertama
#if UNITY_2023_1_OR_NEWER
            var all = Object.FindObjectsByType<SelectionBox>(FindObjectsSortMode.None);
#else
            var all = Object.FindObjectsOfType<SelectionBox>();
#endif
            foreach (var sb in all) { if (sb && sb.IsSelected) { src = sb; break; } }
        }
        if (src == null) return;

        var pv = src.GetComponent<PieceVariant>() ?? src.GetComponentInParent<PieceVariant>();
        if (!pv) return;

        string curIdNorm = NormalizeId(pv.CurrentId);
        if (string.IsNullOrEmpty(curIdNorm)) return;

        int idx = -1;
        for (int i = 0; i < dropdown.options.Count; i++)
            if (NormalizeId(dropdown.options[i].id) == curIdNorm) { idx = i; break; }

        if (idx >= 0)
            dropdown.Select(idx, invoke: false); // ubah caption tanpa men-trigger apply ke piece
    }

    // === Util: ambil semua SelectionBox yang sedang selected ===
    static List<SelectionBox> GetSelectedBoxes()
    {
#if UNITY_2023_1_OR_NEWER
        var arr = Object.FindObjectsByType<SelectionBox>(FindObjectsSortMode.None);
#else
        var arr = Object.FindObjectsOfType<SelectionBox>();
#endif
        var list = new List<SelectionBox>(arr.Length);
        foreach (var sb in arr)
            if (sb && sb.IsSelected) list.Add(sb);
        return list;
    }

    static string NormalizeId(string s) => string.IsNullOrEmpty(s) ? "" : s.Trim().ToLowerInvariant();
}
