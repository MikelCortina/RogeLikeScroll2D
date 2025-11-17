using UnityEngine;

public class UnscaledParticlePlayer : MonoBehaviour
{
    private ParticleSystem ps;

    void Awake()
    {
        ps = GetComponent<ParticleSystem>();
    }

    void Update()
    {
        // Simula con tiempo NO escalado
        ps.Simulate(Time.unscaledDeltaTime, true, false, false);
    }
}
