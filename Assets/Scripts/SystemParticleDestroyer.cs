using UnityEngine;

public class SystemParticleDestroyer : MonoBehaviour
{
    [Tooltip("Tiempo en segundos antes de destruir el objeto")]
    public float tiempoDeVida = 1f;

    void Start()
    {
        Destroy(gameObject, tiempoDeVida);
    }
    // Update is called once per frame
    void Update()
    {
        
    }

   
}
