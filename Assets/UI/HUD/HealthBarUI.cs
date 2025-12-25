using UnityEngine;
using UnityEngine.UI;
using TMPro; // Solo si usas TextMesh Pro

public class HPBarController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image baseBar;       // Representa el maxHP total (gris)
    [SerializeField] private Image currentHPBar;  // Representa el currentHP (rojo)
    [SerializeField] private TextMeshProUGUI  hpText;         // Texto clásico de Unity
    // [SerializeField] private TextMeshProUGUI hpText; // O TextMesh Pro si prefieres

    private StatsManager statsManager;

    void Start()
    {
        statsManager = StatsManager.Instance;

        if (statsManager == null)
        {
            Debug.LogError("⚠️ StatsManager.Instance no encontrado en HPBarController");
            enabled = false;
            return;
        }
    }

    void Update()
    {
        UpdateBar();
    }

    private void UpdateBar()
    {
        if (statsManager == null || statsManager.RuntimeStats == null) return;

        float maxHP = statsManager.RuntimeStats.maxHP;
        float currentHP = Mathf.Clamp(statsManager.RuntimeStats.currentHP, 0, maxHP);

        // Base: capacidad total
        baseBar.fillAmount = 1f;

        // Barra de vida actual
        currentHPBar.fillAmount = currentHP / maxHP;

        // Actualizar texto
        if (hpText != null)
        {
            hpText.text = $"{currentHP:F0} / {maxHP:F0}"; // F0 redondea a entero
        }
    }
}
