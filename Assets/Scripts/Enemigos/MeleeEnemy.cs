using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class MeleeEnemy : EnemyBase
{
    [Header("Melee Attack (Trigger-based)")]
    [SerializeField] private Transform attackPofloat;
    [SerializeField] private float attackRadius = 0.5f;

    protected void FixedUpdate()
    {
        if (target == null)
        {
            GameObject playerObj = FindPlayerByLayerOrTag();
            if (playerObj != null) target = playerObj.transform;
            return;
        }

        if (IsPlayerInRange() && canMove)
        {
            float dist = Vector2.Distance(transform.position, target.position);
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

    protected override void PerformAttack()
    {
        Debug.Log($"{name} performed attack animation (damage by trigger).");
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & playerLayer) != 0)
        {
            var playerHealth = other.GetComponentInParent<PlayerHealth>();
            if (playerHealth != null)
            {
                float damageToDeal = GetContactDamage();
                playerHealth.TakeDamage(damageToDeal);
            }
        }
    }

    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        if (attackPofloat != null)
        {
            Gizmos.color = Color.magenta;
            Gizmos.DrawWireSphere(attackPofloat.position, attackRadius);
        }
    }
}
