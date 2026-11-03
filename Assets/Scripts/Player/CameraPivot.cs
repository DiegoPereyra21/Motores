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

    // para manejo de la camara con los menus de inventario y craft
    [SerializeField] private InventoryUI inventoryUI;
    [SerializeField] private CraftingUI craftingUI;

    //privadas
    private float yaw;
    private float pitch;
    private bool lookEnabled = true;

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
        bool canLook = lookEnabled
            && !(inventoryUI != null && inventoryUI.IsInventoryOpen)
            && !(craftingUI != null && craftingUI.IsMenuOpen);

        if (canLook)
        {
            Vector2 look = lookAction.action.ReadValue<Vector2>() * sensitivity;
            yaw += look.x;
            pitch = Mathf.Clamp(pitch - look.y, minPitch, maxPitch);
        }

        // se reaplica siempre, aunque no se pueda mirar, para que el pivot no herede la rotación del Player
        transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }

    // maneja la visibilidad del mouse
    public void SetLookEnabled(bool enabled)
    {
        lookEnabled = enabled;

        if (enabled)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
    }
}