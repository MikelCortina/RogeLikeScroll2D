using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

/// <summary>
/// SkillTreeUI - versión NON-PERSISTENT entre runs.
/// Todos los unlocks son de la run actual; no se usa PlayerPrefs.
/// Llama a StartNewRun() desde tu RunManager cuando comience una nueva run
/// (o si recargas escena, Start() ya inicializa limpio).
/// </summary>
public class SkillTreeUI : MonoBehaviour
{
    [Header("Prefabs & Containers")]
    public SkillNodeButton defaultNodeButtonPrefab;
    public Transform buttonsContainer;

    [Header("Family placement slots (orden = nodo 0,1,2)")]
    public List<Transform> familySlots = new List<Transform>(3);

    [Header("Families")]
    public List<SkillFamily> skillFamilies = new List<SkillFamily>();

    [Header("UI")]
    public TextMeshProUGUI titleText;

    // instancias actuales en la UI (se destruyen/limpian entre familias / runs)
    private List<SkillNodeButton> instantiatedButtons = new List<SkillNodeButton>();

    // Nodos desbloqueados EN LA RUN ACTUAL (no se guardan en disco)
    private HashSet<string> unlocked = new HashSet<string>();

    private PlayerResources playerResources;
    public SkillTreePanZoom panZoom;

    // suscripción a currency
    private bool currencySubscribed = false;
    private Coroutine subscribeCoroutine;

    // Seguimiento de efectos aplicados durante la run (para poder desactivarlos al acabar la run)
    private HashSet<int> appliedEffectKeys = new HashSet<int>();

    private void Awake()
    {
        playerResources = GameObject.FindWithTag("Player")?.GetComponent<PlayerResources>();
        Debug.Log($"[SkillTreeUI] Awake en {gameObject.name}");
    }

    private IEnumerator Start()
    {
        // asegurar que managers existen
        yield return null;

        // Iniciar run limpio (por si acaso)
        StartNewRun(internalCall: true);

        // Mostrar una familia inicial (opcional)
        ShowRandomFamily();

        // Suscribir a currency
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
            Debug.Log("[SkillTreeUI] Suscrito a OnCurrencyChanged");
        }

