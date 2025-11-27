using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(Image))]
public class RaycastAlphaTarget : Image
{
    // Umbral mínimo de alpha para considerar el pixel como "interactivo"
    [Range(0f, 1f)]
    public float alphaThreshold = 0.1f;

    public override bool Raycast(Vector2 sp, Camera eventCamera)
    {
        if (!base.Raycast(sp, eventCamera))
            return false;

        if (sprite == null)
            return false;

        RectTransform rt = transform as RectTransform;

        // Convertir el punto de pantalla a coordenadas locales del rect
        Vector2 localPoint;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(rt, sp, eventCamera, out localPoint);

        // Normalizar el punto al rango [0,1] dentro del rect
        Rect rect = rt.rect;
        float x = (localPoint.x - rect.x) / rect.width;
        float y = (localPoint.y - rect.y) / rect.height;

        // Convertir a coordenadas de píxel de la textura
        int texX = Mathf.RoundToInt(x * sprite.texture.width);
        int texY = Mathf.RoundToInt(y * sprite.texture.height);

        // Evitar desbordamientos
        if (texX < 0 || texX >= sprite.texture.width || texY < 0 || texY >= sprite.texture.height)
            return false;

        try
        {
            Color pixel = sprite.texture.GetPixel(texX, texY);
            return pixel.a >= alphaThreshold;
        }
        catch
        {
            return false;
        }
    }
}
