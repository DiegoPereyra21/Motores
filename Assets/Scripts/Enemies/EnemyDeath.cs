using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Health))]//mismo q en player, necesario x las duads
public class EnemyDeath : MonoBehaviour
{
    //config muerte
    [SerializeField] private float corpseCleanupDelay = 120f;//tiempo de respaldo para limpiar el cuerpo si nadie lo saquea
    [SerializeField] private float lootedDestroyDelay = 0.5f;//delay al destruir despues de saqueado, para sonido/efecto
    //privadas
    private Health health;
    private Collider enemyCollider;
    private NavMeshAgent agent;//para frenarlo al morir luego de cachear su info
    private ZombieControllerNavMesh zombieController;//ia del zombie, se apaga al morir

    private void Awake()
    {
        health = GetComponent<Health>();
        enemyCollider = GetComponent<Collider>();//ya tener referencia desde el comienzo
        agent = GetComponent<NavMeshAgent>();//cache del navmesh
        zombieController = GetComponent<ZombieControllerNavMesh>();//cache de la ia

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
        //apaga la ia para q no siga persiguiendo/atacando
        if (zombieController != null)
        {
            zombieController.enabled = false;
        }

        //apaga el agente al morir, sino bugs 
        if (agent != null)
        {
            agent.isStopped = true;
            agent.enabled = false;
        }

        //dejo el collider prendido a proposito, asi el raycast de interaccion puede pegarle al cuerpo

        //timer de respaldo por si el player nunca lo saquea
        Invoke(nameof(DestroyCorpse), corpseCleanupDelay);
    }

    //se llama desde el onlooted de lootable, cuando el player ya lo saqueo
    public void OnCorpseLooted()
    {
        //cancelo el timer de respaldo, ya no hace falta
        CancelInvoke(nameof(DestroyCorpse));

        //destruyo con un pequeño delay por si hay sonido/efecto de saqueo
        Invoke(nameof(DestroyCorpse), lootedDestroyDelay);
    }

    private void DestroyCorpse()
    {
        Destroy(gameObject);
    }
}