        subscribeCoroutine = null;
    }

    private void UnsubscribeCurrency()
    {
        if (currencySubscribed && StatsManager.Instance != null)
        {
            StatsManager.Instance.OnCurrencyChanged -= OnCurrencyChanged;
            Debug.Log("[SkillTreeUI] Desuscrito de OnCurrencyChanged");
        }
        currencySubscribed = false;
    }

    private void OnCurrencyChanged(int newCurrency)
    {
        Debug.Log($"[SkillTreeUI] OnCurrencyChanged -> {newCurrency} (refresh {instantiatedButtons.Count} botones)");
        RefreshAllInstantiatedButtons();
    }

    // -------------------------
    // Run lifecycle
    // -------------------------
    /// <summary>
    /// Resetea todo lo relativo a la run actual: quita efectos aplicados por la run anterior,
    /// limpia unlocked, destruye botones. Llama a esto desde tu RunManager cuando empieces una run nueva.
    /// Si start llama a esto con internalCall=true lo hace sin log extra.
    /// </summary>
    public void StartNewRun(bool internalCall = false)
    {
        // desactivar todos los efectos que se aplicaron durante la run anterior
        // (usamos appliedEffectKeys: no necesitamos la referencia directa a cada objeto)
        // Para seguridad intentamos también desactivar cualquier efecto por cada nodo conocido.
        foreach (var family in skillFamilies)
        {
            foreach (var n in family.nodes)
            {
                if (n == null) continue;
                
                    DeactivateEffectInstance(n);
              
            }
        }

        // limpiar tracking
        appliedEffectKeys.Clear();
        unlocked.Clear();

        // limpiar UI
        ClearInstantiatedButtons();
        instantiatedButtons.Clear();

        if (!internalCall)
            Debug.Log("[SkillTreeUI] StartNewRun: runtime unlocked & effects limpiados");
    }

    // -------------------------
    // Unlock logic (runtime only)
    // -------------------------
    public bool IsUnlocked(string nodeId) => unlocked.Contains(nodeId);

    public bool CanUnlock(ItemNode node)
    {
        if (node == null)
        {
            Debug.LogWarning("[SkillTreeUI] CanUnlock: node es null");
            return false;
        }

        if (IsUnlocked(node.nodeId))
        {
            Debug.Log($"[SkillTreeUI] CanUnlock: {node.nodeId} ya desbloqueado (run)");
            return false;
        }

        // prereqs (considera únicamente los unlocks de la run actual)
        if (node.prerequisiteNodeIds != null && node.prerequisiteNodeIds.Count > 0)
        {
            foreach (var p in node.prerequisiteNodeIds)
            {
                if (!IsUnlocked(p))
                {
                    Debug.Log($"[SkillTreeUI] CanUnlock: {node.nodeId} -> falta prereq {p}");
                    return false;
                }
            }
        }

        // currency check (preferir PlayerResources si está presente)
        if (node.cost > 0)
        {
            if (playerResources == null)
            {
                playerResources = GameObject.FindWithTag("Player")?.GetComponent<PlayerResources>();
                if (playerResources == null)
                {
                    Debug.LogWarning($"[SkillTreeUI] CanUnlock: playerResources NO encontrado. Usando StatsManager como respaldo para {node.nodeId}");
                }
            }

            if (playerResources != null)
            {
                if (!playerResources.HasCurrency(node.cost))
                {
                    Debug.Log($"[SkillTreeUI] CanUnlock: {node.nodeId} -> playerResources dice NO hay suficiente currency ({node.cost})");
                    return false;
                }
            }
            else
            {
                if (StatsManager.Instance == null)
                {
                    Debug.LogWarning($"[SkillTreeUI] CanUnlock: ni playerResources ni StatsManager disponibles para comprobar currency de {node.nodeId}");
                    return false;
                }

                int current = StatsManager.Instance.RuntimeStats.currency;
                if (current < node.cost)
                {
                    Debug.Log($"[SkillTreeUI] CanUnlock: {node.nodeId} -> StatsManager currency insuficiente ({current} < {node.cost})");
                    return false;
                }
            }
        }

        return true;
    }

    public void TryUnlock(ItemNode node)
    {
        if (!CanUnlock(node))
        {
            Debug.Log($"[SkillTreeUI] No puede desbloquear {node?.nodeId}");
            return;
        }

        // consumir recursos
        if (playerResources != null && node.cost > 0) playerResources.SpendCurrency(node.cost);

        foreach (var p in node.requiredEffectIdsToRemove)
        {
            RemoveEffect(p);
        }
        // consumir recursos
        if (StatsManager.Instance != null && node.cost > 0)
        {
            StatsManager.Instance.AddCurrency(-node.cost);
        }
        else
        {
            Debug.LogWarning("[SkillTreeUI] TryUnlock: StatsManager.Instance es NULL, no se puede gastar currency");
        }


        // activar efecto
        ApplyEffect(node);

        Debug.Log($"[SkillTreeUI] Nodo desbloqueado: {node.nodeId}");
        Debug.Log("Restante" + StatsManager.Instance.RuntimeStats.currency);

        // registrar en el runtime-only unlocked set
        unlocked.Add(node.nodeId);

        // refrescar UI y aplicar efecto
        RefreshAllInstantiatedButtons();
        ApplyEffect(node);

        Debug.Log($"[SkillTreeUI] Nodo desbloqueado en run: {node.nodeId}");
        if (StatsManager.Instance != null)
            Debug.Log("Restante " + StatsManager.Instance.RuntimeStats.currency);
    }
    private void RemoveEffect(ItemNode node)
    {
        if (node.effectToActivate == null) return;

        // eliminar efecto global
        RunEffectManager.Instance?.DeactivateEffect(node.effectToActivate);

        // eliminar efecto persistente
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

        // activar globalmente por RunEffectManager si aplica
        RunEffectManager.Instance?.ActivateEffect(node.effectToActivate);

        // si es persistente (IPersistentEffect) aplicarlo al player pero solo una vez por instancia
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
        int key = GetEffectKey(node);
        if (!appliedEffectKeys.Contains(key)) return;

        // desactivar globalmente
        RunEffectManager.Instance?.DeactivateEffect(node);

        // si es persistente, quitar del player
        if (node is IPersistentEffect persistent)
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
    // Families & instanciado UI
    // -------------------------
    public void Show(bool show)
    {
        gameObject.SetActive(show);
        Time.timeScale = show ? 0f : 1f;
        RefreshAllInstantiatedButtons();

        if (panZoom != null)
            panZoom.enabled = show;

        Debug.Log($"[SkillTreeUI] Show({show}) called");
    }

    public void ShowRandomFamily()
    {
        if (skillFamilies == null || skillFamilies.Count == 0)
        {
            Debug.LogWarning("[SkillTreeUI] No hay skillFamilies definidas");
            return;
        }
        int idx = Random.Range(0, skillFamilies.Count);
        ShowFamily(skillFamilies[idx]);
    }

    public void ShowFamily(SkillFamily family)
    {
        if (family == null)
        {
            Debug.LogWarning("[SkillTreeUI] ShowFamily: family null");
            return;
        }
        if (familySlots == null || familySlots.Count == 0)
        {
            Debug.LogWarning("[SkillTreeUI] ShowFamily: familySlots no asignados");
            return;
        }

        ClearInstantiatedButtons();

        for (int i = 0; i < familySlots.Count; i++)
        {
            Transform slot = familySlots[i];
            ItemNode node = (i < family.nodes.Length) ? family.nodes[i] : null;
            if (node == null) continue;

            SkillNodeButton prefabToUse = node.buttonPrefab != null ? node.buttonPrefab : defaultNodeButtonPrefab;
            if (prefabToUse == null)
            {
                Debug.LogWarning($"[SkillTreeUI] No hay prefab para node {node.nodeId} y no hay default asignado.");
                continue;
            }

            Transform parent = slot != null ? slot : (buttonsContainer != null ? buttonsContainer : this.transform);

            SkillNodeButton inst = Instantiate(prefabToUse, parent, worldPositionStays: false);
            inst.name = $"{prefabToUse.name}_inst_{node.nodeId}";
            inst.SetNode(node);
            inst.Initialize(this);

            inst.transform.localPosition = Vector3.zero;
            inst.transform.localRotation = Quaternion.identity;
            inst.transform.localScale = Vector3.one;

            instantiatedButtons.Add(inst);
        }

        RefreshAllInstantiatedButtons();

        // aplicar efectos de nodos que ya están desbloqueados en esta run y ahora aparecen en la familia
        foreach (var btn in instantiatedButtons)
        {
            if (btn == null || btn.node == null) continue;
            if (!unlocked.Contains(btn.node.nodeId)) continue;
            ApplyEffect(btn.node);
        }
    }

    private void ClearInstantiatedButtons()
    {
        // antes de destruir, si el botón tiene un efecto que fue aplicado y el nodo NO está desbloqueado
        // en la run (por ejemplo visual temporal), lo desactivamos.
        for (int i = instantiatedButtons.Count - 1; i >= 0; i--)
        {
            var b = instantiatedButtons[i];
            if (b == null)
            {
                instantiatedButtons.RemoveAt(i);
                continue;
            }

            var node = b.node;
            if (node != null)
            {
                // si el nodo NO está desbloqueado (querrás desactivar), o si simplemente limpiamos,
                // llamamos a DeactivateEffectInstance para asegurar que no quede nada aplicado.
                DeactivateEffectInstance(node);
            }

            Destroy(b.gameObject);
            instantiatedButtons.RemoveAt(i);
        }

        instantiatedButtons.Clear();
    }

    private void RefreshAllInstantiatedButtons()
    {
        for (int i = 0; i < instantiatedButtons.Count; i++)
        {
            var b = instantiatedButtons[i];
            if (b == null) continue;
            b.UpdateState();
        }
    }

    // -------------------------
    // Utilities
    // -------------------------
    public string GetMissingRequirements(ItemNode node)
    {
        if (node == null) return "node null";

        var missing = new System.Collections.Generic.List<string>();

        if (node.prerequisiteNodeIds != null)
        {
            foreach (var p in node.prerequisiteNodeIds)
            {
                if (!IsUnlocked(p)) missing.Add($"Requires node: {p}");
            }
        }

        if (playerResources != null && !playerResources.HasCurrency(node.cost))
        {
            missing.Add($"Need {node.cost} currency");
        }

        return missing.Count == 0 ? "None" : string.Join(", ", missing);
    }
}
