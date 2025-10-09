using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Comprobaciones")]
    public Transform groundCheck;
    public float groundRadius = 0.2f;
    public LayerMask Ground;

    private Rigidbody2D rb;
    public bool isGrounded;
    private float moveInput;
    public bool jumpPressed;
    public bool dobleSalto;

    public GameObject riderPrefab; // asignar prefab del jinete en el Inspector
    public RiderController rider1;

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
            if (IsGrounded()&&rider1.isAttached)
            {
                jumpPressed = true;
            }

        }
        if (isGrounded)
        {
            dobleSalto = false;
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
            StartCoroutine(DobleSalto());
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
  
    private IEnumerator DobleSalto()
    {
        yield return new WaitForSeconds(0.1f);
        dobleSalto = true;
    }
}
