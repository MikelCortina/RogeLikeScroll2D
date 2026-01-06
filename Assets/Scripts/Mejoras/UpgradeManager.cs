using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[System.Serializable]
public enum StatCategory
{
    HP,
    Projectile,
    Movimiento,
    FireArm,
    Damage,
    Armor,
    XP,
    Dodge,
    Suerte,
    Knockback,
    Nodo
}

[System.Serializable]
public class UpgradeGroup
{
    public StatCategory category;
    [Tooltip("Arrastra aquí los ScriptableObjects de tipo Upgrade que pertenezcan a esta categoría.")]
    public List<Upgrade> upgrades = new List<Upgrade>();
}

public class UpgradeManager : MonoBehaviour
{
    [SerializeField] private float chanceToOfferSkillNodeUnlock = 0.25f; // 25%
    [SerializeField] private UpgradeQuality skillNodeUpgradeQuality = UpgradeQuality.Epic; // o Legendary

    public static UpgradeManager Instance { get; private set; }

    [Header("Grupos de upgrades")]
    public List<UpgradeGroup> upgradeGroups = new List<UpgradeGroup>();

    [HideInInspector] public List<Upgrade> allUpgrades = new List<Upgrade>();

    // --- NUEVO: tracking runtime ---
    private HashSet<Upgrade> runtimeRemovedUpgrades = new HashSet<Upgrade>();
    private HashSet<ItemNode> runtimeRemovedNodes = new HashSet<ItemNode>();

    private void Awake()
    {
        if (Instance != null && Instance != this) Destroy(gameObject);
        else Instance = this;
        SyncAllUpgradesFromGroups();
    }

    /// <summary>
    /// Rellena la lista runtime allUpgrades a partir de los grupos base
    /// </summary>
    public void SyncAllUpgradesFromGroups()
    {
        allUpgrades.Clear();
        foreach (var g in upgradeGroups)
        {
            if (g == null || g.upgrades == null) continue;
            allUpgrades.AddRange(g.upgrades);
        }
        // eliminar nulos y duplicados
        allUpgrades = allUpgrades.Where(u => u != null).Distinct().ToList();
        // eliminar upgrades que ya fueron aplicados/removidos en esta run
        allUpgrades.RemoveAll(u => runtimeRemovedUpgrades.Contains(u));
    }

    public bool IsNodeUnlocked(ItemNode node)
    {
        if (node == null) return false;
        return node.effectToActivate != null &&
               RunEffectManager.Instance != null &&
               RunEffectManager.Instance.IsEffectActive(node.effectToActivate);
    }

    public void ShowUpgradeOptions(int count)
    {
        List<Upgrade> options = new List<Upgrade>();
        int attempts = 0;
        while (options.Count < count && attempts < 100)
        {
            attempts++;
            if (allUpgrades == null || allUpgrades.Count == 0) break;
            int index = Random.Range(0, allUpgrades.Count);
            Upgrade candidate = allUpgrades[index];
            if (RollSpawnByQuality(candidate.quality))
                options.Add(candidate);
        }
        // Aquí abrirías la UI para que el jugador seleccione uno
    }

    // --- CORRECCIÓN PRINCIPAL ---
    public void ApplyUpgrade(Upgrade upgrade)
    {
        if (upgrade == null) return;

        upgrade.Apply(StatsManager.Instance);

        if (upgrade is Upgrade_SkillNode skillNode)
        {
            var node = skillNode.nodeToUnlock;
            if (node == null) return;

            // --- 1️⃣ Quitar upgrade de listas runtime para que no vuelva a aparecer ---
            allUpgrades.Remove(upgrade);
            runtimeRemovedUpgrades.Add(upgrade); // asegura que no reaparezca
            foreach (var group in upgradeGroups)
                group.upgrades.Remove(upgrade);

            // --- 2️⃣ Aplicar efecto en StatsManager y RunEffectManager ---
            skillNode.Apply(StatsManager.Instance);
            RunEffectManager.Instance?.ActivateEffect(node.effectToActivate);

            // --- 3️⃣ Marcar nodo como desbloqueado en SkillTreeUI ---
            var skillTree = SkillTreeUI.GetInstance();
            if (skillTree != null && !skillTree.unlocked.Contains(node.nodeId))
                skillTree.unlocked.Add(node.nodeId);

            // --- 4️⃣ Marcar cualquier upgrade relacionado con este nodo ---
            RemoveUpgradesRelatedToNode(node);

            // --- 5️⃣ Forzar spawn de la familia si la UI está activa ---
            skillTree?.ForceSpawnFamilyContainingNode(node);
        }
        else
        {
            // Para upgrades normales (opcional: si quieres que tampoco vuelvan a aparecer)
            allUpgrades.Remove(upgrade);
            runtimeRemovedUpgrades.Add(upgrade);
            foreach (var group in upgradeGroups)
                group.upgrades.Remove(upgrade);
        }
    }
    public void RemoveUpgradesRelatedToNode(ItemNode node)
    {
        if (node == null) return;
        var toRemove = allUpgrades
            .Where(u => u is Upgrade_SkillNode sn && sn.nodeToUnlock == node)
            .ToList();

        foreach (var up in toRemove)
        {
            runtimeRemovedUpgrades.Add(up);
            allUpgrades.Remove(up);
        }
    }

