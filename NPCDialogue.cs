using UnityEngine;
using System.Collections;

public class NPCDialogue : MonoBehaviour
{
    [Header("Data")]
    public Conversation conversation;

    [Header("UI")]
    public ChatBubbleUI bubblePrefab;
    public RectTransform uiCanvas;    // Canvas UI (RectTransform)
    public Camera worldCamera;
    public Camera uiCamera;

    [Header("Spawn")]
    public Vector3 bubbleOffset = new Vector3(0f, 1.6f, 0f);

    ChatBubbleUI activeBubble;
    int index;
    bool running;

    public void StartDialogue()
    {
        if (running || conversation == null || conversation.lines.Length == 0) return;

        running = true;
        index = 0;

        // Spawn bubble
        activeBubble = Instantiate(bubblePrefab, uiCanvas);
        var follow = activeBubble.gameObject.AddComponent<UIFollowWorldTarget>();
        follow.target = transform;
        follow.worldOffset = bubbleOffset;
        follow.worldCamera = worldCamera;
        follow.uiCamera = uiCamera;
        follow.canvasRect = uiCanvas;

        ShowCurrentLine();
        StartCoroutine(Loop());
    }

    void ShowCurrentLine()
    {
        activeBubble.ShowLine(conversation.lines[index], conversation.charsPerSecond);
    }

    IEnumerator Loop()
    {
        while (running)
        {
            // Klik kiri / Space → skip atau next
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
            {
                if (activeBubble.IsTyping)
                {
                    activeBubble.SkipTyping();
                }
                else
                {
                    index++;
                    if (index >= conversation.lines.Length) break;
                    ShowCurrentLine();
                }
            }
            yield return null;
        }

        EndDialogue();
    }

    public void EndDialogue()
    {
        running = false;
        if (activeBubble) Destroy(activeBubble.gameObject);
    }

    // Contoh trigger
    void OnMouseDown() { StartDialogue(); }
}
