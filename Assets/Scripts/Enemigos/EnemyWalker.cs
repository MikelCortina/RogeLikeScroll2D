using UnityEngine;

public class EnemyWalker : EnemyBase
{
    private Camera cam;

    protected override void Start()
    {
        base.Start();
        cam = Camera.main;

        // Asegurarse de que siempre mire hacia la izquierda
        if (isFacingRight) Flip();
    }

    private void FixedUpdate()
    {
        if (!canMove || isKnockedBack) return;

        // Movimiento constante hacia la izquierda
        Vector2 velocity = rb.linearVelocity;
        velocity.x = -moveSpeed;
        rb.linearVelocity = velocity;

        // Ajuste de terreno / inclinación
        ApplyInclinationAndStepSmoothing();

        CheckIfOutOfCamera();
    }

    private void CheckIfOutOfCamera()
    {
        if (cam == null) return;

        // Convertimos posición del enemigo a coordenadas de viewport
        Vector3 viewportPos = cam.WorldToViewportPoint(transform.position);

        // Si pasa x < -1, destruir
        if (viewportPos.x < -0.2f)
        {
            Destroy(gameObject);
        }
    }

}
