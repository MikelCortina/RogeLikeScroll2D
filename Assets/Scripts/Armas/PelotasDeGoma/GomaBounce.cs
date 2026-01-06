using UnityEngine;

public class BolaGoma : MonoBehaviour
{
    private int rebotesRestantes;
    private float tiempoVidaRestante;
    private BolaDeGoma efectoPadre;

    public void Inicializar(int maxRebotes, float lifetime, BolaDeGoma padre)
    {
        rebotesRestantes = maxRebotes;
        tiempoVidaRestante = lifetime;
        efectoPadre = padre;
    }

    public void ResetValues()
    {
        rebotesRestantes = 0;
        tiempoVidaRestante = 0;
        efectoPadre = null;
    }

    private void Update()
    {
        if (efectoPadre == null) return;

        tiempoVidaRestante -= Time.deltaTime;
        if (tiempoVidaRestante <= 0)
        {
            efectoPadre.ReturnToPool(gameObject);
        }
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (efectoPadre == null) return;

        rebotesRestantes--;
        if (rebotesRestantes <= 0)
        {
            efectoPadre.ReturnToPool(gameObject);
        }
    }


}
