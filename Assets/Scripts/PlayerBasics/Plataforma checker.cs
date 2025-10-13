using UnityEngine;


public class PlataformaChecker : MonoBehaviour
{
    [Header("Ground Check Settings")]
    public Transform groundCheckPoint; // Punto desde donde se chequea el suelo
    public float groundCheckRadius = 0.2f; // Radio del círculo de chequeo
    public LayerMask groundLayer; // Capa que representa el suelo

    [Header("Debug")]
    public bool isGrounded = false; // Resultado del chequeo

    void Update()
    {
        // Chequeamos si hay colisión con el suelo
        isGrounded = Physics2D.OverlapCircle(groundCheckPoint.position, groundCheckRadius, groundLayer);
        //Debug.Log("[PlataformaChecker] isGrounded: " + isGrounded);

    }
    // Opcional: dibujar gizmo para ver el área del ground check
    void OnDrawGizmosSelected()
    {
        if (groundCheckPoint != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(groundCheckPoint.position, groundCheckRadius);
        }
    }
}
