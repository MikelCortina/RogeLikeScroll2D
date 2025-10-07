using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Movimiento")]
    public float moveForce = 5f;
    public float jumpForce = 7f;
    public float maxSpeed = 5f;
    public float friction = 0.9f;

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

        // Aplica fuerza según la dirección
        if (moveInput > 0)
            rb.AddForce(Vector2.right * moveForce, ForceMode2D.Force);
        else if (moveInput < 0)
            rb.AddForce(Vector2.left * moveForce, ForceMode2D.Force);

        // Limita la velocidad máxima horizontal
        if (rb.linearVelocity.x > maxSpeed)
            rb.linearVelocity = new Vector2(maxSpeed, rb.linearVelocity.y);
        else if (rb.linearVelocity.x < -maxSpeed)
            rb.linearVelocity = new Vector2(-maxSpeed, rb.linearVelocity.y);

     
        rb.linearVelocity = new Vector2(rb.linearVelocity.x * friction, rb.linearVelocity.y);

        // Salto con AddForce (más físico)
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            // Primero ponemos la velocidad vertical a 0 para evitar acumulaciones
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);

            // Luego aplicamos la fuerza de salto hacia arriba
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
        }
    }


    void FixedUpdate()
    {
        isGrounded = Physics2D.OverlapCircle(groundCheck.position, groundRadius, Ground);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
    }
}
