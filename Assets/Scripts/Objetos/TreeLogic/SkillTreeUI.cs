// (El using y la cabecera permanecen igual que tu original)
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

    [Header("UI")]
    public TextMeshProUGUI titleText;

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

    // mantiene el orden en que se instanciaron las familias (para encadenar costes entre familias)
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
    [Tooltip("Offset vertical (en unidades locales de skill tree) que se aplica al nodo mixto sobre la posición media entre nodos requeridos.")]
    public float mixedVerticalOffset = 20f;

    [System.Serializable]
    public class MixedNodeEntry
    {
        public ItemNode mixedNode;
        [Tooltip("Lista de nodeIds requeridos (dos o más). Deben ser nodos finales (últimos) de distintas familias.")]
        public List<string> requiredNodeIds = new List<string>();
    }

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

    // -> MODIFICADO: usar coste efectivo runtime
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
        RefreshAllInstantiatedButtons();
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

            // asignar coste runtime si no existe
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

        // antes de instanciar, registrar la familia en el orden (para el cálculo encadenado)
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

            // calcular y asignar costes runtime (si no están calculados)
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

                    // también quitar del orden de spawn si existe
                    if (spawnedFamilyOrder.Contains(familyName))
                        spawnedFamilyOrder.Remove(familyName);
                }
            }

            // al borrar una familia, eliminar sus costes runtime (si existieran)
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
    // Mixed nodes helpers (sin cambios funcionales importantes)
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
        if (mixedNodes == null || mixedNodes.Count == 0)
        {
            if (instantiatedMixedNodes.Count > 0) Debug.Log("[SkillTreeUI] No hay mixedNodes definidos; destruyendo instancias mixtas existentes.");
            foreach (var kv in instantiatedMixedNodes)
            {
                if (kv.Value != null) Destroy(kv.Value.gameObject);
            }
            instantiatedMixedNodes.Clear();
            return;
        }

        foreach (var entry in mixedNodes)
        {
            if (entry == null || entry.mixedNode == null || entry.requiredNodeIds == null || entry.requiredNodeIds.Count < 2)
            {
                Debug.LogWarning("[SkillTreeUI] MixedNodeEntry inválido o con menos de 2 requeridos.");
                continue;
            }

            string mixedId = entry.mixedNode.nodeId;
            Debug.Log($"[SkillTreeUI] Comprobando mixedNode '{mixedId}' (requiere {entry.requiredNodeIds.Count} nodos).");

            List<SkillNodeButton> requiredButtons = new List<SkillNodeButton>();
            HashSet<string> familyNames = new HashSet<string>();
            bool allPresentAndLast = true;

            foreach (var reqId in entry.requiredNodeIds)
            {
                var btn = FindInstantiatedButtonByNodeId(reqId);
                if (btn == null)
                {
                    Debug.Log($"[SkillTreeUI] Requerido '{reqId}' NO encontrado instanciado para mixed '{mixedId}'.");
                    allPresentAndLast = false;
                    break;
                }

                if (!IsLastNodeInItsFamily(btn.node))
                {
                    Debug.Log($"[SkillTreeUI] Requerido '{reqId}' encontrado pero NO es último nodo de su familia para mixed '{mixedId}'.");
                    allPresentAndLast = false;
                    break;
                }

                var root = GetFamilyRootForButton(btn.transform);
                string famName = root != null ? root.name : ("_no_root_" + btn.name);
                familyNames.Add(famName);

                requiredButtons.Add(btn);
            }
            if (allPresentAndLast)
            {
                if (!AllRequiredFamiliesPresent(requiredButtons))
                {
                    Debug.Log($"[SkillTreeUI] No todas las familias de los nodos requeridos para mixed '{mixedId}' están instanciadas todavía.");
                    allPresentAndLast = false;
                }
            }
            if (allPresentAndLast)
            {
                Vector3 avgWorld = Vector3.zero;
                foreach (var rb in requiredButtons) avgWorld += rb.transform.position;
                avgWorld /= requiredButtons.Count;

                Vector3 avgLocal = this.transform.InverseTransformPoint(avgWorld);
                avgLocal += Vector3.up * mixedVerticalOffset;

                if (instantiatedMixedNodes.ContainsKey(mixedId) && instantiatedMixedNodes[mixedId] != null)
                {
                    var existing = instantiatedMixedNodes[mixedId];
                    existing.transform.SetParent(this.transform, worldPositionStays: false);
                    existing.transform.localPosition = avgLocal;
                    existing.transform.localRotation = Quaternion.identity;
                    existing.transform.localScale = Vector3.one;
                    existing.UpdateState();
                }
                else
                {
                    var prefabToUse = entry.mixedNode.buttonPrefab != null ? entry.mixedNode.buttonPrefab : defaultNodeButtonPrefab;
                    if (prefabToUse == null) continue;

                    var inst = Instantiate(prefabToUse, this.transform, worldPositionStays: false);
                    inst.name = $"{prefabToUse.name}_mixed_inst_{mixedId}";
                    inst.SetNode(entry.mixedNode);
                    inst.Initialize(this);

                    inst.transform.localPosition = avgLocal;
                    inst.transform.localRotation = Quaternion.identity;
                    inst.transform.localScale = Vector3.one;

                    instantiatedMixedNodes[mixedId] = inst;

                    if (unlocked.Contains(mixedId))
                        ApplyEffect(entry.mixedNode);
                }
            }
            else
            {
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
            }
        }

        var toRemove = new List<string>();
        foreach (var kv in instantiatedMixedNodes)
        {
            bool stillDefined = mixedNodes.Any(e => e != null && e.mixedNode != null && e.mixedNode.nodeId == kv.Key);
            if (!stillDefined)
            {
                if (kv.Value != null) Destroy(kv.Value.gameObject);
                toRemove.Add(kv.Key);
            }
        }
        foreach (var k in toRemove) instantiatedMixedNodes.Remove(k);
    }

    // -------------------------
    // Utilities (nuevo metodo de coste runtime)
    // -------------------------
    // Devuelve el coste efectivo (runtime) de un ItemNode: si se calculó, lo devuelve; si no, devuelve node.cost (o defaultFirstNodeCost)
    public int GetNodeEffectiveCost(ItemNode node)
    {
        if (node == null) return 0;
        if (runtimeCosts.TryGetValue(node.nodeId, out int v)) return v;
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
    // Calculo de costes runtime por familia
    // -------------------------
    // Calcula y asigna costes en runtime para todos los nodos de la familia si aún no existen.
    private void ComputeAndAssignCostsForFamily(SkillFamily family)
    {
        if (family == null) return;
        string familyName = string.IsNullOrEmpty(family.name) ? family.GetInstanceID().ToString() : family.name;

        // si ya habíamos calculado costes para los nodos de esta familia (al menos uno), no rehacer
        bool anyUnassigned = false;
        foreach (var n in family.nodes)
        {
            if (n == null) continue;
            if (!runtimeCosts.ContainsKey(n.nodeId)) { anyUnassigned = true; break; }
        }
        if (!anyUnassigned) return;

        // determinar coste inicial anterior global (último coste calculado en la run)
        int lastGlobalCost = -1;
        if (runtimeCosts.Count > 0)
        {
            // tomar el último valor de runtimeCosts (no hay order intrínseco), preferimos usar spawnedFamilyOrder para hallar el último nodo calculado
            // buscamos la última familia del spawnedFamilyOrder que tenga nodos con runtimeCosts y tomamos su último nodo coste
            for (int i = spawnedFamilyOrder.Count - 1; i >= 0; i--)
            {
                string fam = spawnedFamilyOrder[i];
                // buscar si fam coincide con familyName: si es la misma familia entonces lastGlobalCost será computed en el bucle de abajo
                // buscamos nodos instanciados pertenecientes a esa familia en instantiatedButtonsPerContainer
                bool found = false;
                foreach (var kv in instantiatedButtonsPerContainer)
                {
                    var btns = kv.Value;
                    if (btns == null) continue;
                    foreach (var b in btns)
                    {
                        if (b == null || b.node == null) continue;
                        var root = GetFamilyRootForButton(b.transform);
                        if (root != null && root.name.Contains(fam))
                        {
                            if (runtimeCosts.TryGetValue(b.node.nodeId, out int c))
                            {
                                lastGlobalCost = c;
                                found = true;
                            }
                        }
                    }
                    if (found) break;
                }
                if (found) break;
            }
        }

        // Si no hay lastGlobalCost, intentamos tomar cualquier coste runtime existente
        if (lastGlobalCost < 0 && runtimeCosts.Count > 0)
        {
            lastGlobalCost = runtimeCosts.Values.Last();
        }

        // Si aún no hay coste anterior, tomaremos el primero definido por el scriptable o el default
        int previousCostInFamily = -1;
        for (int i = 0; i < family.nodes.Length; i++)
        {
            var node = family.nodes[i];
            if (node == null) continue;

            if (runtimeCosts.ContainsKey(node.nodeId)) // ya asignado, actualizar previousCostInFamily
            {
                previousCostInFamily = runtimeCosts[node.nodeId];
                continue;
            }

            int computed = 0;
            if (i == 0)
            {
                // primer nodo de la familia
                if (spawnedFamilyOrder.Count > 1)
                {
                    // hay familias previas: encadenar con el último coste global si existe
                    if (lastGlobalCost > 0)
                        computed = lastGlobalCost * 2;
                    else
                    {
                        // si no hay lastGlobalCost, usar node.cost o default
                        computed = (node.cost > 0) ? node.cost : defaultFirstNodeCost;
                    }
                }
                else
                {
                    // es la primera familia instanciada (o la lista spawnedFamilyOrder todavía tiene un único entry)
                    computed = (node.cost > 0) ? node.cost : defaultFirstNodeCost;
                }
            }
            else
            {
                // no es el primer nodo de la familia -> doble del anterior en esta familia
                if (previousCostInFamily > 0)
                    computed = previousCostInFamily * 2;
                else
                {
                    // fallback: usar node.cost o default
                    int basec = (node.cost > 0) ? node.cost : defaultFirstNodeCost;
                    computed = basec * (int)Mathf.Pow(2, i); // aproximación
                }
            }

            // evitar overflow y mantener dentro de int razonable
            if (computed < 0 || computed > int.MaxValue / 2) computed = int.MaxValue / 2;

            runtimeCosts[node.nodeId] = computed;
            previousCostInFamily = computed;
            lastGlobalCost = computed;
        }
    }
}
