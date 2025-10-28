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
        if (node == null || treeUI == null) return;

        bool unlocked = treeUI.IsUnlocked(node.nodeId);
        bool canUnlock = treeUI.CanUnlock(node);

        // Si está desbloqueado, ocultar overlay. Si no, mostrar overlay solo si NO se puede desbloquear.
        lockedOverlay.SetActive(!unlocked && !canUnlock);

        // El botón será interactuable solo si se puede desbloquear y no está desbloqueado
        button.interactable = !unlocked && canUnlock;
    }


    private void OnClick()
    {
        if (treeUI != null && node != null)
        {   
            treeUI.TryUnlock(node);
        }
    }
}
