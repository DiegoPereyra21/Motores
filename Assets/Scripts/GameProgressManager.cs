using System;
using UnityEngine;

public enum DayNightPhase
{
    Day,
    Night
}

public class GameProgressController : MonoBehaviour
{
    public static GameProgressController Instance { get; private set; }

    [Header("Duracion de fases (segundos)")]
    [SerializeField, Min(1f)] private float dayDuration = 90f;
    [SerializeField, Min(1f)] private float nightDuration = 90f;

    [Header("Estado inicial")]
    [SerializeField, Min(1)] private int startingDay = 1;

    public int CurrentDay { get; private set; }
    public DayNightPhase CurrentPhase { get; private set; }
    public float PhaseRemainingTime { get; private set; }
    public bool IsWorldPaused { get; private set; }
    public bool IsRunFinished { get; private set; }

    public event Action<DayNightPhase> OnPhaseChanged;
    public event Action<int> OnDayChanged;
    public event Action<float> OnPhaseProgressChanged;
    public event Action OnRunFinished;

    public float CurrentPhaseDuration => CurrentPhase == DayNightPhase.Day ? dayDuration : nightDuration;

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
        if (!initialized || IsWorldPaused || IsRunFinished)
            return;

        PhaseRemainingTime -= Time.deltaTime;

        if (PhaseRemainingTime <= 0f)
        {
            PhaseRemainingTime = 0f;
            AdvancePhase();
        }

        NotifyPhaseProgress();
    }

    public bool PauseWorld()
    {
        if (IsWorldPaused || IsRunFinished)
            return false;

        IsWorldPaused = true;
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;

        return true;
    }

    public bool ResumeWorld()
    {
        if (!IsWorldPaused)
            return false;

        IsWorldPaused = false;
        Time.timeScale = previousTimeScale;

        return true;
    }

    public void ConsumeTime(float seconds = 0f)
    {
        if (!initialized || IsRunFinished || IsWorldPaused || seconds <= 0f)
            return;

        PhaseRemainingTime -= seconds;

        if (PhaseRemainingTime <= 0f)
        {
            PhaseRemainingTime = 0f;
            AdvancePhase();
        }

        NotifyPhaseProgress();
    }

    private void AdvancePhase()
    {
        if (CurrentPhase == DayNightPhase.Day)
        {
            CurrentPhase = DayNightPhase.Night;
            PhaseRemainingTime = nightDuration;

            OnPhaseChanged?.Invoke(CurrentPhase);
            NotifyPhaseProgress();

            return;
        }

        CurrentDay++;

        if (CurrentDay >= 4)
        {
            FinishRun();
            return;
        }

        CurrentPhase = DayNightPhase.Day;
        PhaseRemainingTime = dayDuration;

        OnDayChanged?.Invoke(CurrentDay);
        OnPhaseChanged?.Invoke(CurrentPhase);
        NotifyPhaseProgress();
    }

    private void NotifyPhaseProgress()
    {
        float progress = 1f - (PhaseRemainingTime / CurrentPhaseDuration);
        OnPhaseProgressChanged?.Invoke(Mathf.Clamp01(progress));
    }

    private void FinishRun()
    {
        IsRunFinished = true;
        PhaseRemainingTime = 0f;

        Debug.Log("[GameProgressController] La partida llego al dia 4.");

        OnRunFinished?.Invoke();
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
}