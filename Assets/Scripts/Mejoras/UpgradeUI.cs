using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UpgradeUI : MonoBehaviour
{
    [Header("Panel y Botones")]
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private Button[] upgradeButtons; // Deben ser 3 botones
    [SerializeField] private TextMeshProUGUI[] upgradeNameTexts; // Texto para el nombre de cada mejora
    [SerializeField] private TextMeshProUGUI[] upgradeDescriptionTexts; // Texto para la descripción

    private List<Upgrade> currentUpgrades;

    private void Awake()
    {
        // Panel desactivado al inicio
        if (upgradePanel != null)
            upgradePanel.SetActive(false);
    }

    private void Start()
    {
        if (StatsManager.Instance != null)
            StatsManager.Instance.OnLevelUp += ShowUpgrades;
    }

    /// <summary>
    /// Muestra el panel y actualiza los botones con las mejoras seleccionadas
    /// </summary>
    public void ShowUpgrades(int level)
    {
        //Debug.Log("ShowUpgrades llamado desde OnLevelUp con nivel: " + level);
        if (upgradePanel == null || upgradeButtons.Length != 3)
        {
            Debug.LogError("UpgradeUI no está configurada correctamente");
            return;
        }

        // Obtener 3 upgrades aleatorias del UpgradeManager
        currentUpgrades = UpgradeManager.Instance.GetRandomUpgrades(3);

        for (int i = 0; i < upgradeButtons.Length; i++)
        {
            Upgrade upgrade = currentUpgrades[i];

            // Actualizar textos
            if (upgradeNameTexts.Length > i) upgradeNameTexts[i].text = upgrade.upgradeName;
            if (upgradeDescriptionTexts.Length > i) upgradeDescriptionTexts[i].text = upgrade.description;

            // Remover listeners antiguos y asignar el nuevo
            int index = i; // captura local para lambda
            upgradeButtons[i].onClick.RemoveAllListeners();
            upgradeButtons[i].onClick.AddListener(() => SelectUpgrade(index));
        }

        Debug.Log("ShowUpgrades llamado");
        if (upgradePanel != null)
            upgradePanel.SetActive(true);

        // Pausar juego si quieres (opcional)
        Time.timeScale = 0f;
    }

    /// <summary>
    /// Aplica la mejora seleccionada
    /// </summary>
    private void SelectUpgrade(int index)
    {
        if (index < 0 || index >= currentUpgrades.Count) return;

        Upgrade selectedUpgrade = currentUpgrades[index];
        UpgradeManager.Instance.ApplyUpgrade(selectedUpgrade);

        // Cerrar panel
        upgradePanel.SetActive(false);

        // Reanudar juego si estaba pausado
        Time.timeScale = 1f;
    }
}
    