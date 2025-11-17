using UnityEngine;
using TMPro;
using System.Collections;

public class CurrencyHUD : MonoBehaviour
{
    [Header("Referencias")]
    public TextMeshProUGUI currencyText; // Asigna el TextMeshProUGUI en la UI

    private void Awake()
    {
        if (StatsManager.Instance != null)
        {
            StatsManager.Instance.OnCurrencyChanged += UpdateCurrencyText;
            Debug.Log("Suscribiendo HUD a StatsManager: " + StatsManager.Instance);


            // Asegurar que el HUD muestra el valor actual aunque el evento ya se haya disparado antes
            UpdateCurrencyText(StatsManager.Instance.RuntimeStats.currency);
        }
    }
    private void Start()
    {
        // Inicializar el HUD con el valor actual
        if (StatsManager.Instance != null)
            currencyText.text = StatsManager.Instance.RuntimeStats.currency.ToString();
    }


    public void UpdateCurrencyText(int newAmount)
    {
        Debug.Log("Actualizando HUD de moneda: " + newAmount);
        if (currencyText == null) return;
        currencyText.text = newAmount.ToString();
    }

    private void OnDestroy()
    {
        // Desuscribirse para evitar errores
        if (StatsManager.Instance != null)
        {
            StatsManager.Instance.OnCurrencyChanged -= UpdateCurrencyText;
        }
    }
    private void OnEnable()
    {
        StartCoroutine(SubscribeWhenReady());
    }

    private IEnumerator SubscribeWhenReady()
    {
        // Esperar hasta que StatsManager exista
        while (StatsManager.Instance == null)
            yield return null;

        StatsManager.Instance.OnCurrencyChanged += UpdateCurrencyText;
        UpdateCurrencyText(StatsManager.Instance.RuntimeStats.currency);
    }

    private void OnDisable()
    {
        if (StatsManager.Instance != null)
            StatsManager.Instance.OnCurrencyChanged -= UpdateCurrencyText;
    }
}