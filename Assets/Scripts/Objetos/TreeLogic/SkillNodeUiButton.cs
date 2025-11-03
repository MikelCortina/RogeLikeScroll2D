using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

[RequireComponent(typeof(CanvasGroup))]
public class SkillNodeButton : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
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
    private bool pointerOver = false;

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
            // Para el Image que controla el script
            if (iconImage != null)
                iconImage.sprite = node.icon;

            // Para el Image principal del botón (Source Image del prefab)
            var mainImage = GetComponent<Image>();
            if (mainImage != null)
                mainImage.sprite = node.icon;

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

    // ---- Pointer handlers para tooltip ----
    public void OnPointerEnter(PointerEventData eventData)
    {
        pointerOver = true;
        if (TooltipController.Instance != null)
            TooltipController.Instance.Show(node, eventData.position, treeUI);
    }

    public void OnPointerMove(PointerEventData eventData)
    {
        if (pointerOver && TooltipController.Instance != null)
            TooltipController.Instance.Reposition(eventData.position);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        pointerOver = false;
        if (TooltipController.Instance != null)
            TooltipController.Instance.Hide();
    }

  
    private void OnDisable()
    {
        if (TooltipController.Instance != null) TooltipController.Instance.Hide();
    }
}
