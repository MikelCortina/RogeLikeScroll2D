using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SkillTreeUI : MonoBehaviour
{
    [Header("UI")]
    public List<SkillNodeButton> nodeButtons; // todos los botones que colocaste en el panel
    public TextMeshProUGUI titleText;

    private HashSet<string> unlocked = new HashSet<string>();
    private const string SAVE_KEY = "SkillTreeUnlocked_v2";

    private PlayerResources playerResources;

    public SkillTreePanZoom panZoom;

    // control para evitar doble suscripción
    private bool currencySubscribed = false;
    private Coroutine subscribeCoroutine;

    private void Awake()
    {
        playerResources = GameObject.FindWithTag("Player")?.GetComponent<PlayerResources>();
        Debug.Log($"[SkillTreeUI] Awake/Start llamado en {gameObject.name}. Instancias vivas: {FindObjectsOfType<SkillTreeUI>().Length}");

    }

    private IEnumerator Start()
    {
        // Espera un frame para asegurarte de que StatsManager y PlayerResources estén listos
        yield return null;

        if (nodeButtons == null || nodeButtons.Count == 0)
            nodeButtons = new List<SkillNodeButton>(GetComponentsInChildren<SkillNodeButton>(true));

        foreach (var b in nodeButtons)
            b.Initialize(this);

        RefreshAllButtons();
        ApplyUnlockedEffects();
    }

    private void OnEnable()
    {
        // refrescar UI al activarse
        RefreshAllButtons();
        Debug.Log("[SkillTreeUI] OnEnable -> RefreshAllButtons");

        // lanzar coroutine para suscribir al evento cuando StatsManager esté listo
        if (subscribeCoroutine == null)
            subscribeCoroutine = StartCoroutine(SubscribeToCurrencyWhenReady());
    }

    private void OnDisable()
    {
        // anular suscripción segura
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
        // espera hasta que exista StatsManager.Instance
        while (StatsManager.Instance == null)
            yield return null;

        if (!currencySubscribed)
        {
            StatsManager.Instance.OnCurrencyChanged += OnCurrencyChanged;
            currencySubscribed = true;
            // forzar un refresh con el valor actual
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
        Debug.Log($"[SkillTreeUI] OnCurrencyChanged -> {newCurrency} (refresh {nodeButtons.Count} botones)");
        RefreshAllButtons();
    }

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
            Debug.Log($"[SkillTreeUI] CanUnlock: {node.nodeId} ya desbloqueado");
            return false;
        }

        // prereqs
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

        // currency
        if (node.cost > 0)
        {
            // intentar asegurar playerResources
            if (playerResources == null)
            {
                playerResources = GameObject.FindWithTag("Player")?.GetComponent<PlayerResources>();
                if (playerResources == null)
                {
                    Debug.LogWarning($"[SkillTreeUI] CanUnlock: playerResources NO encontrado. Usando StatsManager como respaldo para comprobar currency para {node.nodeId}");
                }
            }

            // Primer intento: preguntar a playerResources si existe
            if (playerResources != null)
            {
                if (!playerResources.HasCurrency(node.cost))
                {
                    Debug.Log($"[SkillTreeUI] CanUnlock: {node.nodeId} -> playerResources dice que NO hay suficiente currency (cost {node.cost})");
                    return false;
                }
            }
            else
            {
                // Respaldo: usar StatsManager directamente si existe
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

        // todo ok
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

            unlocked.Add(node.nodeId);
        Save();

        // actualizar todos los botones
        RefreshAllButtons();

        // activar efecto
        ApplyEffect(node);

        Debug.Log($"[SkillTreeUI] Nodo desbloqueado: {node.nodeId}");
        Debug.Log("Restante"+StatsManager.Instance.RuntimeStats.currency);
    }

    private void ApplyEffect(ItemNode node)
    {
        if (node.effectToActivate == null) return;

        // efecto global
        RunEffectManager.Instance?.ActivateEffect(node.effectToActivate);

        // efecto persistente
        if (node.effectToActivate is IPersistentEffect persistent)
        {
            var player = GameObject.FindWithTag("Player");
            if (player != null) persistent.ApplyTo(player);
        }
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

    public void Save()
    {
        string data = string.Join(",", unlocked);
        PlayerPrefs.SetString(SAVE_KEY, data);
        PlayerPrefs.Save();
        Debug.Log($"[SkillTreeUI] Guardado {data}");
    }

    public void Load()
    {
        unlocked.Clear();
        string data = PlayerPrefs.GetString(SAVE_KEY, "");
        if (!string.IsNullOrEmpty(data))
        {
            var parts = data.Split(',');
            foreach (var p in parts) if (!string.IsNullOrEmpty(p)) unlocked.Add(p);
        }
        Debug.Log($"[SkillTreeUI] Load -> unlocked count: {unlocked.Count}");
    }

    private void ApplyUnlockedEffects()
    {
        var player = GameObject.FindWithTag("Player");
        if (player == null) return;

        foreach (var b in nodeButtons)
        {
            if (b.node == null) continue;
            if (!unlocked.Contains(b.node.nodeId)) continue;

            ApplyEffect(b.node);
        }
    }

    public void Show(bool show)
    {
        gameObject.SetActive(show);
        Time.timeScale = show ? 0f : 1f;
        RefreshAllButtons();

        if (panZoom != null)
            panZoom.enabled = show; // activar/desactivar control de cámara

        Debug.Log($"[SkillTreeUI] Show({show}) called");
    }

    private void RefreshAllButtons()
    {
        if (nodeButtons == null || nodeButtons.Count == 0)
        {
            nodeButtons = new List<SkillNodeButton>(GetComponentsInChildren<SkillNodeButton>(true));
            Debug.Log($"[SkillTreeUI] Refresh - rellenando nodeButtons: {nodeButtons.Count}");
        }
        foreach (var b in nodeButtons)
        {
            if (b == null) continue;
            b.UpdateState();
        }
    }
    public string GetMissingRequirements(ItemNode node)
    {
        if (node == null) return "node null";

        var missing = new System.Collections.Generic.List<string>();

        // prereqs
        foreach (var p in node.prerequisiteNodeIds)
        {
            if (!IsUnlocked(p)) missing.Add($"Requires node: {p}");
        }

        // currency
        if (playerResources != null && !playerResources.HasCurrency(node.cost))
        {
            missing.Add($"Need {node.cost} currency");
        }


        return missing.Count == 0 ? "None" : string.Join(", ", missing);
    }
}
