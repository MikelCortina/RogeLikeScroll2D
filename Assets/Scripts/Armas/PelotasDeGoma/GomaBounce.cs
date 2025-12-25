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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("enemigo"))
        {
            EnemyBase enemy = collision.GetComponentInParent<EnemyBase>();
            if (enemy != null)
            {
              
                float dmg = StatsCommunicator.Instance.CalculateGunDamage();
                enemy.TakeContactDamage(dmg, false);
                ConsoleManager.Instance.Log($"Rubber-coated bullets damage dealed: {dmg}");
            }
        }
    }
}
