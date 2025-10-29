using UnityEngine;

public interface IPersistentEffect : IEffect
{
    // Se llama cuando el efecto se aplica permanentemente al jugador
    void ApplyTo(GameObject player);

    // (Opcional) para efectos que se puedan remover
    void RemoveFrom(GameObject player);
}
