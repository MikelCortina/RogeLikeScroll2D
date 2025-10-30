using UnityEngine;

public class GoreExplosion : MonoBehaviour
{
    public float explosionForce = 300f;
    public float torqueForce = 200f;

    void Start()
    {
        foreach (var rb in GetComponentsInChildren<Rigidbody2D>())
        {
            Vector2 dir = Random.insideUnitCircle.normalized;

            // Asegurarse de que la dirección no sea hacia abajo
            if (dir.y < 0)
                dir.y = -dir.y;

            rb.AddForce(dir * explosionForce);
            rb.AddTorque(Random.Range(-torqueForce, torqueForce));
        }
    }
}