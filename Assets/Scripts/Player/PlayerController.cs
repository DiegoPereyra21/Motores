using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    //movimiento
    [SerializeField] private float walkSpeed = 2f;//mismo q el blend de animator
    [SerializeField] private float runSpeed = 6f;//x2
    [SerializeField] private float backwardSpeedMultiplier = 1f;//<1 para que retroceder sea mas lento (ajustar las Pos Y negativas del blend si lo cambias)
    [SerializeField] private float acceleration = 10f;
    [SerializeField] private float rotationSmoothTime = 0.12f;
    [SerializeField] private float gravity = -15f;

    //referencias
    [SerializeField] private Transform cameraPivot;//la mejor forma de utilizar la camara, y combinando con cinemachine
    [SerializeField] private CharacterController controller;
    [SerializeField] private Animator animator;

    //sistema de inputs nuevo, elijo directamente el input en vez de usar el inputaction completo
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference sprintAction;

    //privadas
    private static readonly int MoveXHash = Animator.StringToHash("MoveX");
    private static readonly int MoveYHash = Animator.StringToHash("MoveY");
    private Vector3 horizontalVelocity;
    private float verticalVelocity;
    private float rotationVelocity;
    //publicas
    public bool MovementLocked { get; set; } //lo usa PlayerAttack para frenar al player mientras ataca
    //tdo para q el se pause al recibir un ataque de un zombie
    [SerializeField] private float stunImmunityTime = 1f; //tras liberarse, tiempo en que no lo pueden volver a paralizar (evita el stunlock con varios zombies)
    private float stunEndTime;
    private float stunImmuneUntil;
    public bool IsStunned => Time.time < stunEndTime; //paralizado por una mordida (lo setea ZombieAttack)
    private object stunOwner; //quien puso la parálisis, para que solo ese zombie pueda liberarlo

    //para anim de loot
    private float busyEndTime;
    public bool IsBusy => Time.time < busyEndTime; //haciendo una accion de lootear no se mueve ni ataque
    public void SetBusy(float seconds) => busyEndTime = Time.time + seconds;


    public void Stun(float seconds, object owner = null)
    {
        //mientras esta paralizado o en la ventana de inmunidad, otros zombies no lo agarran
        if (Time.time < stunImmuneUntil) return;

        stunEndTime = Time.time + seconds;
        stunImmuneUntil = stunEndTime + stunImmunityTime;
        stunOwner = owner;
    }

    //libera al player antes de tiempo, solo si el agarre lo puso ese mismo owner
    public void ReleaseStun(object owner)
    {
        if (owner == null || stunOwner != owner || !IsStunned) return;

        stunEndTime = Time.time;
        stunImmuneUntil = Time.time + stunImmunityTime; //queda la ventana de inmunidad para poder alejarse
        stunOwner = null;
    }


    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }
    private void OnEnable()
    {
        moveAction.action.Enable();
        sprintAction.action.Enable();
    }
    private void OnDisable()
    {
        moveAction.action.Disable();
        sprintAction.action.Disable();
    }
    private void Update()
    {
        bool locked = MovementLocked || IsStunned || IsBusy;
        Vector2 input = locked ? Vector2.zero : moveAction.action.ReadValue<Vector2>();
        bool isRunning = !locked && sprintAction.action.IsPressed();

        //el cuerpo siempre mira hacia donde mira la camara (si esta ocupado/paralizado no gira)
        if (!locked)
        {
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, cameraPivot.eulerAngles.y, ref rotationVelocity, rotationSmoothTime);
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
        }

        //velocidad objetivo, mas lenta cuanto mas hacia atras vaya (si el multiplier es <1)
        float targetSpeed = input == Vector2.zero ? 0f : (isRunning ? runSpeed : walkSpeed);
        if (input.y < 0f)
            targetSpeed *= Mathf.Lerp(1f, backwardSpeedMultiplier, -input.normalized.y);

        //direccion relativa a la camara, sin rotar al player hacia ella (por eso puede ir de espaldas o de costado)
        Quaternion cameraYaw = Quaternion.Euler(0f, cameraPivot.eulerAngles.y, 0f);
        Vector3 targetVelocity = cameraYaw * new Vector3(input.x, 0f, input.y).normalized * targetSpeed;
        horizontalVelocity = Vector3.Lerp(horizontalVelocity, targetVelocity, acceleration * Time.deltaTime);

        //prefiero aca en vez de en otra funcion
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f; // Pega el personaje al piso
        else
            verticalVelocity += gravity * Time.deltaTime;

        //aplicar los movimientos
        controller.Move((horizontalVelocity + Vector3.up * verticalVelocity) * Time.deltaTime);

        //velocidad en espacio local del player: X = costado, Y = adelante/atras, para el blend tree 2D
        Vector3 localVelocity = transform.InverseTransformDirection(horizontalVelocity);
        animator.SetFloat(MoveXHash, localVelocity.x, 0.1f, Time.deltaTime);
        animator.SetFloat(MoveYHash, localVelocity.z, 0.1f, Time.deltaTime);
    }
}