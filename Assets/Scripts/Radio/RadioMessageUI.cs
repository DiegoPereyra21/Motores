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

    [Header("Audio (murmullo mientras suenan los subtitulos)")]
    [SerializeField] private AudioClip murmurClip; // opcional, despues se puede cambiar por voz real si lo decidimos asi.
    [Range(0f, 1f)] [SerializeField] private float murmurVolume = 0.6f;

    [Header("Tiempos")]
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

    private AudioSource murmurSource;
    private Coroutine playRoutine;
    private Coroutine hintRoutine;

    private bool isPlaying;
    private bool skipRequested;
    private float playStartTime;

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
        StartMurmur();

        yield return Fade(radioPanel, 0f, 1f, fadeDuration);

        if (message.subtitleLines != null)
        {
            foreach (string line in message.subtitleLines)
            {
                if (skipRequested) break;

                yield return TypeLine(line);
                if (skipRequested) break;

                yield return WaitUnscaled(lineHoldTime);
                if (skipRequested) break;

                subtitleLabel.text = string.Empty;
                yield return WaitUnscaled(lineGapTime);
            }
        }

        StopMurmur();

        // si lo salteo, el fade de salida es mas corto
        yield return Fade(radioPanel, 1f, 0f, skipRequested ? fadeDuration * 0.4f : fadeDuration);

        SetDisplay(radioPanel, false);
        subtitleLabel.text = string.Empty;
        isPlaying = false;
        playRoutine = null;

        onFinished?.Invoke();
    }

    // efecto maquina de escribir
    private IEnumerator TypeLine(string line)
    {
        float elapsed = 0f;
        int shown = 0;

        while (shown < line.Length && !skipRequested)
        {
            elapsed += Time.unscaledDeltaTime;
            shown = Mathf.Min(line.Length, Mathf.FloorToInt(elapsed * typeSpeed));
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

    // murmullo en loop mientras dura el mensaje (sin clip asignado no suena nada)
    private void StartMurmur()
    {
        if (murmurClip == null)
            return;

        if (murmurSource == null)
        {
            murmurSource = gameObject.AddComponent<AudioSource>();
            murmurSource.playOnAwake = false;
            murmurSource.loop = true;
            murmurSource.spatialBlend = 0f; // 2D, suena igual desde cualquier lado
        }

        murmurSource.clip = murmurClip;
        murmurSource.volume = murmurVolume;
        murmurSource.Play();
    }

    private void StopMurmur()
    {
        if (murmurSource != null)
            murmurSource.Stop();
    }

    private static void SetDisplay(VisualElement element, bool visible)
    {
        if (element != null)
            element.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
    }
}
