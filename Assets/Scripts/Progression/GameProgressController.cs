using System;
using UnityEngine;

public enum DayNightPhase
{
    Day,
    Night
}

public enum GameEnding
{
    None,
    Victory,
    Defeat,
    Death
}

public class GameProgressController : MonoBehaviour
{
    public static GameProgressController Instance { get; private set; }

    [Header("Velocidad del tiempo")]
    [SerializeField, Range(0.1f, 1f)]
    private float daySpeedWithZombiesAlive = 0.5f;

    [Header("Duracion de fases (segundos)")]
    [SerializeField, Min(1f)] private float dayDuration = 90f;
    [SerializeField, Min(1f)] private float nightDuration = 90f;

    [Header("Estado inicial")]
    [SerializeField, Min(1)] private int startingDay = 1;

    [Header("Cantidad de dias antes del rescate")]
    [SerializeField, Min(1)] private int rescueWaitDays = 4;

    [Header("Item necesario para ganar")]
    [SerializeField] private ItemData requiredEndingItem;

    [Header("Inventario del jugador")]
    [SerializeField] private Inventory playerInventory;

    public int CurrentDay { get; private set; }
    public DayNightPhase CurrentPhase { get; private set; }
    public float PhaseRemainingTime { get; private set; }
    public bool IsWorldPaused { get; private set; }
    public bool IsGameEnded { get; private set; } // indica si la partida termino
    public bool IsWaitingForRadio { get; private set; } // check para esperar la radio

    public GameEnding CurrentEnding { get; private set; } = GameEnding.None;
    public event Action<GameEnding> OnGameEnded; // evento que avisa del fin de la partida

    public event Action<DayNightPhase> OnPhaseChanged;
    public event Action<int> OnDayChanged;
    public event Action<float> OnPhaseProgressChanged;
    public event Action OnRadioWaitStarted;
    //public event Action OnRunFinished;

    public float CurrentPhaseDuration => CurrentPhase == DayNightPhase.Day ? dayDuration : nightDuration;

    public float DaySpeedWithZombiesAlive => daySpeedWithZombiesAlive;

    private float daySpeedMultiplier = 1f;
    private bool initialized;
    private float previousTimeScale = 1f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        CurrentDay = Mathf.Max(1, startingDay);
        CurrentPhase = DayNightPhase.Day;
        PhaseRemainingTime = dayDuration;

        initialized = true;

        OnDayChanged?.Invoke(CurrentDay);
        OnPhaseChanged?.Invoke(CurrentPhase);
        OnPhaseProgressChanged?.Invoke(0f);
    }

    private void Update()
    {
        if (!initialized || IsWorldPaused || IsGameEnded || IsWaitingForRadio)
            return;

        DecreasePhaseTime(Time.deltaTime);
    }

    private void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }

        if (IsWorldPaused)
        {
            Time.timeScale = previousTimeScale;
            IsWorldPaused = false;
        }
    }

    public bool PauseWorld()
    {
        if (IsWorldPaused || IsGameEnded)
            return false;

        IsWorldPaused = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        return true;
    }

    public bool ResumeWorld()
    {
        if (!IsWorldPaused || IsGameEnded)
            return false;

        IsWorldPaused = false;
        Time.timeScale = previousTimeScale;

        return true;
    }

    public void SetDaySpeedMultiplier(float multiplier)
    {
        daySpeedMultiplier = Mathf.Max(0f, multiplier);
    }

    public void ConsumeTime(float seconds = 0f)
    {
        if (!initialized || IsGameEnded || IsWorldPaused || IsWaitingForRadio || seconds <= 0f)
            return;

        DecreasePhaseTime(seconds);
    }

    private void AdvancePhase()
    {
        // transicion de dia a noche
        if (CurrentPhase == DayNightPhase.Day)
        {
            CurrentPhase = DayNightPhase.Night;
            PhaseRemainingTime = nightDuration;

            OnPhaseChanged?.Invoke(CurrentPhase);
            NotifyPhaseProgress();

            return;
        }

        // se cambia de dia
        CurrentDay++;

        // check para el final
        if (CurrentDay >= rescueWaitDays)
        {
            bool hasRequiredItem = requiredEndingItem != null && playerInventory != null && playerInventory.GetItemCount(requiredEndingItem) > 0;

            EndGame(hasRequiredItem ? GameEnding.Victory : GameEnding.Defeat);
            return;
        }

        // transicion de noche a dia si la partida sigue
        CurrentPhase = DayNightPhase.Day;
        PhaseRemainingTime = dayDuration;
        IsWaitingForRadio = true;

        OnDayChanged?.Invoke(CurrentDay);
        OnPhaseChanged?.Invoke(CurrentPhase);
        NotifyPhaseProgress();

        OnRadioWaitStarted?.Invoke();
    }

    private void NotifyPhaseProgress()
    {
        float progress = 1f - (PhaseRemainingTime / CurrentPhaseDuration);
        OnPhaseProgressChanged?.Invoke(Mathf.Clamp01(progress));
    }

    public void ResetGameProgress()
    {
        Time.timeScale = 1f;
        IsWorldPaused = false;
        previousTimeScale = 1f;

        daySpeedMultiplier = 1f;

        CurrentDay = Mathf.Max(1, startingDay);
        CurrentPhase = DayNightPhase.Day;
        PhaseRemainingTime = dayDuration;

        IsGameEnded = false;
        CurrentEnding = GameEnding.None;

        IsWaitingForRadio = false;

        OnDayChanged?.Invoke(CurrentDay);
        OnPhaseChanged?.Invoke(CurrentPhase);
        OnPhaseProgressChanged?.Invoke(0f);
    }

    public void EndGame(GameEnding ending)
    {
        if (!initialized || IsGameEnded || ending == GameEnding.None)
            return;

        IsGameEnded = true;
        CurrentEnding = ending;
        PhaseRemainingTime = 0f;
        IsWaitingForRadio = false;

        if (!IsWorldPaused)
        {
            previousTimeScale = Time.timeScale;
            IsWorldPaused = true;
        }

        //Time.timeScale = 0.5f;

        OnGameEnded?.Invoke(ending);
    }

    private void DecreasePhaseTime(float seconds)
    {
        PhaseRemainingTime -= seconds * daySpeedMultiplier;

        if (PhaseRemainingTime <= 0f)
        {
            PhaseRemainingTime = 0f; // para evitar tiempo con valores negativos
            AdvancePhase();

            return;
        }

        NotifyPhaseProgress();
    }

    public void PauseDayTimerForRadio()
    {
        if (IsGameEnded)
            return;

        IsWaitingForRadio = true;
    }

    public void ResumeDayTimer()
    {
        if (!IsWaitingForRadio || IsGameEnded)
            return;

        IsWaitingForRadio = false;
    }
}