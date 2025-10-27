using UnityEngine;
using System.Collections;
using TMPro; // Aseg�rate de tener esto si usas TextMeshPro

public class PickupEffectItem : MonoBehaviour
{
     private ScriptableObject effectToActivate;
    [SerializeField] private ScriptableObject[] effectList; // Lista de efectos para mostrar
    [SerializeField] private TextMeshProUGUI effectTextUI; // UI para mostrar los nombres
    [SerializeField] private GameObject textObjectPanel;
    [SerializeField] private float displayDuration = 2f; // Duraci�n total de la coroutine
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

        // Elegimos aleatoriamente el efecto que se activar�
        effectToActivate = effectList[Random.Range(0, effectList.Length)];

        float elapsed = 0f;
        int index = 0;

        while (elapsed < displayDuration)
        {
            // Mostrar nombres aleatorios de la lista, pero no afectar al efecto final
            effectTextUI.text = effectList[index].name;
            index = (index + 1) % effectList.Length;

            yield return new WaitForSecondsRealtime(nameChangeInterval);
            elapsed += nameChangeInterval;
        }

        // Mostrar finalmente el nombre del efecto que se aplicar�
        effectTextUI.text = effectToActivate.name;
        yield return new WaitForSecondsRealtime(0.5f); // un peque�o delay para que se vea

        textObjectPanel.SetActive(false);

        // Activamos el efecto seleccionado
        RunEffectManager.Instance.ActivateEffect(effectToActivate);

        // Aplicamos el efecto si es persistente
        if (effectToActivate is IPersistentEffect persistentEffect)
        {
            GameObject player = GameObject.FindWithTag("Player");
            if (player != null)
                persistentEffect.ApplyTo(player);
        }

        Time.timeScale = 1f;
        Destroy(gameObject);
    }


}