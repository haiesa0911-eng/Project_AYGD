using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class SelectionManager : MonoBehaviour
{
    public static SelectionManager I { get; private set; }

    [Header("UI")]
    [Tooltip("Jika aktif, klik pada UI interaktif (Button/Toggle/Slider/Input/TMP/ScrollRect/Scrollbar) diabaikan (tidak clear).")]
    public bool ignoreWhenPointerOverUI = true;

    [Header("World Colliders (WAJIB untuk fitur clear)")]
    [Tooltip("Tarik collider Board_3x4 ke sini.")]
    public Collider2D boardCollider;
    [Tooltip("Tarik collider Background ke sini.")]
    public Collider2D backgroundCollider;

    [Header("Lainnya")]
    [Tooltip("Klik area kosong (tanpa collider apa pun) diasumsikan background → clear.")]
    public bool allowClearOnEmptyWorld = true;

    [Header("Debug")]
    public bool debugLogs = false;

    // ===== STATE =====
    private readonly HashSet<SelectionBox> _selected = new HashSet<SelectionBox>();
    public IReadOnlyCollection<SelectionBox> Current => _selected;
    public SelectionBox Active { get; private set; }
    public event Action<IReadOnlyCollection<SelectionBox>> OnSelectionChanged;

    private Camera _cam;
    private static readonly List<RaycastResult> _uiHits = new List<RaycastResult>();

    void Awake()
    {
        if (I != null && I != this) { Destroy(gameObject); return; }
        I = this;
        _cam = Camera.main;
    }

    void Update()
    {
        if (!Input.GetMouseButtonDown(0)) return;

        // 1) UI interaktif memblokir clear
        if (ignoreWhenPointerOverUI && IsPointerOverInteractiveUI())
        {
            if (debugLogs) Debug.Log("[SelMgr] Click on interactive UI → no clear");
            return;
        }

        // 2) Hit test dunia
        Vector3 world = _cam ? _cam.ScreenToWorldPoint(Input.mousePosition) : (Vector3)Input.mousePosition;
        world.z = 0f;

        var hits = Physics2D.OverlapPointAll((Vector2)world);
        bool emptyWorld = (hits == null || hits.Length == 0);

        // 2a) Jika klik mengenai SelectionBox → JANGAN clear
        if (!emptyWorld)
        {
            for (int i = 0; i < hits.Length; i++)
            {
                var h = hits[i];
                if (!h) continue;
                if (h.GetComponentInParent<SelectionBox>() != null)
                {
                    if (debugLogs) Debug.Log("[SelMgr] Hit SelectionBox → no clear");
                    return;
                }
            }
        }

        // 2b) CLEAR jika mengenai Board_3x4
        if (boardCollider && IsHitSameHierarchy(boardCollider, hits))
        {
            if (debugLogs) Debug.Log("[SelMgr] Hit Board_3x4 → CLEAR");
            Clear();
            return;
        }

        // 2c) CLEAR jika mengenai Background
        if (backgroundCollider && IsHitSameHierarchy(backgroundCollider, hits))
        {
            if (debugLogs) Debug.Log("[SelMgr] Hit Background → CLEAR");
            Clear();
            return;
        }

        // 2d) CLEAR jika klik kosong (opsional)
        if (emptyWorld && allowClearOnEmptyWorld)
        {
            if (debugLogs) Debug.Log("[SelMgr] Empty world → CLEAR");
            Clear();
            return;
        }

        // 2e) Selain kasus di atas → tidak clear
        if (debugLogs) Debug.Log("[SelMgr] Clicked non-board/background world → no clear");
    }

    // ===== Helper: cek apakah salah satu hit berada di objek target (atau turunannya) =====
    private bool IsHitSameHierarchy(Collider2D target, Collider2D[] hits)
    {
        if (target == null || hits == null || hits.Length == 0) return false;

        Transform t = target.transform;
        for (int i = 0; i < hits.Length; i++)
        {
            var h = hits[i];
            if (!h) continue;
            var ht = h.transform;
            if (ht == t || ht.IsChildOf(t)) return true; // dukung collider turunan
        }
        return false;
    }

    // ===== UI helper: hanya UI interaktif yang memblokir =====
    private bool IsPointerOverInteractiveUI()
    {
        if (EventSystem.current == null) return false;

        var ped = new PointerEventData(EventSystem.current) { position = Input.mousePosition };
        _uiHits.Clear();
        EventSystem.current.RaycastAll(ped, _uiHits);

        for (int i = 0; i < _uiHits.Count; i++)
        {
            var go = _uiHits[i].gameObject;
            if (!go) continue;

            if (go.GetComponent<Button>() != null) return true;
            if (go.GetComponent<Toggle>() != null) return true;
            if (go.GetComponent<Slider>() != null) return true;
            if (go.GetComponent<Dropdown>() != null) return true;
            if (go.GetComponent<ScrollRect>() != null) return true;
            if (go.GetComponent<InputField>() != null) return true;
            if (go.GetComponent<TMP_InputField>() != null) return true;
            if (go.GetComponent<Scrollbar>() != null) return true;
        }
        return false;
    }

    // ===== API =====
    public void Select(SelectionBox box, bool additive = false)
    {
        if (box == null) return;
        if (!additive) InternalClear(false);

        _selected.Add(box);
        box.SetSelected(true);
        Active = box;
        RaiseChanged();
    }

    public void Deselect(SelectionBox box)
    {
        if (box == null) return;
        if (_selected.Remove(box))
        {
            box.SetSelected(false);
            if (Active == box) Active = null;
            RaiseChanged();
        }
    }

    public void Clear() => InternalClear(true);

    private void InternalClear(bool notify)
    {
        if (_selected.Count == 0)
        {
            if (notify) RaiseChanged();
            Active = null;
            return;
        }

        foreach (var b in _selected)
            if (b) b.SetSelected(false);

        _selected.Clear();
        Active = null;

        if (notify) RaiseChanged();
    }

    private void RaiseChanged() => OnSelectionChanged?.Invoke(_selected);
}
