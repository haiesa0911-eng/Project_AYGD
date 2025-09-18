using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.EventSystems;

[DisallowMultipleComponent]
public class SelectionLayerLinkToManager : MonoBehaviour, IPointerClickHandler
{
    [Header("Pairing")]
    public SelectionBox worldBox;   // boleh null (layer belum punya object)
    public SelectionLayer uiLayer;  // wajib

    bool _subscribed;

    void Awake()
    {
        if (!uiLayer) uiLayer = GetComponent<SelectionLayer>();
    }

    void OnEnable()
    {
        TrySubscribe();
        if (_subscribed) SyncNow();
        else StartCoroutine(TrySubscribeNextFrame());
    }

    System.Collections.IEnumerator TrySubscribeNextFrame()
    {
        yield return null;
        TrySubscribe();
        if (_subscribed) SyncNow();
    }

    void OnDisable()
    {
        if (SelectionManager.I != null && _subscribed)
            SelectionManager.I.OnSelectionChanged -= HandleSelectionChanged;
        _subscribed = false;
    }

    void TrySubscribe()
    {
        if (SelectionManager.I != null && !_subscribed)
        {
            SelectionManager.I.OnSelectionChanged += HandleSelectionChanged;
            _subscribed = true;
        }
    }

    // ===== Klik pada item Layer =====
    public void OnPointerClick(PointerEventData e)
    {
        bool additive = IsModifierDown();

        if (worldBox == null)
        {
            // Photoshop-like: layer boleh aktif tanpa object
            if (!additive) SelectionLayer.ClearGlobal();              // single → clear UI dulu
            uiLayer.SetSelected(additive ? !uiLayer.IsSelected : true); // toggle/add
            return;
        }

        // Ada object → delegasikan ke SelectionManager (sumber kebenaran)
        if (SelectionManager.I != null)
            SelectionManager.I.Select(worldBox, additive);
        else
            uiLayer.SetSelected(additive ? !uiLayer.IsSelected : true); // fallback (tanpa manager)
    }

    // ===== Sinkronisasi dari dunia (SelectionManager) =====
    void HandleSelectionChanged(IReadOnlyCollection<SelectionBox> cur)
    {
        if (!uiLayer) return;

        // Apakah user sedang additive (CTRL/SHIFT) di world?
        bool additiveHeld =
            Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
            Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

        if (worldBox == null)
        {
            // Layer ini belum punya object; jangan sampai "nyangkut".
            // Jika terjadi perubahan selection di dunia dan bukan additive,
            // padamkan layer ini.
            if (!additiveHeld)
                uiLayer.SetSelected(false);
            return;
        }

        // Normal: sinkron ke apakah worldBox ada di set selection Manager
        bool isSelected = cur != null && cur.Contains(worldBox);
        uiLayer.SetSelected(isSelected);
    }

    void SyncNow()
    {
        if (!uiLayer) return;

        // Jangan memaksa OFF layer kosong di sini; biarkan user bisa memilih layer
        if (worldBox == null) return;

        var cur = SelectionManager.I?.Current;
        bool sel = cur != null && cur.Contains(worldBox);
        uiLayer.SetSelected(sel);
    }

    // ===== Dipanggil Spawner setelah object dibuat =====
    public void AssignWorldBox(SelectionBox newBox)
    {
        worldBox = newBox;

        // Jika layer ini sedang aktif (UI-only) → pindahkan seleksi ke object baru (eksklusif)
        if (uiLayer && uiLayer.IsSelected && SelectionManager.I != null)
            SelectionManager.I.Select(worldBox, additive: false);
        else
            SyncNow();
    }

    private static bool IsModifierDown() =>
        Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
        Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
}
