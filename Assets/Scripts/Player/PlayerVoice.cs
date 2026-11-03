using UnityEngine;

// Voz del player: cada linea se dispara por un evento del juego y sale por el grupo Voz del MainMixer.
//  - Recibir daño: una de las variantes al azar (nunca repite la anterior)
//  - Morir: grito de muerte
//  - Que un zombie lo agarre (queda paralizado): "suelta" o, con poca vida, la version de vida baja (con un tiempo minimo entre una y otra)
//  - Amanecer del dia 2 y del dia 3: una frase para cada uno
//  - Primera noche: frase larga (las demas noches no dice nada)
// Todas salen del mismo AudioSource (un hijo del player, 2D, Output en el grupo Voz), asi no se pisan: una voz mas importante corta a una menos importante.
// La voz del rescate (victoria) no esta aca: suena desde EndingCinematic porque el player se apaga en esa cinematica.
[RequireComponent(typeof(Health))]
public class PlayerVoice : MonoBehaviour
{
    //de menor a mayor: una voz solo corta a otra de igual o menor importancia
    private enum VoicePriority { Dialogue = 0, LetMeGo = 1, Damage = 2, Death = 3 }

    [Header("Referencias")]
    [SerializeField] private AudioSource voiceSource; //AudioSource 2D con Output en el grupo Voz
    [SerializeField] private PlayerController playerController; //si esta vacia se busca sola

    [Header("Daño y muerte")]
    [SerializeField] private VoiceLine[] damageLines; //variantes de quejido
    [SerializeField] private VoiceLine deathLine;

    [Header("Agarre del zombie")]
    [SerializeField] private VoiceLine letMeGoLine;
    [SerializeField] private VoiceLine letMeGoLowHealthLine;
    [SerializeField, Range(0f, 1f)] private float lowHealth = 0.3f; //con menos vida que esto (0 a 1) usa la linea de vida baja
    [SerializeField] private float letMeGoMinInterval = 6f; //segundos minimos entre una y otra, el zombie muerde cada ~1,2 s y no puede decirlo siempre

    [Header("Dia y noche")]
    [SerializeField] private VoiceLine[] dayLines; //posicion 0 = amanecer del dia 2, posicion 1 = amanecer del dia 3
    [SerializeField] private VoiceLine nightLine; //solo en la primera noche

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private Health health;
    private GameProgressController gameProgress;
    private bool isSubscribed;
    private bool wasStunned;
    private bool nightSpoken;
    private float lastNormalized = 1f;
    private float nextLetMeGoTime;
    private int lastDamageIndex = -1;
    private VoicePriority currentPriority;

    private void Awake()
    {
        health = GetComponent<Health>();
        if (!playerController) playerController = GetComponent<PlayerController>();
    }

    private void OnEnable()
    {
        health.onHealthChanged.AddListener(HandleHealthChanged);
        health.onDied.AddListener(HandleDied);
        lastNormalized = health.Normalized;

        TryConnectToGameProgress();
    }

    private void OnDisable()
    {
        health.onHealthChanged.RemoveListener(HandleHealthChanged);
        health.onDied.RemoveListener(HandleDied);

        if (gameProgress != null && isSubscribed)
        {
            gameProgress.OnDayChanged -= HandleDayChanged;
            gameProgress.OnPhaseChanged -= HandlePhaseChanged;
        }

        gameProgress = null;
        isSubscribed = false;
    }

    private void Update()
    {
        if (!isSubscribed)
            TryConnectToGameProgress();

        //el agarre se detecta cuando el player pasa a estar paralizado (la mordida del zombie lo paraliza desde que empieza el golpe)
        bool stunned = playerController != null && playerController.IsStunned;

        if (stunned && !wasStunned)
            HandleGrabbed();

        wasStunned = stunned;
    }

    private void TryConnectToGameProgress()
    {
        if (isSubscribed)
            return;

        gameProgress = GameProgressController.Instance;

        if (gameProgress == null)
            return;

        gameProgress.OnDayChanged += HandleDayChanged;
        gameProgress.OnPhaseChanged += HandlePhaseChanged;
        isSubscribed = true;
    }

    //solo cuando baja la vida (al curarse no) y no si murio con ese golpe (ahi suena el grito de muerte)
    private void HandleHealthChanged(float normalized)
    {
        bool tookDamage = normalized < lastNormalized;
        lastNormalized = normalized;

        if (!tookDamage || health.IsDead || damageLines == null || damageLines.Length == 0)
            return;

        int index = Random.Range(0, damageLines.Length);

        if (damageLines.Length > 1 && index == lastDamageIndex)
            index = (index + Random.Range(1, damageLines.Length)) % damageLines.Length;

        lastDamageIndex = index;
        Speak(damageLines[index], VoicePriority.Damage, "daño");
    }

    private void HandleDied()
    {
        Speak(deathLine, VoicePriority.Death, "muerte");
    }

    private void HandleGrabbed()
    {
        if (health.IsDead || Time.unscaledTime < nextLetMeGoTime)
            return;

        VoiceLine line = health.Normalized < lowHealth && letMeGoLowHealthLine != null && letMeGoLowHealthLine.clip != null
            ? letMeGoLowHealthLine
            : letMeGoLine;

        if (Speak(line, VoicePriority.LetMeGo, "agarre"))
            nextLetMeGoTime = Time.unscaledTime + letMeGoMinInterval;
    }

    //el dia 1 no dice nada (arranca con la radio): dia 2 usa la posicion 0, dia 3 la posicion 1
    private void HandleDayChanged(int day)
    {
        int index = day - 2;

        if (health.IsDead || dayLines == null || index < 0 || index >= dayLines.Length)
            return;

        Speak(dayLines[index], VoicePriority.Dialogue, $"amanecer dia {day}");
    }

    private void HandlePhaseChanged(DayNightPhase phase)
    {
        if (phase != DayNightPhase.Night || nightSpoken || health.IsDead)
            return;

        nightSpoken = true;
        Speak(nightLine, VoicePriority.Dialogue, "primera noche");
    }

    //devuelve true si la linea realmente empezo a sonar
    private bool Speak(VoiceLine line, VoicePriority priority, string reason)
    {
        if (voiceSource == null || line == null || line.clip == null)
            return false;

        //si ya habla alguien mas importante no se la corta
        if (voiceSource.isPlaying && priority < currentPriority)
            return false;

        voiceSource.Stop();
        voiceSource.clip = line.clip;
        voiceSource.volume = line.volume;
        voiceSource.Play();
        currentPriority = priority;

        if (debugLog) Debug.Log($"[PlayerVoice] {reason} -> {line.clip.name} (vol {line.volume:0.00})", this);
        return true;
    }
}
