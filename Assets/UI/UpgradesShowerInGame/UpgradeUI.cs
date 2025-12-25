using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeUI : MonoBehaviour
{
    public const int MAX_PENDING_LEVELS = 6;
    public static UpgradeUI Instance { get; private set; }

    [Header("Panel principal")]
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private Transform buttonsParent; // Donde se instanciarán los 3 botones

    [Header("Prefab del botón")]
    [SerializeField] private GameObject upgradeButtonPrefab;

    [Header("Colores según rareza")]
    [SerializeField] private Color rareColor = Color.blue;
    [SerializeField] private Color epicColor = new Color(0.5f, 0f, 1f);
    [SerializeField] private Color legendaryColor = Color.yellow;

    [Header("HUD de niveles pendientes")]
    [SerializeField] private Image pendingLevelsImage;
    [SerializeField] private Sprite[] levelSprites;
    [SerializeField] private TextMeshProUGUI pendingLevelsText;
    [SerializeField] private GameObject otherTextToHide;
    [SerializeField] private TextMeshProUGUI warningText;

    private List<List<Upgrade>> pendingUpgradeChoices = new List<List<Upgrade>>();
    private List<Upgrade> currentUpgrades;
    private List<GameObject> instantiatedButtons = new List<GameObject>();

    public int PendingCount => pendingUpgradeChoices.Count;
    public bool IsPendingFull => PendingCount >= MAX_PENDING_LEVELS;

    private Coroutine shakeCoroutine;
    private Vector3 originalImagePosition;
    private Color originalMainColor;
    private List<TextMeshProUGUI> childTexts = new List<TextMeshProUGUI>();
    private List<Color> originalChildTextColors = new List<Color>();

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;

        upgradePanel.SetActive(false);
        if (pendingLevelsImage != null)
            originalImagePosition = pendingLevelsImage.rectTransform.localPosition;
    }

    private void Start()
    {
        if (StatsManager.Instance != null)
            StatsManager.Instance.OnLevelUp += QueueUpgrades;
    }

    private void Update()
    {
        otherTextToHide.SetActive(PendingCount > 0);

        if (Input.GetKeyDown(KeyCode.F) && PendingCount > 0 && !upgradePanel.activeSelf)
        {
            currentUpgrades = pendingUpgradeChoices[0];
            pendingUpgradeChoices.RemoveAt(0);
            DisplayUpgradePanel(currentUpgrades);
            UpdatePendingLevelsVisuals();
        }
    }

    public void QueueUpgrades(int level)
    {
        if (pendingUpgradeChoices.Count >= MAX_PENDING_LEVELS)
        {
            Debug.Log("Cola de niveles llena");
            return;
        }

        var upgrades = UpgradeManager.Instance.GetRandomUpgrades(3);
        pendingUpgradeChoices.Add(upgrades);
        UpdatePendingLevelsVisuals();
    }

    public void DisplayUpgradePanel(List<Upgrade> upgrades)
    {
        upgradePanel.SetActive(true);
        StartCoroutine(SetupUI_Coroutine(upgrades));
    }

    private IEnumerator SetupUI_Coroutine(List<Upgrade> upgrades)
    {
        yield return null; // Esperar un frame para evitar problemas con TMP

        // Limpiar botones anteriores
        foreach (var btn in instantiatedButtons)
            if (btn != null) Destroy(btn);
        instantiatedButtons.Clear();

        for (int i = 0; i < upgrades.Count; i++)
        {
            Upgrade upgrade = upgrades[i];
            GameObject buttonObj = Instantiate(upgradeButtonPrefab, buttonsParent);
            instantiatedButtons.Add(buttonObj);

            // Referencias rápidas (puedes optimizar con un componente propio si quieres)
            TextMeshProUGUI nameText = buttonObj.transform.Find("UpgradeName")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI descText = buttonObj.transform.Find("UpgradeDescription")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI stat1 = buttonObj.transform.Find("Stat1")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI stat2 = buttonObj.transform.Find("Stat2")?.GetComponent<TextMeshProUGUI>();
            TextMeshProUGUI stat3 = buttonObj.transform.Find("Stat3")?.GetComponent<TextMeshProUGUI>();
            Image rarityImage = buttonObj.transform.Find("ExtraImage")?.GetComponent<Image>();
            Button button = buttonObj.GetComponent<Button>();

            // Textos
            if (nameText) nameText.text = upgrade.upgradeName;
            if (descText) descText.text = upgrade.description;

            // Stats
            if (stat1) stat1.text = "";
            if (stat2) stat2.text = "";
            if (stat3) stat3.text = "";

            for (int s = 0; s < Mathf.Min(3, upgrade.displayedStats.Length); s++)
            {
                string text = GetStatValueText(upgrade.displayedStats[s]);
                switch (s)
                {
                    case 0: if (stat1) stat1.text = text; break;
                    case 1: if (stat2) stat2.text = text; break;
                    case 2: if (stat3) stat3.text = text; break;
                }
            }

            // Color de rareza
            Color rarityColor = Color.white;
            switch (upgrade.quality)
            {
                case UpgradeQuality.Rare: rarityColor = rareColor; break;
                case UpgradeQuality.Epic: rarityColor = epicColor; break;
                case UpgradeQuality.Legendary: rarityColor = legendaryColor; break;
            }

            if (rarityImage) rarityImage.color = rarityColor;

            // Outline bonito (opcional)
            if (nameText) { nameText.color = Color.white; nameText.outlineColor = Color.black; nameText.outlineWidth = 0.1f; }
            if (descText) { descText.color = Color.white; descText.outlineColor = Color.black; descText.outlineWidth = 0.1f; }

            // Click
            int index = i;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => SelectUpgrade(index));
        }

        Time.timeScale = 0f;
    }

    private void SelectUpgrade(int index)
    {
        if (index < 0 || index >= currentUpgrades.Count) return;

        Upgrade selected = currentUpgrades[index];
        UpgradeManager.Instance.ApplyUpgrade(selected);

        upgradePanel.SetActive(false);
        Time.timeScale = 1f;

        // Limpiar instancias
        foreach (var go in instantiatedButtons)
            if (go != null) Destroy(go);
        instantiatedButtons.Clear();

        // Siguiente cola
        if (pendingUpgradeChoices.Count > 0)
        {
            currentUpgrades = pendingUpgradeChoices[0];
            pendingUpgradeChoices.RemoveAt(0);
            DisplayUpgradePanel(currentUpgrades);
        }

        UpdatePendingLevelsVisuals();
    }

    private string GetStatValueText(StatTypeToShow stat)
    {
        var stats = StatsManager.Instance.RuntimeStats;
        return stat switch
        {
            StatTypeToShow.MaxHP => $"Max HP: {stats.maxHP}",
            StatTypeToShow.FireRate => $"Fire Rate: {stats.fireRate}",
            StatTypeToShow.GunDamage => $"Gun Damage: {stats.gunDamage}",
            StatTypeToShow.CriticalChance => $"Crit Chance: {stats.criticalChance}%",
            // ... el resto igual, solo cambia el formato que quieras
            _ => stat.ToString()
        };
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
        else if (shakeCoroutine != null)
        {
            StopCoroutine(shakeCoroutine);
            shakeCoroutine = null;
            pendingLevelsImage.color = originalMainColor;
            for (int i = 0; i < childTexts.Count; i++)
                childTexts[i].color = originalChildTextColors[i];
            if (warningText) warningText.gameObject.SetActive(false);
        }
    }

    private IEnumerator BlinkImage()
    {
        if (warningText) warningText.gameObject.SetActive(true);
        originalMainColor = pendingLevelsImage.color;

        childTexts.Clear();
        originalChildTextColors.Clear();
        foreach (Transform child in pendingLevelsImage.transform)
        {
            var tmp = child.GetComponent<TextMeshProUGUI>();
            if (tmp != null)
            {
                childTexts.Add(tmp);
                originalChildTextColors.Add(tmp.color);
            }
        }

        Color red = new Color(1f, 0.3f, 0.3f, 1f);
        while (true)
        {
            float t = Mathf.Sin(Time.unscaledTime * 8f) * 0.5f + 0.5f;
            Color c = Color.Lerp(Color.white, red, t);
            pendingLevelsImage.color = c;
            foreach (var txt in childTexts)
                txt.color = c;
            yield return null;
        }
    }
}