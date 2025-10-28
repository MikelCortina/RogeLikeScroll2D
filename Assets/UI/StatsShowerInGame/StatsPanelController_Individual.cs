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
    GunDamage,
    ExplosionDamage,
    ProjectileSpeed,
    FireRate,
    PlayerLevel,
    CurrentXP,
    XpGainMultiplier,
    CriticalChance,
    MeleDodgeChance,
    RangeDodgeChance,
    Knockback,
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
    public SkillTreeUI skillTreeUI;

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

        // Si quieres que al arrancar también se sincronice el skillTree con el estado inicial:
        if (skillTreeUI != null)
            skillTreeUI.Show(startVisible);
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

            // --- NUEVA LÍNEA: sincroniza el SkillTreeUI con el mismo estado (abrir/cerrar) ---
            if (skillTreeUI != null)
            {
                skillTreeUI.Show(newState);
            }
            // ------------------------------------------------------------------------------

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
            case StatType.CurrentMaxHP: raw = s.currentMaxHP; break;
            case StatType.ArmorPercentage: raw = s.armorPercentage; break;
            case StatType.GunDamage: raw = s.gunDamage; break;
            case StatType.ExplosionDamage: raw = s.explosionDamage; break;
            case StatType.FireRate: raw = s.fireRate; break;
            case StatType.PlayerLevel: isFloat = false; return StatsManager.Instance.playerCurrentLevel.ToString();
            case StatType.CurrentXP: isFloat = false; return StatsManager.Instance.currentXP.ToString();
            case StatType.XpGainMultiplier: raw = s.xpGainMultiplier; break;
            case StatType.CriticalChance: raw = s.criticalChance; break;
            case StatType.MeleDodgeChance: raw = s.meleDodgeChance; break;
            case StatType.RangeDodgeChance: raw = s.rangeDodgeChance; break;
            case StatType.Knockback: raw = s.knockback; break;
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
