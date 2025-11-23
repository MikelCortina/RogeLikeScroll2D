using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class FlyingMeleeEnemy : MeleeEnemy
{
    [Header("Movement Settings")]
    [SerializeField] private bool overrideGravity = true;
    [SerializeField] private float flyingMoveSpeed = 4f;
    [SerializeField] private float acceleration = 12f;

    private float prevGravity = 1f;

    [Header("Leaning / Tilt Settings")]
    [SerializeField] private float maxTilt = 25f;       // grados máximos de inclinación
    [SerializeField] private float tiltSpeed = 10f;     // qué tan rápido rota



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
        ApplyTilt(rb.linearVelocity);
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
    private void ApplyTilt(Vector2 velocity)
    {
        // Si no se está moviendo, vuelve al ángulo neutro
        if (velocity.sqrMagnitude < 0.1f)
        {
            float neutral = 0f;
            float newZ = Mathf.LerpAngle(transform.eulerAngles.z, neutral, tiltSpeed * Time.deltaTime);
            transform.rotation = Quaternion.Euler(0f, 0f, newZ);
            return;
        }

        // Dirección horizontal: derecha → positivo, izquierda → negativo
        float tiltPercent = Mathf.Clamp(velocity.x / flyingMoveSpeed, -1f, 1f);

        // Ángulo objetivo según su dirección
        float targetAngle = -tiltPercent * maxTilt;

        // Rotación suave
        float newAngle = Mathf.LerpAngle(
            transform.eulerAngles.z,
            targetAngle,
            tiltSpeed * Time.deltaTime
        );

        transform.rotation = Quaternion.Euler(0f, 0f, newAngle);
    }

    protected override IEnumerator FlashCoroutine()
    {
        Color flashColor = new Color(100f, 50.98f, 35.29f, 1); // Fixed the incorrect usage of 'new Color.red'

        for (int i = 0; i < flashCount; i++)
        {
            for (int j = 0; j < renderersToFlash.Length; j++)
                renderersToFlash[j].material.SetColor("_Color", flashColor);

            yield return new WaitForSeconds(flashDuration);

            for (int j = 0; j < renderersToFlash.Length; j++)
                renderersToFlash[j].material.SetColor("_Color", originalColors[j]);

            yield return new WaitForSeconds(flashDuration);
        }

        for (int j = 0; j < renderersToFlash.Length; j++)
            renderersToFlash[j].material.SetColor("_Color", originalColors[j]);

        flashRoutine = null;
    }



    // ENEMIGO YA NO ATACA
    protected override void PerformAttack() { }
}
