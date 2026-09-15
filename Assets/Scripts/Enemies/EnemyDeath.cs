using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Health))]//mismo q en player, necesario x las duads
public class EnemyDeath : MonoBehaviour
{
    //config muerte
    [SerializeField] private float destroyDelay = 2f;//dependera de la animacion de muerte(PONGO EN 0 PORQUE AL HACER TEST ES RARO, LUEGO CAMBIAR)
    //privadas
    private Health health;
    private Collider enemyCollider;
    private NavMeshAgent agent;//para frenarlo al morir luego de cachear su info
    
    private void Awake()
    {
        health = GetComponent<Health>();
        enemyCollider = GetComponent<Collider>();//ya tener referencia desde el comienzo
        agent = GetComponent<NavMeshAgent>();//cache del navmesh

    }
    private void OnEnable()
    {
        health.onDied.AddListener(HandleDeath);
    }
    private void OnDisable()
    {
        health.onDied.RemoveListener(HandleDeath);
    }
    private void HandleDeath()//se llenara de ifs
    {
        //apaga el agente al morir, sino bugs 
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }
    
        //apagar colisiones para q el player no lo "choque" ya muerto, igual es decision de diseño, vere q quieren luego
        if (enemyCollider != null)
        {
            enemyCollider.enabled = false;
        }

        Destroy(gameObject, destroyDelay);
    }
}