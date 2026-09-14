using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class ZombieController : MonoBehaviour
{
    //movimiento
    [SerializeField] private float moveSpeed = 2.5f; //velocidad al perseguir
    [SerializeField] private float rotationSpeed = 8f; //que tan rapido gira hacia el player
    [SerializeField] private float gravity = -15f; //misma logica que el player

    //referencias
    [SerializeField] private Transform player; //si se deja vacio, se busca solo por tag

    //ataque (lo lee ZombieAttack)
    [SerializeField] private float attackRange = 1.5f; //distancia minima para dejar de moverse y atacar

    //privadas
    private CharacterController controller;
    private float verticalVelocity;
    private Health playerHealth; //referencia al Health del player, para saber si ya murio

    //propiedades publicas para que ZombieAttack pueda leerlas sin duplicar logica
    public Transform Player => player;
    public bool InAttackRange =>
        player != null && Vector3.Distance(transform.position, player.position) <= attackRange;
    public bool IsPlayerDead => playerHealth != null && playerHealth.IsDead; //true si el player ya murio

    private void Awake()
    {
        controller = GetComponent<CharacterController>(); //cache del componente
    }

    private void Start()
    {
        //si no asignaron el player manualmente, lo busca por tag
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj != null)
                player = playerObj.transform;
            else
                Debug.LogWarning($"{name}: no se encontro ningun GameObject con tag 'Player'. Asignalo manualmente en el inspector o revisa el tag del jugador.");
        }

        //cachea el Health del player para poder chequear si ya murio
        if (player != null)
            playerHealth = player.GetComponent<Health>();
    }

    private void Update()
    {
        if (player == null || IsPlayerDead) return; //sin player o ya muerto, no hay nada que perseguir

        //si ya esta en rango de ataque, deja de avanzar y solo aplica gravedad (ZombieAttack hace el resto)
        if (InAttackRange)
        {
            ApplyGravityOnly();
            return;
        }

        //direccion hacia el player, ignorando diferencia de altura
        Vector3 direction = player.position - transform.position;
        direction.y = 0f;
        direction.Normalize();

        //rotar suavemente hacia esa direccion
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