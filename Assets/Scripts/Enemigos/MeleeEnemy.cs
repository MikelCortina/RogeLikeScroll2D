using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Rigidbody2D))]
public class MeleeEnemy : EnemyBase
{
    [Header("Melee Attack (Trigger-based)")]
    [SerializeField] private Transform attackPofloat; // Punto desde el que nace el ataque
    [SerializeField] private Transform attackPofloatEnd; // Punto desde el que nace el ataque
    [SerializeField] private float attackRadius = 0.5f;
    [SerializeField] private GameObject attackPrefab; // Prefab del ataque
    [SerializeField] private float attackDuration = 0.2f;
    [SerializeField] private float arcHeight = 1f;
    [SerializeField] private float startVerticalOffset = 0.5f; // si attackPofloat es null, start = transform.position + down * offset
    [SerializeField] private float attackHorizontalDistance = 0f; // distancia horizontal que cubrirá el arco en la dirección del jugador

    private bool isAttacking = false;


    protected void FixedUpdate()
    {
        // 1️⃣ Buscar target si no hay
        if (target == null)
        {
            GameObject playerObj = FindPlayerByLayerOrTag();
            if (playerObj != null)
            {
                target = playerObj.transform;

            }
            else
            {
                Debug.Log("No hay jugador en escena");
            }
            return;
        }

        // 2️⃣ Si está atacando, no se mueve
        if (isAttacking)
        {

            return;
        }

        // 3️⃣ Comprobar distancia al jugador
        float dist = Vector2.Distance(transform.position, target.position);
        bool inRange = dist <= detectRadius;

    
        // 4️⃣ Movimiento
        if (canMove && dist > stopDistance)
        {
            MoveTowardsPlayer();
            if (animator != null) animator.SetBool("IsMoving", true);
        }
        else
        {

            if (animator != null) animator.SetBool("IsMoving", false);

            // 5️⃣ Intentar ataque si dentro de stopDistance
            if (dist-0.5 <= stopDistance)
            {
  
                TryAttack(); // Gestiona cooldown y llama a PerformAttack()
            }
        }
    }
    protected override void PerformAttack()
    {
        StartCoroutine(AttackCoroutine());
    }
    private IEnumerator AttackCoroutine()
    {
        if (attackPrefab == null)
        {
            Debug.LogWarning("attackPrefab no asignado");
            yield break;
        }

        if (target == null)
        {
            Debug.LogWarning("target es null");
            yield break;
        }



        isAttacking = true;


        // --- Dirección horizontal: hacia donde está el jugador (solo izquierda/derecha) ---
        float dir = Mathf.Sign(target.position.x - transform.position.x); // +1 = derecha, -1 = izquierda

        // Asumimos que attackPofloat.position está debajo del enemigo
        Vector3 startPos = attackPofloat.position;

        // Ajustamos la posición horizontal según la dirección
        startPos.x = transform.position.x + Mathf.Abs(startPos.x - transform.position.x) * dir;

        // Igual para el endPos
        Vector3 endPos = attackPofloatEnd.position;
        endPos.x = transform.position.x + Mathf.Abs(endPos.x - transform.position.x) * dir;


        // Instanciamos el objeto en startPos (puedes hacerlo inactivo y activarlo si prefieres)
        GameObject attackObj = Instantiate(attackPrefab, startPos, Quaternion.identity);
     
        float elapsed = 0f;
        bool dealtDamage = false;
       
        while (elapsed < attackDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / attackDuration);

            // --- Posición lineal horizontal/vertical ---
            Vector3 posLinear = Vector3.Lerp(startPos, endPos, t);
            // --- Arqueo vertical: altura máxima constante ---
            float arcHeightDir = arcHeight * dir;
            float arc = Mathf.Sin(t*Mathf.PI) * arcHeightDir; // Parábola tipo sin(t*π)

            // --- Posición final combinando lineal + arco ---
            Vector3 pos = new Vector3(posLinear.x + arc, posLinear.y , posLinear.z);
            attackObj.transform.position = pos;

            // --- Colisión ---
            if (!dealtDamage)
            {
                Collider2D hit = Physics2D.OverlapCircle(attackObj.transform.position, attackRadius, playerLayer);
                if (hit != null)
                {
                    var playerHealth = hit.GetComponent<PlayerHealth>();
                    if (playerHealth != null)
                    {
                     
                        float damageToDeal = GetContactDamage();
                        playerHealth.TakeMeleDamage(damageToDeal);
                        dealtDamage = true;
                    }
                }
            }

            yield return null;
        }


        // Si quieres comprobar al final (por si se alcanzó cuando t == 1)
        if (!dealtDamage)
        {
            Collider2D hit = Physics2D.OverlapCircle(attackObj.transform.position, attackRadius, playerLayer);
            if (hit != null)
            {
                var playerHealth = hit.GetComponent<PlayerHealth>();
                if (playerHealth != null)
                {
            
                    float damageToDeal = GetContactDamage();
                    playerHealth.TakeMeleDamage(damageToDeal);
                }
            }
 
        }

        Destroy(attackObj);

        lastAttackTime = Time.time;
        isAttacking = false;
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
