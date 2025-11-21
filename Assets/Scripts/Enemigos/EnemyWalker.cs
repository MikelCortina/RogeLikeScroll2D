using UnityEngine;

public class EnemyWalker : EnemyBase
{
    protected override void Start()
    {
        base.Start();
        // Asegurarse de que siempre mire hacia la izquierda
        if (isFacingRight) Flip();
    }

    private void FixedUpdate()
    {
        if (!canMove || isKnockedBack) return;

        // Movimiento constante hacia la izquierda
        Vector2 velocity = rb.linearVelocity;
        velocity.x = -moveSpeed; // siempre hacia la izquierda
        rb.linearVelocity = velocity;

        // Ajuste de terreno / inclinaci�n
        ApplyInclinationAndStepSmoothing();
    }
}
