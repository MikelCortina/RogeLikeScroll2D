// Projectile2D.cs
// Script sencillo para el prefab del proyectil.
// Requisitos:
// - El prefab debe tener Rigidbody2D (BodyType = Dynamic), y un Collider2D (isTrigger opcional).
// - Este script se encarga de mover el proyectil, destruirlo tras lifetime y notificar colisiones.

using UnityEngine;

public class Projectile2D : MonoBehaviour
{
    Rigidbody2D rb;
    float speed = 8f;
    Vector2 direction = Vector2.right;
    public float lifeTime = 5f;
    public int damage = 1;
    public GameObject owner; // opcional: para no da�ar al que dispara

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        // evitar rotaciones raras
        if (rb != null) rb.freezeRotation = true;
        Destroy(gameObject, lifeTime);
    }

    public void Initialize(Vector2 dir, float spd, GameObject ownerObj = null)
    {
        direction = dir.normalized;
        speed = spd;
        owner = ownerObj;
        if (rb != null) rb.linearVelocity = direction * speed;
        // rotaci�n visual del proyectil (opcional)
        float ang = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, ang);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        // si colisiona con el que lo dispar�, ignorar
        if (owner != null && other.gameObject == owner) return;

        // ejemplo: si choca con un enemigo (tag) le podr�as restar vida
        // if (other.CompareTag("enemigo")) { /* aplicar da�o */ }

        // destruir proyectil al colisionar con cualquier cosa (ajusta seg�n necesites)
        Destroy(gameObject);
    }

    // Si prefieres usar OnCollisionEnter2D (no trigger), cambia el collider y este m�todo en consecuencia.
}
