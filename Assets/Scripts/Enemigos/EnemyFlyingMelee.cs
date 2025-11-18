using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FlyingMeleeEnemy : MeleeEnemy
{
    [Header("Movement Settings")]
    [SerializeField] private bool overrideGravity = true;
    [SerializeField] private float flyingMoveSpeed = 4f;
    [SerializeField] private float acceleration = 12f;

    private float prevGravity = 1f;

    protected override void Awake()
    {
        base.Awake();
        if (rb != null)
        {
            prevGravity = rb.gravityScale;
            if (overrideGravity) rb.gravityScale = 0f;
        }
    }

    protected void OnDisable()
    {
        if (rb != null && overrideGravity)
            rb.gravityScale = prevGravity;
    }


    // ------------------------------------------
    // MOVIMIENTO: dirección normalizada al jugador,
    // pero NUNCA permite velocidad vertical positiva.
    // ------------------------------------------
    protected new void FixedUpdate()
    {
        if (target == null)
        {
            GameObject p = FindPlayerByLayerOrTag();
            if (p != null) target = p.transform;
            else return;
        }

        if (!canMove || isKnockedBack)
        {
            rb.linearVelocity = Vector2.zero;
            if (animator != null) animator.SetBool("IsMoving", false);
            return;
        }

        MoveTowardPlayerRestricted();
        if (animator != null) animator.SetBool("IsMoving", true);
    }


    // ------------------------------------------------------------
    // Este movimiento:
    //  - Calcula dirección normalizada hacia el jugador
    //  - Si la dirección apunta hacia arriba, se elimina la componente vertical
    //  - Aplica esa dirección suavemente para que el camino sea siempre el mínimo
    //  - Mantiene “ataque hacia abajo” al no permitir subir nunca
    // ------------------------------------------------------------
    private void MoveTowardPlayerRestricted()
    {
        Vector2 pos = rb.position;
        Vector2 targetPos = (Vector2)target.position;

        Vector2 dir = (targetPos - pos).normalized;

        // Si la dirección apunta hacia arriba → no sube
        if (dir.y > 0f)
            dir.y = 0f;

        dir.Normalize();

        Vector2 targetVel = dir * flyingMoveSpeed;

        Vector2 newVel = Vector2.MoveTowards(
            rb.linearVelocity,
            targetVel,
            acceleration * Time.fixedDeltaTime
        );

        rb.linearVelocity = newVel;

        FlipIfNeeded(newVel.x);
    }


    // ENEMIGO YA NO ATACA
    protected override void PerformAttack() { }
}
