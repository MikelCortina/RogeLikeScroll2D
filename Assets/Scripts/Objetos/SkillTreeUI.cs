using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class SkillTreeUI : MonoBehaviour
{
    [Header("UI")]
    public List<SkillNodeButton> nodeButtons; // todos los botones que colocaste en el panel
    public TextMeshProUGUI titleText;

    private HashSet<string> unlocked = new HashSet<string>();
    private const string SAVE_KEY = "SkillTreeUnlocked_v2";

    private PlayerResources playerResources;

    private void Awake()
    {
        playerResources = GameObject.FindWithTag("Player")?.GetComponent<PlayerResources>();
    }

    private void Start()
    {
        // Si nodeButtons no está asignado por el inspector, buscar en hijos
        if (nodeButtons == null || nodeButtons.Count == 0)
        {
            nodeButtons = new List<SkillNodeButton>(GetComponentsInChildren<SkillNodeButton>(true));
            Debug.Log($"[SkillTreeUI] nodeButtons rellenado dinámicamente: {nodeButtons.Count}");
        }

        // Inicializar siempre (idempotente)
        foreach (var b in nodeButtons)
        {
            if (b == null) continue;
            b.Initialize(this);
        }

        // Primero actualizar la UI para que los nodos se vean desbloqueados
        RefreshAllButtons();

        // Después aplicar efectos de nodos ya desbloqueados
        ApplyUnlockedEffects();
    }
    private void OnEnable()
    {
        // Cada vez que se activa el panel nos aseguramos que el estado de los botones está actualizado
        RefreshAllButtons();
        Debug.Log("[SkillTreeUI] OnEnable -> RefreshAllButtons");
    }

    public bool IsUnlocked(string nodeId) => unlocked.Contains(nodeId);

    public bool CanUnlock(ItemNode node)
    {
        if (node == null || IsUnlocked(node.nodeId)) return false;

        // prereqs
        foreach (var p in node.prerequisiteNodeIds)
            if (!IsUnlocked(p)) return false;

        // currency
        if (playerResources != null && !playerResources.HasCurrency(node.cost)) return false;

        // items
        if (playerResources != null && !playerResources.HasItems(node.requiredItemIds)) return false;

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
        if (playerResources != null && node.requiredItemIds.Count > 0) playerResources.ConsumeItems(node.requiredItemIds);

        unlocked.Add(node.nodeId);
        Save();

        // actualizar todos los botones
        RefreshAllButtons();

        // activar efecto
        ApplyEffect(node);

        Debug.Log($"[SkillTreeUI] Nodo desbloqueado: {node.nodeId}");
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

    // items
    if (playerResources != null)
    {
        foreach (var id in node.requiredItemIds)
        {
            if (!playerResources.HasItems(new System.Collections.Generic.List<string> { id }))
                missing.Add($"Need item: {id} x1");
        }
    }

    return missing.Count == 0 ? "None" : string.Join(", ", missing);
}


}
