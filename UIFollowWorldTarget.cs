using UnityEngine;

public class UIFollowWorldTarget : MonoBehaviour
{
    public Transform target;              // NPC
    public Vector3 worldOffset = new Vector3(0.0f, 1.6f, 0f);
    public Camera worldCamera;            // CameraWorld (tempat NPC dirender)
    public Camera uiCamera;               // CameraUI (untuk Canvas Screen Space – Camera)
    public RectTransform canvasRect;      // RectTransform Canvas UI
    RectTransform self;

    void Awake() => self = (RectTransform)transform;

    void LateUpdate()
    {
        if (!target || !canvasRect || !uiCamera) return;

        Vector3 worldPos = target.position + worldOffset;
        Vector3 screenPos = (worldCamera ? worldCamera : Camera.main).WorldToScreenPoint(worldPos);

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvasRect, screenPos, uiCamera, out var local);
        self.anchoredPosition = local;
    }
}
