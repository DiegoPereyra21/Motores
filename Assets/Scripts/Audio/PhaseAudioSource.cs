using UnityEngine;

// Loop 3D de la escena (rio, cascada, chimenea, lampara, abejas...) que suena segun la fase del juego, con fade al entrar y salir.
//  - Active In: Always (todo el dia y la noche), Day Only (ej: abejas) o Night Only
//  - Wait For First Radio Message: no suena hasta que termina el primer mensaje de radio (asi no se pisa con la voz de la radio)
//  - Al terminar la partida se apaga con fade
// Va en el mismo objeto que el AudioSource (3D, loop, Output en el grupo Ambiente). El volumen que tiene en el Inspector es el volumen maximo.
// Los fades usan tiempo SIN escalar porque la radio y la cinematica pausan el mundo (timeScale = 0).
public class PhaseAudioSource : MonoBehaviour
{
    public enum ActivePhase { Always, DayOnly, NightOnly }

    [Header("Referencias")]
    [SerializeField] private AudioSource source; //si esta vacio se busca en este objeto
    [SerializeField] private RadioInteractable radio; //si esta vacia se busca sola

    [Header("Cuando suena")]
    [SerializeField] private ActivePhase activeIn = ActivePhase.Always;
    [SerializeField] private bool waitForFirstRadioMessage = true;
    [SerializeField] private bool fadeOutOnGameEnd = true;
    [SerializeField] private bool randomStartTime = false; //arranca el loop en un punto al azar (sirve cuando varias fuentes usan el mismo clip, asi no suenan sincronizadas)

    [Header("Fade")]
    [SerializeField] private float fadeTime = 3f; //segundos

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private GameProgressController gameProgress;
    private float baseVolume;
    private float level;
    private bool radioDone;
    private bool gameEnded;
    private bool wasTarget;

    private void Awake()
    {
        if (!source) source = GetComponent<AudioSource>();
        if (radio == null && waitForFirstRadioMessage) radio = FindFirstObjectByType<RadioInteractable>();

        if (source == null)
        {
            Debug.LogError("[PhaseAudioSource] Falta el AudioSource.", this);
            enabled = false;
            return;
        }

        baseVolume = source.volume;
        source.playOnAwake = false;
        source.loop = true;
        source.volume = 0f;
        source.Stop(); //si venia con Play On Awake ya estaba sonando

        //sin radio en la escena no se espera nada
        if (radio == null) radioDone = true;
    }

    private void OnEnable()
    {
        if (radio != null)
            radio.OnMessageFinished += HandleMessageFinished;

        gameProgress = GameProgressController.Instance;

        if (gameProgress != null)
            gameProgress.OnGameEnded += HandleGameEnded;
    }

    private void OnDisable()
    {
        if (radio != null)
            radio.OnMessageFinished -= HandleMessageFinished;

        if (gameProgress != null)
            gameProgress.OnGameEnded -= HandleGameEnded;

        gameProgress = null;
        level = 0f;

        if (source != null) source.Stop();
    }

    private void Update()
    {
        if (gameProgress == null)
        {
            //el controlador puede crearse despues que este objeto
            gameProgress = GameProgressController.Instance;

            if (gameProgress != null)
                gameProgress.OnGameEnded += HandleGameEnded;
        }

        if (!source.enabled) return; //el AudioSource esta apagado a mano (ej: la lampara de la habitacion de la chimenea), no hay nada que reproducir

        bool shouldPlay = ShouldPlay();

        if (debugLog && shouldPlay != wasTarget)
            Debug.Log($"[PhaseAudioSource] {name}: {(shouldPlay ? "entra" : "sale")}", this);

        wasTarget = shouldPlay;

        level = Mathf.MoveTowards(level, shouldPlay ? 1f : 0f, Time.unscaledDeltaTime / Mathf.Max(fadeTime, 0.01f));
        source.volume = baseVolume * Mathf.SmoothStep(0f, 1f, level); //curva suave: arranca y termina despacio

        if (shouldPlay && !source.isPlaying)
        {
            if (randomStartTime && source.clip != null)
                source.time = Random.Range(0f, source.clip.length * 0.99f);

            source.Play();
        }
        else if (!shouldPlay && level <= 0f && source.isPlaying)
            source.Stop();
    }

    private bool ShouldPlay()
    {
        if (gameProgress == null || gameEnded)
            return false;

        if (!radioDone)
            return false;

        switch (activeIn)
        {
            case ActivePhase.DayOnly: return gameProgress.CurrentPhase == DayNightPhase.Day;
            case ActivePhase.NightOnly: return gameProgress.CurrentPhase == DayNightPhase.Night;
            default: return true;
        }
    }

    private void HandleMessageFinished(int day)
    {
        radioDone = true;
    }

    private void HandleGameEnded(GameEnding ending)
    {
        if (fadeOutOnGameEnd)
            gameEnded = true;
    }
}
