using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    //movimiento
    [SerializeField] private float walkSpeed = 2f;//mismo q el blend de animator
    [SerializeField] private float runSpeed = 6f;//x2
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
    private float currentSpeed;
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

        //calc velocidad
        float targetSpeed = input == Vector2.zero ? 0f : (isRunning ? runSpeed : walkSpeed);
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, acceleration * Time.deltaTime);
        //canc dir y rot
        Vector3 moveDirection = Vector3.zero;
        if (input != Vector2.zero)
        {
            float targetAngle = Mathf.Atan2(input.x, input.y) * Mathf.Rad2Deg + cameraPivot.eulerAngles.y;
            float angle = Mathf.SmoothDampAngle(transform.eulerAngles.y, targetAngle, ref rotationVelocity, rotationSmoothTime);
            
            transform.rotation = Quaternion.Euler(0f, angle, 0f);
            moveDirection = Quaternion.Euler(0f, targetAngle, 0f) * Vector3.forward;
        }
        //prefiero aca en vez de en otra funcion
        if (controller.isGrounded && verticalVelocity < 0f)
            verticalVelocity = -2f; // Pega el personaje al piso
        else
            verticalVelocity += gravity * Time.deltaTime;

        //aplicar los movimientos
        controller.Move((moveDirection * currentSpeed + Vector3.up * verticalVelocity) * Time.deltaTime);
        //cambiar el parametro speed del animator para q la animacion cambie
        animator.SetFloat("Speed", currentSpeed, 0.1f, Time.deltaTime);
    }
}