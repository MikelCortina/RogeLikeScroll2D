using UnityEngine;

// Interfaz gen�rica para cualquier efecto del juego
public interface IEffect
{
    // Ejecuta el efecto en una posici�n determinada y opcionalmente con un owner
    void Execute(Vector2 position, GameObject owner = null);
}
