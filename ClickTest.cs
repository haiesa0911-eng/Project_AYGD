using UnityEngine;
public class ClickTest : MonoBehaviour
{
    void OnMouseDown() { Debug.Log($"{name} diklik"); }
    void OnMouseDrag() { Debug.Log($"{name} didrag"); }
    void OnMouseUp() { Debug.Log($"{name} dilepas"); }
}
