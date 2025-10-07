// AreaShooter2D.cs
// Coloca este script en el GameObject que dispara (por ejemplo un enemigo, torre o jugador).
// Requisitos en el inspector:
// - firePoint: Transform desde donde salen los proyectiles.
// - projectilePrefab: Prefab con el script Projectile2D y un Rigidbody2D.
// - radius: radio de detecci�n (se dibuja con gizmos).
// - fireRate: disparos por segundo (cada intervalo dispara a todos los enemigos detectados).
// - useLayerMask (opcional): si quieres optimizar por capa. Si est� vac�o, filtra por tag "enemigo".

using UnityEngine;

public class AreaShooter2D : MonoBehaviour
{
    [Header("Disparo")]
    public Transform firePoint;
    public GameObject projectilePrefab;
    [Tooltip("Velocidad que recibir� el proyectil (se pasa al prefab).")]
    public float projectileSpeed = 8f;
    [Tooltip("Veces por segundo que se realiza un 'barrido' y se dispara a todos los enemigos en radio.")]
    public float fireRate = 1f;

    [Header("Detecci�n")]
    [Tooltip("Radio de detecci�n (Gizmos).")]
    public float radius = 5f;
    [Tooltip("Si quieres limitar la b�squeda a una capa espec�fica, as�gnala aqu�. Si no, deja todo por defecto.")]
    public LayerMask enemyLayer = ~0; // por defecto todo

    [Header("Opciones")]
    [Tooltip("Tag que deben tener los enemigos.")]
    public string enemyTag = "enemigo";
    [Tooltip("Si true se dibujan l�neas hacia los objetivos en el editor.")]
    public bool drawLinesToTargets = true;

    float cooldown = 0f;

    void Reset()
    {
        // Si no hay firePoint, usa este transform
        if (firePoint == null) firePoint = this.transform;
    }

    void Update()
    {
        if (cooldown > 0f) cooldown -= Time.deltaTime;

        if (cooldown <= 0f)
        {
            ShootAtEnemiesInRadius();
            
            cooldown = 1f / Mathf.Max(0.0001f, fireRate);
        }
    }

    void ShootAtEnemiesInRadius()
    {
        if (projectilePrefab == null || firePoint == null)
        {
            Debug.LogWarning("No hay prefab de proyectil o firePoint asignado.");
            return;
        }

        // Recopilamos todos los colliders en el radio
        Collider2D[] hits = Physics2D.OverlapCircleAll(firePoint.position, radius, enemyLayer);
       

        if (hits.Length == 0)
        {
            Debug.Log("[DEBUG] No hay enemigos dentro del radio.");
        }

        foreach (Collider2D c in hits)
        {
            if (c == null)
            {
                Debug.Log("[DEBUG] Collider nulo, se ignora.");
                continue;
            }

            // Filtrado por tag
            if (!c.CompareTag(enemyTag))
            {
                Debug.Log("[DEBUG] Collider " + c.name + " ignorado por tag (esperado '" + enemyTag + "').");
                continue;
            }

            Debug.Log("[DEBUG] Se va a disparar a: " + c.name);

            // Calculamos dirección
            Vector2 targetPos = c.bounds.center;
            Vector2 dir = (targetPos - (Vector2)firePoint.position).normalized;

            // Instanciamos proyectil
            GameObject proj = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            Debug.Log("[DEBUG] Instanciado proyectil: " + proj.name + " en " + proj.transform.position);

            Projectile2D p = proj.GetComponent<Projectile2D>();
            if (p != null)
            {
                p.Initialize(dir, projectileSpeed, gameObject);
            }
            else
            {
                Rigidbody2D rb = proj.GetComponent<Rigidbody2D>();
                if (rb != null)
                {
                    rb.linearVelocity = dir * projectileSpeed;
                }
            }

            Debug.Log("[DEBUG] Disparo realizado desde " + firePoint.position + " hacia " + c.name + " con dirección " + dir);
        }
    }


    void OnDrawGizmosSelected()
    {
        if (firePoint == null) firePoint = this.transform;

        // Dibujamos un punto central para el firePoint
        Gizmos.color = Color.yellow;
        Gizmos.DrawSphere(firePoint.position, 0.1f);

        // Dibujamos el radio de alcance
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.25f); // naranja semitransparente
        Gizmos.DrawWireSphere(firePoint.position, radius);

        // Opcional: dibujar líneas hacia enemigos dentro del radio
#if UNITY_EDITOR
        if (drawLinesToTargets)
        {
            Collider2D[] hits = Physics2D.OverlapCircleAll(firePoint.position, radius, enemyLayer);
            Gizmos.color = new Color(1f, 0f, 0f, 0.5f); // rojo semitransparente
            foreach (Collider2D c in hits)
            {
                if (c != null && c.CompareTag(enemyTag))
                {
                    Gizmos.DrawLine(firePoint.position, c.bounds.center);
                }
            }
        }
#endif
    }
}

