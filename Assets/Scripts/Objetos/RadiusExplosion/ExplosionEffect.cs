using UnityEngine;

[CreateAssetMenu(menuName = "Projectile Effects/Explosion")]
public class ExplosionEffect : ProjectileEffect
{
    public float radius = 2f;
    public float knockback = 300f;
    public LayerMask hittableLayers = ~0;
    public GameObject vfxPrefab;
    public bool destroyVfxAfterSec = true;
    public float vfxLifetime = 2f;
    public bool ignoreOwner = true;

    public override void Execute(Vector2 position, GameObject owner = null)
    {
        float damage = StatsCommunicator.Instance.CalculateDamage();
        // VFX
        if (vfxPrefab != null)
        {
            GameObject vfx = Object.Instantiate(vfxPrefab, position, Quaternion.identity);
            if (destroyVfxAfterSec) Object.Destroy(vfx, vfxLifetime);
        }

        // Área de daño
        Collider2D[] hits = Physics2D.OverlapCircleAll(position, radius, hittableLayers);
        foreach (var col in hits)
        {
            if (col == null) continue;
            if (ignoreOwner && owner != null && col.gameObject == owner) continue;

            EnemyBase enemy = col.GetComponentInParent<EnemyBase>();
            if (enemy != null) enemy.TakeContactDamage(damage);

            Rigidbody2D rb = col.attachedRigidbody;
            if (rb != null)
            {
                Vector2 dir = ((Vector2)rb.position - position).normalized;
                rb.AddForce(dir * knockback);
            }
            Debug.Log("Explosion hit: " + col.name);
        }
    }
}
