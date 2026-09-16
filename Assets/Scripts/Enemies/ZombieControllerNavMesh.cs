using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))] //obligatorio para el zombiecontrollernavmesh, no para el otro script que solo persigue sin importar obstaculos
public class ZombieControllerNavMesh : MonoBehaviour
{
    //movimiento
    [SerializeField] private float moveSpeed = 2.5f;
    [SerializeField] private float rotationSpeed = 8f; //que tan rapido gira,poquito porque sino se ve raro el cambio, luego ajustar para q quede con la animacion de giro q pongamos
    //referencias
    [SerializeField] private Transform player; //si me olvido de asignar se autoasigna 
    //ataque (lo lee ZombieAttack, asi no duplico logica)
    [SerializeField] private float attackRange = 1.5f;
    [SerializeField] private float attackRangeBuffer = 0.3f; //margen extra para salir del rango de ataque. Sin esto, justo en el limite del attackRange el agente entraba y salia del estado "parado" frame a frame y terminaba empujando al player.
    //privadas
    private NavMeshAgent agent;
    private Health playerHealth; //para saber si ya murio
    private bool inAttackRange; //estado, lo actualiza UpdateAttackRangeState() en vez de recalcularlo de una con un solo umbral
    //propiedades publicas para q zombie attack pueda leerlas y no duplicar cosas
    public Transform Player => player;
    public bool InAttackRange => inAttackRange;
    public bool IsPlayerDead => playerHealth != null && playerHealth.IsDead; //verdadero si el player murio

    private void Awake()
    {
        agent = GetComponent<NavMeshAgent>(); //cache
        agent.speed = moveSpeed;
        agent.stoppingDistance = attackRange; //frena al acercarse, para q no quede robotico
        //lo mas importante, para q no gire solo, sino q lo haga con la logica de rotacion de este script
        agent.updateRotation = false;
    }
    private void Start()
    {
        //por las dudas, lo busca por tag en caso de q no me de cuenta de asignarlo antes
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
            else
                Debug.LogWarning("Te olvidaste de pnnerle el tag al player");
        }

        //cachea la ref
        if (player != null)
            playerHealth = player.GetComponent<Health>();
    }
    private void Update()
    {
        //IMPORTAe, para prevenir q el agente no intente moverse ni nada si es q esta mal colocado o si esta desactivado
        if (!agent.enabled || !agent.isOnNavMesh) return;

        //sin player o ya muerto, se queda donde esta (lluego vemos si le ponemos alguna animacion comiendose al player o algoasi)
        if (player == null || IsPlayerDead)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            return;
        }

        UpdateAttackRangeState();

        //frena el movimiento si esta en rango, y intenta golpear con zombieattack
        if (inAttackRange)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero; //isStopped solo no frena instantaneo, el agent sigue desacelerando solo unos frames y eso era lo que empujaba al player
            RotateTowards(player.position - transform.position);
            return;
        }

        //si se va del rango lo seguira persiguiendo
        agent.isStopped = false;
        agent.SetDestination(player.position);
        //importante, para q mire hacia donde va, no hacia el player todo el rato
        RotateTowards(agent.desiredVelocity);
    }

    //actualiza inAttackRange con histeresis: entra en rango de ataque a attackRange, pero solo sale cuando supera attackRange + attackRangeBuffer.
    private void UpdateAttackRangeState()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        if (inAttackRange)
        {
            if (distance > attackRange + attackRangeBuffer)
                inAttackRange = false;
        }
        else
        {
            if (distance <= attackRange)
                inAttackRange = true;
        }
    }
    private void RotateTowards(Vector3 direction)
    {
        direction.y = 0f; //ignora dif de altura, sino giraria raro si el player esta mas arriba o abjao
        //prevenir warning en caso de no haber movimiento
        if (direction.sqrMagnitude < 0.01f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
    }
}