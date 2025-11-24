using UnityEngine;
using UnityEngine.UI;

public class HPBarController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image baseBar;       // Representa el maxHP total (gris)
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
       
        float currentHP = Mathf.Clamp(statsManager.RuntimeStats.currentHP, 0, maxHP);

        // Capa 1: base → capacidad total
        baseBar.fillAmount = 1f;


        // Capa 3: currentHP (proporción sobre el maxHP total también)
        currentHPBar.fillAmount = currentHP / maxHP;
    }
}
