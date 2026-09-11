using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ZombieController : MonoBehaviour
{
    //movimiento
    [SerializeField] private float moveSpeed = 2.5f; //velocidad al perseguir
    [SerializeField] private float rotationSpeed = 8f; //que tan rapido gira hacia el player
    [SerializeField] private float gravity = -15f; //misma logica que el playercontroller

    //referencias
    [SerializeField] private Transform player; //si se deja vacio, se busca solo por tag

    //ataque (lo lee ZombieAttack)
    [SerializeField] private float attackRange = 1.5f; //distancia minima para dejar de moverse y atacar

    //privadas
    private CharacterController controller;
    private float verticalVelocity;

    //propiedades publicas, ZombieAttack usa de aca
    public Transform Player => player;
    public bool InAttackRange =>
        player != null && Vector3.Distance(transform.position, player.position) <= attackRange;

    private void Awake()
    {
        controller = GetComponent<CharacterController>(); //cache del componente
    }

    private void Start()
    {
        //busca por tag el player sino se asigno manualmente
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
            else
                Debug.LogWarning($"{name}: no se encontro ningun GameObject con tag 'Player'. Asignalo manualmente en el inspector o revisa el tag del jugador.");
        }
    }

    private void Update()
    {
        if (player == null) return; 

        //si ya esta en rango de ataque, deja de avanzar y solo aplica gravedad (ZombieAttack hace el resto)
        if (InAttackRange)
        {
            ApplyGravityOnly();
            return;
        }

        //direccion al player
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;
        direction.Normalize();

        //rotacion suave
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);
        }

        UpdateVerticalVelocity();

        //mover el character controller (horizontal + gravedad)
        Vector3 move = direction * moveSpeed + Vector3.up * verticalVelocity;
        controller.Move(move * Time.deltaTime);
    }

    private void UpdateVerticalVelocity()
    {
        //mismo criterio que el PlayerController: pega al piso si esta grounded, sino acumula gravedad
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f;
        else
            verticalVelocity += gravity * Time.deltaTime;
    }

    private void ApplyGravityOnly()
    {
        //se usa cuando el zombie esta quieto atacando, para que no quede flotando
        UpdateVerticalVelocity();
        controller.Move(Vector3.up * verticalVelocity * Time.deltaTime);
    }
}