    private bool RollSpawnByQuality(UpgradeQuality quality)
    {
        float roll = Random.value;
        switch (quality)
        {
            case UpgradeQuality.Rare: return roll < 0.6f;
            case UpgradeQuality.Epic: return roll < 0.3f;
            case UpgradeQuality.Legendary: return roll < 0.1f;
            default: return false;
        }
    }

    public List<Upgrade> GetRandomUpgrades(int count)
    {
        List<Upgrade> options = new List<Upgrade>();
        int attempts = 0;
        bool includeSkillNode = Random.value < chanceToOfferSkillNodeUnlock;
        Upgrade skillNodeUpgrade = null;

        if (includeSkillNode)
        {
            skillNodeUpgrade = TryCreateSkillNodeUpgrade();
            if (skillNodeUpgrade != null)
            {
                options.Add(skillNodeUpgrade);
                count--;
            }
        }

        while (options.Count < Mathf.Max(1, count) && attempts < 200)
        {
            attempts++;
            if (allUpgrades == null || allUpgrades.Count == 0) break;

            int index = Random.Range(0, allUpgrades.Count);
            Upgrade candidate = allUpgrades[index];

            // Añadir esta línea:
            if (runtimeRemovedUpgrades.Contains(candidate)) continue;

            if (options.Contains(candidate)) continue;
            if (RollSpawnByQuality(candidate.quality))
                options.Add(candidate);
        }

        while (options.Count < 3 && allUpgrades.Count > 0)
        {
            var fallback = allUpgrades[Random.Range(0, allUpgrades.Count)];
            if (!options.Contains(fallback))
                options.Add(fallback);
        }

        return options.Take(3).ToList();
    }

    private Upgrade TryCreateSkillNodeUpgrade()
    {
        var skillTree = FindObjectOfType<SkillTreeUI>();
        var runEffectManager = RunEffectManager.Instance;
        if (skillTree == null || runEffectManager == null) return null;

        var availableNodes = skillTree.skillFamilies
            .SelectMany(f => f.nodes ?? new ItemNode[0])
            .Where(n => n != null
                && n.effectToActivate != null
                && !runEffectManager.IsEffectActive(n.effectToActivate)
                && !runtimeRemovedNodes.Contains(n))
            .ToList();

        if (availableNodes.Count == 0) return null;

        var chosenNode = availableNodes[Random.Range(0, availableNodes.Count)];
        runtimeRemovedNodes.Add(chosenNode);

        var upgradeInstance = ScriptableObject.CreateInstance<Upgrade_SkillNode>();
        upgradeInstance.name = $"UNLOCK_{chosenNode.nodeId}";
        upgradeInstance.upgradeName = $"¡NUEVA HABILIDAD!\n{chosenNode.displayName}";
        upgradeInstance.description = $"Desbloquea:\n{chosenNode.description}";
        upgradeInstance.quality = skillNodeUpgradeQuality;
        upgradeInstance.nodeToUnlock = chosenNode;

        // 🔑 AÑADIR AQUÍ: marcar este upgrade runtime como "ya ofrecido"
        runtimeRemovedUpgrades.Add(upgradeInstance);

        return upgradeInstance;
    }

    // --------------------------
    // Nuevo: limpiar runtime al iniciar nueva run
    // --------------------------
    public void StartNewRun()
    {
        // 🔹 Reset efectos primero
        var skillTree = SkillTreeUI.GetInstance();
        if (skillTree != null)
        {
            foreach (var family in skillTree.skillFamilies)
            {
                if (family?.nodes == null) continue;
                foreach (var node in family.nodes)
                {
                    if (node == null) continue;
                    if (RunEffectManager.Instance.IsEffectActive(node.effectToActivate))
                        RunEffectManager.Instance.DeactivateEffect(node.effectToActivate);
                }
            }
        }

        runtimeRemovedUpgrades.Clear();
        SyncAllUpgradesFromGroups();
    }
}
