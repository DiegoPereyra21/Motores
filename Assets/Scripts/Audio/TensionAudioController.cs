using UnityEngine;
using UnityEngine.Audio;

// Tension del audio segun el peligro del player: cuanta menos vida tiene y mas cerca esta un zombie, mas se "apaga" el sonido.
//  - baja el corte del filtro Lowpass del grupo Musica (parametro "MusicLowpass"): la musica se oye ahogada
//  - baja un poco el volumen del Ambiente (parametro "AmbientVol"): quedan mas claros los sonidos de amenaza
// La tension va de 0 (calma) a 1 (peligro maximo) y se suaviza: sube rapido y baja despacio, asi no hay saltos ni tiembla al pasar un zombie.
// Muerto el player la tension queda en 1. Los zombies se buscan por el tag "Enemy" (EnemyDeath se lo saca al morir, asi que solo cuentan los vivos).
public class TensionAudioController : MonoBehaviour
{
    [Header("Mixer")]
    [SerializeField] private AudioMixer mixer;
    [SerializeField] private string lowpassParameter = "MusicLowpass";
    [SerializeField] private string ambientParameter = "AmbientVol";

    [Header("Tension por vida (0 a 1)")]
    [SerializeField, Range(0f, 1f)] private float safeHealth = 0.6f; //con mas vida que esto no hay tension
    [SerializeField, Range(0f, 1f)] private float criticalHealth = 0.2f; //con menos vida que esto la tension es maxima

    [Header("Tension por zombies (metros)")]
    [SerializeField] private float farDistance = 25f; //mas lejos que esto el zombie no cuenta
    [SerializeField] private float nearDistance = 6f; //mas cerca que esto la tension es maxima
    [SerializeField] private float scanInterval = 0.4f; //cada cuanto busca al zombie mas cercano

    [Header("Resultado en el mixer")]
    [SerializeField] private float calmCutoff = 22000f; //Hz de la musica en calma (sin filtrar)
    [SerializeField] private float tenseCutoff = 700f; //Hz con tension maxima (musica ahogada)
    [SerializeField] private float ambientDropDb = 6f; //cuantos dB baja el ambiente con tension maxima

    [Header("Suavizado (segundos)")]
    [SerializeField] private float riseTime = 0.8f; //tiempo en llegar a tension maxima
    [SerializeField] private float fallTime = 3f; //tiempo en volver a la calma

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private Health playerHealth;
    private Transform player;
    private float ambientRestDb;
    private float tension;
    private float appliedTension = -1f;
    private float nearestSqr = float.MaxValue;
    private float nextScanTime;
    private bool ready; //true si el mixer y sus parametros estan bien (si no, el script no toca nada)

    private void Awake()
    {
        if (mixer == null || !mixer.GetFloat(ambientParameter, out ambientRestDb))
        {
            Debug.LogError($"[TensionAudio] No se pudo leer el parametro '{ambientParameter}' del mixer (asignar el MainMixer o revisar que este expuesto).", this);
            enabled = false;
            return;
        }

        ready = true;
    }

    private void Start()
    {
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

        if (playerObj != null)
        {
            player = playerObj.transform;
            playerHealth = playerObj.GetComponent<Health>();
        }

        if (playerHealth == null)
            Debug.LogWarning("[TensionAudio] No se encontro el Player con Health, solo va a contar la distancia a los zombies.", this);
    }

    private void OnDisable()
    {
        //al apagarse devuelve el mixer a la calma (si no arranco bien no toca nada)
        if (mixer == null || !ready) return;

        mixer.SetFloat(lowpassParameter, calmCutoff);
        mixer.SetFloat(ambientParameter, ambientRestDb);
        appliedTension = -1f;
        tension = 0f;
    }

    private void Update()
    {
        if (Time.unscaledTime >= nextScanTime)
        {
            nextScanTime = Time.unscaledTime + scanInterval;
            nearestSqr = FindNearestZombieSqr();
        }

        float target = ComputeTargetTension();

        //sube rapido (el peligro se siente ya) y baja despacio (no se relaja de golpe)
        float time = target > tension ? riseTime : fallTime;
        tension = Mathf.MoveTowards(tension, target, Time.unscaledDeltaTime / Mathf.Max(time, 0.01f));

        if (Mathf.Abs(tension - appliedTension) > 0.002f)
            ApplyToMixer(tension);
    }

    //la tension es la mayor entre la que da la vida baja y la que da el zombie cercano
    private float ComputeTargetTension()
    {
        if (playerHealth != null && playerHealth.IsDead) return 1f;

        float healthTension = playerHealth != null ? Mathf.InverseLerp(safeHealth, criticalHealth, playerHealth.Normalized) : 0f;
        float zombieTension = Mathf.InverseLerp(farDistance, nearDistance, Mathf.Sqrt(nearestSqr));

        return Mathf.Max(healthTension, zombieTension);
    }

    private void ApplyToMixer(float value)
    {
        appliedTension = value;

        //el oido percibe las frecuencias de forma exponencial, por eso el corte se interpola asi y no en linea recta
        float cutoff = calmCutoff * Mathf.Pow(tenseCutoff / calmCutoff, value);

        mixer.SetFloat(lowpassParameter, cutoff);
        mixer.SetFloat(ambientParameter, ambientRestDb - ambientDropDb * value);

        if (debugLog) Debug.Log($"[TensionAudio] tension {value:0.00} -> cutoff {cutoff:0} Hz, ambiente {ambientRestDb - ambientDropDb * value:0.0} dB", this);
    }

    //distancia al cuadrado al zombie vivo mas cercano (float.MaxValue si no hay ninguno)
    private float FindNearestZombieSqr()
    {
        if (player == null) return float.MaxValue;

        float best = float.MaxValue;

        foreach (GameObject zombie in GameObject.FindGameObjectsWithTag("Enemy"))
        {
            float sqr = (zombie.transform.position - player.position).sqrMagnitude;
            if (sqr < best) best = sqr;
        }

        return best;
    }
}
