using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using TMPro;

/// <summary>
/// SkillTreeUI - versión NON-PERSISTENT entre runs.
/// Cada Transform en familyContainers debe tener X hijos (ej: 3) que serán las posiciones "slots".
/// Cuando se spawnea una familia se crea un objeto raíz "family_inst_<container.name>_<family.name>" dentro del container.
/// Esto permite detectar ocupación y evitar duplicados / limpiar correctamente.
/// Añadido: soporte para "mixed nodes" (nodos mixtos) que requieren nodos finales de diferentes familias.
/// </summary>
public class SkillTreeUI : MonoBehaviour
{
    [Header("Prefabs & Containers")]
    public SkillNodeButton defaultNodeButtonPrefab;

    [Header("Plantilla de offsets (opcional)")]
    public List<Transform> familySlots = new List<Transform>(3); // opcional: para ajustar localPosition dentro del root

    [Header("Distribución de familias: cada Transform debe tener N hijos (ej: 3) que serán los slots")]
    public List<Transform> familyContainers = new List<Transform>();

    [Header("Families")]
    public List<SkillFamily> skillFamilies = new List<SkillFamily>();

    [Header("UI")]
    public TextMeshProUGUI titleText;

    // -------------------------
    // --- Spawn cost settings
    // -------------------------
    [Header("Spawn cost (para boton de crear nueva familia)")]
    [Tooltip("Coste base para spawnear la primera familia")]
    public int spawnBaseCost = 10;
    [Tooltip("Coste máximo (tope) para evitar overflow al elevar al cuadrado repetidamente)")]
    public double maxSpawnCost = 1000000000.0; // 1e9 por defecto

    // instancias actuales por contenedor (opcional tracking)
    private Dictionary<Transform, List<SkillNodeButton>> instantiatedButtonsPerContainer = new Dictionary<Transform, List<SkillNodeButton>>();

    // registro de nombres de familias activas para evitar duplicados
    private HashSet<string> activeFamilyNames = new HashSet<string>();

    private HashSet<string> unlocked = new HashSet<string>();
    private PlayerResources playerResources;
    public SkillTreePanZoom panZoom;

    private bool currencySubscribed = false;
    private Coroutine subscribeCoroutine;
    private HashSet<int> appliedEffectKeys = new HashSet<int>();

    private int nextContainerIndex = 0;

    private const string FAMILY_ROOT_PREFIX = "family_inst_";

    // --- spawn cost runtime state ---
    private double nextSpawnCost = 0.0; // el coste actual a pagar (puede ser fraccional pero se redondea al int para cobrar)
    private int spawnUses = 0; // cuántas veces se ha usado el spawn desde el inicio de la run

    // -------------------------
    // --- Mixed nodes feature
    // -------------------------
    [Header("Mixed Nodes (nodos mixtos)")]
    [Tooltip("Lista de definiciones de nodos mixtos. Cada entrada indica el ItemNode 'mixto' y la lista de nodeIds que requiere.")]
    public List<MixedNodeEntry> mixedNodes = new List<MixedNodeEntry>();

    // instancias creadas para nodos mixtos: key = mixedNode.nodeId
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
        // Si tienes un botón UI dedicado al spawn, puedes actualizar su interactable desde fuera usando IsAffordableNextSpawn()
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

