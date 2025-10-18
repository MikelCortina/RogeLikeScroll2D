using UnityEngine;
using UnityEngine.UI;

public class HPBarController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image baseBar;       // Representa el maxHP total (gris)
    [SerializeField] private Image maxHPBar;      // Representa el currentMaxHP (azul)
    [SerializeField] private Image currentHPBar;  // Representa el currentHP (rojo)

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
        float currentMaxHP = Mathf.Clamp(statsManager.RuntimeStats.currentMaxHP, 0, maxHP);
        float currentHP = Mathf.Clamp(statsManager.RuntimeStats.currentHP, 0, currentMaxHP);

        // Capa 1: base → capacidad total
        baseBar.fillAmount = 1f;

        // Capa 2: currentMaxHP (proporción sobre el maxHP total)
        maxHPBar.fillAmount = currentMaxHP / maxHP;

        // Capa 3: currentHP (proporción sobre el maxHP total también)
        currentHPBar.fillAmount = currentHP / maxHP;
    }
}
