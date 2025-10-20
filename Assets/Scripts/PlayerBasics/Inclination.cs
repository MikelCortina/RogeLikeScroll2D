using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class InclinationFollower : MonoBehaviour
{
    [Header("Raycast Settings")]
    public float rayLength = 1f;           // Distancia para "sentir" el suelo
    public LayerMask groundLayer;          // Capa donde están las dunas
    public float verticalOffset = 0.1f;    // Para no hundirse en el suelo

    [Header("Rotation Settings")]
    public bool smoothRotation = true;
    public float rotationSpeed = 10f;      // Velocidad de suavizado

    private Rigidbody2D rb;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    void FixedUpdate()
    {
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, rayLength, groundLayer);

        if (hit.collider != null)
        {
            // Ajustar la posición vertical para "pegar" al suelo
            Vector3 targetPosition = new Vector3(transform.position.x, hit.point.y + verticalOffset, transform.position.z);
            rb.MovePosition(targetPosition);

            // Calcular ángulo según la normal del suelo
            float angle = Mathf.Atan2(hit.normal.y, hit.normal.x) * Mathf.Rad2Deg - 90f;

            if (smoothRotation)
            {
                Quaternion currentRotation = Quaternion.Euler(0, 0, rb.rotation);
                Quaternion targetRotation = Quaternion.Euler(0, 0, angle);
                rb.MoveRotation(Quaternion.Lerp(currentRotation, targetRotation, rotationSpeed * Time.fixedDeltaTime));
            }
            else
            {
                rb.MoveRotation(angle);
            }
        }
    }

    // Opcional: para ver el raycast en la escena
    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * rayLength);
    }
}
