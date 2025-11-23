using UnityEngine;

public class FlipTowardsPlayer : MonoBehaviour
{
    private Transform player;
    public Transform arm; // asigna el brazo desde el inspector

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
    }

    void Update()
    {
        if (player == null) return;

        bool facingRight = player.position.x > transform.position.x;

        // Flip del personaje
        transform.rotation = facingRight ? Quaternion.Euler(0, 0, 0) : Quaternion.Euler(0, 180, 0);

        // Ajuste del brazo
        Vector3 armEuler = arm.localEulerAngles;

        if (facingRight)
        {
            arm.localRotation = Quaternion.Euler(0, 0, armEuler.z); // brazo normal
        }
        else
        {
            // Invertir Z para compensar el flip
            arm.localRotation = Quaternion.Euler(0, 180, -armEuler.z);
        }
    }
}
