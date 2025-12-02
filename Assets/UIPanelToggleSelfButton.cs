using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIPanelToggleSelfButton : MonoBehaviour
{
    [Header("Panel a mover (si no se asigna se usará el mismo objeto)")]
    public RectTransform panel;

    [Header("Configuración de movimiento")]
    public Vector2 direccion = new Vector2(1, 0);
    public float distancia = 300f;
    public float duracion = 0.3f;

    private Vector2 posicionInicial;
    private Vector2 posicionAbierta;
    private bool abierto = false;
    private bool animando = false;
    private Button boton;

    private void Awake()
    {
        if (panel == null)
        {
            panel = GetComponent<RectTransform>();
            Debug.Log("[UIPanelToggle] No se asignó panel, usando el del mismo objeto");
        }

        boton = GetComponent<Button>();
        if (boton == null)
        {
            boton = gameObject.AddComponent<Button>();
            Debug.Log("[UIPanelToggle] No había Button, se añadió automáticamente");
        }

        boton.onClick.AddListener(TogglePanel);
        Debug.Log("[UIPanelToggle] Listener de botón añadido");
    }

    private void Start()
    {
        posicionInicial = panel.anchoredPosition;
        posicionAbierta = posicionInicial + direccion.normalized * distancia;

        Debug.Log("[UIPanelToggle] Script iniciado. Posición inicial = " + posicionInicial + " | Abierta = " + posicionAbierta);
    }

    public void TogglePanel()
    {
        Debug.Log("[UIPanelToggle] BOTÓN PULSADO — estado: " + (abierto ? "Abierto" : "Cerrado") + ", animando=" + animando);

        if (!animando)
        {
            Debug.Log("[UIPanelToggle] → INICIANDO CORRUTINA");
            StartCoroutine(MoverPanel(abierto ? posicionInicial : posicionAbierta));
            abierto = !abierto;
        }
        else
        {
            Debug.Log("[UIPanelToggle] → BLOQUEADO, animando=true");
        }
        panel.transform.SetAsLastSibling();
    }

    private System.Collections.IEnumerator MoverPanel(Vector2 destino)
    {
        animando = true;
        Vector2 origen = panel.anchoredPosition;
        float tiempo = 0f;

        Debug.Log("[UIPanelToggle] Moviendo panel desde " + origen + " → " + destino);

        while (tiempo < duracion)
        {
            panel.anchoredPosition = Vector2.Lerp(origen, destino, tiempo / duracion);
            tiempo += Time.unscaledDeltaTime;
            Debug.Log("[UIPanelToggle] Progreso animación: " + (tiempo / duracion).ToString("F2"));
            yield return null;
        }

        panel.anchoredPosition = destino;
        animando = false;
        Debug.Log("[UIPanelToggle] Movimiento terminado. Nueva posición = " + destino);
    }
}
