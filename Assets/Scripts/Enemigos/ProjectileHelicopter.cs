using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class ProjectileHelicopter : MonoBehaviour
{
    public float speed = 8f;
    public float lifeTime = 5f;
    public GameObject owner;
    public float damageToDeal = 0f;
    public bool isHoming = true;
    public float turningSpeed = 720f;
    public float gravityScale = 1f;

    [Header("Colisión")]
    [SerializeField] private List<string> ignoreLayerNames = new List<string>();

    private Rigidbody2D rb;
    private Transform target;
    private Vector2 direction;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        rb.gravityScale = gravityScale;
        rb.freezeRotation = true;
        Destroy(gameObject, lifeTime);
    }

    public void Initialize(Transform targetTransform, float spd, GameObject ownerObj = null)
    {
        target = targetTransform;
        speed = spd;
        owner = ownerObj;

        if (target != null)
            direction = ((Vector2)target.position - (Vector2)transform.position).normalized;
        else
            direction = transform.right;

        rb.linearVelocity = direction * speed;

        float ang = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, ang);
    }

    private void FixedUpdate()
    {
        if (rb == null) return;

        if (!isHoming) return; // no homing, ya la parábola viene de rb.velocity
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (owner != null && other.gameObject == owner) return;

        // --- IGNORAR CAPAS DEFINIDAS EN LA LISTA ---
        string otherLayerName = LayerMask.LayerToName(other.gameObject.layer);
        if (ignoreLayerNames.Contains(otherLayerName))
            return;


        // Daño al jugador
        PlayerHealth playerHealth = other.GetComponentInParent<PlayerHealth>();
        if (playerHealth != null)
        {
            float damageToDeal = (owner != null && owner.TryGetComponent<EnemyHelicopter>(out var shooter))
     ? shooter.GetContactDamage()
     : 0f;
            Debug.Log("Proyectil enemigo golpea al jugador" + damageToDeal);
            playerHealth.TakeDamage(damageToDeal);
            Destroy(gameObject);
            return;

        }

        // Si choca con cualquier otra cosa sólida, se destruye
        Destroy(gameObject);
    }
}
