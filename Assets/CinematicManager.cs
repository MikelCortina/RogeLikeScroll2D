using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UI;
using System.Collections;

public class CinematicManager : MonoBehaviour
{
    public static CinematicManager Instance;

    [Header("Cinematic Components")]
    public PlayableDirector cinematic;
    public GameObject cinematicPanel;
    public GameObject particleLoop;
    public Button continueButton;

    private bool cinematicPlaying = false;
    private bool cinematicFinished = false;

    private void Awake()
    {
        Instance = this;

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnPressButton);
        }
    }
    private void Update()
    {
        // Permitir que el botón sea pulsable aunque Time.timeScale = 0
        if (cinematicPanel.activeSelf && Input.GetMouseButtonDown(0))
        {
            // Raycast manual hacia el UI
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
            {
                // Llamamos al botón manualmente
                OnPressButton();
            }
        }
    }
    public void StartCinematic()
    {
        cinematicPlaying = true;
        cinematicFinished = false;

        cinematicPanel.SetActive(true);
        particleLoop.SetActive(false);

        if (continueButton != null)
            continueButton.gameObject.SetActive(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        cinematic.Play();

        // Pausar un frame después para registrar input
        StartCoroutine(PauseNextFrame());

        cinematic.stopped += OnCinematicFinished;
    }

    private IEnumerator PauseNextFrame()
    {
        yield return null;
        Time.timeScale = 0f;
    }

    private void OnCinematicFinished(PlayableDirector dir)
    {
        cinematicPlaying = false;
        cinematicFinished = true;

        particleLoop.SetActive(true);

        // El botón sigue activo para reanudar juego
        if (continueButton != null)
            continueButton.gameObject.SetActive(true);
    }

    public void OnPressButton()
    {
        // Caso 1: la cinemática sigue → saltarla
        if (cinematicPlaying)
        {
            cinematic.time = cinematic.duration;
            cinematic.Evaluate();
            cinematic.Stop();

            cinematicPlaying = false;
            cinematicFinished = true;
            return;
        }

        // Caso 2: la cinemática terminó → ocultar panel y reanudar juego
        if (cinematicFinished)
        {
            cinematicPanel.SetActive(false);
            particleLoop.SetActive(true);

            Time.timeScale = 1f;

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            cinematicFinished = false;
        }
    }
}
