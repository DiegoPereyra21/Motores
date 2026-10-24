using System;
using UnityEngine;

// scripts para mensajes de radio que suenan al arrancar la escena o cuando el jugador interactua con la radio de la cabaña
[Serializable]
public class RadioMessage
{
    [TextArea(2, 4)] public string[] subtitleLines; // cada elemento es una frase de subtitulo, se muestran de a una
    [TextArea(1, 3)] public string objectiveHint;   // el "MENSAJE EN PANTALLA" que queda unos segundos despues del mensaje
}

// La radio de la cabaña. El jugador la usa con E (via PlayerInteraction, igual que un objeto looteable) para escuchar
// el mensaje del dia actual. Congela el mundo con GameProgressController.PauseWorld() mientras se usa la radio para escuchar el mensaje.
// El mensaje del Dia 1 suena solo al arrancar la escena. Los de los dias siguientes hay que ir a buscarlos a la radio.
// IMPORTANTE: tiene que estar en la capa "Interact" y con un collider, sino el raycast del player no la detecta.
public class RadioInteractable : MonoBehaviour, IInteractable
{
    [Header("Mensajes (posicion 0 = Dia 1, 1 = Dia 2, 2 = Dia 3)")]
    [SerializeField] private RadioMessage[] messages;

    [Header("Referencias")]
    [SerializeField] private RadioMessageUI messageUI; // el panel de subtitulos
    [SerializeField] private PlayerController playerController; // si estan vacias se buscan solas en Start
    [SerializeField] private CameraPivot cameraPivot;
    [SerializeField] private PlayerAttack playerAttack;

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

    private readonly System.Collections.Generic.HashSet<int> heardDays = new();


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

        // el primer mensaje suena solo: el jugador arranca al lado de la radio
        if (playIntroOnStart)
            StartListening(gameProgress.CurrentDay);
    }

    private void OnDestroy()
    {
        if (gameProgress != null)
            gameProgress.OnDayChanged -= HandleDayChanged;
    }

    // lo llama PlayerInteraction cuando el jugador apunta a la radio y aprieta E
    public void Interact(GameObject interactor)
    {
        if (gameProgress == null || IsListening)
            return;

        // la E que acaba de saltear/cerrar el mensaje tambien le llega a PlayerInteraction, no queremos reabrirlo
        if (Time.unscaledTime < lastCloseTime + reopenBlockTime)
            return;

        // no escuchar si la partida termino o el mundo ya esta pausado por otra cosa (ej: mesa de crafteo)
        if (gameProgress.IsGameEnded || gameProgress.IsWorldPaused)
            return;

        StartListening(gameProgress.CurrentDay);
    }

    // arranca un dia nuevo: si hay un mensaje que todavia no se escucho, avisa al jugador que vaya a la radio
    private void HandleDayChanged(int day)
    {
        if (day > HighestHeardDay && !IsListening)
            messageUI.SetNewMessageHint(true);
    }

    private void StartListening(int day)
    {
        RadioMessage message = GetMessageForDay(day);

        if (message == null)
            return;

        // logica para no pausar el mundo despues de la primera vez que se escucha un mensaje por dia
        bool isFirstListen = !heardDays.Contains(day);

        if (isFirstListen)
        {
            // PauseWorld devuelve false si ya estaba pausado o la partida termino, en ese caso no hacemos nada
            if (!gameProgress.PauseWorld())
                return;

            gameProgress.PauseDayTimerForRadio();
        }

        IsListening = true;
        currentMessageDay = day;

        SetPlayerControl(false);
        messageUI.SetNewMessageHint(false);

        OnMessageStarted?.Invoke(day);
        messageUI.Play(message, HandleMessageFinished);
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
}
