using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ReorderableLayerItem : MonoBehaviour,
    IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [Header("Selection (optional)")]
    [SerializeField] private SelectionLayerLinkToManager selectionLink; // drag dari prefab
    [SerializeField] private bool selectOnDragStart = true;

    [Header("References")]
    public RectTransform itemRoot;
    public RectTransform contentLayer;
    public RectTransform dragLayer;
    public Canvas canvas;

    private RectTransform placeholder;
    private CanvasGroup cg;
    private LayoutElement le;
    private Vector2 dragOffset;

    void Awake()
    {
        if (itemRoot == null) itemRoot = (RectTransform)transform;
        cg = itemRoot.GetComponent<CanvasGroup>() ?? itemRoot.gameObject.AddComponent<CanvasGroup>();
        le = itemRoot.GetComponent<LayoutElement>() ?? itemRoot.gameObject.AddComponent<LayoutElement>();
    }

    public void OnBeginDrag(PointerEventData e)
    {
        if (selectOnDragStart && selectionLink != null)
        {
            // Jika punya worldBox & SelectionManager → jalur “benar” via manager
            if (selectionLink.worldBox != null && SelectionManager.I != null)
            {
                // additive mengikuti tombol modifier seperti klik biasa
                bool additive =
                    Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl) ||
                    Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

                SelectionManager.I.Select(selectionLink.worldBox, additive: additive);
            }
            else
            {
                // Fallback UI-only: nyalakan visual SelectionLayer
                if (selectionLink.uiLayer != null)
                    selectionLink.uiLayer.SetSelected(true);  // men-set alpha active pada targetBox UI, sesuai API SelectionLayer. :contentReference[oaicite:0]{index=0}
            }
        }
            // 1. Buat placeholder
            placeholder = new GameObject("Placeholder", typeof(RectTransform), typeof(LayoutElement))
            .GetComponent<RectTransform>();

        placeholder.SetParent(contentLayer, false);
        placeholder.SetSiblingIndex(itemRoot.GetSiblingIndex());

        var ple = placeholder.GetComponent<LayoutElement>();
        ple.preferredWidth = itemRoot.rect.width;
        ple.preferredHeight = itemRoot.rect.height;

        // ✅ Pastikan placeholder tidak ikut dihitung layout
        ple.ignoreLayout = true;

        // 2. Angkat item keluar dari layout
        le.ignoreLayout = true;
        cg.blocksRaycasts = false;
        itemRoot.SetParent(dragLayer, true);

        // 3. Hitung offset drag
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            itemRoot, e.position, e.pressEventCamera, out var local);
        dragOffset = -local;

    }

    public void OnDrag(PointerEventData e)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            dragLayer, e.position, e.pressEventCamera, out var pos);
        itemRoot.anchoredPosition = pos + dragOffset;

        int newIndex = GetIndexForPointer(e);
        if (newIndex != placeholder.GetSiblingIndex())
            placeholder.SetSiblingIndex(newIndex);
    }

    public void OnEndDrag(PointerEventData e)
    {
        int finalIndex = placeholder.GetSiblingIndex();
        itemRoot.SetParent(contentLayer, false);
        itemRoot.SetSiblingIndex(finalIndex);

        le.ignoreLayout = false;
        cg.blocksRaycasts = true;
        Destroy(placeholder.gameObject);

        LayerOrderManager.I.NotifyOrderChanged();
    }

    private int GetIndexForPointer(PointerEventData e)
    {
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            contentLayer, e.position, e.pressEventCamera, out var localY);

        int index = contentLayer.childCount;
        for (int i = 0; i < contentLayer.childCount; i++)
        {
            var child = (RectTransform)contentLayer.GetChild(i);
            if (child == placeholder) continue;

            float childMid = child.anchoredPosition.y - (child.rect.height * 0.5f);
            if (localY.y > childMid) { index = i; break; }
        }
        return index;
    }
}
