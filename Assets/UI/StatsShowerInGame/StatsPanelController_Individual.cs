using UnityEngine;
using System.Collections.Generic;
using TMPro;
using UnityEngine.UI;

public enum StatType
{
    MaxHP,
    CurrentHP,
    CurrentMaxHP,
    ArmorPercentage,
    BaseDamage,
    ProjectileSpeed,
    FireRate,
    Radius,
    PlayerLevel,
    CurrentXP,
    XpGainMultiplier,
    CriticalChance,
    DodgeChance,
    Luck,
    WormLuck,
    RiderMultiplier,
    RunnerMultiplier,
    Harvester
}

[System.Serializable]
public class StatConfig
{
    public StatType stat;
    public string displayName;
    public Sprite icon;
    public bool showIcon = true;
    public Color labelColor = Color.white;
    public Color valueColor = Color.white;
    public int labelFontSize = 16;
    public int valueFontSize = 16;
    public bool showPercent = false;
    public string suffix = "";
    public bool enabled = true;
}

public class StatsPanelController_Individual : MonoBehaviour
{
    [Header("UI References")]
    public GameObject panel;
    public GameObject statLinePrefab; // Prefab con StatLineUI
    public Transform contentParent;   // Contenedor con VerticalLayoutGroup

    [Header("Options")]
    public bool startVisible = false;
    public bool pauseOnOpen = true;
    public bool showCursorOnOpen = true;

    [Header("Optional refs")]
    public MonoBehaviour crosshairMouseFollow;

    [Header("Stat configuration")]
    public List<StatConfig> statConfigs = new List<StatConfig>();

    private readonly List<GameObject> spawnedLines = new List<GameObject>();

    void Awake()
    {
        if (panel == null || statLinePrefab == null || contentParent == null)
        {
            Debug.LogWarning("[StatsPanelController_Individual] faltan referencias (panel, statLinePrefab, contentParent).");
            enabled = false;
            return;
        }

        panel.SetActive(startVisible);
        ApplyGameState(startVisible);
    }

    void OnEnable()
    {
        if (StatsManager.Instance != null)
        {
            StatsManager.Instance.OnHealthChanged += OnHealthChanged;
            StatsManager.Instance.OnLevelUp += OnLevelUp;
        }

        RefreshUI();
    }

    void OnDisable()
    {
        if (StatsManager.Instance != null)
        {
            StatsManager.Instance.OnHealthChanged -= OnHealthChanged;
            StatsManager.Instance.OnLevelUp -= OnLevelUp;
        }

        if (pauseOnOpen) Time.timeScale = 1f;
        if (showCursorOnOpen)
        {
            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            bool newState = !panel.activeSelf;
            panel.SetActive(newState);
            ApplyGameState(newState);

            if (crosshairMouseFollow != null)
            {
                var method = crosshairMouseFollow.GetType().GetMethod("ForceRefresh");
                if (method != null) method.Invoke(crosshairMouseFollow, null);
            }

            if (newState) RefreshUI();
        }
    }

    private void ApplyGameState(bool isPanelOpen)
    {
        if (pauseOnOpen) Time.timeScale = isPanelOpen ? 0f : 1f;

        if (showCursorOnOpen)
        {
            if (isPanelOpen)
            {
                Cursor.visible = true;
                Cursor.lockState = CursorLockMode.None;
            }
            else
            {
                Cursor.visible = false;
                Cursor.lockState = CursorLockMode.Locked;
            }
        }
    }

    private void OnHealthChanged(float c, float m) { if (panel.activeSelf) RefreshUI(); }
    private void OnLevelUp(int lvl) { if (panel.activeSelf) RefreshUI(); }

    public void RefreshUI()
    {
        if (StatsManager.Instance == null || StatsManager.Instance.RuntimeStats == null)
        {
            ClearSpawnedLines();
            return;
        }

        StatsData s = StatsManager.Instance.RuntimeStats;
        ClearSpawnedLines();

        foreach (var cfg in statConfigs)
        {
            if (!cfg.enabled) continue;

            string label = string.IsNullOrEmpty(cfg.displayName) ? cfg.stat.ToString() : cfg.displayName;
            string valueString = GetStatString(s, cfg);

            var go = Instantiate(statLinePrefab, contentParent);
            go.name = "StatLine_" + cfg.stat.ToString();
            var ui = go.GetComponent<StatLineUI>();
            if (ui != null)
            {
                ui.Setup(cfg.icon, label, valueString,
                         cfg.labelColor, cfg.valueColor,
                         cfg.labelFontSize, cfg.valueFontSize, cfg.showIcon);
            }
            go.SetActive(true);
            ui.labelText.ForceMeshUpdate();
            ui.valueText.ForceMeshUpdate();

            spawnedLines.Add(go);
        }
        Canvas.ForceUpdateCanvases();
    }

    private void ClearSpawnedLines()
    {
        for (int i = spawnedLines.Count - 1; i >= 0; --i)
        {
            if (spawnedLines[i] != null) Destroy(spawnedLines[i]);
        }
        spawnedLines.Clear();
    }

    private string GetStatString(StatsData s, StatConfig cfg)
    {
        float raw = 0f;
        bool isFloat = true;

        switch (cfg.stat)
        {
            case StatType.MaxHP: raw = s.maxHP; break;
            case StatType.CurrentHP: raw = s.currentHP; break;
            case StatType.CurrentMaxHP: raw = s.currentMaxHP; break;
            case StatType.ArmorPercentage: raw = s.armorPercentage; break;
            case StatType.BaseDamage: raw = s.baseDamage; break;
            case StatType.ProjectileSpeed: raw = s.projectileSpeed; break;
            case StatType.FireRate: raw = s.fireRate; break;
            case StatType.Radius: raw = s.radius; break;
            case StatType.PlayerLevel: isFloat = false; return StatsManager.Instance.playerCurrentLevel.ToString();
            case StatType.CurrentXP: isFloat = false; return StatsManager.Instance.currentXP.ToString();
            case StatType.XpGainMultiplier: raw = s.xpGainMultiplier; break;
            case StatType.CriticalChance: raw = s.criticalChance; break;
            case StatType.DodgeChance: raw = s.dodgeChance; break;
            case StatType.Luck: raw = s.luck; break;
            case StatType.WormLuck: raw = s.towerLuck; break;
            case StatType.RiderMultiplier: raw = s.riderMuliplier; break;
            case StatType.RunnerMultiplier: raw = s.runnerMuliplier; break;
            case StatType.Harvester: raw = s.harvester; break;
            default: raw = 0f; break;
        }

        if (isFloat)
        {
            if (float.IsNaN(raw) || float.IsInfinity(raw)) return "N/A";
            if (cfg.showPercent) return (raw).ToString("0.##") + (string.IsNullOrEmpty(cfg.suffix) ? "%" : cfg.suffix);
            return raw.ToString("0.##") + cfg.suffix;
        }

        return "N/A";
    }
}
