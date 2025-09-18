using UnityEngine;
using UnityEngine.EventSystems;

public class Spawner : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Spawn Target")]
    [SerializeField] private GameObject piecePrefab;
    [SerializeField] private Transform worldParent;
    [SerializeField] private Camera worldCamera;

    [Header("Single Instance")]
    [Tooltip("Batasi hanya satu object aktif yang dikelola spawner ini.")]
    [SerializeField] private bool limitToSingleInstance = true;

    [Header("Layer Binding")]
    [Tooltip("Layer UI pasangan spawner ini (drag Layer 1, Layer 2, dll. di Content_Layer).")]
    [SerializeField] private LayerSlot linkedLayer; // dipakai untuk reorder

    [Header("Preview")]
    [SerializeField] private bool makePreviewTransparent = true;
    [SerializeField, Range(0.1f, 1f)] private float previewAlpha = 0.6f;

    [Header("Selection")]
    [Tooltip("Nyalakan selection (garis biru) segera setelah prefab di-spawn dari tombol.")]
    [SerializeField] private bool autoSelectOnSpawn = true;
    [Tooltip("Biarkan selection tetap ON setelah berhasil ditempatkan.")]
    [SerializeField] private bool keepSelectedAfterPlace = true;
    [Tooltip("Daftarkan ke SelectionManager (jika ada) agar menjadi selection aktif.")]
    [SerializeField] private bool registerToSelectionManager = true;
    [Tooltip("Opsional: assign SelectionManager secara eksplisit. Jika kosong, akan dicari otomatis.")]
    [SerializeField] private SelectionManager selectionManager;

    // --- runtime (sesi drag) ---
    private GameObject spawnedGO;
    private SpriteRenderer[] spawnedRenderers;
    private PieceSnapper spawnedSnapper;
    private SelectionBox spawnedSelBox;

    // --- link ke UI Selection Layer (dicari otomatis dari linkedLayer) ---
    private SelectionLayerLinkToManager linkToManager; // <— NEW

    // --- single-instance state ---
    private GameObject liveInstance;
    private bool IsLiveAlive => liveInstance != null;

    // --- counter offset untuk Sortable ---
    private int runningOffset = 0;

    void Awake()
    {
        if (!worldCamera) worldCamera = Camera.main;
        if (!selectionManager) selectionManager = FindManagerSafe();

        // Cari komponen link pada GO yang sama dengan linkedLayer atau parent-nya
        if (linkedLayer)
        {
            var t = linkedLayer.transform;
            linkToManager = t.GetComponent<SelectionLayerLinkToManager>()
                         ?? t.GetComponentInParent<SelectionLayerLinkToManager>();
        }
    }

    // ========================= IBeginDragHandler =========================
    public void OnBeginDrag(PointerEventData e)
    {
        if (!piecePrefab || !worldCamera) return;
        if (limitToSingleInstance && IsLiveAlive) return;

        Vector3 wp = ScreenToWorld(e.position);

        // Spawn langsung
        spawnedGO = Instantiate(piecePrefab, wp, Quaternion.identity, worldParent);
        CacheComponents(spawnedGO);

        // === langsung dianggap placed ===
        if (linkedLayer)
        {
            var sortable = spawnedGO.GetComponent<Sortable>() ?? spawnedGO.AddComponent<Sortable>();
            sortable.localOffset = runningOffset++;
            linkedLayer.Register(sortable);
            linkedLayer.ReapplyOrders();
        }

        // Hubungkan ke SelectionLayerLinkToManager
        if (spawnedSelBox)
        {
            var linkToMgr = linkedLayer
                ? linkedLayer.GetComponent<SelectionLayerLinkToManager>()
                  ?? linkedLayer.GetComponentInParent<SelectionLayerLinkToManager>()
                : null;

            if (linkToMgr) linkToMgr.AssignWorldBox(spawnedSelBox);
        }

        // Tetapkan sebagai live instance
        if (limitToSingleInstance) liveInstance = spawnedGO;

        // Collider sudah boleh aktif
        SetCollidersEnabled(spawnedGO, true);

        // Masih bisa ikut drag
        if (spawnedSnapper != null) spawnedSnapper.BeginExternalDrag(wp);

        // Auto-select
        if (spawnedSelBox != null && autoSelectOnSpawn)
        {
            spawnedSelBox.SetSelected(true);
            TryRegisterSelection(spawnedSelBox);
        }

        // Transparansi preview tetap bisa
        if (makePreviewTransparent)
            SetRenderersAlpha(spawnedRenderers, previewAlpha);
    }

    // ========================= IDragHandler =========================
    public void OnDrag(PointerEventData e)
    {
        if (!spawnedGO || spawnedSnapper == null) return;

        Vector3 wp = ScreenToWorld(e.position);
        spawnedSnapper.UpdateExternalDrag(wp);
        spawnedGO.transform.position = wp;
    }

    // ========================= IEndDragHandler =========================
    public void OnEndDrag(PointerEventData e)
    {
        if (!spawnedGO || spawnedSnapper == null) return;

        // Pulihkan visual preview
        if (makePreviewTransparent && spawnedRenderers != null)
            SetRenderersAlpha(spawnedRenderers, 1f);

        // Aktifkan kembali collider sebelum commit
        SetCollidersEnabled(spawnedGO, true);

        // Commit ke PieceSnapper
        bool placed = spawnedSnapper.EndExternalDrag();

        if (!placed)
        {
            Destroy(spawnedGO);
        }
        else
        {
            if (limitToSingleInstance) liveInstance = spawnedGO;

            if (spawnedSelBox != null && !keepSelectedAfterPlace)
                spawnedSelBox.SetSelected(false);

            // === Integrasi ke sistem Layer (reorder) ===
            if (linkedLayer)
            {
                var sortable = spawnedGO.GetComponent<Sortable>() ?? spawnedGO.AddComponent<Sortable>();
                sortable.localOffset = runningOffset++;
                linkedLayer.Register(sortable);
                linkedLayer.ReapplyOrders();
            }

            // === Hubungkan ke SelectionLayerLinkToManager sebagai World Box ===
            // Memakai API yang sudah ada: AssignWorldBox(SelectionBox)
            if (linkToManager && spawnedSelBox)
            {
                linkToManager.AssignWorldBox(spawnedSelBox);
            }
        }

        // Bersihkan refs sesi drag
        spawnedGO = null;
        spawnedSnapper = null;
        spawnedSelBox = null;
        spawnedRenderers = null;
    }

    // ========================= Helpers =========================
    private void CacheComponents(GameObject go)
    {
        spawnedSnapper = go ? go.GetComponent<PieceSnapper>() : null;
        // fallback ke child bila SelectionBox tidak di root prefab
        spawnedSelBox = go ? (go.GetComponent<SelectionBox>() ?? go.GetComponentInChildren<SelectionBox>(true)) : null;
        spawnedRenderers = go ? go.GetComponentsInChildren<SpriteRenderer>(true) : null;
    }

    private Vector3 ScreenToWorld(Vector2 screenPos)
    {
        Vector3 p = worldCamera.ScreenToWorldPoint(screenPos);
        p.z = 0f;
        return p;
    }

    private void SetCollidersEnabled(GameObject go, bool enabled)
    {
        if (!go) return;
        var cols = go.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < cols.Length; i++)
            cols[i].enabled = enabled;
    }

    // dirapikan: gunakan array yang sudah di-cache
    private void SetRenderersAlpha(SpriteRenderer[] renderers, float a)
    {
        if (renderers == null) return;
        foreach (var r in renderers)
        {
            if (!r) continue;
            var c = r.color; c.a = a; r.color = c;
        }
    }

    private void TryRegisterSelection(SelectionBox box)
    {
        if (!registerToSelectionManager || box == null) return;

        var mgr = SelectionManager.I ? SelectionManager.I
                 : (selectionManager ? selectionManager : FindManagerSafe());

        if (mgr != null) mgr.Select(box, additive: false);
    }

    private SelectionManager FindManagerSafe()
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<SelectionManager>();
#else
        return Object.FindObjectOfType<SelectionManager>();
#endif
    }

    // ========================= Public Utility =========================
    public void ClearLiveIfNull()
    {
        if (!IsLiveAlive) liveInstance = null;
    }
}
