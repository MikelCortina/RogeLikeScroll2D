using UnityEngine;



[RequireComponent(typeof(Rigidbody2D))]

public class KnifeProjectile2D : MonoBehaviour

{

    [Tooltip("Opcional: referencia al owner para evitar golpearse a sí mismo.")]

    public GameObject owner;



    [Tooltip("Tiempo en segundos antes de destruir el proyectil automáticamente.")]

    public float lifetime = 4f;



    private Rigidbody2D rb;

    private bool facingRight = true;



    private void Awake()

    {

        rb = GetComponent<Rigidbody2D>();

        if (rb != null)

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



    private void Update()

    {

        // Detectar dirección y voltear sprite si cambia

        if (rb != null)

        {

            if (rb.linearVelocity.x > 0 && !facingRight)

            {

                Flip();

            }

            else if (rb.linearVelocity.x < 0 && facingRight)

            {

                Flip();

            }

        }

    }



    private void Flip()

    {

        facingRight = !facingRight;

        Vector3 scale = transform.localScale;

        scale.x *= -1;

        transform.localScale = scale;

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

            }

        }

    }

}