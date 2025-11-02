using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class RangedMeleeEnemy : MeleeEnemy
{
    [Header("Ranged (Parabolic)")]
    [SerializeField] private GameObject projectilePrefab; // Prefab con ParabolicProjectile
    [SerializeField] private float rangedDistanceThreshold = 5f; // si player está más a la derecha que esto, lanzará proyectil
    [SerializeField] private float projectileDuration = 1.0f; // tiempo que tarda el proyectil en recorrer la parábola
    [SerializeField] private float projectileArcHeight = 2.0f; // altura del arco (control point)
    [SerializeField] private Transform projectileSpawnPoint; // desde dónde sale el proyectil (si null, usa transform.position)
    [SerializeField] private LayerMask playerLayerForProjectile; // para detección de colisión en el proyectil
    [SerializeField] private float projectileDamage = 10f;
    [SerializeField] private float rangedCooldown = 1.2f;

    private float lastRangedTime = -999f;

    protected override void PerformAttack()
    {
        // Decide si hacer melee o ranged
        if (target == null)
        {
            base.PerformAttack();
            return;
        }

        float horizontalDelta = target.position.x - transform.position.x;
        bool playerIsRightFar = horizontalDelta > rangedDistanceThreshold;

        if (playerIsRightFar && Time.time >= lastRangedTime + rangedCooldown && projectilePrefab != null)
        {
            StartCoroutine(RangedAttackCoroutine());
        }
        else
        {
            // fallback al ataque melee normal
            base.PerformAttack();
        }
    }

    private IEnumerator RangedAttackCoroutine()
    {
        isAttacking = true;

        // small windup (opcional) - si quieres animación, usa animator.SetTrigger(...)
        yield return null;

        Vector3 spawn = projectileSpawnPoint != null ? projectileSpawnPoint.position : transform.position;
        // Ajusta spawn en el eje X hacia la dirección del jugador (coherencia con tu Melee)
        float dir = Mathf.Sign(target.position.x - transform.position.x);
        spawn.x = transform.position.x + Mathf.Abs(spawn.x - transform.position.x) * dir;

        // Instanciamos
        GameObject projGO = Instantiate(projectilePrefab, spawn, Quaternion.identity);
        ParabolicProjectile proj = projGO.GetComponent<ParabolicProjectile>();
        if (proj == null)
        {
            Debug.LogWarning("El prefab de projectile no contiene ParabolicProjectile.cs");
            Destroy(projGO);
        }
        else
        {
            // el objetivo será la posición del jugador en el momento del lanzamiento
            Vector3 targetPos = target.position;
            proj.Initialize(spawn, targetPos, projectileArcHeight, projectileDuration, projectileDamage, playerLayerForProjectile);
        }

        lastRangedTime = Time.time;

        // espera el cooldown mínimo antes de permitir otras acciones (si quieres que el enemigo esté inmóvil mientras dispara, puede esperarse aquí)
        yield return new WaitForSeconds(0.1f);

        isAttacking = false;
    }

    // Exponer OnDrawGizmos para visualizar threshold en el editor
    protected override void OnDrawGizmosSelected()
    {
        base.OnDrawGizmosSelected();
        Gizmos.color = Color.cyan;
        Vector3 right = transform.position + Vector3.right * rangedDistanceThreshold;
        Gizmos.DrawLine(transform.position, right);
        Gizmos.DrawWireSphere(right, 0.15f);
    }
}
