using UnityEngine;
using System.Collections;
using TMPro; // Asegúrate de tener esto si usas TextMeshPro

public class PickupEffectItem : MonoBehaviour
{
     private ScriptableObject effectToActivate;
    [SerializeField] private ScriptableObject[] effectList; // Lista de efectos para mostrar
    [SerializeField] private TextMeshProUGUI effectTextUI; // UI para mostrar los nombres
    [SerializeField] private GameObject textObjectPanel;
    [SerializeField] private float displayDuration = 2f; // Duración total de la coroutine
    [SerializeField] private float nameChangeInterval = 0.1f; // Intervalo entre nombres


    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") )
        {
            Time.timeScale = 0f; // Pausar el juego
            StartCoroutine(ShowEffectNamesAndActivate());
        }
    }


    public IEnumerator ShowEffectNamesAndActivate()
    {
        textObjectPanel.SetActive(true);

        // Elegimos aleatoriamente el efecto que se activará
        effectToActivate = effectList[Random.Range(0, effectList.Length)];

        float elapsed = 0f;
        int index = Random.Range(0, effectList.Length);

        while (elapsed < displayDuration)
        {
            effectTextUI.text = effectList[index].name;
            index = (index + 1) % effectList.Length;

            yield return new WaitForSecondsRealtime(nameChangeInterval);
            elapsed += nameChangeInterval;
        }

        textObjectPanel.SetActive(false);

        // Activamos el efecto seleccionado
        RunEffectManager.Instance.ActivateEffect(effectToActivate);

        Time.timeScale = 1f;
        Destroy(gameObject);
    }

}