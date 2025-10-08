using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Comprobaciones")]
    public Transform groundCheck;
    public float groundRadius = 0.2f;
    public LayerMask Ground;

    private Rigidbody2D rb;
    private bool isGrounded;
    private float moveInput;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        moveInput = Input.GetAxisRaw("Horizontal");


       
    }

    void FixedUpdate()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, Ground);
        // Obtiene stats dinámicos del StatsManager
        float moveForce = StatsManager.Instance.RuntimeStats.moveForce;
        float maxSpeed = StatsManager.Instance.RuntimeStats.maxSpeed;
        float jumpForce = StatsManager.Instance.RuntimeStats.jumpForce;
        float friction = StatsManager.Instance.RuntimeStats.friction;

        // Aplica fuerza horizontal según la dirección
        if (moveInput > 0)
            rb.AddForce(Vector2.right * moveForce, ForceMode2D.Force);
        else if (moveInput < 0)
            rb.AddForce(Vector2.left * moveForce, ForceMode2D.Force);

        // Limita la velocidad máxima horizontal
        float clampedX = Mathf.Clamp(rb.linearVelocity.x, -maxSpeed, maxSpeed);
        rb.linearVelocity = new Vector2(clampedX * friction, rb.linearVelocity.y);

        // Salto
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f); // Reset velocidad vertical
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
    }
}
