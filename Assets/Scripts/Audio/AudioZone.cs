using UnityEngine;

// Ambiente de una zona: el AudioSource suena (con fade) mientras el player esta dentro de este trigger. Ej: crujidos de madera sobre el puente.
// Va en un objeto con un Collider en Is Trigger que cubra la zona (se marca solo al agregar el componente). El AudioSource puede estar en este
// mismo objeto o en otro (3D, loop, Output en el grupo Ambiente). El volumen que tiene en el Inspector es el volumen maximo.
// Los fades usan tiempo SIN escalar para que sigan andando aunque el mundo este pausado.
[RequireComponent(typeof(Collider))]
public class AudioZone : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private AudioSource source; //si esta vacio se busca en este objeto

    [Header("Zona")]
    [SerializeField] private string playerTag = "Player";

    [Header("Fade")]
    [SerializeField] private float fadeIn = 1.5f; //segundos
    [SerializeField] private float fadeOut = 2f;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private float baseVolume;
    private float level;
    private int insideCount; //colliders del player adentro (por si tiene mas de uno con el tag)

    private void Reset()
    {
        Collider zone = GetComponent<Collider>();
        if (zone != null) zone.isTrigger = true;
    }

    private void Awake()
    {
        if (!source) source = GetComponent<AudioSource>();

        if (source == null)
        {
            Debug.LogError("[AudioZone] Falta el AudioSource.", this);
            enabled = false;
            return;
        }

        baseVolume = source.volume;
        source.playOnAwake = false;
        source.loop = true;
        source.volume = 0f;
        source.Stop();
    }

    private void OnDisable()
    {
        insideCount = 0;
        level = 0f;

        if (source != null) source.Stop();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        insideCount++;

        if (debugLog && insideCount == 1) Debug.Log($"[AudioZone] {name}: el player entra", this);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag(playerTag))
            return;

        insideCount = Mathf.Max(0, insideCount - 1);

        if (debugLog && insideCount == 0) Debug.Log($"[AudioZone] {name}: el player sale", this);
    }

    private void Update()
    {
        bool gameEnded = GameProgressController.Instance != null && GameProgressController.Instance.IsGameEnded;
        bool shouldPlay = insideCount > 0 && !gameEnded;

        float time = shouldPlay ? fadeIn : fadeOut;
        level = Mathf.MoveTowards(level, shouldPlay ? 1f : 0f, Time.unscaledDeltaTime / Mathf.Max(time, 0.01f));
        source.volume = baseVolume * Mathf.SmoothStep(0f, 1f, level); //curva suave: arranca y termina despacio

        if (shouldPlay && !source.isPlaying)
            source.Play();
        else if (!shouldPlay && level <= 0f && source.isPlaying)
            source.Stop();
    }
}
