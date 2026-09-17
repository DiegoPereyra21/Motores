using UnityEngine;
using UnityEngine.Events;

//Fases de juego
public enum DayNightPhase
{
    Day,
    Night
}

//Motor (reloj) de tiempos para dia/noche.
public class DayNightCycle : MonoBehaviour
{
    [Header("Duracion de fases (segundos)")]
    [SerializeField] private float dayDuration = 90f;   //1 minuto y medio
    [SerializeField] private float nightDuration = 180f; //3 minutos

    [Header("Duracion de la demo")]
    //lo configuro con 3 noches para cumplir con el tiempo estimado de la demo (abierto a cambios)
    [SerializeField] private int totalNights = 3;

    [Header("Eventos")]
    //eventos de inicio de cada fase.
    public UnityEvent onDayStart;
    public UnityEvent onNightStart;

    //valor de 0 a 1 para DayNightVisuals.cs para mover el sol de forma gradual en vez de saltar de dia a noche de una.
    public UnityEvent<float> onPhaseProgressChanged;

    //evento para terminar la demo (cuando se completa la ultima noche). Se suscribe DemoEndUI.cs para mostrar la pantalla de fin de demo.
    public UnityEvent onDemoEnd;

    //fase actual del ciclo. Publica para que sea de facil acceso para otros scripts (capaz ponemos un HUD que indique cuanto queda del dia/noche).
    public DayNightPhase CurrentPhase { get; private set; }

    //contador de noches
    public int CurrentNight { get; private set; }

    //progreso normalizado (0 a 1) dentro de la fase actual. Mismo valor que se manda por onPhaseProgressChanged
    public float PhaseProgress01 { get; private set; }

    //cuanto tiempo (en segundos) paso desde que arranco la fase actual. Se resetea a 0 en cada cambio de fase.
    private float phaseTimer;

    //true una vez que se dispara onDemoEnd para parar el juego.
    private bool demoEnded;

    //duracion de la fase actual
    private float CurrentPhaseDuration => CurrentPhase == DayNightPhase.Day ? dayDuration : nightDuration;

    private void Start()
    {
        //entendiendo que arrancamos de noche
        StartNextNight();
    }

    private void Update()
    {
        if (demoEnded) return; //GameOver

        phaseTimer += Time.deltaTime;
        PhaseProgress01 = Mathf.Clamp01(phaseTimer / CurrentPhaseDuration);
        onPhaseProgressChanged.Invoke(PhaseProgress01);

        if (phaseTimer >= CurrentPhaseDuration)
            SwitchPhase();
    }

    //metodo para las fases de dia/noche
    private void SwitchPhase()
    {
        phaseTimer = 0f;

        if (CurrentPhase == DayNightPhase.Night)
        {
            if (CurrentNight >= totalNights)
            {
                EndDemo();
                return;
            }

            CurrentPhase = DayNightPhase.Day;
            Debug.Log($"[DayNightCycle] Cambio de fase -> DIA (despues de la Noche {CurrentNight})"); //DEBUG TEMPORAL: confirma en consola que el cambio de fase realmente ocurre
            onDayStart.Invoke();
        }
        else
        {
            StartNextNight();
        }
    }

    //metodo para la noche siguiente. Incrementa el contador de noches y dispara el evento onNightStart.
    private void StartNextNight()
    {
        CurrentNight++;
        CurrentPhase = DayNightPhase.Night;
        Debug.Log($"[DayNightCycle] Cambio de fase -> NOCHE {CurrentNight}"); //DEBUG TEMPORAL: confirma en consola que el cambio de fase realmente ocurre
        onNightStart.Invoke();
    }

    //Cierre del ciclo
    private void EndDemo()
    {
        demoEnded = true;
        Debug.Log("[DayNightCycle] Demo terminada"); //DEBUG TEMPORAL
        onDemoEnd.Invoke();
    }
}