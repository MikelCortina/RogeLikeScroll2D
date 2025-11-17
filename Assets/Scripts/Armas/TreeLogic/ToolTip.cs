using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TooltipController : MonoBehaviour
{
    public static TooltipController Instance { get; private set; }

    [Header("Prefab & Canvas")]
    [Tooltip("Prefab con TooltipView componente")]
    public GameObject tooltipPrefab;
    [Tooltip("Canvas donde se instanciará el tooltip. Si está vacío, buscará el primer Canvas activo.")]
    public Canvas uiCanvas;

    [Header("Positioning")]
    public Vector2 screenOffset = new Vector2(16f, -16f);

    private RectTransform canvasRect;
    private GameObject currentTooltip;
    private RectTransform tooltipRect;
    private TooltipView tooltipView;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        if (uiCanvas == null)
            uiCanvas = FindObjectOfType<Canvas>();

        if (uiCanvas == null)
            Debug.LogError("[TooltipController] No Canvas found in scene. Assign uiCanvas in inspector.");

        if (uiCanvas != null)
            canvasRect = uiCanvas.GetComponent<RectTransform>();
    }

    public void Show(ItemNode node, Vector2 screenPosition, SkillTreeUI treeUI)
    {
        if (node == null || tooltipPrefab == null || uiCanvas == null) return;

        if (currentTooltip == null)
        {
            currentTooltip = Instantiate(tooltipPrefab, uiCanvas.transform, false);
            tooltipRect = currentTooltip.GetComponent<RectTransform>();
            tooltipView = currentTooltip.GetComponent<TooltipView>();

            var cg = currentTooltip.GetComponent<CanvasGroup>();
            if (cg == null) cg = currentTooltip.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;
        }

        if (tooltipView != null) tooltipView.SetData(node, treeUI);

        currentTooltip.SetActive(true);
        Reposition(screenPosition);
    }

    public void Hide()
    {
        if (currentTooltip != null)
            currentTooltip.SetActive(false);
    }

    public void Reposition(Vector2 screenPosition)
    {
        if (currentTooltip == null || tooltipRect == null || canvasRect == null) return;

        Vector2 anchored;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPosition + screenOffset, uiCanvas.worldCamera, out anchored);
        tooltipRect.anchoredPosition = anchored;

        // Clamp to canvas so tooltip doesn't escape screen
        Vector2 tooltipSize = tooltipRect.rect.size;
        Vector2 canvasSize = canvasRect.rect.size;

        Vector2 pivot = tooltipRect.pivot;
        Vector2 leftTop = new Vector2(anchored.x - pivot.x * tooltipSize.x, anchored.y + (1f - pivot.y) * tooltipSize.y);
        Vector2 rightBottom = leftTop + new Vector2(tooltipSize.x, -tooltipSize.y);

        Vector2 clamped = anchored;

        // Horizontal
        if (leftTop.x < -canvasSize.x / 2f) clamped.x += (-canvasSize.x / 2f - leftTop.x);
        if (rightBottom.x > canvasSize.x / 2f) clamped.x -= (rightBottom.x - canvasSize.x / 2f);

        // Vertical
        if (rightBottom.y < -canvasSize.y / 2f) clamped.y += (-canvasSize.y / 2f - rightBottom.y);
        if (leftTop.y > canvasSize.y / 2f) clamped.y -= (leftTop.y - canvasSize.y / 2f);

        tooltipRect.anchoredPosition = clamped;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}
