using UnityEngine;

public class TooltipController : MonoBehaviour
{
    public static TooltipController Instance { get; private set; }

    [Header("Prefab & Canvas")]
    public GameObject tooltipPrefab;
    public Canvas uiCanvas;

    [Header("Posición FIJA en el Canvas")]
    public RectTransform fixedTooltipParent;     // Asigna aquí el panel/área donde quieres que aparezca (ej: un Empty con RectTransform)
    public Vector2 fixedAnchor = new Vector2(1, 0); // (1,0) = esquina inferior derecha, (0,1) = superior izquierda, etc.
    public Vector2 fixedOffset = new Vector2(-20, 20); // Offset desde la esquina (en píxeles)

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
        {
            Debug.LogError("[TooltipController] No Canvas found in scene.");
            return;
        }

        // Si no asignaste un padre fijo, usamos el canvas completo
        if (fixedTooltipParent == null)
        {
            GameObject go = new GameObject("TooltipFixedAnchor");
            go.transform.SetParent(uiCanvas.transform);
            fixedTooltipParent = go.AddComponent<RectTransform>();
            fixedTooltipParent.anchorMin = fixedAnchor;
            fixedTooltipParent.anchorMax = fixedAnchor;
            fixedTooltipParent.pivot = fixedAnchor;
            fixedTooltipParent.anchoredPosition = fixedOffset;
        }
    }

    public void Show(ItemNode node, Vector2 screenPosition, SkillTreeUI treeUI)
    {
        if (node == null || tooltipPrefab == null || uiCanvas == null) return;

        if (currentTooltip == null)
        {
            currentTooltip = Instantiate(tooltipPrefab, fixedTooltipParent, false);
            tooltipRect = currentTooltip.GetComponent<RectTransform>();
            tooltipView = currentTooltip.GetComponent<TooltipView>();

            // Aseguramos que el pivot y anchors estén bien para posicionamiento fijo
            tooltipRect.anchorMin = new Vector2(0, 1);  // Superior izquierda del padre
            tooltipRect.anchorMax = new Vector2(0, 1);
            tooltipRect.pivot = new Vector2(0, 1);
            tooltipRect.anchoredPosition = Vector2.zero;

            var cg = currentTooltip.GetComponent<CanvasGroup>();
            if (cg == null) cg = currentTooltip.AddComponent<CanvasGroup>();
            cg.blocksRaycasts = false;
            cg.interactable = false;
        }

        tooltipView.SetData(node, treeUI);
        currentTooltip.SetActive(true);

        // Ya no usamos Reposition con el ratón → posición fija
    }

    public void Hide()
    {
        if (currentTooltip != null)
            currentTooltip.SetActive(false);
    }


    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }
}