using UnityEngine;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(RectTransform))]
public class TooltipView : MonoBehaviour
{
    [Header("UI Elements")]
    public TextMeshProUGUI titleText;
    public TextMeshProUGUI descriptionText;
    public TextMeshProUGUI costText;

    [Header("Optional")]
    public CanvasGroup canvasGroup;

    public void SetData(ItemNode node, SkillTreeUI treeUI)
    {
    

        // Título
        if (titleText != null)
        {
            string displayName = node != null
                ? (string.IsNullOrEmpty(node.displayName) ? node.nodeId : node.displayName)
                : "";
            titleText.text = displayName;
        }

        // Descripción
        if (descriptionText != null)
            descriptionText.text = node != null ? (node.description ?? "") : "";

        // Coste (usa coste efectivo si hay treeUI, si no el del nodo)
        int effectiveCost = 0;
        if (node != null)
        {
            if (treeUI != null)
                effectiveCost = Mathf.Max(0, treeUI.GetNodeEffectiveCost(node));
            else
                effectiveCost = node.cost > 0 ? node.cost : 0;
        }

        if (costText != null)
            costText.text = "Price_"+$"{effectiveCost}" ;
    }
}