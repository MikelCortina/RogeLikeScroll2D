using UnityEngine;

/// <summary>
/// Añadir a transform hijo dentro del prefab de plataforma para marcarlo como spawn point.
/// waveSpace: el valor que usas para seleccionar spawn points (1, 2, 5, 10, etc).
/// isTower: si este punto es exclusivo para torres/gusanos.
/// </summary>
public class SpawnPoint : MonoBehaviour
{
    [Tooltip("Valor de waveSpace para el que debería usarse este spawn point. Ej: 1, 2, 5, 10.")]
    public float waveSpace = 1f;

    [Tooltip("Marcar si este punto es exclusivo para spawn de torres u otros spawns especiales.")]
    public bool isTower = false;
}
