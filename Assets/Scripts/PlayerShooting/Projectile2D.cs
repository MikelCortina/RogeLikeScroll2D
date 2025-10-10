using UnityEngine;

public class Projectile2D : MonoBehaviour
{
    Rigidbody2D rb;
    float speed = 8f;
    Vector2 direction = Vector2.right;
    public float lifeTime = 5f;
    public GameObject owner;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.freezeRotation = true;
        Destroy(gameObject, lifeTime);
    }

    public void Initialize(Vector2 dir, float spd, GameObject ownerObj = null)
    {
        direction = dir.normalized;
        speed = spd;
        owner = ownerObj;
        if (rb != null) rb.linearVelocity = direction * speed;

        float ang = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, ang);
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (owner != null && other.gameObject == owner) return;

        EnemyBase enemy = other.GetComponentInParent<EnemyBase>();
        if (enemy != null)
        {
            float dmg = StatsManager.Instance.RuntimeStats.projectileDamage; // StatsManager.Instance.RuntimeStats.damagePercentage / 100f;
            enemy.TakeContactDamage(dmg);
        }

        Destroy(gameObject);
    }
}
