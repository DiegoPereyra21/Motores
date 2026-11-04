using UnityEngine;

// Musica del juego: cada momento tiene su pista y se pasa de una a otra con un fade (crossfade).
//  - Dia: arranca cuando termina el mensaje de radio (justo cuando empieza la cinematica de paneo) y suena en todas las fases de dia
//  - Noche: suena durante la fase de noche
//  - Muerte: suena en la cinematica de muerte del player
//  - Victoria / derrota: la musica se apaga con fade para que se escuche la cinematica del helicoptero
// Cada pista es un AudioSource propio (con Output en el grupo Musica del MainMixer), asi el ducking de la radio y la tension siguen funcionando sobre el grupo.
// Los fades usan tiempo SIN escalar porque la radio y la cinematica pausan el mundo (timeScale = 0).
public class MusicController : MonoBehaviour
{
    private enum MusicState { None, Day, Night, Death }

    [Header("Pistas (AudioSource con su clip y Output en el grupo Musica, Play On Awake apagado)")]
    [SerializeField] private AudioSource dayMusic; //Loop activado
    [SerializeField] private AudioSource nightMusic; //Loop activado
    [SerializeField] private AudioSource deathMusic;

    [Header("Referencias")]
    [SerializeField] private RadioInteractable radio; //si esta vacia se busca sola

    [Header("Fades (segundos)")]
    [SerializeField] private float dayNightFade = 2f; //al pasar de dia a noche y al reves
    [SerializeField] private float deathFade = 0.8f; //al morir: corta rapido la pista anterior y entra la de muerte
    [SerializeField] private float endingFade = 1.5f; //al terminar con victoria o derrota la musica se apaga

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private AudioSource[] tracks; //mismo orden que MusicState (sin None): dia, noche, muerte
    private float[] baseVolume; //volumen que tiene cada AudioSource en el Inspector (el fade lo multiplica)
    private float[] level; //cuanto del volumen base suena ahora (0 a 1)
    private float[] target; //hacia donde va cada pista (0 o 1)
    private float[] fadeTime;

    private GameProgressController gameProgress;
    private bool isSubscribed;
    private bool dayUnlocked; //la musica del dia recien puede sonar despues del primer mensaje de radio
    private MusicState state = MusicState.None;

    private void Awake()
    {
        tracks = new[] { dayMusic, nightMusic, deathMusic };
        baseVolume = new float[tracks.Length];
        level = new float[tracks.Length];
        target = new float[tracks.Length];
        fadeTime = new float[tracks.Length];

        for (int i = 0; i < tracks.Length; i++)
        {
            fadeTime[i] = 1f;

            if (tracks[i] == null) continue;

            baseVolume[i] = tracks[i].volume;
            tracks[i].playOnAwake = false;
            tracks[i].volume = 0f;
        }

        if (radio == null) radio = FindFirstObjectByType<RadioInteractable>();
    }

    private void OnEnable()
    {
        if (radio != null)
            radio.OnMessageFinished += HandleMessageFinished;

        TryConnectToGameProgress();
    }

    private void OnDisable()
    {
        if (radio != null)
            radio.OnMessageFinished -= HandleMessageFinished;

        if (gameProgress != null && isSubscribed)
        {
            gameProgress.OnPhaseChanged -= HandlePhaseChanged;
            gameProgress.OnGameEnded -= HandleGameEnded;
        }

        gameProgress = null;
        isSubscribed = false;

        //al apagarse corta todas las pistas
        for (int i = 0; i < tracks.Length; i++)
        {
            level[i] = 0f;
            target[i] = 0f;

            if (tracks[i] != null) tracks[i].Stop();
        }

        state = MusicState.None;
    }

    private void Update()
    {
        if (!isSubscribed)
            TryConnectToGameProgress();

        for (int i = 0; i < tracks.Length; i++)
        {
            AudioSource source = tracks[i];
            if (source == null) continue;

            level[i] = Mathf.MoveTowards(level[i], target[i], Time.unscaledDeltaTime / Mathf.Max(fadeTime[i], 0.01f));
            source.volume = baseVolume[i] * level[i];

            if (target[i] > 0f && !source.isPlaying)
                source.Play();
            else if (target[i] <= 0f && level[i] <= 0f && source.isPlaying)
                source.Stop();
        }
    }

    private void TryConnectToGameProgress()
    {
        if (isSubscribed)
            return;

        gameProgress = GameProgressController.Instance;

        if (gameProgress == null)
            return;

        gameProgress.OnPhaseChanged += HandlePhaseChanged;
        gameProgress.OnGameEnded += HandleGameEnded;
        isSubscribed = true;

        ApplyPhase(gameProgress.CurrentPhase);
    }

    //termino el mensaje de radio: desde ahora puede sonar la musica del dia (la primera vez coincide con el inicio de la cinematica de paneo)
    private void HandleMessageFinished(int day)
    {
        dayUnlocked = true;

        if (gameProgress != null)
            ApplyPhase(gameProgress.CurrentPhase);
    }

    private void HandlePhaseChanged(DayNightPhase phase) => ApplyPhase(phase);

    private void ApplyPhase(DayNightPhase phase)
    {
        if (gameProgress != null && gameProgress.IsGameEnded)
            return;

        if (phase == DayNightPhase.Night)
            SetState(MusicState.Night, dayNightFade);
        else if (dayUnlocked)
            SetState(MusicState.Day, dayNightFade);
    }

    private void HandleGameEnded(GameEnding ending)
    {
        if (ending == GameEnding.Death)
            SetState(MusicState.Death, deathFade);
        else
            SetState(MusicState.None, endingFade);
    }

    //la pista del estado sube a su volumen y las demas bajan a 0
    private void SetState(MusicState newState, float fade)
    {
        if (state == newState)
            return;

        state = newState;

        for (int i = 0; i < tracks.Length; i++)
        {
            target[i] = (int)newState == i + 1 ? 1f : 0f;
            fadeTime[i] = fade;
        }

        if (debugLog) Debug.Log($"[Music] estado -> {newState} (fade {fade:0.0}s)", this);
    }
}
