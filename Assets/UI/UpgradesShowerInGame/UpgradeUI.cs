using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;

public class UpgradeUI : MonoBehaviour
{
    public const int MAX_PENDING_LEVELS = 6;
    public static UpgradeUI Instance { get; private set; }

    [Header("Panel y Botones")]
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private Button[] upgradeButtons;
    [SerializeField] private TextMeshProUGUI[] upgradeNameTexts;
    [SerializeField] private TextMeshProUGUI[] upgradeDescriptionTexts;
    [Header("Textos para las 3 estadísticas por botón")]
    [SerializeField] private TextMeshProUGUI[] statSlot1Texts;
    [SerializeField] private TextMeshProUGUI[] statSlot2Texts;
    [SerializeField] private TextMeshProUGUI[] statSlot3Texts;
    [SerializeField] private Image[] extraImages;   // ← Añade esto en tu script    


    [Header("Colores según rareza")]
    [SerializeField] private Color rareColor = Color.blue;
    [SerializeField] private Color epicColor = new Color(0.5f, 0f, 1f);
    [SerializeField] private Color legendaryColor = Color.yellow;

    [Header("HUD de niveles pendientes")]
    [SerializeField] private Image pendingLevelsImage;           // Imagen que muestra el estado
    [SerializeField] private Sprite[] levelSprites;              // Sprites: índice 0 a 6
    [SerializeField] private TextMeshProUGUI pendingLevelsText;  // (opcional)

    private List<TextMeshProUGUI> childTexts = new List<TextMeshProUGUI>();
    private List<Color> originalChildTextColors = new List<Color>();
    private Color originalMainColor;
    [SerializeField] private GameObject otherTextToHide; // Nuevo: texto a ocultar
    [SerializeField] private TextMeshProUGUI warningText;  // (opcional)

    private List<List<Upgrade>> pendingUpgradeChoices = new List<List<Upgrade>>();
    private List<Upgrade> currentUpgrades;

    public int PendingCount => pendingUpgradeChoices.Count;
    public bool IsPendingFull => PendingCount >= MAX_PENDING_LEVELS;

    private Coroutine shakeCoroutine;
    private Vector3 originalImagePosition;

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        if (upgradePanel != null)
            upgradePanel.SetActive(false);

        if (pendingLevelsImage != null)
            originalImagePosition = pendingLevelsImage.rectTransform.localPosition;

