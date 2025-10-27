using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class DamageOnTrigger : MonoBehaviour
{
    public float knockback = 5f;
    public bool ignoreOwner = true;
    public GameObject owner;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (ignoreOwner && other.gameObject == owner) return;

        if (other.CompareTag("enemigo"))
        {
            EnemyBase enemy = other.GetComponentInParent<EnemyBase>();
            Rigidbody2D rb = other.attachedRigidbody;
            Vector2 dir = ((Vector2)other.transform.position - (Vector2)transform.position).normalized;

            if (enemy != null)
            {
                float dmg = StatsCommunicator.Instance.CalculateDamage();
                enemy.TakeContactDamage(dmg);
                enemy.ApplyKnockback(dir * knockback);
            }
            else if (rb != null)
            {
                rb.AddForce(dir * knockback, ForceMode2D.Impulse);
            }
        }
    }
}
