using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class ObjectSlide : MonoBehaviour
{
    [Header("Positions")]
    public bool useLocalPosition = false;   // true = localPosition, false = world position
    public Vector3 startPos;                // posisi tampil
    public Vector3 endPos;                  // posisi sembunyi

    [Header("Animation")]
    [Range(0.05f, 2f)] public float duration = 0.35f;
    public AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);
    public bool useUnscaledTime = true;

    [Header("Init")]
    public bool setStartOnEnable = true;    // snap ke start saat aktif
    public bool startActive = true;         // aktif saat enable

    Coroutine anim;

    void OnEnable()
    {
        if (setStartOnEnable)
        {
            gameObject.SetActive(startActive);
            SetPos(startPos);
        }
    }

    // === API untuk Button ===
    public void Open()
    {
        if (!gameObject.activeSelf) gameObject.SetActive(true);
        SetPos(endPos);                      // mulai dari posisi sembunyi
        PlayTo(startPos, null);              // geser ke tampil
    }

    public void Close()
    {
        SetPos(startPos);                    // mulai dari posisi tampil
        PlayTo(endPos, () => gameObject.SetActive(false));  // sembunyikan lalu nonaktif
    }

    // === Core ===
    void PlayTo(Vector3 target, System.Action onComplete)
    {
        if (anim != null) StopCoroutine(anim);
        anim = StartCoroutine(AnimateTo(target, onComplete));
    }

    IEnumerator AnimateTo(Vector3 target, System.Action onComplete)
    {
        Vector3 from = GetPos();
        float t = 0f, dur = Mathf.Max(0.0001f, duration);

        while (t < 1f)
        {
            t += (useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime) / dur;

            float baseT = Mathf.Clamp01(t);
            float k = (ease != null) ? Mathf.Clamp01(ease.Evaluate(baseT)) : baseT; // ✅ perbaikan

            SetPos(Vector3.Lerp(from, target, k));   // aman (tanpa overshoot)
            yield return null;
        }

        SetPos(target);
        onComplete?.Invoke();
        anim = null;
    }


    Vector3 GetPos() => useLocalPosition ? transform.localPosition : transform.position;

    void SetPos(Vector3 p)
    {
        if (useLocalPosition) transform.localPosition = p;
        else transform.position = p;
    }

#if UNITY_EDITOR
    [ContextMenu("Capture Current As Start")] void CaptureStart() { startPos = GetPos(); }
    [ContextMenu("Capture Current As End")] void CaptureEnd() { endPos = GetPos(); }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Vector3 a = useLocalPosition && transform.parent ? transform.parent.TransformPoint(startPos) : startPos;
        Vector3 b = useLocalPosition && transform.parent ? transform.parent.TransformPoint(endPos) : endPos;
        Gizmos.DrawSphere(a, 0.1f);
        Gizmos.DrawSphere(b, 0.1f);
        Gizmos.DrawLine(a, b);
    }
#endif
}
