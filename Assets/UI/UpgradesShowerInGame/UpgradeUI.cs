// UpgradeUI.cs
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeUI : MonoBehaviour
{
    [Header("Panel y Botones")]
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private Button[] upgradeButtons;
    [SerializeField] private TextMeshProUGUI[] upgradeNameTexts;
    [SerializeField] private TextMeshProUGUI[] upgradeDescriptionTexts;

    [Header("Colores según rareza")]
    [SerializeField] private Color rareColor = Color.blue;
    [SerializeField] private Color epicColor = new Color(0.5f, 0f, 1f);
    [SerializeField] private Color legendaryColor = Color.yellow;

    [Header("HUD")]
    [SerializeField] private TextMeshProUGUI pendingLevelsText;

    private List<List<Upgrade>> pendingUpgradeChoices = new List<List<Upgrade>>();
    private List<Upgrade> currentUpgrades;

    private void Awake()
    {
        if (upgradePanel != null)
            upgradePanel.SetActive(false);

        UpdatePendingLevelsText();
    }

    private void Start()
    {
        if (StatsManager.Instance != null)
            StatsManager.Instance.OnLevelUp += QueueUpgrades;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (pendingUpgradeChoices.Count > 0 && !upgradePanel.activeSelf)
            {
                currentUpgrades = pendingUpgradeChoices[0];
                pendingUpgradeChoices.RemoveAt(0);
                DisplayUpgradePanel(currentUpgrades);
                UpdatePendingLevelsText();
            }
        }
    }

    public void QueueUpgrades(int level)
    {
        var upgrades = UpgradeManager.Instance.GetRandomUpgrades(3);
        pendingUpgradeChoices.Add(upgrades);
        UpdatePendingLevelsText();
    }

    private void DisplayUpgradePanel(List<Upgrade> upgrades)
    {
        for (int i = 0; i < upgradeButtons.Length; i++)
        {
            Upgrade upgrade = upgrades[i];
            upgradeNameTexts[i].text = upgrade.upgradeName;
            upgradeDescriptionTexts[i].text = upgrade.description;

            Image buttonImage = upgradeButtons[i].GetComponent<Image>();
            switch (upgrade.quality)
            {
                case UpgradeQuality.Rare: buttonImage.color = rareColor; break;
                case UpgradeQuality.Epic: buttonImage.color = epicColor; break;
                case UpgradeQuality.Legendary: buttonImage.color = legendaryColor; break;
            }

            int index = i;
            upgradeButtons[i].onClick.RemoveAllListeners();
            upgradeButtons[i].onClick.AddListener(() => SelectUpgrade(index));
        }

        upgradePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    private void SelectUpgrade(int index)
    {
        if (index < 0 || index >= currentUpgrades.Count) return;

        Upgrade selectedUpgrade = currentUpgrades[index];
        UpgradeManager.Instance.ApplyUpgrade(selectedUpgrade);

        upgradePanel.SetActive(false);
        Time.timeScale = 1f;

        if (pendingUpgradeChoices.Count > 0)
        {
            currentUpgrades = pendingUpgradeChoices[0];
            pendingUpgradeChoices.RemoveAt(0);
            DisplayUpgradePanel(currentUpgrades);
        }

        UpdatePendingLevelsText();
    }

    private void UpdatePendingLevelsText()
    {
        if (pendingLevelsText != null)
        {
            pendingLevelsText.text = $"Mejoras pendientes: {pendingUpgradeChoices.Count}";
        }
    }
}

// UpgradeManager.cs (sin cambios necesarios para esta funcionalidad)
// Solo asegúrate de que GetRandomUpgrades(int count) esté implementado correctamente como ya lo tienes.