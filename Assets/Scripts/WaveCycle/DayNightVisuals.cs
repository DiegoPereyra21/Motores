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

    [Header("Skybox")]
    //El skybox seguia manteniendo brillo. Con esto se ajusta la exposicion y la noche tiene oscuridad mas creible.
    [SerializeField] private float daySkyboxExposure = 1.3f; //valor por defecto del skybox, de dia se ve bien el cielo
    [SerializeField] private float nightSkyboxExposure = 0.1f; //bien bajo, de noche casi no se ve el cielo

    //Angulo Y original de la luz como valor fijo
    private float sunYAngle;

    //instancia propia del material del skybox
    private Material skyboxMaterial;

    private void OnEnable()
    {
        sunYAngle = sunLight.transform.eulerAngles.y; //se guarda una unica vez, al activarse el script

        if (RenderSettings.skybox != null)
        {
            skyboxMaterial = new Material(RenderSettings.skybox);
            RenderSettings.skybox = skyboxMaterial;
        }

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

        if (skyboxMaterial != null)
            skyboxMaterial.SetFloat("_Exposure", daySkyboxExposure);
    }

    //arranque de la noche
    private void HandleNightStart()
    {
        sunLight.color = nightColor;
        sunLight.intensity = nightIntensity;

        if (skyboxMaterial != null)
            skyboxMaterial.SetFloat("_Exposure", nightSkyboxExposure);
    }

    //corre todos los frames y se importa de DayNightCycle, va de 0 a 1. Como esta normalizado, funciona para el dia y para la noche aunque duren tiempos diferentes.
    private void HandleProgress(float progress01)
    {
        //logica de vuelta al sol de 360 grados para simular la iluminacion del sol.
        float angle = dayNightCycle.CurrentPhase == DayNightPhase.Day
            ? Mathf.Lerp(daySunriseAngle, daySunsetAngle, progress01)
            : Mathf.Lerp(daySunsetAngle, daySunriseAngle + 360f, progress01);

        //usamos el angulo Y cacheado en Awake/OnEnable en vez de releerlo del transform (ver comentario en sunYAngle). Esto me hacia flickering en la luz del dia.
        sunLight.transform.rotation = Quaternion.Euler(angle, sunYAngle, 0f);
    }
}