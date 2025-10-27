using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class KnifeProjectile2D : MonoBehaviour
{
    [Tooltip("Opcional: referencia al owner para evitar golpearse a sí mismo.")]
    public GameObject owner;

    [Tooltip("Tiempo en segundos antes de destruir el proyectil automáticamente.")]
    public float lifetime = 4f;
    private void Awake()
    {
        if (TryGetComponent<Rigidbody2D>(out var rb))
        {
            Debug.Log("[KnifeProjectile2D] Rigidbody2D found.");
        }
        else
        {
            Debug.LogWarning("[KnifeProjectile2D] No Rigidbody2D on prefab!");
        }

        var col = GetComponent<Collider2D>();
        if (col != null) Debug.Log("[KnifeProjectile2D] Collider2D found, isTrigger=" + col.isTrigger);
        else Debug.LogWarning("[KnifeProjectile2D] No Collider2D on prefab!");
    }
    private void Start()
    {
        // Destruir automáticamente después de 'lifetime' segundos
        Destroy(gameObject, lifetime);
    }
    private void OnEnable()
    {
        Debug.Log("[KnifeProjectile2D] enabled");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Ignorar al owner
        if (owner != null && other.gameObject == owner) return;

        if (other.gameObject.CompareTag("Ground"))
        {
            Debug.Log("KnifeProjectile2D: Impacto con suelo, destruyendo proyectil.");
            Destroy(gameObject);
        }
        else if (other.gameObject.CompareTag("enemigo"))
        {
            EnemyBase enemy = other.GetComponentInParent<EnemyBase>();
            if (enemy != null)
            {
                Debug.Log("KnifeProjectile2D: Impacto con enemigo, aplicando daño y destruyendo proyectil.");
                float dmg = StatsCommunicator.Instance.CalculateGunDamage();
                enemy.TakeContactDamage(dmg);
                Destroy(gameObject); // destruir al impactar con un enemigo
            }
        }
    }
}
