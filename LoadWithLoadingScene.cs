using UnityEngine;

public class LoadWithLoadingScene : MonoBehaviour
{
    [SerializeField] private string targetScene = "Gameplay";

    public void Go()
    {
        if (string.IsNullOrEmpty(targetScene)) targetScene = "Gameplay";
        LoadingGate.NextScene = targetScene;
        // Fade → pindah ke LoadingScene → Fade in
        StartCoroutine(SceneTransition.FadeSwap("LoadingScene", 0.3f, 0.3f));
    }
}
