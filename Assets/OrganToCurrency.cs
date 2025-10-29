using UnityEngine;

public class OrganToCurrency : MonoBehaviour
{
    [Header("Configuraci�n")]
    public PlayerResources Resources; // referencia opcional; si no est� se intentar� encontrarla
    public int value = 1;
    public string pickupTag = "Horse"; // tag que dispara la recolecci�n (por defecto "Horse")

    void Awake()
    {
        if (Resources == null)
        {
            Resources = FindObjectOfType<PlayerResources>();
            if (Resources != null)
                Debug.Log("[OrganToCurrency] PlayerResources linked automatically: " + Resources.name);
            else
                Debug.LogWarning("[OrganToCurrency] No PlayerResources encontrado. Asigna la referencia en el inspector.");
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision == null) return;

        if (collision.gameObject.CompareTag(pickupTag))
        {
            if (Resources == null)
            {
                Debug.LogWarning("[OrganToCurrency] Intentando a�adir currency pero Resources es null.");
            }
            else
            {
                Resources.AddCurrency(value);
                Debug.Log($"[OrganToCurrency] A�adido {value} currency. Total ahora: {Resources.currency}");
            }

            // Destruye el objeto de forma segura
            Destroy(gameObject);
        }
    }
}
