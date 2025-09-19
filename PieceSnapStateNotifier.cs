using System.Collections;
using UnityEngine;

[RequireComponent(typeof(PieceSnapper))]
public sealed class PieceSnapStateNotifier : MonoBehaviour
{
    private PieceSnapper snapper;
    private bool prevSnapped;
    private ScoreController sc;

    void Awake()
    {
        snapper = GetComponent<PieceSnapper>();
        prevSnapped = (snapper != null && snapper.IsSnapped);
#if UNITY_2023_1_OR_NEWER
        sc = Object.FindFirstObjectByType<ScoreController>();
#else
        sc = FindObjectOfType<ScoreController>();
#endif
    }

    void Update()
    {
        if (snapper == null) return;

        bool now = snapper.IsSnapped;
        if (now != prevSnapped)
        {
            // Evaluasi segera (agar UI terasa responsif)...
            if (sc != null) sc.EvaluateNow();

            // ...lalu evaluasi ulang di akhir frame, setelah destroy/unsnap benar2 tuntas
            StartCoroutine(DeferredEvaluate());

            prevSnapped = now;
        }
    }

    IEnumerator DeferredEvaluate()
    {
        yield return new WaitForEndOfFrame();    // tunggu semua perubahan scene selesai
        if (sc != null) sc.EvaluateNow();
    }

    // Jika object dinonaktifkan/dihancurkan (mis. karena dibuang), paksa evaluasi
    void OnDisable()
    {
        if (sc != null) sc.EvaluateNow();
    }
}
