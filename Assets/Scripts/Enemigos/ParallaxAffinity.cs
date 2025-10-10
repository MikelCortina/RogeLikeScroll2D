using UnityEngine;

/// <summary>
/// Componente simple que indica la layer de parallax a la que pertenece este GameObject.
/// Pon este componente en el prefab del enemigo (o en el prefab de props) y define layerIndex.
/// </summary>
public class ParallaxAffinity : MonoBehaviour
{
    [Tooltip("�ndice de layer en ParallaxController.layers (0 = m�s cercano).")]
    public int layerIndex = 0;
}
