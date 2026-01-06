using UnityEngine;
using System;
using UnityEngine.Pool;

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

    public event Action OnExplode;

    private bool hasExploded = false;

    private void OnEnable()
    {
        // Reset estado
        hasExploded = false;

        if (TryGetComponent<Rigidbody2D>(out var rb))
        {
            rb.simulated = true;
            rb.AddTorque(UnityEngine.Random.Range(-500f, 500f));
        }

        // ← CLAVE: Ignorar colisiones con el owner y todos sus colliders
        if (ignoreOwnerCollision && owner != null && TryGetComponent<Collider2D>(out var myCollider))
        {
            Collider2D[] ownerColliders = owner.GetComponentsInChildren<Collider2D>();
            foreach (var col in ownerColliders)
            {
                Physics2D.IgnoreCollision(myCollider, col, true);
            }
        }
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

        // Ya ignoramos al owner por Physics2D.IgnoreCollision, pero por seguridad:
        if (ignoreOwnerCollision && owner != null && col.gameObject == owner) return;

        if (!string.IsNullOrEmpty(enemyTag) && col.CompareTag(enemyTag))
        {
            Explode();
            return;
        }

        if (!string.IsNullOrEmpty(groundTag) && col.CompareTag(groundTag))
        {
            Explode();
            return;
        }

        int colLayer = col.gameObject.layer;
        if (((enemyLayerMask.value & (1 << colLayer)) != 0) ||
            ((groundLayerMask.value & (1 << colLayer)) != 0))
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

        OnExplode?.Invoke();
        gameObject.SetActive(false);
    }
}