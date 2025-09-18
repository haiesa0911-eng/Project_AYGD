using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneTransition
{
    public static IEnumerator FadeSwap(string sceneName, float fadeOut = 0.3f, float fadeIn = 0.3f)
    {
        if (ScreenFader.I != null) yield return ScreenFader.I.FadeOut(fadeOut);

        AsyncOperation op = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Single);
        while (!op.isDone) yield return null;

        if (ScreenFader.I != null) yield return ScreenFader.I.FadeIn(fadeIn);
    }
}
