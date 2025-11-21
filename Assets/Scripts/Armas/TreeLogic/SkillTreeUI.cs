using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

public class SkillTreeUI : MonoBehaviour
{
    [Header("Prefabs & Containers")]
    public SkillNodeButton defaultNodeButtonPrefab;

    [Header("Plantilla de offsets (opcional)")]
    public List<Transform> familySlots = new List<Transform>(3);

    [Header("Distribución de familias: cada Transform debe tener N hijos (ej: 3) que serán los slots")]
    public List<Transform> familyContainers = new List<Transform>();

    [Header("Families")]
    public List<SkillFamily> skillFamilies = new List<SkillFamily>();

    [Header("Mixed Node Containers (donde aparecerán los mixed nodes)")]
    public List<Transform> mixedNodeContainers = new List<Transform>();

    [Header("UI")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI precioRefresh;

    // -------------------------
    // --- Spawn cost settings (familias)
    // -------------------------
    [Header("Spawn cost (para boton de crear nueva familia)")]
    [Tooltip("Coste base para spawnear la primera familia")]
    public int spawnBaseCost = 10;
    [Tooltip("Coste máximo (tope) para evitar overflow al elevar al cuadrado repetidamente)")]
    public double maxSpawnCost = 1000000000.0; // 1e9 por defecto

    // Nuevo: ajustes para costes de nodos en runtime
    [Header("Runtime cost settings")]
    [Tooltip("Coste por defecto para el primer nodo si el ScriptableObject no lo define.")]
    public int defaultFirstNodeCost = 20;

    // instancias actuales por contenedor (opcional tracking)
    private Dictionary<Transform, List<SkillNodeButton>> instantiatedButtonsPerContainer = new Dictionary<Transform, List<SkillNodeButton>>();

    // registro de nombres de familias activas para evitar duplicados
    private HashSet<string> activeFamilyNames = new HashSet<string>();

    // runtime cost tracking: nodeId -> computed cost en esta run
    private Dictionary<string, int> runtimeCosts = new Dictionary<string, int>();

    // mantiene el orden en que se instanciaron las familias (para encadenar costes entre familias) [no usado en la versión simplificada]
    private List<string> spawnedFamilyOrder = new List<string>();

    private HashSet<string> unlocked = new HashSet<string>();
    public PlayerResources playerResources;
    public SkillTreePanZoom panZoom;

    private bool currencySubscribed = false;
    private Coroutine subscribeCoroutine;
    private HashSet<int> appliedEffectKeys = new HashSet<int>();

    private int nextContainerIndex = 0;

    private const string FAMILY_ROOT_PREFIX = "family_inst_";

    // --- spawn cost runtime state ---
    private double nextSpawnCost = 0.0;
    private int spawnUses = 0;

    // -------------------------
    // --- Mixed nodes feature
    // -------------------------
    [Header("Mixed Nodes (nodos mixtos)")]
    [Tooltip("Lista de definiciones de nodos mixtos. Cada entrada indica el ItemNode 'mixto' y la lista de nodeIds que requiere.")]
    public List<MixedNodeEntry> mixedNodes = new List<MixedNodeEntry>();

    private Dictionary<string, SkillNodeButton> instantiatedMixedNodes = new Dictionary<string, SkillNodeButton>();

    [System.Serializable]
    public class MixedNodeEntry
    {
        public ItemNode mixedNode;
        [Tooltip("Lista de nodeIds requeridos (dos o más). Deben ser nodos finales (últimos) de distintas familias.")]
        public List<string> requiredNodeIds = new List<string>();
    }

    // *** NUEVO: seguimiento del último coste comprado (solo informativo)
    private int lastPurchasedCost = -1;

    // *** NUEVO: coste global del siguiente objeto a comprar (siempre se duplica tras cada compra)
    private int nextPurchaseCostRuntime = -1;

    private void Awake()
    {
        playerResources = GameObject.FindWithTag("Player")?.GetComponent<PlayerResources>();
    }

    private IEnumerator Start()
    {
        yield return null;
        StartNewRun(internalCall: true);

        if (familyContainers != null && familyContainers.Count > 0)
            ShowRandomFamilyInContainer(familyContainers[0]);
        else
            ShowRandomFamily();

        if (subscribeCoroutine == null)
            subscribeCoroutine = StartCoroutine(SubscribeToCurrencyWhenReady());
    }

    private void OnEnable()
    {
        RefreshAllInstantiatedButtons();
        if (subscribeCoroutine == null)
            subscribeCoroutine = StartCoroutine(SubscribeToCurrencyWhenReady());
    }

    private void OnDisable()
    {
        UnsubscribeCurrency();
        if (subscribeCoroutine != null)
        {
            StopCoroutine(subscribeCoroutine);
            subscribeCoroutine = null;
        }
    }

    private void OnDestroy()
    {
        UnsubscribeCurrency();
    }

    private void Update()
    {
        if (precioRefresh != null)
            precioRefresh.text = GetNextSpawnCost().ToString();
    }

    private IEnumerator SubscribeToCurrencyWhenReady()
    {
        while (StatsManager.Instance == null)
            yield return null;

        if (!currencySubscribed)
        {
            StatsManager.Instance.OnCurrencyChanged += OnCurrencyChanged;
            currencySubscribed = true;
            OnCurrencyChanged(StatsManager.Instance.RuntimeStats.currency);
        }

        subscribeCoroutine = null;
    }

    private void UnsubscribeCurrency()
    {
        if (currencySubscribed && StatsManager.Instance != null)
        {
            StatsManager.Instance.OnCurrencyChanged -= OnCurrencyChanged;
        }
        currencySubscribed = false;
    }

    private void OnCurrencyChanged(int newCurrency)
    {
        RefreshAllInstantiatedButtons();
    }

    // -------------------------
    // Run lifecycle
    // -------------------------
    public void StartNewRun(bool internalCall = false)
    {
        foreach (var family in skillFamilies)
        {
            if (family == null || family.nodes == null) continue;
            foreach (var n in family.nodes)
            {
                if (n == null) continue;
                DeactivateEffectInstance(n);
            }
        }

        appliedEffectKeys.Clear();
        unlocked.Clear();

        ClearAllInstantiatedButtons();
        instantiatedButtonsPerContainer.Clear();
        activeFamilyNames.Clear();
        spawnedFamilyOrder.Clear();

        // Reset spawn cost state al inicio de la run
        spawnUses = 0;
        nextSpawnCost = Mathf.Max(1, spawnBaseCost);

        // limpiar costes runtime
        runtimeCosts.Clear();

        // reset del último coste comprado en la run
        lastPurchasedCost = -1;

        // *** NUEVO: primer coste global = fijo
        nextPurchaseCostRuntime = defaultFirstNodeCost;
    }

    // -------------------------
    // Unlock logic (runtime only)
    // -------------------------
    public bool IsUnlocked(string nodeId) => unlocked.Contains(nodeId);

    private bool AllRequiredFamiliesPresent(List<SkillNodeButton> requiredButtons)
    {
        HashSet<Transform> familiesFound = new HashSet<Transform>();

        foreach (var btn in requiredButtons)
        {
            var root = GetFamilyRootForButton(btn.transform);
            if (root != null)
                familiesFound.Add(root);
        }

        return familiesFound.Count >= 2;
    }

    public bool CanUnlock(ItemNode node)
    {
        if (node == null) return false;
        if (IsUnlocked(node.nodeId)) return false;

        if (node.prerequisiteNodeIds != null)
        {
            foreach (var p in node.prerequisiteNodeIds)
                if (!IsUnlocked(p)) return false;
        }

        int cost = GetNodeEffectiveCost(node);

        if (cost > 0)
        {
            if (playerResources == null)
                playerResources = GameObject.FindWithTag("Player")?.GetComponent<PlayerResources>();

            if (playerResources != null)
                return playerResources.HasCurrency(cost);

            if (StatsManager.Instance == null) return false;
            return StatsManager.Instance.RuntimeStats.currency >= cost;
        }

        return true;
    }

    public void TryUnlock(ItemNode node)
    {
        if (!CanUnlock(node)) return;

        int cost = GetNodeEffectiveCost(node);
        if (playerResources != null && cost > 0) playerResources.SpendCurrency(cost);

        foreach (var p in node.requiredEffectIdsToRemove)
            RemoveEffect(p);

        if (StatsManager.Instance != null && cost > 0)
            StatsManager.Instance.AddCurrency(-cost);

        ApplyEffect(node);
        unlocked.Add(node.nodeId);

        // *** NUEVO: actualizar tracking de compra y el coste global siguiente
        if (cost > 0)
        {
            lastPurchasedCost = cost;
            runtimeCosts[node.nodeId] = cost;

            long doubled = (long)cost * 2L;
            if (doubled > int.MaxValue) nextPurchaseCostRuntime = int.MaxValue;
            else nextPurchaseCostRuntime = (int)doubled;

            // *** NUEVO: actualizar el coste mostrado de TODOS los no comprados al nuevo global
            UpdateAllUnpurchasedRuntimeCostsToGlobal();
        }

        RefreshAllInstantiatedButtons();
    }

    private void UpdateAllUnpurchasedRuntimeCostsToGlobal()
    {
        if (nextPurchaseCostRuntime <= 0) nextPurchaseCostRuntime = defaultFirstNodeCost;

        // Actualizar botones instanciados por contenedor
        foreach (var kv in instantiatedButtonsPerContainer)
        {
            var list = kv.Value;
            if (list == null) continue;
            foreach (var b in list)
            {
                if (b == null || b.node == null) continue;
                string id = b.node.nodeId;
                if (!IsUnlocked(id))
                {
                    runtimeCosts[id] = nextPurchaseCostRuntime;
                }
            }
        }

        // Actualizar nodos mixtos instanciados (si aplican)
        foreach (var kv in instantiatedMixedNodes)
        {
            var btn = kv.Value;
            if (btn == null || btn.node == null) continue;
            if (!IsUnlocked(btn.node.nodeId))
            {
                runtimeCosts[btn.node.nodeId] = nextPurchaseCostRuntime;
            }
        }
    }

    private void RemoveEffect(ItemNode node)
    {
        if (node.effectToActivate == null) return;
        RunEffectManager.Instance?.DeactivateEffect(node.effectToActivate);
        if (node.effectToActivate is IPersistentEffect persistent)
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null) persistent.RemoveFrom(player);
        }
    }

    // -------------------------
    // Effects management
    // -------------------------
    private void ApplyEffect(ItemNode node)
    {
        if (node == null || node.effectToActivate == null) return;
        int key = GetEffectKey(node.effectToActivate);
        RunEffectManager.Instance?.ActivateEffect(node.effectToActivate);
        if (!appliedEffectKeys.Contains(key))
        {
            if (node.effectToActivate is IPersistentEffect persistent)
            {
                var player = GameObject.FindWithTag("Player");
                if (player != null) persistent.ApplyTo(player);
            }
            appliedEffectKeys.Add(key);
        }
    }

    private void DeactivateEffectInstance(ItemNode node)
    {
        if (node == null) return;
        int key = GetEffectKey(node.effectToActivate);
        if (!appliedEffectKeys.Contains(key)) return;
        RunEffectManager.Instance?.DeactivateEffect(node.effectToActivate);
        if (node.effectToActivate is IPersistentEffect persistent)
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null) persistent.RemoveFrom(player);
        }
        appliedEffectKeys.Remove(key);
    }

    private int GetEffectKey(Object effectObj)
    {
        if (effectObj == null) return 0;
        return effectObj.GetInstanceID();
    }

    // -------------------------
    // Families & instanciado UI (con root por familia para evitar solapamiento)
    // -------------------------
    public void Show(bool show)
    {
        gameObject.SetActive(show);
        Time.timeScale = show ? 0f : 1f;
        RefreshAllInstantiatedButtons();
        if (panZoom != null) panZoom.enabled = show;
    }

    public void ShowRandomFamily()
    {
        if (skillFamilies == null || skillFamilies.Count == 0) return;
        int idx = Random.Range(0, skillFamilies.Count);
        ShowFamily(skillFamilies[idx]);
    }

    public void ShowFamily(SkillFamily family)
    {
        if (family == null || familySlots == null || familySlots.Count == 0) return;

        ClearAllInstantiatedButtons();
        instantiatedButtonsPerContainer.Clear();

        Transform parent = this.transform;
        List<SkillNodeButton> created = new List<SkillNodeButton>();

        for (int i = 0; i < familySlots.Count; i++)
        {
            ItemNode node = (i < family.nodes.Length) ? family.nodes[i] : null;
            if (node == null) continue;

            var prefabToUse = node.buttonPrefab != null ? node.buttonPrefab : defaultNodeButtonPrefab;
            if (prefabToUse == null) continue;

            var inst = Instantiate(prefabToUse, parent, worldPositionStays: false);
            inst.name = $"{prefabToUse.name}_inst_{node.nodeId}";

            // asignar coste runtime si no existe (usará el coste global actual)
            ComputeAndAssignCostsForFamily(family);

            inst.SetNode(node);
            inst.Initialize(this);

            inst.transform.localPosition = familySlots[i] != null ? familySlots[i].localPosition : Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one;

            created.Add(inst);
        }

        instantiatedButtonsPerContainer[parent] = created;
        RefreshAllInstantiatedButtons();
        UpdateMixedNodesVisibility();
    }

    public void ShowFamilyInContainer(SkillFamily family, Transform container)
    {
        if (family == null)
        {
            Debug.LogWarning("[SkillTreeUI] ShowFamilyInContainer: family == null");
            return;
        }
        if (container == null)
        {
            Debug.LogWarning("[SkillTreeUI] ShowFamilyInContainer: container == null");
            return;
        }

        string familyName = string.IsNullOrEmpty(family.name) ? family.GetInstanceID().ToString() : family.name;

        if (skillFamilies == null || skillFamilies.Count == 0)
            Debug.LogWarning("[SkillTreeUI] ShowFamilyInContainer: skillFamilies está vacío.");

        Debug.Log($"[SkillTreeUI] Intentando spawnear family '{familyName}' en container '{container.name}'.");

        if (activeFamilyNames.Contains(familyName))
        {
            Debug.Log($"[SkillTreeUI] Family '{familyName}' ya está activa en otra posición. Abortando spawn.");
            return;
        }

        for (int i = 0; i < container.childCount; i++)
        {
            if (container.GetChild(i).name.StartsWith(FAMILY_ROOT_PREFIX))
            {
                Debug.Log($"[SkillTreeUI] Container '{container.name}' ya ocupado por otra familia. Abortando spawn.");
                return;
            }
        }

        int expectedSlots = Mathf.Max((familySlots != null ? familySlots.Count : 0), family.nodes.Length);
        if (container.childCount < expectedSlots)
        {
            Debug.LogWarning($"[SkillTreeUI] Container {container.name} tiene {container.childCount} hijos, se esperaban {expectedSlots}.");
        }

        GameObject familyRoot = new GameObject($"{FAMILY_ROOT_PREFIX}{container.name}_{familyName}");
        familyRoot.transform.SetParent(container, worldPositionStays: false);
        familyRoot.transform.localPosition = Vector3.zero;
        familyRoot.transform.localRotation = Quaternion.identity;
        familyRoot.transform.localScale = Vector3.one;

        List<SkillNodeButton> created = new List<SkillNodeButton>();
        int createdCount = 0;

        spawnedFamilyOrder.Add(familyName);

        for (int i = 0; i < family.nodes.Length; i++)
        {
            ItemNode node = family.nodes[i];
            if (node == null) continue;

            Transform slotTransform = (i < container.childCount) ? container.GetChild(i) : null;
            var prefabToUse = node.buttonPrefab != null ? node.buttonPrefab : defaultNodeButtonPrefab;

            if (prefabToUse == null)
            {
                Debug.LogWarning($"[SkillTreeUI] Nodo '{node.nodeId}' no tiene prefab (buttonPrefab y defaultNodeButtonPrefab son null). No se instanciara ese nodo.");
                continue;
            }

            // calcular y asignar costes runtime (tomará el coste global actual)
            ComputeAndAssignCostsForFamily(family);

            var inst = Instantiate(prefabToUse, familyRoot.transform, worldPositionStays: false);
            inst.name = $"{prefabToUse.name}_inst_{node.nodeId}";
            inst.SetNode(node);
            inst.Initialize(this);

            if (slotTransform != null)
            {
                inst.transform.localPosition = slotTransform.localPosition;
            }
            else if (familySlots != null && i < familySlots.Count && familySlots[i] != null)
            {
                inst.transform.localPosition = familySlots[i].localPosition;
            }
            else
            {
                inst.transform.localPosition = Vector3.zero;
            }

            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one;

            created.Add(inst);
            createdCount++;
        }

        instantiatedButtonsPerContainer[container] = created;
        activeFamilyNames.Add(familyName);

        Debug.Log($"[SkillTreeUI] Family '{familyName}' instanciada en '{container.name}' con {createdCount} nodos creados.");

        RefreshAllInstantiatedButtons();
        UpdateMixedNodesVisibility();

        foreach (var btn in created)
        {
            if (btn == null || btn.node == null) continue;
            if (!unlocked.Contains(btn.node.nodeId)) continue;
            ApplyEffect(btn.node);
        }
    }

    private Transform GetNextAvailableFamilyContainer()
    {
        if (familyContainers == null || familyContainers.Count == 0)
            return this.transform;

        foreach (var c in familyContainers)
        {
            if (c == null) continue;

            bool tieneFamilyRoot = false;
            for (int i = 0; i < c.childCount; i++)
            {
                if (c.GetChild(i).name.StartsWith(FAMILY_ROOT_PREFIX))
                {
                    tieneFamilyRoot = true;
                    break;
                }
            }
            if (!tieneFamilyRoot)
            {
                bool childOcupado = false;
                for (int i = 0; i < c.childCount; i++)
                {
                    var child = c.GetChild(i);
                    var anyBtn = child.GetComponentInChildren<SkillNodeButton>();
                    if (anyBtn != null)
                    {
                        childOcupado = true;
                        break;
                    }
                }

                if (!childOcupado)
                    return c;
            }
        }

        for (int i = 0; i < familyContainers.Count; i++)
        {
            int idx = (nextContainerIndex + i) % familyContainers.Count;
            if (familyContainers[idx] != null)
            {
                nextContainerIndex = (idx + 1) % familyContainers.Count;
                return familyContainers[idx];
            }
        }

        return this.transform;
    }

    public void SpawnRandomFamilyInNextContainer()
    {
        if (skillFamilies == null || skillFamilies.Count == 0) return;

        Transform target = GetNextAvailableFamilyContainer();
        if (target == null) return;

        var availableFamilies = skillFamilies.Where(f => !activeFamilyNames.Contains(f.name)).ToList();
        if (availableFamilies.Count == 0)
        {
            Debug.Log("[SkillTreeUI] No hay familias disponibles para spawnear (todas ya están instanciadas).");
            return;
        }

        int idx = Random.Range(0, availableFamilies.Count);
        ShowFamilyInContainer(availableFamilies[idx], target);
    }

    public void ShowRandomFamilyInContainer(Transform container)
    {
        if (skillFamilies == null || skillFamilies.Count == 0 || container == null) return;

        var availableFamilies = skillFamilies.Where(f => !activeFamilyNames.Contains(f.name)).ToList();
        if (availableFamilies.Count == 0)
        {
            Debug.Log("[SkillTreeUI] Todas las familias están activas; no se instanciará una nueva.");
            return;
        }

        int idx = Random.Range(0, availableFamilies.Count);
        ShowFamilyInContainer(availableFamilies[idx], container);
    }

    // PUBLIC API para UI: devuelve el coste redondeado actual que necesita el botón para spawnear.
    public int GetNextSpawnCost()
    {
        if (nextSpawnCost <= 0) nextSpawnCost = Mathf.Max(1, spawnBaseCost);
        return Mathf.Clamp((int)Mathf.Ceil((float)nextSpawnCost), 0, int.MaxValue);
    }

    public bool IsAffordableNextSpawn()
    {
        int cost = GetNextSpawnCost();

        if (playerResources == null)
            playerResources = GameObject.FindWithTag("Player")?.GetComponent<PlayerResources>();

        if (playerResources != null)
            return playerResources.HasCurrency(cost);

        if (StatsManager.Instance != null)
            return StatsManager.Instance.RuntimeStats.currency >= cost;

        return false;
    }

    public void TrySpawnRandomFamilyWithCost()
    {
        if (nextSpawnCost <= 0) nextSpawnCost = Mathf.Max(1, spawnBaseCost);

        int cost = GetNextSpawnCost();

        bool canPay = false;
        if (playerResources == null)
            playerResources = GameObject.FindWithTag("Player")?.GetComponent<PlayerResources>();

        if (playerResources != null)
            canPay = playerResources.HasCurrency(cost);
        else if (StatsManager.Instance != null)
            canPay = StatsManager.Instance.RuntimeStats.currency >= cost;

        if (!canPay)
        {
            Debug.Log("[SkillTreeUI] No hay suficiente currency para spawnear una familia. Coste: " + cost);
            return;
        }

        if (playerResources != null)
            playerResources.SpendCurrency(cost);

        if (StatsManager.Instance != null)
            StatsManager.Instance.AddCurrency(-cost);

        SpawnRandomFamilyInNextContainer();

        spawnUses++;
        double newCost = nextSpawnCost * nextSpawnCost;
        if (double.IsInfinity(newCost) || newCost > maxSpawnCost)
            nextSpawnCost = maxSpawnCost;
        else
            nextSpawnCost = newCost;

        RefreshAllInstantiatedButtons();
        UpdateMixedNodesVisibility();
    }

    private void ClearInstantiatedButtonsInContainer(Transform container)
    {
        if (container == null) return;

        List<Transform> toDestroy = new List<Transform>();
        for (int i = 0; i < container.childCount; i++)
        {
            var child = container.GetChild(i);
            if (child.name.StartsWith(FAMILY_ROOT_PREFIX))
                toDestroy.Add(child);
        }

        foreach (var t in toDestroy)
        {
            var btns = t.GetComponentsInChildren<SkillNodeButton>();
            foreach (var b in btns)
            {
                if (b != null && b.node != null) DeactivateEffectInstance(b.node);
            }

            string rootName = t.name;
            var parts = rootName.Split('_');
            if (parts.Length >= 3)
            {
                int firstUnderscore = rootName.IndexOf('_');
                int secondUnderscore = rootName.IndexOf('_', firstUnderscore + 1);
                if (secondUnderscore >= 0 && secondUnderscore + 1 < rootName.Length)
                {
                    string familyName = rootName.Substring(secondUnderscore + 1);
                    if (activeFamilyNames.Contains(familyName))
                        activeFamilyNames.Remove(familyName);

                    if (spawnedFamilyOrder.Contains(familyName))
                        spawnedFamilyOrder.Remove(familyName);
                }
            }

            var btnsInside = t.GetComponentsInChildren<SkillNodeButton>();
            foreach (var b in btnsInside)
            {
                if (b != null && b.node != null)
                {
                    if (runtimeCosts.ContainsKey(b.node.nodeId))
                        runtimeCosts.Remove(b.node.nodeId);
                }
            }

            Destroy(t.gameObject);
        }

        if (instantiatedButtonsPerContainer.ContainsKey(container))
            instantiatedButtonsPerContainer.Remove(container);

        if (toDestroy.Count == 0)
        {
            for (int i = container.childCount - 1; i >= 0; i--)
            {
                var child = container.GetChild(i);
                var anyBtn = child.GetComponentInChildren<SkillNodeButton>();
                if (anyBtn != null)
                {
                    var btns = child.GetComponentsInChildren<SkillNodeButton>();
                    foreach (var b in btns)
                        if (b != null && b.node != null) DeactivateEffectInstance(b.node);
                    Destroy(child.gameObject);
                }
            }
        }

        UpdateMixedNodesVisibility();
    }

    private void ClearAllInstantiatedButtons()
    {
        var keys = instantiatedButtonsPerContainer.Keys.ToList();
        foreach (var k in keys)
            ClearInstantiatedButtonsInContainer(k);

        if (familyContainers != null)
        {
            foreach (var c in familyContainers)
            {
                ClearInstantiatedButtonsInContainer(c);
            }
        }

        // reiniciar tracking runtime
        runtimeCosts.Clear();
        spawnedFamilyOrder.Clear();
        activeFamilyNames.Clear();

        // reset último coste comprado también
        lastPurchasedCost = -1;

        // *** NUEVO: reset del coste global del siguiente objeto
        nextPurchaseCostRuntime = defaultFirstNodeCost;
    }

    private void RefreshAllInstantiatedButtons()
    {
        foreach (var kv in instantiatedButtonsPerContainer)
        {
            var list = kv.Value;
            for (int i = 0; i < list.Count; i++)
            {
                var b = list[i];
                if (b == null) continue;
                b.UpdateState();
            }
        }

        if (familyContainers != null)
        {
            for (int i = 0; i < familyContainers.Count; i++)
            {
                var c = familyContainers[i];
                for (int j = 0; j < c.childCount; j++)
                {
                    var child = c.GetChild(j);
                    var btn = child.GetComponentInChildren<SkillNodeButton>();
                    if (btn != null) btn.UpdateState();
                }
            }
        }

        UpdateMixedNodesVisibility();
    }

    // -------------------------
    // Mixed nodes helpers (ahora usan contenedores asignados)
    // -------------------------
    private bool IsLastNodeInItsFamily(ItemNode node)
    {
        if (node == null) return false;
        foreach (var fam in skillFamilies)
        {
            if (fam == null || fam.nodes == null || fam.nodes.Length == 0) continue;

            ItemNode lastNonNull = null;
            for (int i = fam.nodes.Length - 1; i >= 0; i--)
            {
                if (fam.nodes[i] == null) continue;
                lastNonNull = fam.nodes[i];
                break;
            }

            if (lastNonNull != null && lastNonNull.nodeId == node.nodeId)
                return true;
        }
        return false;
    }

    private SkillNodeButton FindInstantiatedButtonByNodeId(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId)) return null;

        foreach (var kv in instantiatedButtonsPerContainer)
        {
            var list = kv.Value;
            if (list == null) continue;
            foreach (var b in list)
            {
                if (b != null && b.node != null && b.node.nodeId == nodeId)
                    return b;
            }
        }

        var allButtons = GameObject.FindObjectsOfType<SkillNodeButton>();
        foreach (var b in allButtons)
        {
            if (b != null && b.node != null && b.node.nodeId == nodeId)
                return b;
        }

        return null;
    }

    private Transform GetFamilyRootForButton(Transform btnTransform)
    {
        if (btnTransform == null) return null;
        Transform t = btnTransform;
        while (t != null)
        {
            if (t.name != null && t.name.StartsWith(FAMILY_ROOT_PREFIX)) return t;
            t = t.parent;
        }
        return null;
    }

    private void UpdateMixedNodesVisibility()
    {
        // limpiar los mixed nodes eliminados en la lista
        var toRemoveInvalid = new List<string>();
        foreach (var kv in instantiatedMixedNodes)
        {
            if (!mixedNodes.Any(m => m.mixedNode != null && m.mixedNode.nodeId == kv.Key))
            {
                if (kv.Value != null) Destroy(kv.Value.gameObject);
                toRemoveInvalid.Add(kv.Key);
            }
        }
        foreach (var k in toRemoveInvalid)
            instantiatedMixedNodes.Remove(k);

        // verificar contenedores
        if (mixedNodeContainers == null || mixedNodeContainers.Count == 0)
        {
            Debug.LogWarning("No hay mixedNodeContainers asignados.");
            return;
        }

        // liberar contenedores: si un mixed node deja de estar activo, su contenedor queda libre
        HashSet<Transform> usedContainers = new HashSet<Transform>();
        foreach (var kv in instantiatedMixedNodes)
        {
            if (kv.Value != null)
                usedContainers.Add(kv.Value.transform.parent);
        }

        // índice del siguiente contenedor disponible
        int containerIndex = 0;

        for (int i = 0; i < mixedNodes.Count; i++)
        {
            MixedNodeEntry entry = mixedNodes[i];
            if (entry == null || entry.mixedNode == null)
                continue;

            string mixedId = entry.mixedNode.nodeId;

            // requisitos
            bool allRequirementsUnlocked = entry.requiredNodeIds.All(id => unlocked.Contains(id));

            if (!allRequirementsUnlocked)
            {
                // destruir si existía
                if (instantiatedMixedNodes.ContainsKey(mixedId))
                {
                    var inst = instantiatedMixedNodes[mixedId];
                    if (inst != null)
                    {
                        if (inst.node != null) DeactivateEffectInstance(inst.node);
                        Destroy(inst.gameObject);
                    }
                    instantiatedMixedNodes.Remove(mixedId);
                }
                continue;
            }

            // si ya está instanciado, actualizar y continuar
            if (instantiatedMixedNodes.ContainsKey(mixedId) && instantiatedMixedNodes[mixedId] != null)
            {
                instantiatedMixedNodes[mixedId].UpdateState();
                continue;
            }

            // buscar siguiente contenedor disponible
            Transform container = null;

            while (containerIndex < mixedNodeContainers.Count)
            {
                Transform candidate = mixedNodeContainers[containerIndex];

                containerIndex++;

                if (!usedContainers.Contains(candidate))
                {
                    container = candidate;
                    usedContainers.Add(candidate);
                    break;
                }
            }

            if (container == null)
            {
                Debug.LogWarning("No quedan contenedores disponibles para mixed nodes.");
                break;
            }

            // instanciar mixed node
            var prefab = entry.mixedNode.buttonPrefab != null
                ? entry.mixedNode.buttonPrefab
                : defaultNodeButtonPrefab;

            if (prefab == null) continue;

            var mixedBtn = Instantiate(prefab, container, false);
            mixedBtn.name = $"MixedNode_inst_{mixedId}";
            mixedBtn.SetNode(entry.mixedNode);
            mixedBtn.Initialize(this);

            mixedBtn.transform.localPosition = Vector3.zero;
            mixedBtn.transform.localRotation = Quaternion.identity;
            mixedBtn.transform.localScale = Vector3.one;

            instantiatedMixedNodes[mixedId] = mixedBtn;

            if (unlocked.Contains(mixedId))
                ApplyEffect(entry.mixedNode);
        }
    }

    // -------------------------
    // Utilities (coste runtime global)
    // -------------------------
    // Devuelve el coste efectivo (runtime) de un ItemNode:
    // 1) si se calculó en runtimeCosts, úsalo
    // 2) si no, usa el coste global actual (nextPurchaseCostRuntime)
    // 3) como último fallback, node.cost o defaultFirstNodeCost
    public int GetNodeEffectiveCost(ItemNode node)
    {
        if (node == null) return 0;

        if (runtimeCosts.TryGetValue(node.nodeId, out int v))
            return v;

        if (nextPurchaseCostRuntime > 0)
            return nextPurchaseCostRuntime;

        if (node.cost > 0) return node.cost;
        return defaultFirstNodeCost;
    }

    public string GetMissingRequirements(ItemNode node)
    {
        if (node == null) return "node null";
        var missing = new List<string>();
        if (node.prerequisiteNodeIds != null)
        {
            foreach (var p in node.prerequisiteNodeIds)
                if (!IsUnlocked(p)) missing.Add($"Requires node: {p}");
        }

        int effectiveCost = GetNodeEffectiveCost(node);
        if (playerResources != null && !playerResources.HasCurrency(effectiveCost))
            missing.Add($"Need {effectiveCost} currency");
        return missing.Count == 0 ? "None" : string.Join(", ", missing);
    }

    // -------------------------
    // Cálculo de costes runtime por familia (versión simplificada: coste global)
    // -------------------------
    private void ComputeAndAssignCostsForFamily(SkillFamily family)
    {
        if (family == null) return;

        if (nextPurchaseCostRuntime <= 0) nextPurchaseCostRuntime = defaultFirstNodeCost;

        for (int i = 0; i < family.nodes.Length; i++)
        {
            var node = family.nodes[i];
            if (node == null) continue;

            // Si el nodo ya está desbloqueado, respetamos su coste histórico (ya en runtimeCosts)
            if (unlocked.Contains(node.nodeId)) continue;

            // Asignar/actualizar el coste mostrado al coste global actual
            runtimeCosts[node.nodeId] = Mathf.Clamp(nextPurchaseCostRuntime, 0, int.MaxValue / 2);
        }
    }
}
