
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class HUDController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private UIDocument uiDocument;

    [Header("Quick Selector - Jugador")]
    [SerializeField] private InputActionReference quickActionReference;

    [Header("Quick Selector - Efectos")]
    [SerializeField] private CameraPivot cameraPivot;
    [SerializeField, Range(0f, 1f)] private float slowMotionScale = 0.5f;

    private bool isSubscribed;
    private Label dayLabel;
    private VisualElement selector;
    private InputAction quickSelectorAction;
    private VisualElement wheel;
    private VisualElement[] slots;
    private bool isQuickSelectorOpen;
    private float previousTimeScale = 1f;
    private GameProgressController gameProgress;

    private void Awake()
    {
        if (uiDocument == null)
            uiDocument = GetComponent<UIDocument>();

        if (uiDocument == null)
        {
            Debug.LogError("[HUDController] No se encontro UIDocument.", this);
            return;
        }

        VisualElement root = uiDocument.rootVisualElement;

        dayLabel = root.Q<Label>("DayLabel");
        selector = root.Q<VisualElement>("Selector");

        if (dayLabel == null)
        {
            Debug.LogError("[HUDController] No se encontro DayLabel en el UXML.", this);
        }

        if (selector == null)
        {
            Debug.LogError("[HUDController] No se encontro Selector en el UXML.", this);
        }
        else
        {
            selector.style.display = DisplayStyle.None;
        }

        wheel = root.Q<VisualElement>("Wheel");

        slots = new VisualElement[]
        {
            root.Q<VisualElement>("Slot1"),
            root.Q<VisualElement>("Slot2"),
            root.Q<VisualElement>("Slot3"),
            root.Q<VisualElement>("Slot4")
        };
    }

    private void Start()
    {
        foreach (VisualElement slot in slots)
        {
            if (slot == null)
                continue;

            slot.RegisterCallback<PointerEnterEvent>(OnSlotPointerEnter);
            slot.RegisterCallback<PointerLeaveEvent>(OnSlotPointerLeave);
        }
    }

    private void OnEnable()
    {
        TryConnectToGameProgress();
        ConnectQuickSelector();
    }

    private void ConnectQuickSelector()
    {
        if (quickSelectorAction != null)
            return;

        if (quickActionReference == null)
        {
            Debug.LogError("[HUD] Asigna la referencia de QuickAction en el Inspector.", this);
            return;
        }

        quickSelectorAction = quickActionReference.action;

        if (quickSelectorAction == null)
        {
            Debug.LogError("[HUD] La referencia no contiene una accion.", this);
            return;
        }

        quickSelectorAction.started += OnQuickSelectorStarted;
        quickSelectorAction.canceled += OnQuickSelectorCanceled;
    }

    private void Update()
    {
        if (!isSubscribed)
            TryConnectToGameProgress();
    }

    private void TryConnectToGameProgress()
    {
        if (isSubscribed)
            return;

        gameProgress = GameProgressController.Instance;

        if (gameProgress == null)
            return;

        gameProgress.OnDayChanged += UpdateDay;
        isSubscribed = true;

        UpdateDay(gameProgress.CurrentDay);
    }

    private void OnDisable()
    {
        if (isSubscribed && gameProgress != null)
            gameProgress.OnDayChanged -= UpdateDay;

        if (quickSelectorAction != null)
        {
            quickSelectorAction.started -= OnQuickSelectorStarted;
            quickSelectorAction.canceled -= OnQuickSelectorCanceled;
            quickSelectorAction = null;
        }

        if (selector != null)
            selector.style.display = DisplayStyle.None;

        isSubscribed = false;
        gameProgress = null;
    }

    private void UpdateDay(int day)
    {
        if (dayLabel == null)
            return;

        dayLabel.text = $"DAY {day}";
    }

    private void OnQuickSelectorStarted(InputAction.CallbackContext context)
    {
        OpenQuickSelector();
    }

    private void OnQuickSelectorCanceled(InputAction.CallbackContext context)
    {
        CloseQuickSelector();
    }

    private void OpenQuickSelector()
    {
        if (isQuickSelectorOpen)
            return;

        isQuickSelectorOpen = true;

        UnityEngine.Cursor.visible = true;
        UnityEngine.Cursor.lockState = CursorLockMode.None;

        previousTimeScale = Time.timeScale;
        Time.timeScale = slowMotionScale;

        if (cameraPivot != null)
            cameraPivot.enabled = false;

        if (selector != null)
            selector.style.display = DisplayStyle.Flex;
    }

    private void CloseQuickSelector()
    {
        if (!isQuickSelectorOpen)
            return;

        isQuickSelectorOpen = false;

        UnityEngine.Cursor.visible = false;
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;

        Time.timeScale = previousTimeScale;

        if (cameraPivot != null)
            cameraPivot.enabled = true;

        if (selector != null)
            selector.style.display = DisplayStyle.Flex;
    }

    private void OnSlotPointerEnter(PointerEnterEvent evt)
    {
        if (!isQuickSelectorOpen)
            return;

        if (evt.currentTarget is not VisualElement slot)
            return;

        slot.style.scale = new Scale(new Vector2(1.08f, 1.08f));
    }

    private void OnSlotPointerLeave(PointerLeaveEvent evt)
    {
        if (evt.currentTarget is not VisualElement slot)
            return;

        slot.style.scale = new Scale(Vector2.one);
    }
}
