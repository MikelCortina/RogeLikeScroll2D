using UnityEngine;
using System.Linq; // ← NUEVO: Para ordenar hits por distancia

[CreateAssetMenu(menuName = "Projectile Effects/Spread Shot")]
public class SpreadShotEffect : ProjectileEffect
{
    [Range(0.1f, 2f)]
    public float sideDamageMultiplier = 0.7f;

    [Range(5f, 90f)]
    public float spreadAngle = 30f;

    [Range(0.5f, 2f)]
    public float sideSpeedMultiplier = 1f; // ← NUEVO: Multiplicador de velocidad

    [Range(0.5f, 2f)]
    public float sideRangeMultiplier = 1f; // ← NUEVO: Multiplicador de rango (default 1 = mismo que principal)

    public bool sideBulletsCanPenetrate = true;

    public GameObject sideProjectilePrefab;

    public override void Execute(Vector2 impactPosition, GameObject owner) { }

    public void ExecuteOnShoot(Vector2 firePointPos, Vector2 mainDirection, GameObject owner)
    {
        AreaShooter2D shooter = owner.GetComponent<AreaShooter2D>();
        if (shooter == null) return;

        Vector2 leftDir = Quaternion.Euler(0, 0, spreadAngle) * mainDirection;
        Vector2 rightDir = Quaternion.Euler(0, 0, -spreadAngle) * mainDirection;

        float baseDamage = StatsCommunicator.Instance.CalculateGunDamage();
        float sideDamage = baseDamage * sideDamageMultiplier;

        float sideRange = shooter.maxRange * sideRangeMultiplier;
        float sideSpeed = shooter.projectileSpeed * sideSpeedMultiplier;

        ShootSideBullet(firePointPos, leftDir.normalized, sideDamage, sideRange, sideSpeed, shooter);
        ShootSideBullet(firePointPos, rightDir.normalized, sideDamage, sideRange, sideSpeed, shooter);
    }

    private void ShootSideBullet(Vector2 startPos, Vector2 dir, float damage, float sideRange, float sideSpeed, AreaShooter2D shooter)
    {
        // ✅ RAYCAST: CircleCastAll en enemyLayer (SÍ AFECTA A ENEMIGOS)
        RaycastHit2D[] hits = Physics2D.CircleCastAll(
            startPos,
            0.1f,
            dir,
            sideRange,
            shooter.enemyLayer
        );

        // ✅ CLAVE: Ordenar por distancia (bugfix unsorted hits)
        RaycastHit2D[] sortedHits = hits.OrderBy(h => h.distance).ToArray();

        Vector2 finalHitPoint = startPos + dir * sideRange;
        int penetration = sideBulletsCanPenetrate ? CalculatePenetrationCount() : 0;

        foreach (var hit in sortedHits)
        {
            if (!hit.collider) continue;

            // ✅ Manejo de proyectiles enemigos (como en principal)
            if (hit.collider.CompareTag(shooter.enemyProjectileTag))
            {
                Projectile2D enemyProjComponent = hit.collider.GetComponentInParent<Projectile2D>();
                if (enemyProjComponent != null)
                    enemyProjComponent.gameObject.SetActive(false);
                else
                    Destroy(hit.collider.gameObject);
                continue; // No consume penetración
            }

            // ✅ DAÑO A ENEMIGOS
            if (hit.collider.CompareTag(shooter.enemyTag))
            {
                EnemyBase enemy = hit.collider.GetComponentInParent<EnemyBase>();
                if (enemy != null)
                {
                    enemy.TakeContactDamage(damage, true);
                    ConsoleManager.Instance.Log($"Side bullet damage: {damage}"); // ← DEBUG
                }

                penetration--;
                if (penetration <= 0)
                {
                    finalHitPoint = hit.point;
                    break;
                }
            }
        }

        SpawnVisual(startPos, dir, finalHitPoint, sideSpeed, shooter);
    }

    private void SpawnVisual(Vector2 pos, Vector2 dir, Vector2 end, float speed, AreaShooter2D shooter)
    {
        GameObject visual = sideProjectilePrefab != null
            ? Object.Instantiate(sideProjectilePrefab)
            : shooter.GetPooledProjectile();

        visual.transform.position = pos;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        visual.transform.rotation = Quaternion.Euler(0, 0, angle);
        visual.SetActive(true);

        Projectile2D p = visual.GetComponent<Projectile2D>();
        p?.InitializeVisual(dir, speed, end, null);

        if (sideProjectilePrefab != null)
            Object.Destroy(visual, 3f);
    }

    private int CalculatePenetrationCount()
    {
        int total = 0;
        foreach (var effect in RunEffectManager.Instance.GetActiveEffects())
            if (effect is PenetrationEffect p)
                total += p.penetrationCount;
        return total;
    }
}