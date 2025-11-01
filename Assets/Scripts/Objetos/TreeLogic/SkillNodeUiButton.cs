using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]
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

    // Inicializa el botón con referencia al SkillTreeUI.
    // Esto fija el listener de botón de forma segura.
    public void Initialize(SkillTreeUI ui)
    {
        treeUI = ui;

        // proteger contra referencias nulas
        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }

        // actualizar visuales si ya hay un node asignado
        if (node != null)
        {
            if (iconImage != null) iconImage.sprite = node.icon;
            if (nameText != null) nameText.text = node.displayName;
            if (costText != null) costText.text = node.cost > 0 ? node.cost.ToString() : "";
        }

        UpdateState();
    }

    // Reasigna el node al botón (para instanciación dinámica)
    public void SetNode(ItemNode newNode)
    {
        node = newNode;

        if (node != null)
        {
            if (iconImage != null) iconImage.sprite = node.icon;
            if (nameText != null) nameText.text = node.displayName;
            if (costText != null) costText.text = node.cost > 0 ? node.cost.ToString() : "";
        }
        else
        {
            if (iconImage != null) iconImage.sprite = null;
            if (nameText != null) nameText.text = "";
            if (costText != null) costText.text = "";
        }

        UpdateState();
    }

    // Actualiza estado visual: lockedOverlay e interactable
    public void UpdateState()
    {
        if (node == null)
        {
            // si no hay node, desactivar canvas group para que no obtenga eventos
            var cg = GetComponent<CanvasGroup>();
            if (cg != null) cg.interactable = false;
            return;
        }

        if (treeUI == null)
        {
            Debug.LogWarning($"[SkillNodeButton] treeUI NULL para node {node.nodeId} ({gameObject.name})");
            return;
        }

        bool unlocked = treeUI.IsUnlocked(node.nodeId);
        bool canUnlock = treeUI.CanUnlock(node);

        // debug útil
        int playerCurrency = -999;
        try
        {
            playerCurrency = StatsManager.Instance != null ? StatsManager.Instance.RuntimeStats.currency : -1;
        }
        catch { playerCurrency = -999; }

        Debug.Log($"[SkillNodeButton] UpdateState -> node:{node.nodeId} unlocked:{unlocked} canUnlock:{canUnlock} cost:{node.cost} playerCurrency:{playerCurrency} onObject:{gameObject.name}");

        if (lockedOverlay != null)
            lockedOverlay.SetActive(!unlocked && !canUnlock);

        if (button != null)
            button.interactable = !unlocked && canUnlock;

        var cg2 = GetComponent<CanvasGroup>();
        if (cg2 != null) cg2.interactable = true;
    }

    private void OnClick()
    {
        if (treeUI == null)
        {
            Debug.LogWarning("[SkillNodeButton] OnClick: treeUI NULL");
            return;
        }
        if (node == null)
        {
            Debug.LogWarning("[SkillNodeButton] OnClick: node NULL");
            return;
        }

        if (!treeUI.CanUnlock(node))
        {
            string missingInfo = treeUI.GetMissingRequirements(node);
            Debug.Log($"[SkillTreeUI] No puede desbloquear {node.nodeId}. Faltan: {missingInfo}");
            return;
        }

        treeUI.TryUnlock(node);
    }
}
