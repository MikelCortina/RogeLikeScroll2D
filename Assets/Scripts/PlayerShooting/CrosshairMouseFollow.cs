using System.Linq;
using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(RectTransform))]
public class CrosshairMouseFollow : MonoBehaviour
{
    [Header("References")]
    [Tooltip("RectTransform del Image que actúa como mirilla (NO el mismo GameObject del script idealmente).")]
    [SerializeField] private RectTransform crosshairRect;

    [Tooltip("Canvas que contiene la mirilla (si no se asigna se busca en los padres).")]
    [SerializeField] private Canvas canvas;

    [Header("UI blocking")]
    [Tooltip("Paneles que, si están activos, desactivan la mirilla y reactivan el cursor.")]
    [SerializeField] private GameObject[] blockingPanels;

    [Header("Cursor / Lock configuration")]
    [Tooltip("Lock mode que queremos durante el gameplay (mirilla visible).")]
    [SerializeField] private CursorLockMode gameplayLockMode = CursorLockMode.Locked;
    [Tooltip("Lock mode que queremos cuando hay UI (mirilla oculta).")]
    [SerializeField] private CursorLockMode uiLockMode = CursorLockMode.None;
    [Tooltip("Ocultar cursor del sistema durante gameplay (mirilla visible).")]
    [SerializeField] private bool hideCursorDuringGameplay = true;

    // --- estado interno ---
    private Image crosshairImage;
    private RectTransform canvasRect;
    private Camera canvasCamera;
    private Camera mainCamera;

    // Estado actual para evitar llamadas redundantes
    private bool isCrosshairModeActive = false;

    // Guardamos el estado original para restaurar si se desactiva todo el script
    private bool initialCursorVisible;
    private CursorLockMode initialLockMode;

    private void Awake()
    {
        if (crosshairRect == null) crosshairRect = GetComponent<RectTransform>();
        if (canvas == null) canvas = GetComponentInParent<Canvas>();

        crosshairImage = crosshairRect != null ? crosshairRect.GetComponent<Image>() : null;
        canvasRect = canvas != null ? canvas.GetComponent<RectTransform>() : null;
        mainCamera = Camera.main;

        if (canvas != null)
            canvasCamera = canvas.renderMode == RenderMode.ScreenSpaceCamera ? (canvas.worldCamera ?? mainCamera) : null;

        // Guardamos estado original para restaurar en OnDisable
        initialCursorVisible = Cursor.visible;
        initialLockMode = Cursor.lockState;

        if (crosshairImage != null) crosshairImage.raycastTarget = false;
    }

    private void OnEnable()
    {
        // Forzamos estado inicial coherente según paneles bloqueantes
        ForceRefresh(forceApply: true);
    }

    private void Update()
    {
        bool shouldShow = !IsAnyBlockingPanelActive();

        // Cambiar visibilidad / cursor sólo si ha cambiado el modo
        if (shouldShow != isCrosshairModeActive)
            SetCrosshairMode(shouldShow);

        if (!shouldShow) return;

        // Actualizar posición de la mirilla (solo si está visible)
        if (crosshairRect == null) return;

        if (canvas == null || canvas.renderMode == RenderMode.WorldSpace)
        {
            if (mainCamera == null) mainCamera = Camera.main;
            if (mainCamera == null) return;

            Vector3 mouse = Input.mousePosition;
            float z = Mathf.Max(mainCamera.nearClipPlane + 0.1f, 0.1f);
            Vector3 worldPos = mainCamera.ScreenToWorldPoint(new Vector3(mouse.x, mouse.y, z));
            crosshairRect.position = worldPos;
        }
        else
        {
            if (canvasRect == null) return;
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, Input.mousePosition, canvasCamera, out localPoint);
            crosshairRect.anchoredPosition = localPoint;
        }
    }

    private bool IsAnyBlockingPanelActive()
    {
        if (blockingPanels == null || blockingPanels.Length == 0) return false;
        return blockingPanels.Any(p => p != null && p.activeInHierarchy);
    }

    /// <summary>
    /// Cambia entre modo mirilla (true) y modo cursor/UI (false).
    /// </summary>
    private void SetCrosshairMode(bool showCrosshair)
    {
        // Evitar trabajo innecesario
        if (showCrosshair == isCrosshairModeActive) return;
        isCrosshairModeActive = showCrosshair;

        if (showCrosshair)
        {
            // Mostrar mirilla visualmente (no desactivar el GameObject del script)
            if (crosshairRect != null) crosshairRect.gameObject.SetActive(true);
            if (crosshairImage != null) crosshairImage.enabled = true;

            // Cursor y lock para gameplay
            if (hideCursorDuringGameplay) Cursor.visible = false;
            Cursor.lockState = gameplayLockMode;
        }
        else
        {
            // Ocultar solo la mirilla visual (no desactivar el GameObject del script)
            if (crosshairImage != null) crosshairImage.enabled = false;
            if (crosshairRect != null) crosshairRect.gameObject.SetActive(false);

            // Cursor y lock para UI
            Cursor.visible = true;
            Cursor.lockState = uiLockMode;
        }
    }

    /// <summary>
    /// Fuerza la sincronización (útil si otro script cambió paneles / lock state).
    /// </summary>
    public void ForceRefresh(bool forceApply = false)
    {
        bool shouldShow = !IsAnyBlockingPanelActive();
        if (forceApply)
        {
            // Aplicar aunque el flag sea igual
            isCrosshairModeActive = !shouldShow; // para que SetCrosshairMode lo aplique
        }
        SetCrosshairMode(shouldShow);

        // Si mostramos, actualizar la posición inmediatamente
        if (shouldShow && crosshairRect != null && canvas != null)
        {
            Vector2 localPoint;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, Input.mousePosition, canvasCamera, out localPoint);
            crosshairRect.anchoredPosition = localPoint;
        }
    }

    private void OnDisable()
    {
        // Restaurar estado original por seguridad
        Cursor.visible = initialCursorVisible;
        Cursor.lockState = initialLockMode;
    }

    private void OnApplicationFocus(bool hasFocus)
    {
        if (!hasFocus)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
        }
        else
        {
            // Forzar refresco al volver a foco para evitar estados inconsistentes
            ForceRefresh(forceApply: true);
        }
    }

#if UNITY_EDITOR
    private void Reset()
    {
        crosshairRect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
    }
#endif
}
