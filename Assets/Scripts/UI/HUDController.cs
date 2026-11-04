
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
    [SerializeField] private float selectionDeadZone = 20f;

    [Header("Acceso rapido")]
    [SerializeField] private QuickAccessController quickAccessController;
    [SerializeField] private int quickSelectorSortOrder = 10;

    [Header("Notificaciones de loot")]
    [SerializeField] private Inventory playerInventory;
    [SerializeField] private float lootNotificationDuration = 2.5f;

    [Header("Feedback de interacciones")]
    [SerializeField] private PlayerInteraction playerInteraction;
    [SerializeField] private float interactionFeedbackDuration = 2.5f;

    private VisualElement lootNotificationContainer;

    private int hoveredSlotIndex = -1;

    private VisualElement[] slotIcons;
    private VisualElement[] durabilityBars;
    private VisualElement[] durabilityFills;
    private Label[] consumableAmounts;

    //audio (el AudioSource tiene que tener Output en el grupo SFX del MainMixer, si esta vacio se busca en este mismo objeto)
    [Header("Audio")]
    [SerializeField] private AudioSource sfxSource;
    [SerializeField] private AudioClip tutorialToggleClip;//suena al abrir y al cerrar el tutorial

    private bool isSubscribed;
    private Label dayLabel;
    private Label timeLabel;
    private VisualElement healthFill;
    private VisualElement selector;
    private VisualElement wheel;
    private VisualElement[] slots;
    private bool isQuickSelectorOpen;
    public bool IsQuickSelectorOpen => isQuickSelectorOpen;
    private float originalSortOrder = 0;
    private float previousTimeScale = 1f;
    private Vector2 wheelScreenCenter;

    private VisualElement interactionFeedback;
    private Label interactionFeedbackTitle;
    private Label interactionFeedbackText;
    private IVisualElementScheduledItem interactionFeedbackSchedule;
    

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

        originalSortOrder = uiDocument.sortingOrder;

        lootNotificationContainer = root.Q<VisualElement>("LootNotificationContainer");

        interactionFeedback = root.Q<VisualElement>("InteractionFeedback");
        interactionFeedbackTitle = root.Q<Label>("InteractionFeedbackTitle");
        interactionFeedbackText = root.Q<Label>("InteractionFeedbackText");

        if (dayLabel == null)
            Debug.LogError("[HUDController] No se encontro DayLabel en el UXML.", this);

        if (healthFill == null)
            Debug.LogError("[HUDController] No se encontro HealthFill en el UXML.", this);

        if (selector == null)
            Debug.LogError("[HUDController] No se encontro Selector en el UXML.", this);

        if (lootNotificationContainer == null)
        {
            Debug.LogError("[HUDController] No se encontro LootNotificationContainer.", this);
        }

        if (interactionFeedback == null)
            Debug.LogError("[HUDController] No se encontro InteractionFeedback.", this);

        if (interactionFeedbackTitle == null)
            Debug.LogError("[HUDController] No se encontro InteractionFeedbackTitle.", this);

        if (interactionFeedbackText == null)
            Debug.LogError("[HUDController] No se encontro InteractionFeedbackText.", this);

        wheel = root.Q<VisualElement>("Wheel");

        slots = new VisualElement[]
        {
            root.Q<VisualElement>("Slot1"),
            root.Q<VisualElement>("Slot2"),
            root.Q<VisualElement>("Slot3"),
            root.Q<VisualElement>("Slot4")
        };

        slotIcons = new VisualElement[4];
        durabilityBars = new VisualElement[4];
        durabilityFills = new VisualElement[4];
        consumableAmounts = new Label[4];

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                continue;

            slotIcons[i] = slots[i].Q<VisualElement>($"Icon{i + 1}");
            durabilityBars[i] = slots[i].Q<VisualElement>("Durability");
            durabilityFills[i] = slots[i].Q<VisualElement>("DurabilityFill");

            // etiqueta de cantidad para consumibles
            consumableAmounts[i] = new Label();
            consumableAmounts[i].name = $"ConsumableAmount{i + 1}";

            consumableAmounts[i].style.position = Position.Absolute;
            consumableAmounts[i].style.right = 2;
            consumableAmounts[i].style.bottom = 0;
            consumableAmounts[i].style.minWidth = 26;
            consumableAmounts[i].style.height = 23;
            consumableAmounts[i].style.fontSize = 17;
            consumableAmounts[i].style.color = Color.white;
            consumableAmounts[i].style.backgroundColor = new Color(0f, 0f, 0f, 0.8f);
            consumableAmounts[i].style.unityTextAlign = TextAnchor.MiddleCenter;
            consumableAmounts[i].style.display = DisplayStyle.None;

            slots[i].Add(consumableAmounts[i]);
        }
    }

    private void Update()
    {
        if (!isSubscribed)
            TryConnectToGameProgress();

        if (isQuickSelectorOpen)
            UpdateWheelSelection();
    }

    private void OnEnable()
    {
        TryConnectToGameProgress();
        ConnectInputActions();
        StartCoroutine(InitializeHealthHUD());
        StartCoroutine(InitializeInventoryHUD());
        ConnectInteractionFeedback();
        ConnectQuickAccess();
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

        if (quickAccessController != null)
        {
            quickAccessController.OnQuickAccessChanged -= UpdateQuickAccess;
            quickAccessController.OnSelectionChanged -= UpdateSelectedSlot;
        }

        if (playerInventory != null)
            playerInventory.OnItemAdded -= HandleItemAdded;

        if (uiDocument != null)
            uiDocument.sortingOrder = originalSortOrder;

        if (playerInteraction != null)
        {
            playerInteraction.OnUnavailableActionAttempted -= ShowInteractionFeedback;
            playerInteraction.OnActionExecuted -= ShowSuccessfulInteractionFeedback;
        }

        interactionFeedbackSchedule?.Pause();

        dayChangedHandler = null;
        phaseChangedHandler = null;
        isSubscribed = false;
        gameProgress = null;
    }

    private System.Collections.IEnumerator InitializeHealthHUD()
    {
        yield return null;

        ConnectHealth();
    }

    private System.Collections.IEnumerator InitializeInventoryHUD()
    {
        yield return null;

        if (playerInventory == null)
            playerInventory = FindFirstObjectByType<Inventory>();

        if (playerInventory == null)
        {
            Debug.LogWarning("[HUDController] No se encontro Inventory.", this);
            yield break;
        }

        playerInventory.OnItemAdded -= HandleItemAdded;
        playerInventory.OnItemAdded += HandleItemAdded;
    }

    private void HandleItemAdded(ItemData item, int amount)
    {
        if (item == null || amount <= 0 || lootNotificationContainer == null)
            return;

        // crea una notificacion independiente
        VisualElement notification = new VisualElement();
        notification.AddToClassList("loot-notification");

        VisualElement icon = new VisualElement();
        icon.AddToClassList("loot-notification-icon");

        if (item.icon != null)
            icon.style.backgroundImage = new StyleBackground(item.icon);

        Label text = new Label($"+ {amount} {item.itemName}");
        text.AddToClassList("loot-notification-text");

        notification.Add(icon);
        notification.Add(text);

        lootNotificationContainer.Add(notification);

        // activa la animación de entrada
        notification.schedule.Execute(() => {
            notification.AddToClassList("visible");
        }).StartingIn(10);

        // inicia la desaparicion despues del tiempo configurado
        notification.schedule.Execute(() => {
            notification.RemoveFromClassList("visible");
            notification.AddToClassList("leaving");

            // retira el elemento cuando termina la animacion
            notification.schedule.Execute(() =>
            {
                notification.RemoveFromHierarchy();
            }).StartingIn(250);

        }).StartingIn((long)(lootNotificationDuration * 1000));
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
        if (hoveredSlotIndex >= 0 && quickAccessController != null)
        {
            quickAccessController.SelectSlot(hoveredSlotIndex);
        }

        UpdateHoveredSlot(-1);
        hoveredSlotIndex = -1;
        CloseQuickSelector();
    }

    private void OpenQuickSelector()
    {
        if (GameProgressController.Instance != null && GameProgressController.Instance.IsFinaleActive)
            return;

        if (isQuickSelectorOpen)
            return;

        isQuickSelectorOpen = true;

        uiDocument.sortingOrder = quickSelectorSortOrder;

        UnityEngine.Cursor.visible = true;
        UnityEngine.Cursor.lockState = CursorLockMode.None;

        previousTimeScale = Time.timeScale;
        Time.timeScale = slowMotionScale;

        if (cameraPivot != null)
            cameraPivot.SetLookEnabled(false);

        if (wheel != null && wheel.panel != null && Mouse.current != null)
        {
            VisualElement root = uiDocument.rootVisualElement;

            if (root.layout.width > 0 && root.layout.height > 0)
            {
                Vector2 wheelCenter = wheel.worldBound.center;

                float scaleX = Screen.width / root.layout.width;
                float scaleY = Screen.height / root.layout.height;

                wheelScreenCenter = new Vector2(wheelCenter.x * scaleX, Screen.height - (wheelCenter.y * scaleY));

                Mouse.current.WarpCursorPosition(wheelScreenCenter);
            }
        }

        hoveredSlotIndex = quickAccessController != null ? quickAccessController.SelectedIndex : -1;

        UpdateSelectedSlot(hoveredSlotIndex);
    }

    private void CloseQuickSelector()
    {
        if (!isQuickSelectorOpen)
            return;

        isQuickSelectorOpen = false;

        uiDocument.sortingOrder = originalSortOrder;

        UnityEngine.Cursor.visible = false;
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;

        Time.timeScale = previousTimeScale;

        if (cameraPivot != null)
            cameraPivot.SetLookEnabled(true);
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
        {
            Debug.LogError("[HUDController] No se asigno Player Health en el Inspector.", this);
            return;
        }

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
        if (!isTutorialOpen && GameProgressController.Instance != null && GameProgressController.Instance.IsFinaleActive)
            return;

        if (tutorialOverlay == null)
            return;

        isTutorialOpen = !isTutorialOpen;

        tutorialOverlay.style.display = isTutorialOpen ? DisplayStyle.Flex : DisplayStyle.None;
        PlaySfx(tutorialToggleClip);

        if (isTutorialOpen)
        {
            UnityEngine.Cursor.visible = true;
            UnityEngine.Cursor.lockState = CursorLockMode.None;

            if (cameraPivot != null)
                cameraPivot.SetLookEnabled(false);
        }
        else
        {
            UnityEngine.Cursor.visible = false;
            UnityEngine.Cursor.lockState = CursorLockMode.Locked;

            if (cameraPivot != null)
                cameraPivot.SetLookEnabled(true);
        }
    }

    private void UpdateQuickAccess()
    {
        if (quickAccessController == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            InventorySlot equippedSlot = quickAccessController.GetEquippedSlot(i);

            if (slotIcons[i] == null)
                continue;

            if (consumableAmounts[i] != null)
            {
                consumableAmounts[i].text = "";
                consumableAmounts[i].style.display = DisplayStyle.None;
            }

            if (equippedSlot == null || equippedSlot.IsEmpty)
            {
                slotIcons[i].style.backgroundImage = StyleKeyword.None;
                slotIcons[i].style.display = DisplayStyle.None;

                UpdateDurabilityBar(i, null);
                continue;
            }

            slotIcons[i].style.backgroundImage = new StyleBackground(equippedSlot.item.icon);
            slotIcons[i].style.display = DisplayStyle.Flex;

            if (equippedSlot.item.TryGetConsumableData(out ConsumableData consumableData))
            {
                consumableAmounts[i].text = equippedSlot.amount.ToString();
                consumableAmounts[i].style.display = DisplayStyle.Flex;
            }

            UpdateDurabilityBar(i, equippedSlot);
        }

        UpdateSelectedSlot(quickAccessController.SelectedIndex);
    }

    private void UpdateDurabilityBar(int index, InventorySlot slot)
    {
        if (durabilityBars[index] == null || durabilityFills[index] == null)
            return;

        if (slot == null || slot.IsEmpty || !slot.item.TryGetWeaponData(out WeaponData weaponData))
        {
            durabilityBars[index].style.display = DisplayStyle.None;
            return;
        }

        durabilityBars[index].style.display = DisplayStyle.Flex;

        float durabilityPercent = weaponData.maxDurability > 0 ? (float)slot.weaponDurability / weaponData.maxDurability : 0f;

        durabilityFills[index].style.width = Length.Percent(Mathf.Clamp01(durabilityPercent) * 100f);
    }

    private void UpdateSelectedSlot(int selectedIndex)
    {
        if (slots == null || quickAccessController == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                continue;

            InventorySlot equippedSlot = quickAccessController.GetEquippedSlot(i);

            bool hasItem = equippedSlot != null && !equippedSlot.IsEmpty;
            bool isSelected = i == selectedIndex && hasItem;

            slots[i].EnableInClassList("selected", isSelected);
        }
    }
    private void ConnectQuickAccess()
    {
        if (quickAccessController == null)
            quickAccessController = FindFirstObjectByType<QuickAccessController>();

        if (quickAccessController == null)
        {
            Debug.LogWarning("[HUDController] No se encontro QuickAccessController", this);
            return;
        }

        quickAccessController.OnQuickAccessChanged += UpdateQuickAccess;
        quickAccessController.OnSelectionChanged += UpdateSelectedSlot;

        UpdateQuickAccess();
        UpdateSelectedSlot(quickAccessController.SelectedIndex);
    }

    private int GetSlotIndexFromDirection(Vector2 direction)
    {
        if (direction.x < 0 && direction.y >= 0)
            return 0; // superior izquierdo

        if (direction.x >= 0 && direction.y >= 0)
            return 1; // superior derecho

        if (direction.x < 0 && direction.y < 0)
            return 2; // inferior izquierdo

        return 3;     // inferior derecho
    }
    private void UpdateWheelSelection()
    {
        if (!isQuickSelectorOpen || Mouse.current == null)
            return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();

        Vector2 direction = mousePosition - wheelScreenCenter;

        // evita seleccionar por movimientos demasiado pequeños
        if (direction.magnitude < selectionDeadZone)
            return;

        int newIndex = GetSlotIndexFromDirection(direction);

        if (newIndex == hoveredSlotIndex)
            return;

        hoveredSlotIndex = newIndex;

        // actualizamos el resaltado sin confirmar
        UpdateHoveredSlot(hoveredSlotIndex);
    }

    private void UpdateHoveredSlot(int hoveredIndex)
    {
        if (slots == null)
            return;

        for (int i = 0; i < slots.Length; i++)
        {
            if (slots[i] == null)
                continue;

            slots[i].EnableInClassList("hovered", i == hoveredIndex);
        }
    }

    private void ConnectInteractionFeedback()
    {
        if (playerInteraction == null)
            playerInteraction = FindFirstObjectByType<PlayerInteraction>();

        if (playerInteraction == null)
        {
            Debug.LogWarning("[HUDController] No se encontro PlayerInteraction.", this);
            return;
        }

        playerInteraction.OnUnavailableActionAttempted -= ShowInteractionFeedback;
        playerInteraction.OnUnavailableActionAttempted += ShowInteractionFeedback;

        playerInteraction.OnActionExecuted -= ShowSuccessfulInteractionFeedback;
        playerInteraction.OnActionExecuted += ShowSuccessfulInteractionFeedback;
    }

    private string GetInteractionFeedbackTitle(OpeningPlacementError error)
    {
        switch (error)
        {
            case OpeningPlacementError.AlreadyProtected:
                return "ABERTURA PROTEGIDA";

            case OpeningPlacementError.InvalidPosition:
                return "UBICACION INCORRECTA";

            case OpeningPlacementError.PlayerUnavailable:
                return "ACCION NO DISPONIBLE";

            case OpeningPlacementError.MissingMaterials:
                return "MATERIALES INSUFICIENTES";

            case OpeningPlacementError.ConfigurationError:
                return "ERROR DE CONFIGURACION";

            default:
                return "ACCION NO DISPONIBLE";
        }
    }

    private void ShowInteractionFeedback(InteractionOption option)
    {
        if (option == null)
            return;

        string reason = string.IsNullOrEmpty(option.UnavailableReason) ? "No podes realizar esta accion." : option.UnavailableReason;

        ShowInteractionFeedbackMessage(GetInteractionFeedbackTitle(option.FeedbackType), reason);
    }

    private void ShowInteractionFeedbackMessage(string title, string message)
    {
        if (interactionFeedback == null || interactionFeedbackTitle == null || interactionFeedbackText == null)
            return;

        interactionFeedbackTitle.text = title;
        interactionFeedbackText.text = message;

        interactionFeedbackSchedule?.Pause();

        interactionFeedback.RemoveFromClassList("visible");

        interactionFeedback.schedule.Execute(() => {
            interactionFeedback.AddToClassList("visible");
        }).StartingIn(10);

        interactionFeedbackSchedule = interactionFeedback.schedule.Execute(() => {
            interactionFeedback.RemoveFromClassList("visible");
        }).StartingIn((long)(interactionFeedbackDuration * 1000));
    }

    private void ShowSuccessfulInteractionFeedback(InteractionOption option)
    {
        if (option == null || string.IsNullOrEmpty(option.ResultMessage))
            return;

        ShowInteractionFeedbackMessage("INFORMACION", option.ResultMessage);
    }

    private void PlaySfx(AudioClip clip)
    {
        if (sfxSource == null) sfxSource = GetComponent<AudioSource>();

        if (sfxSource != null && clip != null)
            sfxSource.PlayOneShot(clip);

    }
}
