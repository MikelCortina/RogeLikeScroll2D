using UnityEngine;
using System.Collections.Generic;

public class ContinuousRandomMover : MonoBehaviour
{
    [Header("Objetos a mover")]
    public List<GameObject> objectsToMove;

    [Header("Puntos de destino")]
    public List<Transform> waypoints;

    [Header("Configuración")]
    public float speed = 1f; // velocidad de movimiento

    private List<Transform> currentDestinations;

    void Start()
    {
        currentDestinations = new List<Transform>();
        for (int i = 0; i < objectsToMove.Count; i++)
        {
            currentDestinations.Add(GetRandomWaypoint());
        }
    }

    void Update()
    {
        float deltaTime = Time.unscaledDeltaTime;

        for (int i = 0; i < objectsToMove.Count; i++)
        {
            if (objectsToMove[i] == null) continue;

            // Si no hay destino, elegimos uno
            if (currentDestinations[i] == null)
            {
                currentDestinations[i] = GetRandomWaypoint();
                if (currentDestinations[i] == null) continue;
            }

            // Movemos el objeto hacia su destino
            objectsToMove[i].transform.position = Vector3.MoveTowards(
                objectsToMove[i].transform.position,
                currentDestinations[i].position,
                speed * deltaTime
            );

            // Si llegó al destino, elegimos inmediatamente otro
            if (Vector3.Distance(objectsToMove[i].transform.position, currentDestinations[i].position) < 0.01f)
            {
                currentDestinations[i] = GetRandomWaypoint();
            }
        }
    }

    Transform GetRandomWaypoint()
    {
        if (waypoints.Count == 0) return null;
        return waypoints[Random.Range(0, waypoints.Count)];
    }
}
