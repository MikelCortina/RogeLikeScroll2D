using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeUI : MonoBehaviour
{
    public const int MAX_PENDING_LEVELS = 6;
    public static UpgradeUI Instance { get; private set; }

    [Header("Panel y Botones")]
    [SerializeField] private GameObject upgradePanel;
    [SerializeField] private Button[] upgradeButtons;
    [SerializeField] private TextMeshProUGUI[] upgradeNameTexts;
    [SerializeField] private TextMeshProUGUI[] upgradeDescriptionTexts;

    [Header("Colores según rareza")]
    [SerializeField] private Color rareColor = Color.blue;
    [SerializeField] private Color epicColor = new Color(0.5f, 0f, 1f);
    [SerializeField] private Color legendaryColor = Color.yellow;

    [Header("HUD de niveles pendientes")]
    [SerializeField] private Image pendingLevelsImage;           // Imagen que muestra el estado
    [SerializeField] private Sprite[] levelSprites;              // Sprites: índice 0 a 6
    [SerializeField] private TextMeshProUGUI pendingLevelsText;  // (opcional)

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

        UpdatePendingLevelsVisuals();
    }

    private void UpdatePendingLevelsVisuals()
    {
        // Texto (opcional)
        if (pendingLevelsText != null)
            pendingLevelsText.text = $"Mejoras pendientes: {PendingCount}";

        // Sprite en HUD
        if (pendingLevelsImage != null && levelSprites != null && levelSprites.Length > 0)
        {
            int index = Mathf.Clamp(PendingCount, 0, levelSprites.Length - 1);
            pendingLevelsImage.sprite = levelSprites[index];
        }

        // Si llega al máximo → activar vibración
        if (PendingCount >= MAX_PENDING_LEVELS)
        {
            if (shakeCoroutine == null)
                shakeCoroutine = StartCoroutine(ShakeImage());
        }
        else
        {
            if (shakeCoroutine != null)
            {
                StopCoroutine(shakeCoroutine);
                shakeCoroutine = null;
                pendingLevelsImage.rectTransform.localPosition = originalImagePosition;
            }
        }
    }

    /// <summary>
    /// Hace que el icono vibre ligeramente (efecto "atención") mientras está activo
    /// </summary>
    private IEnumerator ShakeImage()
    {
        float amplitude = 1.75f; // intensidad de vibración (px)
        float frequency = 27.5f; // velocidad

        while (true)
        {
            if (pendingLevelsImage == null) yield break;

            float offsetX = Mathf.Sin(Time.time * frequency) * amplitude;
            float offsetY = Mathf.Cos(Time.time * frequency * 1.3f) * amplitude * 0.5f;
            pendingLevelsImage.rectTransform.localPosition = originalImagePosition + new Vector3(offsetX, offsetY, 0);

            yield return null;
        }
    }
}
