using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class PenTrailControl : MonoBehaviour
{
    [SerializeField] private ParticleSystem ps;

    // Cadangan nilai awal (dibaca dari Inspector saat runtime)
    ParticleSystem.MinMaxCurve cachedRateTime, cachedRateDist;
    ParticleSystemRenderer rend;
    bool cachedRendererEnabled = true;
    float cachedStartSize;
    Color cachedStartColorA;
    float cachedLifetime;

    void Awake()
    {
        if (!ps) ps = GetComponentInChildren<ParticleSystem>(true);
        if (!ps) return;

        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.stopAction = ParticleSystemStopAction.None;
        main.playOnAwake = false;

        // Cache parameter krusial (supaya kalau Animator/Timeline mengubah ke 0, kita balikan)
        var em = ps.emission;
        cachedRateTime = em.rateOverTime;
        cachedRateDist = em.rateOverDistance;

        cachedStartSize = main.startSize.constant;                  // asumsi memakai konstanta
        cachedStartColorA = main.startColor.color;                   // asumsi konstanta juga
        cachedLifetime = main.startLifetime.constant;

        rend = ps.GetComponent<ParticleSystemRenderer>();
        if (rend) cachedRendererEnabled = rend.enabled;
    }

    // Panggil di awal gerak maju
    public void StartLeavingMarks()
    {
        if (!ps) return;

        // Pulihkan nilai visual penting (antisipasi keyframe yang mematikan semuanya)
        var main = ps.main;
        if (main.startSize.mode == ParticleSystemCurveMode.Constant) main.startSize = cachedStartSize;
        if (main.startColor.mode == ParticleSystemGradientMode.Color) main.startColor = cachedStartColorA;
        if (main.startLifetime.mode == ParticleSystemCurveMode.Constant) main.startLifetime = cachedLifetime;

        if (rend) rend.enabled = cachedRendererEnabled;

        var em = ps.emission;
        em.enabled = true;

        // Jika rateTime/distance kebetulan “dibunuh” (jadi 0), kembalikan cadangan
        if (IsZero(em.rateOverTime)) em.rateOverTime = cachedRateTime;
        if (IsZero(em.rateOverDistance)) em.rateOverDistance = cachedRateDist;

        // Play ulang sistem (dengan anak/sub-emitters)
        ps.Play(true);
    }

    // Panggil SESAAT SEBELUM teleport/reset posisi
    public void StopLeavingMarksButKeepOld()
    {
        if (!ps) return;
        var em = ps.emission;
        em.enabled = false;
        ps.Stop(true, ParticleSystemStopBehavior.StopEmitting);
    }

    static bool IsZero(ParticleSystem.MinMaxCurve c)
    {
        // aman untuk kasus constant; untuk curve, anggap tidak 0
        return c.mode == ParticleSystemCurveMode.Constant && Mathf.Approximately(c.constantMax, 0f);
    }

    // Util debug cepat — panggil sementara dari tombol/ContextMenu untuk memastikan terlihat
    [ContextMenu("DEBUG: Emit 10")]
    void DebugEmit()
    {
        if (!ps) return;
        var em = ps.emission; em.enabled = true;
        ps.Emit(10);  // jika ini juga tidak terlihat, masalah ada di material/sorting/camera
    }
}
