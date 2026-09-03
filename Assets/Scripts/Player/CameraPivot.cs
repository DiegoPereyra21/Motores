using UnityEngine;
using UnityEngine.InputSystem;

public class CameraPivot : MonoBehaviour
{
    //movimiento de camara
    [SerializeField] private float sensitivity = 0.15f;//luego cambiar en config
    [SerializeField] private float minPitch = -30f;//poner mucho menos luego, no tiene sentido q puedas ver de tan bajo en este game
    [SerializeField] private float maxPitch = 70f;
    [SerializeField] private bool lockCursor = true;

    //inputs(luego testear con joystick)
    [SerializeField] private InputActionReference lookAction;

    //privadas
    private float yaw;
    private float pitch;
    private void Start()
    {
        Vector3 angles = transform.eulerAngles;
        yaw = angles.y;
        pitch = angles.x;
    }
    private void OnEnable()
    {
        lookAction.action.Enable();
        if (lockCursor) 
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }
    private void OnDisable()
    {
        lookAction.action.Disable();
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    private void LateUpdate()//late para que se mueva luego del player
    {
        Vector2 look = lookAction.action.ReadValue<Vector2>() * sensitivity;
        yaw += look.x;
        pitch = Mathf.Clamp(pitch - look.y, minPitch, maxPitch);
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}