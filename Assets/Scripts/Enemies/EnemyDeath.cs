using UnityEngine;

[RequireComponent(typeof(Health))]//mismo q en player, necesario x las duads
public class EnemyDeath : MonoBehaviour
{
    //config muerte
    [SerializeField] private float destroyDelay = 2f;//dependera de la animacion de muerte 
    //privadas
    private Health health;
    private Collider enemyCollider;
    private void Awake()
    {
        health = GetComponent<Health>();
        enemyCollider = GetComponent<Collider>();//ya tener referencia desde el comienzo
    }
    private void OnEnable()
    {
        health.onDied.AddListener(HandleDeath);
    }
    private void OnDisable()
    {
        health.onDied.RemoveListener(HandleDeath);
    }
    private void HandleDeath()
    {
        //apagar colisiones para q el player no lo "choque" ya muerto, igual es decision de diseño, vere q quieren luego
        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }

        Destroy(gameObject, destroyDelay);
    }
}