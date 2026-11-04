using System;
using UnityEngine;
using System.Collections.Generic;

// scripts para mensajes de radio que suenan al arrancar la escena o cuando el jugador interactua con la radio de la cabaña
[Serializable]
public class RadioMessage
{
    [TextArea(2, 4)] public string[] subtitleLines; // cada elemento es una frase de subtitulo, se muestran de a una
    [TextArea(1, 3)] public string objectiveHint;   // el "MENSAJE EN PANTALLA" que queda unos segundos despues del mensaje
    public AudioClip voiceClip;                     // la voz que lee el mensaje (sale por el grupo Voz). Si esta vacia solo se muestran los subtitulos
    public float[] lineStartTimes;                  // segundos del audio en que empieza a decirse cada frase (mismo orden que subtitleLines). Si esta vacio, o no coincide la cantidad, se reparte por caracteres
}

// La radio de la cabaña. El jugador la usa con E (via PlayerInteraction, igual que un objeto looteable) para escuchar
// el mensaje del dia actual. Congela el mundo con GameProgressController.PauseWorld() mientras se usa la radio para escuchar el mensaje.
// El mensaje del Dia 1 suena solo al arrancar la escena. Los de los dias siguientes hay que ir a buscarlos a la radio.
// Al terminar la ultima noche queda disponible el mensaje final (posicion rescueWaitDays - 1), se escucha una sola vez.
// IMPORTANTE: tiene que estar en la capa "Interact" y con un collider, sino el raycast del player no la detecta.
public class RadioInteractable : MonoBehaviour, IInteractable
{
    [Header("Mensajes (posicion 0 = Dia 1, 1 = Dia 2, 2 = Dia 3, 3 = final)")]
    [SerializeField] private RadioMessage[] messages;

    [Header("Referencias")]
    [SerializeField] private RadioMessageUI messageUI; // el panel de subtitulos
    [SerializeField] private PlayerController playerController; // si estan vacias se buscan solas en Start
    [SerializeField] private CameraPivot cameraPivot;
    [SerializeField] private PlayerAttack playerAttack;
    [SerializeField] private InteractionActionData listenAction;

    [Header("Config")]
    [SerializeField] private bool playIntroOnStart = true; // el mensaje del Dia 1 suena solo al arrancar
    [SerializeField] private float reopenBlockTime = 0.3f; // evita que la misma E que saltea el mensaje lo reabra

    // el dia mas alto cuyo mensaje ya se escucho (0 = ninguno). Lo va a usar la mesa de crafteo para desbloquear recetas.
    public int HighestHeardDay { get; private set; }
    public bool IsListening { get; private set; }

    public event Action<int> OnMessageStarted;  // avisa el dia del mensaje que arranca (para el audio de Wwise, por ejemplo)
    public event Action<int> OnMessageFinished; // avisa el dia del mensaje que termino (o fue salteado)

    private GameProgressController gameProgress;
    private int currentMessageDay;
    private float lastCloseTime = -10f; // Time.unscaledTime de la ultima vez que se cerro un mensaje

    private readonly HashSet<int> heardDays = new();

    // durante el final la radio pasa el mensaje final en vez del del dia actual
    private int ActiveDay => gameProgress.IsFinaleActive ? gameProgress.FinaleDay : gameProgress.CurrentDay;


    // Start y no Awake: asi GameProgressController.Instance ya existe seguro
    private void Start()
    {
        gameProgress = GameProgressController.Instance;

        if (gameProgress == null)
        {
            Debug.LogError("[RadioInteractable] No se encontro GameProgressController.", this);
            return;
        }

        if (messageUI == null)
        {
            Debug.LogError("[RadioInteractable] No se asigno RadioMessageUI.", this);
            return;
        }

        FindMissingPlayerReferences();

        gameProgress.OnDayChanged += HandleDayChanged;
        gameProgress.OnFinaleStarted += HandleFinaleStarted;

        // el primer mensaje suena solo: el jugador arranca al lado de la radio
        if (playIntroOnStart)
            StartListening(gameProgress.CurrentDay);
    }

    private void OnDestroy()
    {
        if (gameProgress != null)
        {
            gameProgress.OnDayChanged -= HandleDayChanged;
            gameProgress.OnFinaleStarted -= HandleFinaleStarted;
        }
    }

    public bool Interact(InteractionActionData action, GameObject interactor)
    {
        if (action == null || listenAction == null || action.ActionId != listenAction.ActionId || !CanOpenInteraction(interactor))
            return false;

        if (AnyZombieAlive())
            return false;

        return StartListening(ActiveDay);
    }

    // arranca un dia nuevo: si hay un mensaje que todavia no se escucho, avisa al jugador que vaya a la radio
    private void HandleDayChanged(int day)
    {
        if (day > HighestHeardDay && !IsListening)
            messageUI.SetNewMessageHint(true);
    }

