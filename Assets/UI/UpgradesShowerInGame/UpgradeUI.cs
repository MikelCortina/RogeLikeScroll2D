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
        if(PendingCount == 0)
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
