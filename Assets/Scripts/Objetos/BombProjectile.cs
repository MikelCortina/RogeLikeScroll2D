using UnityEngine;
using System;

[RequireComponent(typeof(Rigidbody2D))]
public class BombProjectile : MonoBehaviour
{
    [Header("Referencia al efecto (ScriptableObject)")]
    public ExplosionEffect explosionEffect;

    [Header("Propietario / control")]
    public GameObject owner;
    public bool ignoreOwnerCollision = true;

    [Header("Lógica de colisión")]
    public LayerMask groundLayerMask = 1 << 8;
    public LayerMask enemyLayerMask = ~0;
    public string groundTag = "Ground";
    public string enemyTag = "enemigo";

    [Header("Tiempo de vida")]
    public float lifetime = 6f;

    // Evento para pooling
    public event Action OnExplode;

    // control interno
    private bool hasExploded = false;

    private void Start()
    {
      
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (hasExploded) return;
        TryExplodeOnCollider(collision.collider);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (hasExploded) return;
        TryExplodeOnCollider(other);
    }

    private void TryExplodeOnCollider(Collider2D col)
    {
        if (col == null) return;

        if (ignoreOwnerCollision && owner != null && col.gameObject == owner) return;

        if (!string.IsNullOrEmpty(enemyTag) && col.gameObject.CompareTag(enemyTag))
        {
            Explode();
            return;
        }

        if (!string.IsNullOrEmpty(groundTag) && col.gameObject.CompareTag(groundTag))
        {
            Explode();
            return;
        }

        int colLayerMask = 1 << col.gameObject.layer;
        if ((enemyLayerMask & colLayerMask) != 0 || (groundLayerMask & colLayerMask) != 0)
        {
            Explode();
        }
    }

    private void Explode()
    {
        if (hasExploded) return;
        hasExploded = true;

        if (TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
        }

        if (explosionEffect != null)
        {
            explosionEffect.Execute(transform.position, owner);
        }
        else
        {
            Debug.LogWarning("[BombProjectile] explosionEffect no asignado.");
        }

        // Invocar evento para pooling antes de "desaparecer"
        OnExplode?.Invoke();

        // Para pooling, no destruimos si vamos a reutilizar
        gameObject.SetActive(false);

        // Si quieres destrucción definitiva (sin pooling) descomenta:
        // Destroy(gameObject);
    }

    private void OnEnable()
    {
        // Resetear estado para reutilización desde el pool
        hasExploded = false;
        if (TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.simulated = true;
        }
    }
}