    // termino la ultima noche: aviso de mensaje nuevo (si justo estaba escuchando, se muestra al cerrar)
    private void HandleFinaleStarted()
    {
        if (!IsListening)
            messageUI.SetNewMessageHint(true);
    }

    private bool StartListening(int day)
    {
        if (IsListening)
            return false;

        RadioMessage message = GetMessageForDay(day);

        if (message == null)
            return false;

        // logica para no pausar el mundo despues de la primera vez que se escucha un mensaje por dia
        bool isFirstListen = !heardDays.Contains(day);

        if (isFirstListen)
        {
            // PauseWorld devuelve false si ya estaba pausado o la partida termino, en ese caso no hacemos nada
            if (!gameProgress.PauseWorld())
                return false;

            gameProgress.PauseDayTimerForRadio();
        }

        IsListening = true;
        currentMessageDay = day;


        SetPlayerControl(false);
        messageUI.SetNewMessageHint(false);

        OnMessageStarted?.Invoke(day);
        messageUI.Play(message, HandleMessageFinished);

        return true;
    }

    // lo llama RadioMessageUI cuando el mensaje termina solo o el jugador lo saltea con E
    private void HandleMessageFinished()
    {
        IsListening = false;
        lastCloseTime = Time.unscaledTime;

        SetPlayerControl(true);
        gameProgress.ResumeWorld();
        gameProgress.ResumeDayTimer();

        HighestHeardDay = Mathf.Max(HighestHeardDay, currentMessageDay);

        heardDays.Add(currentMessageDay);

        // el aviso del objetivo queda un rato en pantalla ("Usa la mesa de trabajo...", etc.)
        messageUI.ShowObjectiveHint(GetMessageForDay(currentMessageDay).objectiveHint);

        // si arranco el final mientras se re-escuchaba otro mensaje, se avisa recien ahora
        if (gameProgress.IsFinaleActive && !heardDays.Contains(gameProgress.FinaleDay))
            messageUI.SetNewMessageHint(true);

        OnMessageFinished?.Invoke(currentMessageDay);
    }

    // si el dia no tiene mensaje propio usa el ultimo que haya cargado
    private RadioMessage GetMessageForDay(int day)
    {
        if (messages == null || messages.Length == 0)
            return null;

        return messages[Mathf.Clamp(day - 1, 0, messages.Length - 1)];
    }

    // frena (o devuelve) el control del jugador mientras suena la radio
    private void SetPlayerControl(bool enabledState)
    {
        if (playerController != null) playerController.enabled = enabledState;
        if (cameraPivot != null) cameraPivot.enabled = enabledState; // al apagarse suelta el cursor y al prenderse lo traba de nuevo
        if (playerAttack != null) playerAttack.enabled = enabledState;
    }

    private void FindMissingPlayerReferences()
    {
        if (playerController == null) playerController = FindFirstObjectByType<PlayerController>();
        if (cameraPivot == null) cameraPivot = FindFirstObjectByType<CameraPivot>();
        if (playerAttack == null) playerAttack = FindFirstObjectByType<PlayerAttack>();
    }

    // los zombies muertos pierden el tag Enemy (EnemyDeath), asi que solo cuentan los vivos
    private static bool AnyZombieAlive()
    {
        return GameObject.FindWithTag("Enemy") != null;
    }

    public bool CanOpenInteraction(GameObject interactor)
    {
        if (interactor == null || gameProgress == null || IsListening || gameProgress.IsGameEnded || gameProgress.IsWorldPaused)
            return false;

        // el mensaje final se escucha una sola vez, despues solo queda tirar la bengala
        if (gameProgress.IsFinaleActive && heardDays.Contains(gameProgress.FinaleDay))
            return false;

        if (Time.unscaledTime < lastCloseTime + reopenBlockTime)
            return false;

        return GetMessageForDay(ActiveDay) != null;
    }

    public IReadOnlyList<InteractionOption> GetActions(GameObject interactor)
    {
        List<InteractionOption> options = new();

        if (!CanOpenInteraction(interactor) || listenAction == null)
            return options;

        bool alreadyHeard = heardDays.Contains(ActiveDay);

        string actionName = alreadyHeard ? "Volver a escuchar" : "Escuchar radio";

        // con zombies vivos la accion queda deshabilitada y muestra el mensaje (mismo sistema que las escaleras)
        options.Add(new InteractionOption(
            listenAction,
            isAvailable: !AnyZombieAlive(),
            unavailableReason: "Todavia quedan zombies vivos. Eliminalos a todos antes de usar la radio.",
            feedbackType: OpeningPlacementError.PlayerUnavailable,
            displayName: actionName));

        return options;
    }
}