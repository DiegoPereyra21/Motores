using UnityEngine;

// Ambiente 2D del mapa: una capa para el dia y otra para la noche, con crossfade al cambiar de fase.
//  - Dia: arranca cuando termina el primer mensaje de radio (justo cuando empieza la cinematica de paneo), igual que la musica
//  - Noche: suena durante la fase de noche
//  - Fin de la partida (victoria, derrota o muerte): se apaga con fade para que se escuchen las cinematicas
// Cada capa es un AudioSource propio (2D, loop, Output en el grupo Ambiente del MainMixer), asi AmbientVol y la tension siguen funcionando sobre el grupo.
// Los fades usan tiempo SIN escalar porque la radio y la cinematica pausan el mundo (timeScale = 0).
public class AmbienceController : MonoBehaviour
{
    private enum AmbienceState { None, Day, Night }

    [Header("Capas (AudioSource 2D con su clip y Output en el grupo Ambiente, Loop activado, Play On Awake apagado)")]
    [SerializeField] private AudioSource dayAmbience;
    [SerializeField] private AudioSource nightAmbience;

    [Header("Referencias")]
    [SerializeField] private RadioInteractable radio; //si esta vacia se busca sola

    [Header("Fades (segundos)")]
    [SerializeField] private float dayNightFade = 4f; //al pasar de dia a noche y al reves
    [SerializeField] private float endingFade = 1.5f; //al terminar la partida

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private AudioSource[] layers; //mismo orden que AmbienceState (sin None): dia, noche
    private float[] baseVolume; //volumen que tiene cada AudioSource en el Inspector (el fade lo multiplica)
    private float[] level; //cuanto del volumen base suena ahora (0 a 1)
    private float[] target; //hacia donde va cada capa (0 o 1)
    private float fadeTime = 1f;

    private GameProgressController gameProgress;
    private bool isSubscribed;
    private bool dayUnlocked; //el ambiente recien puede sonar despues del primer mensaje de radio
    private AmbienceState state = AmbienceState.None;

    private void Awake()
    {
        layers = new[] { dayAmbience, nightAmbience };
        baseVolume = new float[layers.Length];
        level = new float[layers.Length];
        target = new float[layers.Length];

        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i] == null) continue;

            baseVolume[i] = layers[i].volume;
            layers[i].playOnAwake = false;
            layers[i].loop = true;
            layers[i].volume = 0f;
            layers[i].Stop(); //si venia con Play On Awake ya estaba sonando
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

        //al apagarse corta todas las capas
        for (int i = 0; i < layers.Length; i++)
        {
            level[i] = 0f;
            target[i] = 0f;

            if (layers[i] != null) layers[i].Stop();
        }

        state = AmbienceState.None;
    }

    private void Update()
    {
        if (!isSubscribed)
            TryConnectToGameProgress();

        for (int i = 0; i < layers.Length; i++)
        {
            AudioSource source = layers[i];
            if (source == null) continue;

            level[i] = Mathf.MoveTowards(level[i], target[i], Time.unscaledDeltaTime / Mathf.Max(fadeTime, 0.01f));
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

    //termino el mensaje de radio: desde ahora puede sonar el ambiente (la primera vez coincide con el inicio de la cinematica de paneo)
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

        if (!dayUnlocked)
            return;

        SetState(phase == DayNightPhase.Night ? AmbienceState.Night : AmbienceState.Day, dayNightFade);
    }

    private void HandleGameEnded(GameEnding ending)
    {
        SetState(AmbienceState.None, endingFade);
    }

    //la capa del estado sube a su volumen y la otra baja a 0
    private void SetState(AmbienceState newState, float fade)
    {
        if (state == newState)
            return;

        state = newState;
        fadeTime = fade;

        for (int i = 0; i < layers.Length; i++)
            target[i] = (int)newState == i + 1 ? 1f : 0f;

        if (debugLog) Debug.Log($"[Ambience] estado -> {newState} (fade {fade:0.0}s)", this);
    }
}
