using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

[RequireComponent(typeof(CanvasGroup))]
public class ScreenFader : MonoBehaviour
{
    public static ScreenFader I;                 // global instance
    [Range(0.05f, 2f)] public float defaultDuration = 0.3f;

    CanvasGroup cg;
    bool autoFadeInPending;
    float autoFadeInDuration = 0.3f;

    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);

        cg = GetComponent<CanvasGroup>();
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
        cg.interactable = false;
    }

    public IEnumerator FadeOut(float duration = -1f)
    {
        if (duration < 0f) duration = defaultDuration;
        cg.blocksRaycasts = true;
        float t = 0f, a0 = cg.alpha;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(a0, 1f, t / duration);
            yield return null;
        }
        cg.alpha = 1f;
    }

    public IEnumerator FadeIn(float duration = -1f)
    {
        if (duration < 0f) duration = defaultDuration;
        float t = 0f, a0 = cg.alpha;
        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            cg.alpha = Mathf.Lerp(a0, 0f, t / duration);
            yield return null;
        }
        cg.alpha = 0f;
        cg.blocksRaycasts = false;
    }

    // Minta otomatis FadeIn setelah scene berikutnya selesai dimuat/diaktifkan.
    public void ScheduleFadeInOnNextScene(float duration = -1f)
    {
        autoFadeInPending = true;
        if (duration > 0f) autoFadeInDuration = duration;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnSceneLoaded(Scene s, LoadSceneMode m)
    {
        if (!autoFadeInPending) return;
        autoFadeInPending = false;
        SceneManager.sceneLoaded -= OnSceneLoaded;
        StartCoroutine(FadeIn(autoFadeInDuration));
    }
}
