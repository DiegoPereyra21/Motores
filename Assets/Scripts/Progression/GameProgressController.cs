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
    public bool IsFinaleActive { get; private set; } // termino la ultima noche: tiempo congelado, solo se puede caminar (y tirar la bengala)
    public bool IsFinaleLocked { get; private set; } // ya se escucho el mensaje final: solo se puede tirar la bengala (E)

    public GameEnding CurrentEnding { get; private set; } = GameEnding.None;
    public event Action<GameEnding> OnGameEnded; // evento que avisa del fin de la partida

    public event Action<DayNightPhase> OnPhaseChanged;
    public event Action<int> OnDayChanged;
    public event Action<float> OnPhaseProgressChanged;
    public event Action OnRadioWaitStarted;
    public event Action OnFinaleStarted; // termino la ultima noche (lo escucha FinaleSequence)
    //public event Action OnRunFinished;

    public float CurrentPhaseDuration => CurrentPhase == DayNightPhase.Day ? dayDuration : nightDuration;

    public float DaySpeedWithZombiesAlive => daySpeedWithZombiesAlive;

    // el "dia" del mensaje de radio final (posicion rescueWaitDays - 1 del array de la radio)
    public int FinaleDay => rescueWaitDays;

    public bool HasRequiredEndingItem => requiredEndingItem != null && playerInventory != null && playerInventory.GetItemCount(requiredEndingItem) > 0;

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
        if (!initialized || IsWorldPaused || IsGameEnded || IsWaitingForRadio || IsFinaleActive)
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
        if (!initialized || IsGameEnded || IsWorldPaused || IsWaitingForRadio || IsFinaleActive || seconds <= 0f)
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

        // termino la ultima noche: no se cambia de dia, queda de noche y arranca el final
        if (CurrentDay + 1 >= rescueWaitDays)
        {
            StartFinale();
            return;
        }

        // se cambia de dia
        CurrentDay++;

        // transicion de noche a dia si la partida sigue
        CurrentPhase = DayNightPhase.Day;
        PhaseRemainingTime = dayDuration;
        IsWaitingForRadio = true;

        OnDayChanged?.Invoke(CurrentDay);
        OnPhaseChanged?.Invoke(CurrentPhase);
        NotifyPhaseProgress();

        OnRadioWaitStarted?.Invoke();
    }

    private void StartFinale()
    {
        IsFinaleActive = true;
        PhaseRemainingTime = 0f;
        NotifyPhaseProgress();

        // si no hay FinaleSequence en la escena, se comporta como antes
        if (OnFinaleStarted == null)
        {
            EndGame(HasRequiredEndingItem ? GameEnding.Victory : GameEnding.Defeat);
            return;
        }

        OnFinaleStarted.Invoke();
    }

    public void LockFinale()
    {
        if (!IsFinaleActive || IsGameEnded)
            return;

        IsFinaleLocked = true;
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
        IsFinaleActive = false;
        IsFinaleLocked = false;

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
        IsFinaleActive = false;

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