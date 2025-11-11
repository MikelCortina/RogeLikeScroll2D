using UnityEngine;
using System.Collections.Generic;

public class FollowSpritePoint : MonoBehaviour
{
    [Header("Objeto que tiene el SpriteRenderer")]
    public SpriteRenderer spriteRenderer;

    [Header("Número del punto del Physics Shape (normalmente 0)")]
    public int pointIndex = 0;

    [Header("Offset adicional")]
    public float offsetX = 0f;
    public float offsetY = 0f;

    private List<Vector2> points = new List<Vector2>();
    public RiderController RiderController;

    void LateUpdate()
    {
        if (RiderController.isAttached == true)
        {
            Sprite sprite = spriteRenderer.sprite;
            if (sprite == null) return;

            // Obtener puntos del Physics Shape
            points.Clear();
            sprite.GetPhysicsShape(0, points);

            if (points.Count == 0) return;
            if (pointIndex >= points.Count) pointIndex = 0;

            // Punto en local del sprite
            Vector2 localPoint = points[pointIndex];

            // Convertir a mundo
            Vector3 world = spriteRenderer.transform.TransformPoint(localPoint);

            // Aplicar offset
            world.x += offsetX;
            world.y += offsetY;

            // Aplicar posición final
            transform.position = world;
        }
    }
}
