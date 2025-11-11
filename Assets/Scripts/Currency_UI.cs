using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDController : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI currencyText;
    public Image currencyIcon;
    public Sprite currencySprite;

    [Header("Resources reference (optional)")]
    public PlayerResources Resources; // puedes asignarlo desde el inspector

    void Awake()
    {
        // Autovincula si no se asignó en el inspector
        if (Resources == null)
        {
            Resources = FindObjectOfType<PlayerResources>();
            if (Resources == null)
                Debug.LogWarning("[HUDController] No PlayerResources encontrado en la escena.");
            else
                Debug.Log("[HUDController] PlayerResources linked automatically: " + Resources.name);
        }
    }

    void OnEnable()
    {
        if (currencyIcon != null && currencySprite != null)
            currencyIcon.sprite = currencySprite;

        if (Resources != null)
        {
            StatsManager.Instance.OnCurrencyChanged += UpdateCurrencyDisplay;
            // Actualiza la UI inmediatamente con el valor actual
            UpdateCurrencyDisplay(StatsManager.Instance.RuntimeStats.currency);
        }
    }

    void OnDisable()
    {
        if (Resources != null)
            StatsManager.Instance.OnCurrencyChanged -= UpdateCurrencyDisplay;
    }

    // Método público usado como callback del evento
    void UpdateCurrencyDisplay(int newAmount)
    {
        if (currencyText == null)
        {
            Debug.LogWarning("[HUDController] currencyText no está asignado.");
            return;
        }

        currencyText.text = newAmount.ToString();
    }
}
