using UnityEngine;
using UnityEngine.EventSystems;

public class DragHandleForwarder : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private ReorderableLayerItem parentReorder;

    void Awake()
    {
        parentReorder = GetComponentInParent<ReorderableLayerItem>();
    }

    public void OnBeginDrag(PointerEventData eventData) => parentReorder?.OnBeginDrag(eventData);
    public void OnDrag(PointerEventData eventData) => parentReorder?.OnDrag(eventData);
    public void OnEndDrag(PointerEventData eventData) => parentReorder?.OnEndDrag(eventData);
}
