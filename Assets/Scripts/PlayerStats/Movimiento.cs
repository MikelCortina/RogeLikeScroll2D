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
    private bool jumpPressed;

    public GameObject riderPrefab; // asignar prefab del jinete en el Inspector
    private bool riderSpawned = false;

    void Start()
    {
       // SpawnRider();
        rb = GetComponent<Rigidbody2D>();
    }

    void Update()
    {
        // Leer entrada en Update
        moveInput = Input.GetAxisRaw("Horizontal");

        if (Input.GetButtonDown("Jump"))
        {
            if (IsGrounded())
            {
                jumpPressed = true;
            }
           /*else if (!riderSpawned)
            {
                riderSpawned = true;
            }*/
        }
    }

    void FixedUpdate()
    {
        // COMPRUEBA valores en StatsManager aquí (cada FixedUpdate, sincronizado con física)
        float jumpForce = StatsManager.Instance.RuntimeStats.jumpForce;
        float moveForce = StatsManager.Instance.RuntimeStats.moveForce;
        float maxSpeed = StatsManager.Instance.RuntimeStats.maxSpeed;
        float friction = StatsManager.Instance.RuntimeStats.friction;

        // Aplicar movimiento horizontal con AddForce (mejor en FixedUpdate)
        if (moveInput != 0f)
        {
            rb.AddForce(Vector2.right * moveInput * moveForce, ForceMode2D.Force);
        }
        else
        {
            // Si no hay input, aplicar fricción
            rb.linearVelocity = new Vector2(rb.linearVelocity.x * friction, rb.linearVelocity.y);
        }

        // Limitar velocidad horizontal
        float clampedX = Mathf.Clamp(rb.linearVelocity.x, -maxSpeed, maxSpeed);
        rb.linearVelocity = new Vector2(clampedX, rb.linearVelocity.y);

        // Salto: aplicar impulso una vez
        if (jumpPressed)
        {
            // reset de la velocidad vertical para saltos consistentes
            rb.linearVelocity = new Vector2(rb.linearVelocity.x, 0f);
            rb.AddForce(Vector2.up * jumpForce, ForceMode2D.Impulse);
            jumpPressed = false;
        }
    }

    private bool IsGrounded()
    {
        if (groundCheck == null) return false;
        return Physics2D.OverlapCircle(groundCheck.position, groundRadius, Ground);
    }

    void OnDrawGizmosSelected()
    {
        if (groundCheck == null) return;
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(groundCheck.position, groundRadius);
    }
    void SpawnRider()
    {
        GameObject rider = Instantiate(riderPrefab);
        RiderController riderController = rider.GetComponent<RiderController>();

        // Sacar el jumpForce del StatsManager para sincronizar
        riderController.jumpForce = StatsManager.Instance.RuntimeStats.jumpForce;

        riderController.Init(transform); // pasar el transform del caballo
    }
}
