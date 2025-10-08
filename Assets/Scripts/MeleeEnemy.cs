// MeleeEnemy.cs
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MeleeEnemy : EnemyBase
{
    [Header("Melee Attack (Trigger-based)")]
    [SerializeField] private Transform attackPoint; // opcional, solo para gizmos
    [SerializeField] private float attackRadius = 0.5f;

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if (target == null)
        {
            GameObject playerObj = FindPlayerByLayerOrTag();
            if (playerObj != null)
                target = playerObj.transform;
            return;
        }

        float dist = Vector2.Distance(transform.position, target.position);

        if (IsPlayerInRange() && canMove)
        {
            if (dist > stopDistance)
            {
                MoveTowardsPlayer();
                if (animator != null) animator.SetBool("IsMoving", true);
            }
            else
            {
                StopMovement();
                if (animator != null) animator.SetBool("IsMoving", false);
            }
        }
        else
        {
            StopMovement();
            if (animator != null) animator.SetBool("IsMoving", false);
        }
    }

    /// <summary>
    /// Override de PerformAttack solo para mantener animaciones,
    /// pero el daño real se aplica por trigger.
    /// </summary>
    protected override void PerformAttack()
    {
        // opcional: reproducir efecto de ataque, sonido, etc.
        Debug.Log($"{name} performed attack animation (damage by trigger).");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // Solo dañar al jugador usando playerLayer heredado del padre
        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            var playerHealth = other.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                int damageToDeal = GetContactDamage(); // usa contactDamage ajustado por nivel del padre
                Debug.Log($"Trigger hit {playerHealth.name}, applying {damageToDeal} damage");
                playerHealth.TakeDamage(damageToDeal);
            }
        }
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();

        if (attackPoint != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(attackPoint.position, attackRadius);
        }
    }
}
