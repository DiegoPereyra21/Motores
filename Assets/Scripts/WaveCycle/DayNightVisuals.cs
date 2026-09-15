using UnityEngine;

//Esto trae data del script DayNightCycle y la aplica a la luz de la escena, pero no tiene logica de tiempo ni de fases. Todo eso lo maneja DayNightCycle
public class DayNightVisuals : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private DayNightCycle dayNightCycle; //el reloj del que escucha los eventos
    [SerializeField] private Light sunLight; //Directional Light de la escena (sol)

    [Header("Dia")]
    [SerializeField] private Color dayColor = Color.white; //color de la luz durante el dia
    [SerializeField] private float dayIntensity = 1.2f; //intensidad de la luz durante el dia
    [SerializeField] private float daySunriseAngle = 10f;  //rotacion en X al arrancar el dia (sol bajo, "amaneciendo")
    [SerializeField] private float daySunsetAngle = 170f;  //rotacion en X al terminar el dia (sol del otro lado, "atardeciendo")

    [Header("Noche")]
    [SerializeField] private Color nightColor = new Color(0.15f, 0.2f, 0.35f); //azulado tenue, luz de luna
    [SerializeField] private float nightIntensity = 0.05f; //intensidad muy baja, casi oscuridad total

    private void OnEnable()
    {
        //suscriptor de los 3 eventos de DayNightCycle. LLama al OnEnable() antes del Start.
        dayNightCycle.onDayStart.AddListener(HandleDayStart);
        dayNightCycle.onNightStart.AddListener(HandleNightStart);
        dayNightCycle.onPhaseProgressChanged.AddListener(HandleProgress);
    }

    private void OnDisable()
    {
        //apaga los listeners cuando se boletea el objeto
        dayNightCycle.onDayStart.RemoveListener(HandleDayStart);
        dayNightCycle.onNightStart.RemoveListener(HandleNightStart);
        dayNightCycle.onPhaseProgressChanged.RemoveListener(HandleProgress);
    }

    //arranque del dia
    private void HandleDayStart()
    {
        sunLight.color = dayColor;
        sunLight.intensity = dayIntensity;
    }

    //arranque de la noche
    private void HandleNightStart()
    {
        sunLight.color = nightColor;
        sunLight.intensity = nightIntensity;
    }

    //corre todos los frames y se importa de DayNightCycle, va de 0 a 1. Como esta normalizado, funciona para el dia y para la noche aunque duren tiempos diferentes.
    private void HandleProgress(float progress01)
    {
        //logica de vuelta al sol de 360 grados para simular la iluminacion del sol.
        float angle = dayNightCycle.CurrentPhase == DayNightPhase.Day
            ? Mathf.Lerp(daySunriseAngle, daySunsetAngle, progress01)
            : Mathf.Lerp(daySunsetAngle, daySunriseAngle + 360f, progress01);

        Vector3 currentEuler = sunLight.transform.eulerAngles;
        sunLight.transform.rotation = Quaternion.Euler(angle, currentEuler.y, 0f); //solo tocamos X, dejamos Y como esta
    }
}