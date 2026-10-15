using UnityEngine;
using UnityEngine.UIElements;

public class HUDController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private UIDocument uiDocument;

    private bool isSubscribed;
    private Label phaseLabel;
    private Label dayLabel;

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

        phaseLabel = root.Q<Label>("PhaseLabel");
        dayLabel = root.Q<Label>("DayLabel");

        if (phaseLabel == null || dayLabel == null)
        {
            Debug.LogError("[HUDController] No se encontraron PhaseLabel o DayLabel en el UXML.", this);
        }
    }

    private void Update()
    {
        if (!isSubscribed)
        {
            TryConnectToGameProgress();
        }
    }

    private void OnEnable()
    {
        TryConnectToGameProgress();
    }

    private void TryConnectToGameProgress()
    {
        if (isSubscribed)
            return;

        gameProgress = GameProgressController.Instance;

        if (gameProgress == null)
            return;

        gameProgress.OnPhaseChanged += UpdatePhase;
        gameProgress.OnDayChanged += UpdateDay;

        isSubscribed = true;

        UpdatePhase(gameProgress.CurrentPhase);
        UpdateDay(gameProgress.CurrentDay);
    }

    private void OnDisable()
    {
        if (isSubscribed && gameProgress != null)
        {
            gameProgress.OnPhaseChanged -= UpdatePhase;
            gameProgress.OnDayChanged -= UpdateDay;
        }

        isSubscribed = false;
        gameProgress = null;
    }

    private void UpdatePhase(DayNightPhase phase)
    {
        if (phaseLabel == null)
            return;

        phaseLabel.text = phase switch
        {
            DayNightPhase.Day => "DIA",
            DayNightPhase.Night => "NOCHE",
            _ => string.Empty
        };
    }

    private void UpdateDay(int day)
    {
        if (dayLabel == null)
            return;

        dayLabel.text = $"DIA {day}";
    }
}