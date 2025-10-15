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

    [Header("Colores según rareza")]
    [SerializeField] private Color rareColor = Color.blue;
    [SerializeField] private Color epicColor = new Color(0.5f, 0f, 1f); // morado
    [SerializeField] private Color legendaryColor = Color.yellow;

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

            // Cambiar color del fondo según rareza
            Image buttonImage = upgradeButtons[i].GetComponent<Image>();
            if (buttonImage != null)
            {
                switch (upgrade.quality)
                {
                    case UpgradeQuality.Rare:
                        buttonImage.color = rareColor;
                        break;
                    case UpgradeQuality.Epic:
                        buttonImage.color = epicColor;
                        break;
                    case UpgradeQuality.Legendary:
                        buttonImage.color = legendaryColor;
                        break;
                }
            }

            // Remover listeners antiguos y asignar el nuevo
            int index = i; // captura local para lambda
            upgradeButtons[i].onClick.RemoveAllListeners();
            upgradeButtons[i].onClick.AddListener(() => SelectUpgrade(index));
        }

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
