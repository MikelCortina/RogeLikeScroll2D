using UnityEngine;

public class OrganMover : MonoBehaviour
{
    public float groundSpeed = -2f;      // Velocidad constante del suelo
    public float acceleration = 5f;      // Qué tan rápido acelera hacia la velocidad del suelo

    private bool onGround = false;
    private float currentSpeed = 0f;

    void Update()
    {
        if (onGround)
        {
            // Aumenta gradualmente la velocidad hacia la del suelo
            currentSpeed = Mathf.MoveTowards(currentSpeed, groundSpeed, acceleration * Time.deltaTime);
            transform.Translate(currentSpeed * Time.deltaTime, 0, 0);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.gameObject.CompareTag("Ground"))
        {
            onGround = true;
        }
    }

   
}