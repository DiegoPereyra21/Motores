using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

// UI de la radio (UI Toolkit, RadioMessage.uxml). Muestra los subtitulos sobre una imagen de fondo.
// Tambien maneja los dos avisos chicos: "hay un mensaje nuevo" y el mensaje de objetivo que queda unos segundos despues de escuchar.
// Todo corre con tiempo SIN escala (unscaled), porque el mundo esta pausado (Time.timeScale = 0) mientras se escuchan las transmisiones.
[RequireComponent(typeof(UIDocument))]
public class RadioMessageUI : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference skipAction; // la misma accion Interact (E) que usa PlayerInteraction

    [Header("Visuales")]
    [SerializeField] private Texture2D backgroundImage; // la imagen de la radio, difuminada. Si esta vacia queda fondo oscuro

    [Header("Audio (la voz que lee el mensaje)")]
    [SerializeField] private AudioSource voiceSource; // AudioSource 2D con Output en el grupo Voz del MainMixer, el clip lo pone cada RadioMessage
    [Range(0f, 1f)] [SerializeField] private float voiceVolume = 1f;
    [Range(0.3f, 1f)] [SerializeField] private float typeFraction = 0.85f; // con voz: que parte del tiempo de cada frase se usa para escribirla, el resto queda la frase completa
    [SerializeField] private float voiceEndWait = 4f; // maximo que espera a que termine la voz si los subtitulos terminaron antes
    [SerializeField] private float voiceSkipFade = 0.15f; // fade de la voz cuando el jugador saltea el mensaje

    [Header("Tiempos (sin voz, o mensajes sin clip)")]
    [SerializeField] private float typeSpeed = 30f; // caracteres por segundo
    [SerializeField] private float lineHoldTime = 1.8f; // cuanto se queda la frase completa antes de pasar a la siguiente
    [SerializeField] private float lineGapTime = 0.4f; // pausa entre frases
    [SerializeField] private float fadeDuration = 0.6f;
    [SerializeField] private float hintDuration = 6f; // cuanto dura el mensaje de objetivo en pantalla
    [SerializeField] private float ignoreSkipTime = 0.3f; // al abrir ignora E un instante, es la misma E que abrio la radio

    private VisualElement radioPanel;
    private VisualElement radioBackground;
    private Label subtitleLabel;
    private Label objectiveHintLabel;
    private Label newMessageLabel;

    private Coroutine playRoutine;
    private Coroutine hintRoutine;
    private Coroutine voiceFadeRoutine;

    private bool isPlaying;
    private bool skipRequested;
    private float playStartTime;

    //para q no aparezca el consejo en la intro de camara
    private bool holdHints;
    private string pendingHint;


    private void OnEnable()
    {
        // se buscan en OnEnable porque el UIDocument reconstruye el arbol cada vez que se activa
        VisualElement root = GetComponent<UIDocument>().rootVisualElement;

        radioPanel = root.Q<VisualElement>("RadioPanel");
        radioBackground = root.Q<VisualElement>("RadioBackground");
        subtitleLabel = root.Q<Label>("SubtitleLabel");
        objectiveHintLabel = root.Q<Label>("ObjectiveHintLabel");
        newMessageLabel = root.Q<Label>("NewMessageLabel");

        if (radioPanel == null || subtitleLabel == null)
            Debug.LogError("[RadioMessageUI] Faltan RadioPanel o SubtitleLabel en el UXML.", this);

        if (backgroundImage != null && radioBackground != null)
            radioBackground.style.backgroundImage = new StyleBackground(backgroundImage);

        // todo arranca oculto
        SetDisplay(radioPanel, false);
        SetDisplay(objectiveHintLabel, false);
        SetDisplay(newMessageLabel, false);

        if (skipAction != null)
            skipAction.action.Enable();
    }

    private void OnDisable()
    {
        // si se apaga el panel a mitad de mensaje la voz no queda sonando sola
        if (voiceSource != null)
            voiceSource.Stop();
    }

    private void Update()
    {
        if (!isPlaying || skipAction == null)
            return;

        // E saltea el mensaje entero
        if (skipAction.action.WasPressedThisFrame() && Time.unscaledTime - playStartTime > ignoreSkipTime)
            skipRequested = true;
    }

    // muestra un mensaje de radio y avisa con onFinished cuando termina (solo o salteado)
    public void Play(RadioMessage message, Action onFinished)
    {
        if (playRoutine != null)
            StopCoroutine(playRoutine);

        playRoutine = StartCoroutine(PlayRoutine(message, onFinished));
    }

    // cartelito "Hay un mensaje nuevo en la radio", queda hasta que se escuche
    public void SetNewMessageHint(bool visible)
    {
        SetDisplay(newMessageLabel, visible);
    }

    // el "MENSAJE EN PANTALLA" que queda unos segundos despues de escuchar la radio
    public void ShowObjectiveHint(string text)
    {
        if (string.IsNullOrWhiteSpace(text) || objectiveHintLabel == null)
            return;

        if (holdHints)
        {
            pendingHint = text;
            return;
        }

        if (hintRoutine != null)
            StopCoroutine(hintRoutine);

        hintRoutine = StartCoroutine(HintRoutine(text));
    }

    private IEnumerator PlayRoutine(RadioMessage message, Action onFinished)
    {
        isPlaying = true;
        skipRequested = false;
        playStartTime = Time.unscaledTime;

        subtitleLabel.text = string.Empty;
        radioPanel.style.opacity = 0f;
        SetDisplay(radioPanel, true);

        // la voz arranca junto con el fade de entrada, asi los subtitulos se ajustan a lo que queda de audio
        bool hasVoice = StartVoice(message);

        yield return Fade(radioPanel, 0f, 1f, fadeDuration);

        if (message.subtitleLines != null)
        {
            if (hasVoice)
                yield return PlayLinesSyncedToVoice(message);
            else
                yield return PlayLinesFixedTiming(message.subtitleLines);
        }

        // si los subtitulos terminaron antes que la voz, deja que la voz termine
        if (hasVoice)
            yield return WaitForVoiceEnd();

        StopVoice(skipRequested);

        // si lo salteo, el fade de salida es mas corto
        yield return Fade(radioPanel, 1f, 0f, skipRequested ? fadeDuration * 0.4f : fadeDuration);

        SetDisplay(radioPanel, false);
        subtitleLabel.text = string.Empty;
        isPlaying = false;
        playRoutine = null;

        onFinished?.Invoke();
    }

    // tiempos fijos (typeSpeed, lineHoldTime y lineGapTime), para los mensajes sin voz
    private IEnumerator PlayLinesFixedTiming(string[] lines)
    {
        foreach (string line in lines)
        {
            if (skipRequested) break;

            yield return TypeLine(line, typeSpeed);
            if (skipRequested) break;

            yield return WaitUnscaled(lineHoldTime);
            if (skipRequested) break;

            subtitleLabel.text = string.Empty;
            yield return WaitUnscaled(lineGapTime);
        }
    }

    // con voz: cada frase empieza cuando se la dice en el audio (message.lineStartTimes). Si no hay tiempos cargados,
    // lo que queda de audio se reparte entre las frases segun su cantidad de caracteres (aproximado).
    // Cada frase se escribe a la velocidad justa para terminar un poco antes de que empiece la siguiente.
    private IEnumerator PlayLinesSyncedToVoice(RadioMessage message)
    {
        string[] lines = message.subtitleLines;
        float clipLength = message.voiceClip.length;
        float[] starts = GetLineStartTimes(message, clipLength);

        for (int i = 0; i < lines.Length; i++)
        {
            if (skipRequested) break;

            float lineStart = starts[i];
            float lineEnd = i + 1 < lines.Length ? starts[i + 1] : clipLength;

            // espera el momento en que se dice esta frase, medido sobre la posicion real del audio (no sobre el reloj):
            // si la voz tarda en arrancar o se atrasa, los subtitulos la esperan y no se adelantan
            yield return WaitForVoiceTime(lineStart);
            if (skipRequested) break;

            float typeTime = Mathf.Max((lineEnd - lineStart) * typeFraction, 0.05f);
            yield return TypeLine(lines[i], lines[i].Length / typeTime);
            if (skipRequested) break;

            // la frase completa queda hasta que empieza la siguiente (la ultima, hasta que termina la voz)
            yield return WaitForVoiceTime(lineEnd);
            if (skipRequested) break;

            subtitleLabel.text = string.Empty;
        }
    }

    // los tiempos cargados en el mensaje, o un reparto por caracteres si faltan
    private float[] GetLineStartTimes(RadioMessage message, float clipLength)
    {
        string[] lines = message.subtitleLines;

        if (message.lineStartTimes != null && message.lineStartTimes.Length == lines.Length)
            return message.lineStartTimes;

        float[] starts = new float[lines.Length];
        float availableTime = Mathf.Max(clipLength - fadeDuration, 1f);

        int totalChars = 0;
        foreach (string line in lines)
            totalChars += line.Length;

        float elapsed = fadeDuration;

        for (int i = 0; i < lines.Length; i++)
        {
            starts[i] = elapsed;
            if (totalChars > 0) elapsed += availableTime * lines[i].Length / totalChars;
        }

        return starts;
    }

    // efecto maquina de escribir (speed = caracteres por segundo)
    private IEnumerator TypeLine(string line, float speed)
    {
        float elapsed = 0f;
        int shown = 0;

        while (shown < line.Length && !skipRequested)
        {
            elapsed += Time.unscaledDeltaTime;
            shown = Mathf.Min(line.Length, Mathf.FloorToInt(elapsed * speed));
            subtitleLabel.text = line.Substring(0, shown);
            yield return null;
        }

        if (!skipRequested)
            subtitleLabel.text = line;
    }

    private IEnumerator HintRoutine(string text)
    {
        objectiveHintLabel.text = text;
        objectiveHintLabel.style.opacity = 0f;
        SetDisplay(objectiveHintLabel, true);

        yield return Fade(objectiveHintLabel, 0f, 1f, 0.5f);
        yield return new WaitForSecondsRealtime(hintDuration);
        yield return Fade(objectiveHintLabel, 1f, 0f, 0.5f);

        SetDisplay(objectiveHintLabel, false);
        hintRoutine = null;
    }

    // espera sin escala de tiempo, y se corta apenas el jugador saltea
    private IEnumerator WaitUnscaled(float seconds)
    {
        float elapsed = 0f;

        while (elapsed < seconds && !skipRequested)
        {
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    // espera a que el audio llegue al segundo indicado. Si la voz termina (o se salteo) deja de esperar
    private IEnumerator WaitForVoiceTime(float seconds)
    {
        while (!skipRequested && voiceSource != null && voiceSource.isPlaying && voiceSource.time < seconds)
            yield return null;
    }

    private IEnumerator Fade(VisualElement element, float from, float to, float duration)
    {
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            element.style.opacity = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
            yield return null;
        }

        element.style.opacity = to;
    }

    // arranca la voz del mensaje. Devuelve false si el mensaje no tiene clip o no hay AudioSource (en ese caso queda solo con subtitulos)
    private bool StartVoice(RadioMessage message)
    {
        if (message.voiceClip == null)
            return false;

        if (voiceSource == null)
            voiceSource = GetComponent<AudioSource>();

        if (voiceSource == null)
        {
            Debug.LogWarning("[RadioMessageUI] El mensaje tiene voz pero falta asignar el AudioSource (Output en el grupo Voz).", this);
            return false;
        }

        if (voiceFadeRoutine != null)
        {
            StopCoroutine(voiceFadeRoutine);
            voiceFadeRoutine = null;
        }

        voiceSource.Stop();
        voiceSource.clip = message.voiceClip;
        voiceSource.volume = voiceVolume;
        voiceSource.loop = false;
        voiceSource.Play();

        return true;
    }

    // espera a que termine la voz (con un maximo), o se corta si el jugador saltea
    private IEnumerator WaitForVoiceEnd()
    {
        float waited = 0f;

        while (voiceSource != null && voiceSource.isPlaying && !skipRequested && waited < voiceEndWait)
        {
            waited += Time.unscaledDeltaTime;
            yield return null;
        }
    }

    // al saltear la voz baja con un fade cortito, para que no se corte de golpe
    private void StopVoice(bool withFade)
    {
        if (voiceSource == null || !voiceSource.isPlaying)
            return;

        if (withFade)
            voiceFadeRoutine = StartCoroutine(FadeOutVoice());
        else
            voiceSource.Stop();
    }

    private IEnumerator FadeOutVoice()
    {
        float startVolume = voiceSource.volume;
        float elapsed = 0f;

        while (elapsed < voiceSkipFade)
        {
            elapsed += Time.unscaledDeltaTime;
            voiceSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / voiceSkipFade);
            yield return null;
        }

        voiceSource.Stop();
        voiceSource.volume = voiceVolume;
        voiceFadeRoutine = null;
    }

    private static void SetDisplay(VisualElement element, bool visible)
    {
        if (element != null)
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // mientras esta en true los avisos de objetivo no se muestran, quedan en espera y salen al volver a false
    public bool HoldHints
    {
        get => holdHints;
        set
        {
            holdHints = value;
            if (!holdHints && pendingHint != null)
            {
                string text = pendingHint;
                pendingHint = null;
                ShowObjectiveHint(text);
            }
        }
    }
}
