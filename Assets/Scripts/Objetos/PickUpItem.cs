using UnityEngine;
using System.Collections;
using TMPro; // Asegúrate de tener esto si usas TextMeshPro

public class PickupEffectItem : MonoBehaviour
{
    [SerializeField] private ScriptableObject effectToActivate;
    [SerializeField] private ScriptableObject[] effectList; // Lista de efectos para mostrar
    [SerializeField] private TextMeshProUGUI effectTextUI; // UI para mostrar los nombres
    [SerializeField] private GameObject textObjectPanel;
    [SerializeField] private float displayDuration = 2f; // Duración total de la coroutine
    [SerializeField] private float nameChangeInterval = 0.1f; // Intervalo entre nombres

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Time.timeScale = 0f; // Pausar el juego
            StartCoroutine(ShowEffectNamesAndActivate());
        }
    }

    private IEnumerator ShowEffectNamesAndActivate()
    {
        float elapsed = 0f;
        int index = 0;

        while (elapsed < displayDuration)
        {
            textObjectPanel.SetActive(true);
            effectTextUI.text = effectList[index].name;
            index = (index + 1) % effectList.Length;
            yield return new WaitForSecondsRealtime(nameChangeInterval);
            elapsed += nameChangeInterval;
        }

        textObjectPanel.SetActive(false);
        // Activar el efecto final (el que estaba en pantalla)
        RunEffectManager.Instance.ActivateEffect(effectToActivate);

        Time.timeScale = 1f; // Reanudar el juego
        Destroy(gameObject); // Recogerlo una sola vez
    }
}