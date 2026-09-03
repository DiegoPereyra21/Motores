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
        Vector2 input = moveAction.action.ReadValue<Vector2>();
        bool isRunning = sprintAction.action.IsPressed();
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