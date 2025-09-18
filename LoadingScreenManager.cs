using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class LoadingScreenManager : MonoBehaviour
{
    [Header("UI Referensi")]
    public Slider progressBar;
    public Text progressText;
    public RectTransform spinner;

    [Header("Konfigurasi")]
    [Range(0f, 3f)] public float minimumShowTime = 0.5f;
    public float spinnerSpeed = 180f;

    void Start()
    {
        StartCoroutine(RunLoading());
    }

    IEnumerator RunLoading()
    {
        string target = string.IsNullOrEmpty(LoadingGate.NextScene) ? "Gameplay" : LoadingGate.NextScene;

        // MULAI load async dan JANGAN aktifkan dulu
        AsyncOperation op = SceneManager.LoadSceneAsync(target);
        op.allowSceneActivation = false;

        float shown = 0f;

        while (!op.isDone)
        {
            // Update progress 0..0.9 (sebelum aktivasi)
            float p = Mathf.Clamp01(op.progress / 0.9f);
            if (progressBar) progressBar.value = p;
            if (progressText) progressText.text = (p * 100f).ToString("F0") + "%";
            if (spinner) spinner.Rotate(0f, 0f, -spinnerSpeed * Time.unscaledDeltaTime);

            shown += Time.unscaledDeltaTime;

            // Ketika loading selesai (siap aktivasi)
            if (op.progress >= 0.9f)
            {
                // Pastikan layar loading terlihat minimal beberapa waktu (anti-kedip)
                if (shown < minimumShowTime)
                    yield return new WaitForSecondsRealtime(minimumShowTime - shown);

                // Transisi: fade-out global dulu, lalu jadwalkan fade-in setelah scene aktif
                if (ScreenFader.I != null)
                {
                    yield return ScreenFader.I.FadeOut(0.25f);
                    ScreenFader.I.ScheduleFadeInOnNextScene(0.25f);
                }

                // AKTIFKAN scene target → LoadingScene ter-unload otomatis
                op.allowSceneActivation = true;

                // Setelah mengizinkan aktivasi, keluar dari coroutine
                yield break;
            }

            yield return null;
        }
    }
}
