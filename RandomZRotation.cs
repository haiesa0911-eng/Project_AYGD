using UnityEngine;

public class RandomZRotation : MonoBehaviour
{
    [Header("Pengaturan Interval")]
    [SerializeField] private float interval = 1f;

    [Header("Range Random Rotasi (Z Axis)")]
    [SerializeField] private float minZ = -20f;
    [SerializeField] private float maxZ = 20f;

    private void Start()
    {
        // Jalankan fungsi berulang setiap interval detik
        InvokeRepeating(nameof(SetRandomRotation), 0f, interval);
    }

    private void SetRandomRotation()
    {
        // Ambil nilai random sesuai range dari inspector
        float randomZ = Random.Range(minZ, maxZ);

        // Terapkan rotasi baru hanya pada sumbu Z
        transform.rotation = Quaternion.Euler(0f, 0f, randomZ);
    }
}