        // Reset spawn cost state al inicio de la run
        spawnUses = 0;
        nextSpawnCost = Mathf.Max(1, spawnBaseCost);
    }

    // -------------------------
    // Unlock logic (runtime only)
    // -------------------------
    public bool IsUnlocked(string nodeId) => unlocked.Contains(nodeId);
    // Devuelve true si todas las familias de los botones requeridos están instanciadas en la escena
    private bool AllRequiredFamiliesPresent(List<SkillNodeButton> requiredButtons)
    {
        HashSet<Transform> familiesFound = new HashSet<Transform>();

        foreach (var btn in requiredButtons)
        {
            var root = GetFamilyRootForButton(btn.transform);
            if (root != null)
                familiesFound.Add(root);
        }

        // Deben ser al menos tantas familias distintas como nodos requeridos
        return familiesFound.Count >= 2; // aquí 2 porque los mixtos requieren nodos de familias distintas
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

        if (node.cost > 0)
        {
            if (playerResources == null)
                playerResources = GameObject.FindWithTag("Player")?.GetComponent<PlayerResources>();

            if (playerResources != null)
                return playerResources.HasCurrency(node.cost);

            if (StatsManager.Instance == null) return false;
            return StatsManager.Instance.RuntimeStats.currency >= node.cost;
        }

        return true;
    }

    public void TryUnlock(ItemNode node)
    {
        if (!CanUnlock(node)) return;

        if (playerResources != null && node.cost > 0) playerResources.SpendCurrency(node.cost);

        foreach (var p in node.requiredEffectIdsToRemove)
            RemoveEffect(p);

        if (StatsManager.Instance != null && node.cost > 0)
            StatsManager.Instance.AddCurrency(-node.cost);

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

    // antiguo fallback: instancia en this.transform usando familySlots localPositions
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

    // Nuevo: crea un objeto raíz dentro del container y coloca los botones con posiciones de los hijos (slots)
    // NO elimina una familia ya existente en el container: si está ocupado devuelve sin hacer nada.
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

        // si la familia ya está activa en cualquier contenedor, no la volvemos a spawnear
        if (activeFamilyNames.Contains(familyName))
        {
            Debug.Log($"[SkillTreeUI] Family '{familyName}' ya está activa en otra posición. Abortando spawn.");
            return;
        }

        // si el container ya contiene un family root, no lo reemplazamos
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

    // Devuelve el siguiente container que no tiene family_root (libre). Si ninguno libre, devuelve el primer container por round-robin.
    private Transform GetNextAvailableFamilyContainer()
    {
        if (familyContainers == null || familyContainers.Count == 0)
            return this.transform;

        foreach (var c in familyContainers)
        {
            if (c == null) continue;

            // comprobar si existe un family_inst_ dentro del container (ocupado)
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
                // comprobar que ninguno de sus hijos esté ocupado por botones (child.childCount > 0 y tenga SkillNodeButton)
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

        // round-robin fallback (si todos ocupados)
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

    // Intenta spawnear una familia aleatoria en el siguiente container libre.
    // No spawnea si la familia escogida ya está activa en otro sitio; intenta elegir otra familia no activa.
    // Nota: este método NO verifica ni descuenta currency. Usar TrySpawnRandomFamilyWithCost para spawn con coste.
    public void SpawnRandomFamilyInNextContainer()
    {
        if (skillFamilies == null || skillFamilies.Count == 0) return;

        Transform target = GetNextAvailableFamilyContainer();
        if (target == null) return;

        // crear lista de familias disponibles (no activas)
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

        // preferir familias no activas
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
        // asegurar inicialización
        if (nextSpawnCost <= 0) nextSpawnCost = Mathf.Max(1, spawnBaseCost);
        // redondear hacia arriba
        return Mathf.Clamp((int)Mathf.Ceil((float)nextSpawnCost), 0, int.MaxValue);
    }

    // PUBLIC API para UI: si el jugador puede permitirse el spawn ahora mismo.
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

    // Método que debes conectar al botón UI (OnClick): intentará cobrar y spawnear una familia aleatoria.
    // Si no hay moneda suficiente no hará nada.
    public void TrySpawnRandomFamilyWithCost()
    {
        // asegurar estado inicial del coste
        if (nextSpawnCost <= 0) nextSpawnCost = Mathf.Max(1, spawnBaseCost);

        int cost = GetNextSpawnCost();

        // comprobar si podemos pagar
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
            // podrías añadir aquí un feedback UI (popup, sonido, etc.)
            return;
        }

        // deducir moneda (primero playerResources si existe, y también StatsManager para mantener ambos sincronizados)
        if (playerResources != null)
            playerResources.SpendCurrency(cost);

        if (StatsManager.Instance != null)
            StatsManager.Instance.AddCurrency(-cost);

        // Hacer el spawn real
        SpawnRandomFamilyInNextContainer();

        // actualizar contador y elevar al cuadrado el siguiente coste
        spawnUses++;
        // cálculo: nextSpawnCost = min(nextSpawnCost ^ 2, maxSpawnCost)
        double newCost = nextSpawnCost * nextSpawnCost;
        if (double.IsInfinity(newCost) || newCost > maxSpawnCost)
            nextSpawnCost = maxSpawnCost;
        else
            nextSpawnCost = newCost;

        // forzar refresh de UI
        RefreshAllInstantiatedButtons();
        UpdateMixedNodesVisibility();
    }

    // Limpia SOLO los family_inst_ dentro del container (si existen). Si no existen, intenta limpiar botones sueltos.
    // NOTA: ahora esta función borra family roots y elimina de activeFamilyNames el nombre correspondiente.
    private void ClearInstantiatedButtonsInContainer(Transform container)
    {
        if (container == null) return;

        // destruir cualquier child que sea family_inst_...
        List<Transform> toDestroy = new List<Transform>();
        for (int i = 0; i < container.childCount; i++)
        {
            var child = container.GetChild(i);
            if (child.name.StartsWith(FAMILY_ROOT_PREFIX))
                toDestroy.Add(child);
        }

        foreach (var t in toDestroy)
        {
            // antes de destruir, desactivar efectos de botones que contenga
            var btns = t.GetComponentsInChildren<SkillNodeButton>();
            foreach (var b in btns)
            {
                if (b != null && b.node != null) DeactivateEffectInstance(b.node);
            }

            // intentar extraer el nombre de la familia del nombre del root para limpiar activeFamilyNames
            // formato: FAMILY_ROOT_PREFIX + container.name + "_" + familyName
            string rootName = t.name;
            var parts = rootName.Split('_');
            if (parts.Length >= 3)
            {
                // tomar todo lo que va después de los dos primeros guiones bajos como familyName (por si familyName contiene guiones)
                int firstUnderscore = rootName.IndexOf('_');
                int secondUnderscore = rootName.IndexOf('_', firstUnderscore + 1);
                if (secondUnderscore >= 0 && secondUnderscore + 1 < rootName.Length)
                {
                    string familyName = rootName.Substring(secondUnderscore + 1);
                    if (activeFamilyNames.Contains(familyName))
                        activeFamilyNames.Remove(familyName);
                }
            }

            Destroy(t.gameObject);
        }

        // limpiar registro en diccionario si existía
        if (instantiatedButtonsPerContainer.ContainsKey(container))
            instantiatedButtonsPerContainer.Remove(container);

        // si no había family_inst_, intentamos eliminar botones sueltos hijos del container (compatibilidad)
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

        // tras limpiar familias, actualizar nodos mixtos
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

        // tras refrescar, asegurar que los mixed nodes estén en la posición correcta
        UpdateMixedNodesVisibility();
    }

    // -------------------------
    // Mixed nodes helpers
    // -------------------------
    // Comprueba si un ItemNode es el "último" nodo de su familia (último no-nulo en el array)
    // Comprueba si un ItemNode es el "último" nodo de su familia (último no-nulo en el array)
    private bool IsLastNodeInItsFamily(ItemNode node)
    {
        if (node == null) return false;
        foreach (var fam in skillFamilies)
        {
            if (fam == null || fam.nodes == null || fam.nodes.Length == 0) continue;

            // buscar el último nodo no-nulo de la familia fam
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
        return false; // no se encontró en ninguna familia como último
    }

    // Obtiene el SkillNodeButton instanciado para un nodeId si existe (busca en instantiatedButtonsPerContainer)
    private SkillNodeButton FindInstantiatedButtonByNodeId(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId)) return null;

        // búsqueda rápida en registros por contenedor
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

        // fallback: búsqueda directa en escena (puede ocurrir si se instanciaron fuera de nuestro diccionario)
        var allButtons = GameObject.FindObjectsOfType<SkillNodeButton>();
        foreach (var b in allButtons)
        {
            if (b != null && b.node != null && b.node.nodeId == nodeId)
                return b;
        }

        return null;
    }

    // Dado un botón, sube por su jerarquía para encontrar el root de familia (nombre que empieza por FAMILY_ROOT_PREFIX).
    // Si no encuentra, devuelve el transform padre más cercano (o null).
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

    // Actualiza creación/posicionamiento de nodos mixtos según condiciones actuales.
    private void UpdateMixedNodesVisibility()
    {
        if (mixedNodes == null || mixedNodes.Count == 0)
        {
            // si no hay definiciones, destruir cualquier instancia previa
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
            // Si está ok, crear o posicionar
            if (allPresentAndLast)
            {
                // calcular punto medio de las posiciones world de botones requeridos
                Vector3 avgWorld = Vector3.zero;
                foreach (var rb in requiredButtons) avgWorld += rb.transform.position;
                avgWorld /= requiredButtons.Count;

                // aplicar offset vertical hacia arriba en el espacio local de this.transform
                Vector3 avgLocal = this.transform.InverseTransformPoint(avgWorld);
                avgLocal += Vector3.up * mixedVerticalOffset;

                // si ya existe instanciado, reposicionar; si no, instanciar
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
                    // crear instancia del botón mixto
                    var prefabToUse = entry.mixedNode.buttonPrefab != null ? entry.mixedNode.buttonPrefab : defaultNodeButtonPrefab;
                    if (prefabToUse == null) continue;

                    var inst = Instantiate(prefabToUse, this.transform, worldPositionStays: false);
                    inst.name = $"{prefabToUse.name}_mixed_inst_{mixedId}";
                    inst.SetNode(entry.mixedNode);
                    inst.Initialize(this);

                    inst.transform.localPosition = avgLocal;
                    inst.transform.localRotation = Quaternion.identity;
                    inst.transform.localScale = Vector3.one;

                    // registrar
                    instantiatedMixedNodes[mixedId] = inst;

                    // si el nodo ya está desbloqueado, aplicar su efecto
                    if (unlocked.Contains(mixedId))
                        ApplyEffect(entry.mixedNode);
                }
            }
            else
            {
                // si no se cumplen las condiciones, borrar instancia si existe
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

        // también limpiar cualquier instancia que ya no tenga definición válida en la lista
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
    // Utilities
    // -------------------------
    public string GetMissingRequirements(ItemNode node)
    {
        if (node == null) return "node null";
        var missing = new List<string>();
        if (node.prerequisiteNodeIds != null)
        {
            foreach (var p in node.prerequisiteNodeIds)
                if (!IsUnlocked(p)) missing.Add($"Requires node: {p}");
        }
        if (playerResources != null && !playerResources.HasCurrency(node.cost))
            missing.Add($"Need {node.cost} currency");
        return missing.Count == 0 ? "None" : string.Join(", ", missing);
    }
}
