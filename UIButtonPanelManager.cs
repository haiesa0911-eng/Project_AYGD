using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
[RequireComponent(typeof(Button))]
public class UIButtonPanelManager : MonoBehaviour
{
    [Header("Target yang akan dibuka (hanya UI Panel)")]
    [Tooltip("Kompat lama. Jika panelsToOpen kosong, ini dipakai sebagai satu-satunya target open.")]
    public GameObject panelToOpen;

    [Tooltip("Daftar target UI Panel yang akan DIBUKA bersamaan.")]
    public GameObject[] panelsToOpen;

    [Header("Target UI Panel lain yang akan ditutup otomatis")]
    public GameObject[] panelsToClose;

    [Header("Urutan Eksekusi")]
    [Tooltip("True = buka dulu semuanya, baru tutup yang lain.")]
    public bool openFirstThenClose = true;

    [Header("Fallback untuk objek TANPA UIPanelSlide")]
    [Tooltip("OPEN: aktifkan objek tanpa UIPanelSlide.")]
    public bool fallbackActivateNonSlide = true;

    [Tooltip("CLOSE: nonaktifkan objek tanpa UIPanelSlide.")]
    public bool fallbackDeactivateNonSlide = true;

    private Button button;

    void Reset()
    {
        button = GetComponent<Button>();
    }

    void Awake()
    {
        button = GetComponent<Button>();
        if (button == null)
        {
            Debug.LogError($"[{nameof(UIButtonPanelManager)}] Tidak menemukan Button pada '{name}'.");
            enabled = false;
            return;
        }
    }

    void OnEnable()
    {
        button.onClick.AddListener(ManagePanels);
    }

    void OnDisable()
    {
        button.onClick.RemoveListener(ManagePanels);
    }

    public void ManagePanels()
    {
        // Gabungkan panelToOpen & panelsToOpen jadi satu set unik
        var toOpen = new HashSet<GameObject>();
        if (panelsToOpen != null)
        {
            foreach (var go in panelsToOpen)
                if (go != null) toOpen.Add(go);
        }
        if (panelToOpen != null) toOpen.Add(panelToOpen);

        if (openFirstThenClose)
        {
            OpenGroup(toOpen);
            CloseOthersSkipping(toOpen);
        }
        else
        {
            CloseOthersSkipping(toOpen);
            OpenGroup(toOpen);
        }
    }

    void OpenGroup(HashSet<GameObject> group)
    {
        foreach (var go in group)
        {
            if (!TryOpen(go))
            {
                if (fallbackActivateNonSlide)
                    go.SetActive(true);
            }
        }
    }

    void CloseOthersSkipping(HashSet<GameObject> toOpen)
    {
        if (panelsToClose == null) return;

        foreach (var p in panelsToClose)
        {
            if (p == null) continue;
            if (toOpen.Contains(p)) continue;

            if (!TryClose(p))
            {
                if (fallbackDeactivateNonSlide)
                    p.SetActive(false);
            }
        }
    }

    bool TryOpen(GameObject go)
    {
        var ui = go.GetComponent<UIPanelSlide>();
        if (ui != null) { ui.Open(); return true; }
        return false;
    }

    bool TryClose(GameObject go)
    {
        var ui = go.GetComponent<UIPanelSlide>();
        if (ui != null) { ui.Close(); return true; }
        return false;
    }
}
