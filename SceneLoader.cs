using UnityEngine;
using UnityEngine.SceneManagement; // penting untuk load/unload scene
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    [Header("Nama Scene Tujuan")]
    public string sceneToLoad = "Gameplay"; // default ke Gameplay
    public string sceneToUnload = "MainMenu"; // default ke MainMenu

    void Start()
    {
        // Pastikan button punya komponen Button
        Button btn = GetComponent<Button>();
        if (btn != null)
        {
            btn.onClick.AddListener(ChangeScene);
        }
    }

    public void ChangeScene()
    {
        // Unload MainMenu
        if (SceneManager.GetSceneByName(sceneToUnload).isLoaded)
        {
            SceneManager.UnloadSceneAsync(sceneToUnload);
        }

        // Load Gameplay
        SceneManager.LoadScene(sceneToLoad, LoadSceneMode.Single);
    }
}
