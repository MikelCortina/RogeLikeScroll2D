using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public class BulletRainBullet : MonoBehaviour
{
    [Header("Configuración")]
    public float lifetime = 10f;

    private Rigidbody2D rb;
    private BulletRainEffect owner;
    private float lifeTimer;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Init(BulletRainEffect effect, Vector2 velocity)
    {
        owner = effect;
        lifeTimer = lifetime;

        rb.linearVelocity = velocity;
        gameObject.SetActive(true);
    }

    private void Update()
    {
        lifeTimer -= Time.deltaTime;
        if (lifeTimer <= 0f)
            ReturnToPool();
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Ground"))
        {
            // daño aquí si quieres
            ReturnToPool();
        }
        if (other.CompareTag("enemigo"))
        {
            EnemyBase enemy = other.GetComponentInParent<EnemyBase>();
            if (enemy != null)
            {
            
                float dmg = StatsCommunicator.Instance.CalculateGunDamage();
                enemy.TakeContactDamage(dmg/5, false);
                ConsoleManager.Instance.Log($"Bullet from rain damage dealed: {dmg/5}");
                ReturnToPool();
            }
        }
    }

    private void OnBecameInvisible()
    {
        ReturnToPool();
    }

    private void ReturnToPool()
    {
        if (!gameObject.activeSelf) return;

        owner?.ReturnBulletToPool(gameObject);
    }
}