        UpdatePendingLevelsVisuals();
    }

    private void Start()
    {
        if (StatsManager.Instance != null)
            StatsManager.Instance.OnLevelUp += QueueUpgrades;
    }

    private void Update()
    {
        if (PendingCount == 0)
        {
            otherTextToHide.SetActive(false);
        }
        else
        {
            otherTextToHide.SetActive(true);
        }
        if (Input.GetKeyDown(KeyCode.F))
        {
            if (pendingUpgradeChoices.Count > 0 && !upgradePanel.activeSelf)
            {
                currentUpgrades = pendingUpgradeChoices[0];
                pendingUpgradeChoices.RemoveAt(0);
                DisplayUpgradePanel(currentUpgrades);
                UpdatePendingLevelsVisuals();
            }
        }
    }

    public void QueueUpgrades(int level)
    {
        if (pendingUpgradeChoices.Count >= MAX_PENDING_LEVELS)
        {
            Debug.Log("Nivel no añadido: mejoras pendientes al máximo (6).");
            return;
        }

        var upgrades = UpgradeManager.Instance.GetRandomUpgrades(3);
        pendingUpgradeChoices.Add(upgrades);
        UpdatePendingLevelsVisuals();
    }
    private string GetStatValueText(StatTypeToShow stat)
    {
        var stats = StatsManager.Instance.RuntimeStats;

        switch (stat)
        {
            case StatTypeToShow.MaxHP: return $"Current_HP Max_{stats.maxHP}";
            case StatTypeToShow.FireRate: return $"Current_FireRate_{stats.fireRate}";
            case StatTypeToShow.GunDamage: return $"Current_GunDamage_{stats.gunDamage}";
            case StatTypeToShow.ExplosionDamage: return $"Current_ExplosionDamage_{stats.explosionDamage}";
            case StatTypeToShow.MeleeArmor: return $"Current_MeleeArmor_{stats.meleArmorPercentage}";
            case StatTypeToShow.RangedArmor: return $"Current_RangedArmor_{stats.rangedArmorPercentage}";
            case StatTypeToShow.CriticalChance: return $"Current_CriticalChance_{stats.criticalChance}";
            case StatTypeToShow.DodgeChance: return $"Current_DodgeChance_{stats.dodgeChance}";
            case StatTypeToShow.Knockback: return $"Current_Knockback_{stats.knockback}";
            case StatTypeToShow.ProjectileSpeed: return $"Current_ProjectileSpeed_{stats.projectileSpeed}";
            case StatTypeToShow.XP_Gain: return $"Current_XP Mult_{stats.xpGainMultiplier}";
            case StatTypeToShow.Luck: return $"Current_Luck_{stats.luck}";
            case StatTypeToShow.Radius: return $"Current_Radius_{stats.radius}";
            case StatTypeToShow.CurrencyGain: return $"Current_Currency_{stats.currency}";
            case StatTypeToShow.OrganValue: return $"Current_Organ_{stats.organValue}";
            case StatTypeToShow.Harvester: return $"Current_Harvester_{stats.harvester}";
            case StatTypeToShow.SpawnRate: return $"Current_SpawnRate_{stats.spawnRate}";
            default: return stat.ToString();
        }
    }


    public void DisplayUpgradePanel(List<Upgrade> upgrades)
    {
        // ACTIVAR UI PRIMERO → así TMP inicializa materiales
        if (!upgradePanel.activeInHierarchy)
            upgradePanel.SetActive(true);

        StartCoroutine(SetupUI(upgrades));
    }


    private IEnumerator SetupUI(List<Upgrade> upgrades)
    {
        yield return null; // 🔥 evita el NullReference en outlineWidth

        for (int i = 0; i < upgradeButtons.Length; i++)
        {
            Upgrade upgrade = upgrades[i];

            upgradeNameTexts[i].text = upgrade.upgradeName;
            upgradeDescriptionTexts[i].text = upgrade.description;

            statSlot1Texts[i].text = "";
            statSlot2Texts[i].text = "";
            statSlot3Texts[i].text = "";

            for (int s = 0; s < Mathf.Min(upgrade.statsToShow, upgrade.displayedStats.Length); s++)
            {
                string statText = GetStatValueText(upgrade.displayedStats[s]);
                switch (s)
                {
                    case 0: statSlot1Texts[i].text = statText; break;
                    case 1: statSlot2Texts[i].text = statText; break;
                    case 2: statSlot3Texts[i].text = statText; break;
                }
            }

            Color chosenColor = Color.white;
            switch (upgrade.quality)
            {
                case UpgradeQuality.Rare: chosenColor = rareColor; break;
                case UpgradeQuality.Epic: chosenColor = epicColor; break;
                case UpgradeQuality.Legendary: chosenColor = legendaryColor; break;
            }

            if (upgradeNameTexts[i] != null)
            {
                upgradeNameTexts[i].color = Color.white;
                upgradeNameTexts[i].outlineColor = Color.black;
                upgradeNameTexts[i].outlineWidth = 0.1f; // ✔ sin crashear
            }

            if (upgradeDescriptionTexts[i] != null)
            {
                upgradeDescriptionTexts[i].color = Color.white;
                upgradeDescriptionTexts[i].outlineColor = Color.black;
                upgradeDescriptionTexts[i].outlineWidth = 0.1f;
            }

            if (extraImages != null && extraImages.Length > i)
                extraImages[i].color = chosenColor;

            int index = i;
            upgradeButtons[i].onClick.RemoveAllListeners();
            upgradeButtons[i].onClick.AddListener(() => SelectUpgrade(index));
        }

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

        UpdatePendingLevelsVisuals();
    }

    private void UpdatePendingLevelsVisuals()
    {
        if (pendingLevelsText != null)
            pendingLevelsText.text = $"Mejoras pendientes: {PendingCount}";

        if (pendingLevelsImage != null && levelSprites != null && levelSprites.Length > 0)
        {
            int index = Mathf.Clamp(PendingCount, 0, levelSprites.Length - 1);
            pendingLevelsImage.sprite = levelSprites[index];
        }

        if (PendingCount >= MAX_PENDING_LEVELS)
        {
            if (shakeCoroutine == null)
                shakeCoroutine = StartCoroutine(BlinkImage());
        }
        else
        {
            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
                shakeCoroutine = null;

                // Restaurar color original
                pendingLevelsImage.color = originalMainColor;

                for (int i = 0; i < childTexts.Count; i++)
                    childTexts[i].color = originalChildTextColors[i];

                // Ocultar warning y mostrar el otro texto
                if (warningText != null)
                    warningText.gameObject.SetActive(false);
            }
        }
    }
    /// <summary>
    /// Hace que el icono vibre ligeramente (efecto "atención") mientras está activo
    /// </summary>
    private IEnumerator BlinkImage()
    {
        if (pendingLevelsImage == null) yield break;

        // Activar texto de advertencia
        if (warningText != null)
            warningText.gameObject.SetActive(true);

        // Guardar color original del icono
        originalMainColor = pendingLevelsImage.color;

        // Recoger todos los textos hijos
        childTexts.Clear();
        originalChildTextColors.Clear();

        foreach (Transform child in pendingLevelsImage.transform)
        {
            TextMeshProUGUI tmp = child.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                childTexts.Add(tmp);
                originalChildTextColors.Add(tmp.color);
            }
        }

        Color redColor = new Color(1f, 0f, 0f, 1f);
        float speed = 8f;
        float intensity = 1.4f;

        while (true)
        {
            float t = (Mathf.Sin(Time.time * speed) * intensity + 1f) * 0.5f;
            t = Mathf.Clamp01(t);

            Color blink = redColor;
            blink.a = Mathf.Lerp(0.2f, 1f, t);

            // Cambiar la imagen principal
            pendingLevelsImage.color = blink;

            // Cambiar TODOS los textos hijos
            foreach (var txt in childTexts)
            {
                if (txt != null)
                    txt.color = blink;
            }

            yield return null;
        }
    }

}
