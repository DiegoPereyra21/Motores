
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

public class HUDController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private UIDocument uiDocument;

    [Header("UI Tutorial")]
    [SerializeField] private UIDocument tutorialDocument;

    [Header("Jugador")]
    [SerializeField] private Health playerHealth;

    [Header("Input Actions")]
    [SerializeField] private InputActionAsset inputActions;

    [Header("Quick Selector - Efectos")]
    [SerializeField] private CameraPivot cameraPivot;
    [SerializeField, Range(0f, 1f)] private float slowMotionScale = 0.5f;

    private bool isSubscribed;
    private Label dayLabel;
    private Label timeLabel;
    private VisualElement healthFill;
    private VisualElement selector;
    private VisualElement wheel;
    private VisualElement[] slots;
    private bool isQuickSelectorOpen;
    private float previousTimeScale = 1f;

    private InputAction quickSelectorAction;
    private InputAction tutorialAction;
    private VisualElement tutorialOverlay;
    private bool isTutorialOpen;

    private Action<int> dayChangedHandler;
    private Action<DayNightPhase> phaseChangedHandler;
    private Action<InputAction.CallbackContext> tutorialHandler;

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

        if (tutorialDocument != null)
        {
            tutorialOverlay = tutorialDocument.rootVisualElement.Q<VisualElement>("TutorialOverlay");
            tutorialOverlay.style.display = DisplayStyle.None;
        }

        VisualElement root = uiDocument.rootVisualElement;

        dayLabel = root.Q<Label>("DayLabel");
        timeLabel = root.Q<Label>("TimeLabel");
        selector = root.Q<VisualElement>("Selector");
        healthFill = root.Q<VisualElement>("HealthFill");

        if (dayLabel == null)
            Debug.LogError("[HUDController] No se encontro DayLabel en el UXML.", this);

        if (healthFill == null)
            Debug.LogError("[HUDController] No se encontro HealthFill en el UXML.", this);

        if (selector == null)
            Debug.LogError("[HUDController] No se encontro Selector en el UXML.", this);


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

    private void Update()
    {
        if (!isSubscribed)
            TryConnectToGameProgress();
    }

    private void OnEnable()
    {
        TryConnectToGameProgress();
        ConnectInputActions();
        ConnectHealth();
    }

    private void OnDisable()
    {
        if (quickSelectorAction != null)
        {
            quickSelectorAction.started -= OnQuickSelectorStarted;
            quickSelectorAction.canceled -= OnQuickSelectorCanceled;
            quickSelectorAction = null;
        }

        if (tutorialAction != null)
        {
            if (tutorialHandler != null)
                tutorialAction.performed -= tutorialHandler;

            tutorialAction = null;
            tutorialHandler = null;
        }

        if (gameProgress != null)
        {
            gameProgress.OnDayChanged -= dayChangedHandler;
            gameProgress.OnPhaseChanged -= phaseChangedHandler;
            gameProgress.OnPhaseProgressChanged -= UpdateTimeLabel;
        }

        if (playerHealth != null)
            playerHealth.onHealthChanged.RemoveListener(UpdateHealthBar);

        if (tutorialOverlay != null)
        {
            isTutorialOpen = false;
            tutorialOverlay.style.display = DisplayStyle.None;
        }

        if (selector != null)
            selector.style.display = DisplayStyle.None;

        dayChangedHandler = null;
        phaseChangedHandler = null;
        isSubscribed = false;
        gameProgress = null;
    }

    private void TryConnectToGameProgress()
    {
        if (isSubscribed)
            return;

        gameProgress = GameProgressController.Instance;

        if (gameProgress == null)
            return;

        dayChangedHandler = _ => UpdateDayNightLabel();
        phaseChangedHandler = _ => UpdateDayNightLabel();

        gameProgress.OnDayChanged += dayChangedHandler;
        gameProgress.OnPhaseChanged += phaseChangedHandler;
        gameProgress.OnPhaseProgressChanged += UpdateTimeLabel;

        isSubscribed = true;

        UpdateDayNightLabel();
        UpdateTimeLabel(0f);
    }

    private void ConnectInputActions()
    {
        if (inputActions == null)
        {
            Debug.LogError("[HUDController] Asigna InputSystem_Actions.", this);
            return;
        }

        quickSelectorAction = inputActions.FindAction("UI/QuickAction");
        tutorialAction = inputActions.FindAction("UI/Tutorial");

        if (quickSelectorAction == null)
            Debug.LogError("[HUDController] No se encontro UI/QuickAction.", this);
        else
        {
            quickSelectorAction.started += OnQuickSelectorStarted;
            quickSelectorAction.canceled += OnQuickSelectorCanceled;
        }

        if (tutorialAction == null)
        {
            Debug.LogError("[HUDController] No se encontro UI/Tutorial.", this);
        }
        else
        {
            tutorialHandler = _ => ToggleTutorial();
            tutorialAction.performed += tutorialHandler;
        }
    }

    private void UpdateDayNightLabel()
    {
        if (dayLabel == null || GameProgressController.Instance == null)
            return;

        string phase = GameProgressController.Instance.CurrentPhase == DayNightPhase.Day ? "DIA" : "NOCHE";

        dayLabel.text = $"{phase} {GameProgressController.Instance.CurrentDay}";
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

    public void HideHUD()
    {
        if (uiDocument != null)
            uiDocument.rootVisualElement.style.display = DisplayStyle.None;
    }

    public void ShowHUD()
    {
        if (uiDocument != null)
            uiDocument.rootVisualElement.style.display = DisplayStyle.Flex;
    }

    private void UpdateTimeLabel(float progress)
    {
        if (timeLabel == null || GameProgressController.Instance == null)
            return;

        int seconds = Mathf.CeilToInt(GameProgressController.Instance.PhaseRemainingTime);
        int minutes = seconds / 60;
        seconds %= 60;

        timeLabel.text = $"{minutes:00}:{seconds:00}";
    }

    private void ConnectHealth()
    {
        if (playerHealth == null)
            playerHealth = FindFirstObjectByType<Health>();

        if (playerHealth == null)
            return;

        playerHealth.onHealthChanged.RemoveListener(UpdateHealthBar);
        playerHealth.onHealthChanged.AddListener(UpdateHealthBar);

        UpdateHealthBar(playerHealth.Normalized);
    }

    private void UpdateHealthBar(float normalizedHealth)
    {
        if (healthFill == null)
            return;

        healthFill.style.height = Length.Percent(normalizedHealth * 100f);
        healthFill.style.width = Length.Percent(100f);
    }

    private void ToggleTutorial()
    {
        if (tutorialOverlay == null)
            return;

        isTutorialOpen = !isTutorialOpen;

        tutorialOverlay.style.display = isTutorialOpen ? DisplayStyle.Flex : DisplayStyle.None;

        if (isTutorialOpen)
        {
            UnityEngine.Cursor.visible = true;
            UnityEngine.Cursor.lockState = CursorLockMode.None;

            if (cameraPivot != null)
                cameraPivot.enabled = false;
        }
        else
        {
            UnityEngine.Cursor.visible = false;
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;

            if (cameraPivot != null)
                cameraPivot.enabled = true;
        }
    }
}
