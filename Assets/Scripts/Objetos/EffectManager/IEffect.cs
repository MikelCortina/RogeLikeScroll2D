using UnityEngine;

// Interfaz genérica para cualquier efecto del juego
public interface IEffect
{
    // Ejecuta el efecto en una posición determinada y opcionalmente con un owner
    void Execute(Vector2 position, GameObject owner = null);
}
