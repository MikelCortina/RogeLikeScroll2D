using UnityEngine;

public class InventoryTooltipController : MonoBehaviour
{
    public static InventoryTooltipController Instance { get; private set; }

    public GameObject tooltipPrefab;
    public Canvas uiCanvas;
    public RectTransform fixedParent;
    public Vector2 fixedOffset = new Vector2(20, -20);

    private GameObject tooltipObj;
    private InventoryTooltipView tooltipView;
    private RectTransform tooltipRect;


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

        if (fixedParent == null)
        {
            GameObject anchor = new GameObject("InventoryTooltipAnchor");
            anchor.transform.SetParent(uiCanvas.transform);
            fixedParent = anchor.AddComponent<RectTransform>();
            fixedParent.anchorMin = new Vector2(1, 1);
            fixedParent.anchorMax = new Vector2(1, 1);
            fixedParent.pivot = new Vector2(1, 1);
            fixedParent.anchoredPosition = fixedOffset;
        }
    }


    public void Show(IObjetos item)
    {
        if (item == null || tooltipPrefab == null) return;

        if (tooltipObj == null)
        {
            tooltipObj = Instantiate(tooltipPrefab, fixedParent);
            tooltipRect = tooltipObj.GetComponent<RectTransform>();
            tooltipView = tooltipObj.GetComponent<InventoryTooltipView>();
            tooltipObj.SetActive(false);
        }

        tooltipView.SetData(item);
        tooltipObj.SetActive(true);

        tooltipRect.SetAsLastSibling();
    }

    public void Hide()
    {
        if (tooltipObj != null)
            tooltipObj.SetActive(false);
    }
}
