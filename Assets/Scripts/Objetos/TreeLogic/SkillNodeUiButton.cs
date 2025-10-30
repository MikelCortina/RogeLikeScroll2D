using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SkillNodeButton : MonoBehaviour
{
    [Header("UI Components")]
    public Image iconImage;
    public TextMeshProUGUI nameText;
    public TextMeshProUGUI costText;
    public GameObject lockedOverlay;
    public Button button;

    [Header("Node Data")]
    public ItemNode node;

    private SkillTreeUI treeUI;


    // Inicializa el nodo desde SkillTreeUI
    public void Initialize(SkillTreeUI ui)
    {
        treeUI = ui;
        if (node != null)
        {
            iconImage.sprite = node.icon;
            nameText.text = node.displayName;
            costText.text = node.cost > 0 ? node.cost.ToString() : "";
        }

        button.onClick.RemoveAllListeners();
        button.onClick.AddListener(OnClick);

        UpdateState();
    }


    public void UpdateState()
    {
        if (node == null)
        {
            Debug.LogWarning($"[SkillNodeButton] UpdateState: node NULL en {gameObject.name}");
            return;
        }
        if (treeUI == null)
        {
            Debug.LogWarning($"[SkillNodeButton] UpdateState: treeUI NULL para node {node.nodeId} ({gameObject.name})");
            return;
        }

        bool unlocked = treeUI.IsUnlocked(node.nodeId);
        bool canUnlock = treeUI.CanUnlock(node);

        // debug completo
        int playerCurrency = -999;
        try
        {
            playerCurrency = StatsManager.Instance != null ? StatsManager.Instance.RuntimeStats.currency : -1;
        }
        catch { playerCurrency = -999; }

        Debug.Log($"[SkillNodeButton] UpdateState -> node:{node.nodeId} unlocked:{unlocked} canUnlock:{canUnlock} cost:{node.cost} playerCurrency:{playerCurrency} lockedOverlayAssigned:{(lockedOverlay != null)} buttonAssigned:{(button != null)} onObject:{gameObject.name}");

        // protección por si faltan referencias
        if (lockedOverlay == null)
        {
            Debug.LogWarning($"[SkillNodeButton] lockedOverlay NO asignado en inspector para {node.nodeId} ({gameObject.name})");
        }
        else
        {
            lockedOverlay.SetActive(!unlocked && !canUnlock);
        }

        if (button == null)
        {
            Debug.LogWarning($"[SkillNodeButton] button NO asignado en inspector para {node.nodeId} ({gameObject.name})");
        }
        else
        {
            button.interactable = !unlocked && canUnlock;
        }
    }


    private void OnClick()
    {
        if (treeUI != null && node != null)
        {
            if (!treeUI.CanUnlock(node))
            {
                // Mostrar requisitos faltantes en el debug
                string missingInfo = treeUI.GetMissingRequirements(node);
                Debug.Log($"[SkillTreeUI] No puede desbloquear {node.nodeId}. Faltan: {missingInfo}");
                return;
            }

            treeUI.TryUnlock(node);
        }
    }

}
