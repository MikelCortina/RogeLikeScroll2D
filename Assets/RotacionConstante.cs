using UnityEngine;

public class RotacionConstante2D : MonoBehaviour
{
    [SerializeField] private float velocidadRotacion = 180f;

    void Update()
    {
        transform.Rotate(0, 0, velocidadRotacion * Time.deltaTime);
    }
}
