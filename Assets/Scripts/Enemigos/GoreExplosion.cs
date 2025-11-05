using UnityEngine;

public class GoreExplosion : MonoBehaviour
{
    [Header("Fuerzas base")]
    public float minExplosionForce = 200f;
    public float maxExplosionForce = 500f;

    [Header("Torque base")]
    public float minTorque = -300f;
    public float maxTorque = 300f;

    void Start()
    {
        foreach (var rb in GetComponentsInChildren<Rigidbody2D>())
        {
            if (rb == null) continue;

            // Dirección aleatoria
            Vector2 dir = Random.insideUnitCircle.normalized;

            // Evitar que salgan hacia abajo
            if (dir.y < 0)
                dir.y = -dir.y;

            // Fuerza aleatoria dentro del rango
            float randomForce = Random.Range(minExplosionForce, maxExplosionForce);

            // Aplicar fuerza y torque independientes
            rb.AddForce(dir * randomForce);
            rb.AddTorque(Random.Range(minTorque, maxTorque));
        }
    }
}
