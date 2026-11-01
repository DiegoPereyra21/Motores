using System.Collections;
using UnityEngine;
using UnityEngine.Audio;

// Ducking de la musica: mientras suena una voz (hoy la radio) baja el volumen del grupo Musica con un fade,
// y cuando ya no queda ninguna voz lo devuelve a su nivel normal. Asi la voz se entiende por encima de la musica.
// Maneja el parametro expuesto "MusicVol" del MainMixer. Los fades usan tiempo SIN escalar porque la radio pausa el mundo (timeScale = 0).
// Si despues hay mas voces (dialogos del player, etc.) alcanza con llamar a BeginVoice() al empezar y EndVoice() al terminar.
public class AudioDuckingController : MonoBehaviour
{
    [Header("Mixer")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private string musicParameter = "MusicVol";

    [Header("Ducking")]
    [SerializeField] private float duckAmountDb = 12f; //cuantos dB baja la musica respecto de su nivel normal
    [SerializeField] private float fadeOutTime = 0.4f; //que tan rapido baja cuando empieza la voz
    [SerializeField] private float fadeInTime = 1.5f; //que tan lento vuelve cuando termina (mas lento queda mas natural)

    [Header("Referencias")]
    [SerializeField] private RadioInteractable radio; //si esta vacia se busca sola

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private float restDb; //nivel normal de la musica, se lee del mixer al arrancar
    private float currentDb;
    private int activeVoices; //cuantas voces suenan a la vez, la musica vuelve recien cuando es 0
    private Coroutine fadeRoutine;
    private bool ready; //true si el mixer y el parametro estan bien (si no, el script no toca nada)

    // Awake y no Start: la radio avisa el mensaje del Dia 1 en SU Start, y para ese momento ya hay que saber el nivel normal
    private void Awake()
    {
        if (radio == null) radio = FindFirstObjectByType<RadioInteractable>();

        if (mixer == null || !mixer.GetFloat(musicParameter, out restDb))
        {
            Debug.LogError($"[AudioDucking] No se pudo leer el parametro '{musicParameter}' del mixer (asignar el MainMixer o revisar que este expuesto).", this);
            enabled = false;
            return;
        }

        currentDb = restDb;
        ready = true;
    }

    private void OnEnable()
    {
        if (radio == null) return;

        radio.OnMessageStarted += HandleMessageStarted;
        radio.OnMessageFinished += HandleMessageFinished;
    }

    private void OnDisable()
    {
        if (radio != null)
        {
            radio.OnMessageStarted -= HandleMessageStarted;
            radio.OnMessageFinished -= HandleMessageFinished;
        }

        //al apagarse deja la musica como estaba, que no quede "ducked" para siempre (si no arranco bien no toca nada)
        if (!ready) return;

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        mixer.SetFloat(musicParameter, restDb);
        activeVoices = 0;
        currentDb = restDb;
    }

    private void HandleMessageStarted(int day) => BeginVoice();
    private void HandleMessageFinished(int day) => EndVoice();

    public void BeginVoice()
    {
        activeVoices++;
        UpdateTarget();
    }

    public void EndVoice()
    {
        activeVoices = Mathf.Max(0, activeVoices - 1);
        UpdateTarget();
    }

    //con al menos una voz activa la musica baja, sin ninguna vuelve a su nivel
    private void UpdateTarget()
    {
        bool ducked = activeVoices > 0;
        float target = ducked ? restDb - duckAmountDb : restDb;
        float time = ducked ? fadeOutTime : fadeInTime;

        if (fadeRoutine != null) StopCoroutine(fadeRoutine);
        fadeRoutine = StartCoroutine(FadeTo(target, time));

        if (debugLog) Debug.Log($"[AudioDucking] voces={activeVoices} -> objetivo {target:0.0} dB en {time:0.0}s", this);
    }

    private IEnumerator FadeTo(float target, float time)
    {
        float start = currentDb;

        //si el fade anterior quedo a mitad de camino, este tarda proporcionalmente menos (sin saltos bruscos)
        float duration = Mathf.Max(0.01f, time * Mathf.Abs(target - start) / Mathf.Max(duckAmountDb, 0.01f));
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            currentDb = Mathf.Lerp(start, target, elapsed / duration);
            mixer.SetFloat(musicParameter, currentDb);
            yield return null;
        }

        currentDb = target;
        mixer.SetFloat(musicParameter, currentDb);
        fadeRoutine = null;
    }
}
