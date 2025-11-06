using System;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class AngleRangeSpriteSwitcher : MonoBehaviour
{
    [Serializable]
    public struct AngleRangeEntry
    {
        [Tooltip("Ángulo inicial en grados (0..360). Inclusivo.")]
        [Range(0f, 360f)] public float startAngle;
        [Tooltip("Ángulo final en grados (0..360). Exclusivo. Si end < start, el rango envuelve 0°.")]
        [Range(0f, 360f)] public float endAngle;
        [Tooltip("Sprite que se asignará al SpriteRenderer cuando el mouse/touch esté dentro de este rango.")]
        public Sprite sprite;
        [Tooltip("Opcional: si true, al seleccionar este rango el sprite se volteará en X.")]
        public bool flipX;
        [Tooltip("Opcional: color aplicado al sprite cuando está seleccionado. " +
                 "Si no quieres cambiar el color deja el alpha en 0 o desactiva 'overrideColor'.")]
        public Color color;
        [Tooltip("Si true, aplica el color incluso si tiene alpha = 0 (útil para colores transparentes intencionales).")]
        public bool overrideColor;
    }

    public enum InputMode { Mouse, Touch, Both }

    [Header("Rangos angulares")]
    [Tooltip("Lista de rangos angulares y su sprite asociado.")]
    public List<AngleRangeEntry> ranges = new List<AngleRangeEntry>();

    [Header("Referencia y opciones")]
    [Tooltip("Transform desde el que se calcula el ángulo (si es null usa este transform).")]
    public Transform referencePoint;
    [Tooltip("Usar Camera.main para ScreenToWorldPoint (recomendado en 2D).")]
    public bool useMainCamera = true;
    [Tooltip("Si true solo cambiará el sprite cuando cambie el rango actual.")]
    public bool onlyOnChange = true;
    [Tooltip("Sprite opcional por defecto si ningún rango coincide. Dejar vacío para no cambiar.")]
    public Sprite defaultSprite;
    [Tooltip("Color por defecto aplicado cuando ningún rango coincide.")]
    public Color defaultColor = Color.white;

    [Header("Input")]
    public InputMode inputMode = InputMode.Mouse;

    [Header("SpriteRenderer")]
    [Tooltip("SpriteRenderer que será modificado (si lo dejas vacío usa el del mismo GameObject).")]
    public SpriteRenderer targetRenderer;

    [Header("Debug")]
    [Tooltip("Activa para ver logs de qué rango se activa (útil para depurar).")]
    public bool debugLogs = false;

    private SpriteRenderer spriteRenderer;
    private int lastRangeIndex = -1;

    void Awake()
    {
        spriteRenderer = targetRenderer != null ? targetRenderer : GetComponent<SpriteRenderer>();
        if (referencePoint == null) referencePoint = transform;

        // Inicializa color por defecto si corresponde
        if (spriteRenderer != null)
        {
            spriteRenderer.color = defaultColor;
            if (defaultSprite != null) spriteRenderer.sprite = defaultSprite;
        }
    }

    void Update()
    {
        if (spriteRenderer == null) return;

        Vector3 inputScreenPos;
        bool haveInput = GetInputScreenPosition(out inputScreenPos);
        if (!haveInput) return;

        Vector3 worldPos;
        if (useMainCamera && Camera.main != null)
        {
            // Calculamos z correcto para ScreenToWorldPoint usando la posición del referencePoint
            float z = Camera.main.WorldToScreenPoint(referencePoint.position).z;
            worldPos = Camera.main.ScreenToWorldPoint(new Vector3(inputScreenPos.x, inputScreenPos.y, z));
        }
        else
        {
            // Fallback: convertimos la posición de pantalla a world suponiendo que referencePoint está en Z=0 del mundo
            // Nota: si tu escena usa otra cámara, es mejor dejar useMainCamera=true.
            worldPos = Camera.main != null
                ? Camera.main.ScreenToWorldPoint(new Vector3(inputScreenPos.x, inputScreenPos.y, Camera.main.nearClipPlane))
                : new Vector3(inputScreenPos.x, inputScreenPos.y, referencePoint.position.z);
        }

        Vector2 dir = worldPos - referencePoint.position;
        if (dir.sqrMagnitude < 0.0001f) return;

        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg; // -180..180
        if (angle < 0f) angle += 360f; // 0..360

        int idx = FindRangeIndex(angle);
        if (idx < 0)
        {
            // Ningún rango coincide
            if (!onlyOnChange || lastRangeIndex != -1)
            {
                if (debugLogs) Debug.Log($"[AngleRangeSpriteSwitcher] Ningún rango para angle={angle:F1} -> aplicar default.");
                ApplyDefault();
                lastRangeIndex = -1;
            }
            return;
        }

        if (!onlyOnChange || idx != lastRangeIndex)
        {
            if (debugLogs) Debug.Log($"[AngleRangeSpriteSwitcher] angle={angle:F1} -> rango {idx} ({ranges[idx].startAngle}..{ranges[idx].endAngle})");
            ApplyRange(ranges[idx]);
            lastRangeIndex = idx;
        }
    }

    bool GetInputScreenPosition(out Vector3 screenPos)
    {
        screenPos = Vector3.zero;
        // Touch
        if ((inputMode == InputMode.Touch || inputMode == InputMode.Both) && Input.touchCount > 0)
        {
            screenPos = Input.GetTouch(0).position;
            return true;
        }

        // Mouse
        if (inputMode == InputMode.Mouse || inputMode == InputMode.Both)
        {
            screenPos = Input.mousePosition;
            return true;
        }

        return false;
    }

    int FindRangeIndex(float angle)
    {
        for (int i = 0; i < ranges.Count; i++)
        {
            var r = ranges[i];
            if (IsAngleInRange(angle, r.startAngle, r.endAngle))
                return i;
        }
        return -1;
    }

    static bool IsAngleInRange(float angle, float start, float end)
    {
        angle = Normalize360(angle);
        start = Normalize360(start);
        end = Normalize360(end);

        if (Mathf.Approximately(start, end))
        {
            // start == end -> todo el círculo
            return true;
        }

        if (start < end)
        {
            return angle >= start && angle < end;
        }
        else
        {
            // envuelve 0
            return angle >= start || angle < end;
        }
    }

    static float Normalize360(float a)
    {
        a %= 360f;
        if (a < 0f) a += 360f;
        return a;
    }

    void ApplyRange(AngleRangeEntry entry)
    {
        if (entry.sprite != null) spriteRenderer.sprite = entry.sprite;
        spriteRenderer.flipX = entry.flipX;

        // Solo aplicar color si el user lo ha configurado (alpha > 0) o ha marcado overrideColor
        if (entry.overrideColor || entry.color.a > 0.0001f)
        {
            spriteRenderer.color = entry.color;
        }
    }

    void ApplyDefault()
    {
        if (defaultSprite != null) spriteRenderer.sprite = defaultSprite;
        spriteRenderer.color = defaultColor;
        spriteRenderer.flipX = false;
    }

#if UNITY_EDITOR
    // Gizmos para ver los rangos en Scene view
    void OnDrawGizmosSelected()
    {
        if (referencePoint == null) referencePoint = transform;
        Vector3 origin = referencePoint.position;
        float radius = 1f;

        if (ranges != null && ranges.Count > 0)
        {
            for (int i = 0; i < ranges.Count; i++)
            {
                var r = ranges[i];
                DrawRangeGizmo(origin, radius, r.startAngle, r.endAngle, i);
            }
        }

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(origin, 0.05f);
    }

    void DrawRangeGizmo(Vector3 origin, float radius, float start, float end, int index)
    {
        float s = Normalize360(start);
        float e = Normalize360(end);

        float totalAngle;
        if (Mathf.Approximately(s, e))
            totalAngle = 360f;
        else if (s < e)
            totalAngle = e - s;
        else
            totalAngle = (360f - s) + e;

        int segments = Mathf.Max(2, Mathf.CeilToInt((32 * totalAngle) / 360f));
        Vector3 prev = origin + AngleToVector(s) * radius;
        for (int k = 1; k <= segments; k++)
        {
            float t = (k / (float)segments) * totalAngle;
            float ang = s + t;
            ang = Normalize360(ang);
            Vector3 next = origin + AngleToVector(ang) * radius;
            Gizmos.color = Color.Lerp(Color.green, Color.red, (float)k / segments);
            Gizmos.DrawLine(prev, next);
            prev = next;
        }
    }

    static Vector3 AngleToVector(float angleDeg)
    {
        float a = angleDeg * Mathf.Deg2Rad;
        return new Vector3(Mathf.Cos(a), Mathf.Sin(a), 0f);
    }
#endif
}
