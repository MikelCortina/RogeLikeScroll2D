using UnityEngine;

public class BoneLookAtMouseLocal : MonoBehaviour
{
    [Header("Referencia Local")]
    [Tooltip("Define si el ratón está 'a la izquierda' o 'a la derecha' del pivote (eje Y) de este objeto.")]
    public Transform pivotReference; // Opcional: para usar otro objeto como referencia, si es null, usa el propio Transform

    // --- HUESOS/SPRITES QUE ROTAN ---
    [Header("Huesos/Sprites Lado Izquierdo (Rotan)")]
    [Tooltip("Estos transform se rotarán cuando el ratón esté a la izquierda del pivote del objeto.")]
    public Transform[] leftSideBones;

    [Header("Huesos/Sprites Lado Derecho (Rotan)")]
    [Tooltip("Estos transform se rotarán cuando el ratón esté a la derecha del pivote del objeto.")]
    public Transform[] rightSideBones;

    [Header("Opciones Comunes de Rotación")]
    public bool invertDirection = false;

    [Header("Rotación Extra por Lado")]
    [Tooltip("Ajuste de ángulo adicional cuando el ratón está a la izquierda del objeto.")]
    public float leftExtraRotation = 0f;
    [Tooltip("Ajuste de ángulo adicional cuando el ratón está a la derecha del objeto.")]
    public float rightExtraRotation = 0f;


    // --- SPRITES/GAMEOBJECTS QUE SOLO SE ACTIVAN/DESACTIVAN ---
    [Header("Sprites/GameObjects Lado Izquierdo (Activar/Desactivar)")]
    public GameObject[] leftSideSpritesToToggle;

    [Header("Sprites/GameObjects Lado Derecho (Activar/Desactivar)")]
    public GameObject[] rightSideSpritesToToggle;


    // --- Variables internas ---
    private bool _isMouseOnLeftSide = false;
    private bool _hasInitialized = false;

    void Start()
    {
        // Si no se asigna una referencia, usamos el propio Transform del script.
        if (pivotReference == null)
        {
            pivotReference = transform;
        }

        // Determinar el estado inicial y aplicarlo.
        bool initialMouseOnLeftSide = CheckLocalMousePosition();
        _isMouseOnLeftSide = initialMouseOnLeftSide;
        ApplyToggleState(_isMouseOnLeftSide);

        _hasInitialized = true;
    }

    void Update()
    {
        // 1. Determinar la posición mundial del ratón
        Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorldPos.z = 0f;

        // 2. Comprobar la posición del ratón respecto al objeto (nuevo método de comprobación)
        bool currentMouseOnLeftSide = CheckLocalMousePosition(mouseWorldPos);

        // 3. Gestionar la activación/desactivación de sprites/GameObjects
        if (!_hasInitialized || currentMouseOnLeftSide != _isMouseOnLeftSide)
        {
            ApplyToggleState(currentMouseOnLeftSide);
            _isMouseOnLeftSide = currentMouseOnLeftSide;
            _hasInitialized = true;
        }

        // 4. Determinar la rotación extra y los huesos activos
        float activeExtraRotation = _isMouseOnLeftSide ? leftExtraRotation : rightExtraRotation;
        Transform[] activeBones = _isMouseOnLeftSide ? leftSideBones : rightSideBones;

        // 5. Aplicar la lógica de "mirar al ratón" a los huesos/sprites activos
        ApplyLookAtMouse(activeBones, mouseWorldPos, activeExtraRotation);
    }

    /// <summary>
    /// Comprueba si el ratón está a la izquierda del pivote del objeto en su espacio local.
    /// </summary>
    private bool CheckLocalMousePosition(Vector3? mouseWorldPos = null)
    {
        Vector3 currentMouseWorldPos = mouseWorldPos ?? Camera.main.ScreenToWorldPoint(Input.mousePosition);

        // Convertimos la posición mundial del ratón a espacio local del objeto de referencia
        // Esto tiene en cuenta la posición, rotación y escala del objeto.
        Vector3 mouseLocalPos = pivotReference.InverseTransformPoint(currentMouseWorldPos);

        // El ratón está a la izquierda si su coordenada X local es negativa.
        return mouseLocalPos.x < 0f;
    }

    /// <summary>
    /// Aplica la rotación para que los huesos miren a la posición del ratón.
    /// </summary>
    private void ApplyLookAtMouse(Transform[] bonesToRotate, Vector3 targetPosition, float extraRotationValue)
    {
        foreach (Transform bone in bonesToRotate)
        {
            if (bone == null) continue;

            Vector2 direction = targetPosition - bone.position;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            if (invertDirection)
                angle += 180f;

            angle += extraRotationValue;

            bone.rotation = Quaternion.Euler(0f, 0f, angle);
        }
    }

    /// <summary>
    /// Activa/desactiva los GameObjects basados en si el ratón está a la izquierda del objeto.
    /// </summary>
    private void ApplyToggleState(bool onLeftSide)
    {
        // Activar/desactivar los GameObjects que solo se conmutan
        SetGameObjectsActive(leftSideSpritesToToggle, onLeftSide);
        SetGameObjectsActive(rightSideSpritesToToggle, !onLeftSide);

        // Activar/desactivar los huesos que rotan
        SetGameObjectsActive(GetGameObjectsFromTransforms(leftSideBones), onLeftSide);
        SetGameObjectsActive(GetGameObjectsFromTransforms(rightSideBones), !onLeftSide);
    }

    /// <summary>
    /// Función auxiliar para activar/desactivar una lista de GameObjects.
    /// </summary>
    private void SetGameObjectsActive(GameObject[] gameObjects, bool active)
    {
        if (gameObjects == null) return;
        foreach (GameObject go in gameObjects)
        {
            if (go != null && go.activeSelf != active)
            {
                go.SetActive(active);
            }
        }
    }

    /// <summary>
    /// Convierte un array de Transforms a un array de GameObjects.
    /// </summary>
    private GameObject[] GetGameObjectsFromTransforms(Transform[] transforms)
    {
        if (transforms == null) return new GameObject[0];
        GameObject[] gos = new GameObject[transforms.Length];
        for (int i = 0; i < transforms.Length; i++)
        {
            if (transforms[i] != null)
            {
                gos[i] = transforms[i].gameObject;
            }
        }
        return gos;
    }
}