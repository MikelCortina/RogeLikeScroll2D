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

    [Header("Upgrade Object")]
    public GameObject upgradeObject; // GameObject con SpriteRenderer
    private SpriteRenderer upgradeRenderer;

    private bool cinematicPlaying = false;
    private bool cinematicFinished = false;

    private IObjetos currentUpgrade;

    private void Awake()
    {
        Instance = this;

        if (continueButton != null)
        {
            continueButton.onClick.RemoveAllListeners();
            continueButton.onClick.AddListener(OnPressButton);
        }

        if (upgradeObject != null)
        {
            upgradeRenderer = upgradeObject.GetComponent<SpriteRenderer>();
            if (upgradeRenderer != null)
                upgradeObject.SetActive(false); // ocultamos al inicio
        }
    }

    private void Update()
    {
        if (cinematicPanel.activeSelf && Input.GetMouseButtonDown(0))
        {
            if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
                OnPressButton();
        }
    }

    public void StartCinematic()
    {
        cinematicPlaying = true;
        cinematicFinished = false;

        cinematicPanel.SetActive(true);
        particleLoop.SetActive(false);

        // Elegimos un objeto aleatorio
        currentUpgrade = ObjectManager.Instance.GetRandomObject();

        // NO activamos el upgradeObject aquí
        if (upgradeRenderer != null && currentUpgrade != null)
        {
            upgradeRenderer.sprite = currentUpgrade.icon;
        }

        if (continueButton != null)
            continueButton.gameObject.SetActive(true);

        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;

        cinematic.Play();
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

        // Activamos el upgradeObject justo después de la animación
        if (upgradeObject != null && currentUpgrade != null)
            upgradeObject.SetActive(true);

        if (continueButton != null)
            continueButton.gameObject.SetActive(true);
    }

    public void OnPressButton()
    {
        if (cinematicPlaying)
        {
            cinematic.time = cinematic.duration;
            cinematic.Evaluate();
            cinematic.Stop();
            cinematicPlaying = false;
            cinematicFinished = true;
            return;
        }

        if (cinematicFinished)
        {
            cinematicPanel.SetActive(false);
            particleLoop.SetActive(true);

            if (currentUpgrade != null)
                currentUpgrade.ApplyEffect(); // aplicamos la mejora

            if (upgradeObject != null)
                upgradeObject.SetActive(false); // ocultamos objeto

            Time.timeScale = 1f;

            Cursor.visible = false;
            Cursor.lockState = CursorLockMode.Locked;

            cinematicFinished = false;
            currentUpgrade = null;
        }
    }
}
