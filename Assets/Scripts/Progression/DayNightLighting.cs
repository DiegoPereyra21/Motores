using UnityEngine;
using UnityEngine.Rendering;

public class DayNightLighting : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private GameProgressController gameProgressController;
    [SerializeField] private Light directionalSunLight;

    [Header("Skybox - Dia")]
    [SerializeField] private Material daySkyboxMaterial;
    [SerializeField, Min(0f)] private float daySkyboxExposure = 1.3f;

    [Header("Skybox - Noche")]
    [SerializeField] private Material nightSkyboxMaterial;
    [SerializeField, Min(0f)] private float nightSkyboxExposure = 0.1f;

    [Header("Iluminacion ambiental")]
    [SerializeField, Min(0f)] private float ambientIntensity = 0.7f;

    [Header("Reflejos del entorno")]
    [SerializeField, Min(0f)] private float reflectionIntensity = 0.4f;
    [SerializeField, Min(1)] private int reflectionBounces = 1;

    [Header("Iluminacion Dia")]
    [SerializeField] private Color dayLightColor = Color.white;
    [SerializeField] private float dayLightIntensity = 1.2f;
    [SerializeField] private float sunriseRotationAngle = 10f;
    [SerializeField] private float sunsetRotationAngle = 170f;

    [Header("Iluminacion Noche")]
    [SerializeField] private Color nightLightColor = new Color(0.15f, 0.2f, 0.35f);
    [SerializeField] private float nightLightIntensity = 0.05f;

    [Header("Niebla")]
    [SerializeField] private bool enableFog = true;
    [SerializeField] private FogMode fogMode = FogMode.ExponentialSquared;
    [SerializeField] private Color fogColor = Color.gray;
    [SerializeField, Min(0f)] private float dayFogDensity = 0.01f;
    [SerializeField, Min(0f)] private float nightFogDensity = 0.025f;

    private float initialSunYRotation;
    private Material originalSkyboxMaterial;
    private Material runtimeDaySkybox;
    private Material runtimeNightSkybox;

    // valores originales del entorno
    private AmbientMode originalAmbientMode;
    private DefaultReflectionMode originalReflectionMode;
    private float originalAmbientIntensity;
    private float originalReflectionIntensity;
    private int originalReflectionBounces;
    private bool originalFog;
    private FogMode originalFogMode;
    private Color originalFogColor;
    private float originalFogDensity;

    private Color originalSunColor;
    private float originalSunIntensity;
    private Quaternion originalSunRotation;


    // guarda la rotacion inicial del sol y la referencia al skybox original
    private void Awake()
    {
        if (directionalSunLight == null)
        {
            Debug.LogError("[DayNightLighting] No se asigno la luz direccional.", this);
            enabled = false;
            return;
        }

        originalSunColor = directionalSunLight.color;
        originalSunIntensity = directionalSunLight.intensity;
        originalSunRotation = directionalSunLight.transform.rotation;

        initialSunYRotation = directionalSunLight.transform.eulerAngles.y;

        SaveOriginalEnvironment();
    }

    // prepara el skybox, se suscribe a eventos y actualiza el estado visual inicial
    private void OnEnable()
    {
        if (gameProgressController == null)
        {
            Debug.LogError("[DayNightLighting] No se asigno GameProgressController.", this);

            return;
        }

        CreateRuntimeSkyboxes();

        gameProgressController.OnPhaseChanged += UpdateEnvironmentLighting;
        gameProgressController.OnPhaseProgressChanged += UpdateSunRotation;

        ApplySharedEnvironmentSettings();

        UpdateEnvironmentLighting(gameProgressController.CurrentPhase);
        UpdateSunRotation(CalculatePhaseProgress());
    }

    // cancela las suscripciones a eventos y restaura el skybox original
    private void OnDisable()
    {
        if (gameProgressController != null)
        {
            gameProgressController.OnPhaseChanged -= UpdateEnvironmentLighting;
            gameProgressController.OnPhaseProgressChanged -= UpdateSunRotation;
        }

        RestoreEnvironment();
    }

    // guarda la configuracion original de Unity
    private void SaveOriginalEnvironment()
    {
        originalSkyboxMaterial = RenderSettings.skybox;

        originalAmbientMode = RenderSettings.ambientMode;
        originalReflectionMode = RenderSettings.defaultReflectionMode;

        originalAmbientIntensity = RenderSettings.ambientIntensity;
        originalReflectionIntensity = RenderSettings.reflectionIntensity;
        originalReflectionBounces = RenderSettings.reflectionBounces;

        originalFog = RenderSettings.fog;
        originalFogMode = RenderSettings.fogMode;
        originalFogColor = RenderSettings.fogColor;
        originalFogDensity = RenderSettings.fogDensity;
    }

    // crea instancias independientes de los materiales
    private void CreateRuntimeSkyboxes()
    {
        if (daySkyboxMaterial != null && runtimeDaySkybox == null)
        {
            runtimeDaySkybox = new Material(daySkyboxMaterial);
            runtimeDaySkybox.name = daySkyboxMaterial.name + " (Runtime)";
        }

        if (nightSkyboxMaterial != null && runtimeNightSkybox == null)
        {
            runtimeNightSkybox = new Material(nightSkyboxMaterial);
            runtimeNightSkybox.name = nightSkyboxMaterial.name + " (Runtime)";
        }

        SetSkyboxExposure(runtimeDaySkybox, daySkyboxExposure);
        SetSkyboxExposure(runtimeNightSkybox, nightSkyboxExposure);
    }

    // configuracion ambiental compartida por ambas fases
    private void ApplySharedEnvironmentSettings()
    {
        RenderSettings.ambientMode = AmbientMode.Skybox;
        RenderSettings.ambientIntensity = ambientIntensity;

        RenderSettings.defaultReflectionMode = DefaultReflectionMode.Skybox;
        RenderSettings.reflectionIntensity = reflectionIntensity;
        RenderSettings.reflectionBounces = reflectionBounces;

        RenderSettings.fog = enableFog;
        RenderSettings.fogMode = fogMode;
        RenderSettings.fogColor = fogColor;
    }

    // cambia el skybox, la niebla y la luz segun la fase
    private void UpdateEnvironmentLighting(DayNightPhase phase)
    {
        Debug.Log($"[DayNightLighting] Fase: {phase} | Fog Density: {(phase == DayNightPhase.Day ? dayFogDensity : nightFogDensity)}");

        bool isDay = phase == DayNightPhase.Day;

        directionalSunLight.color = isDay ? dayLightColor : nightLightColor;

        directionalSunLight.intensity = isDay ? dayLightIntensity : nightLightIntensity;

        Material selectedSkybox = isDay ? runtimeDaySkybox : runtimeNightSkybox;

        if (selectedSkybox != null)
        {
            RenderSettings.skybox = selectedSkybox;
        }
        else
        {
            Debug.LogWarning($"[DayNightLighting] Falta asignar el Skybox de {(isDay ? "dia" : "noche")}.", this);
        }

        RenderSettings.fog = enableFog;
        RenderSettings.fogDensity = isDay ? dayFogDensity : nightFogDensity;

        DynamicGI.UpdateEnvironment();
    }

    // aplica la exposicion sin modificar el asset original
    private void SetSkyboxExposure(Material material, float exposure)
    {
        if (material == null)
            return;

        if (material.HasProperty("_Exposure"))
        {
            material.SetFloat("_Exposure", exposure);
        }
    }

    // calcula la rotacion del "sol" segun el progreso de la fase actual
    private void UpdateSunRotation(float progress01)
    {
        float angle;

        if (gameProgressController.CurrentPhase == DayNightPhase.Day)
        {
            angle = Mathf.Lerp(sunriseRotationAngle, sunsetRotationAngle, progress01);
        }
        else
        {
            angle = Mathf.Lerp(sunsetRotationAngle, sunriseRotationAngle + 360f, progress01);
        }

        directionalSunLight.transform.rotation = Quaternion.Euler(angle, initialSunYRotation, 0f);
    }

    // calcula el progreso de la fase actual con un valor entre 0 y 1
    private float CalculatePhaseProgress()
    {
        if (gameProgressController.CurrentPhaseDuration <= 0f)
            return 0f;

        float progress = 1f - gameProgressController.PhaseRemainingTime / gameProgressController.CurrentPhaseDuration;

        return Mathf.Clamp01(progress);
    }

    // restaura el entorno original y libera las instancias
    private void RestoreEnvironment()
    {
        RenderSettings.skybox = originalSkyboxMaterial;

        RenderSettings.ambientMode = originalAmbientMode;
        RenderSettings.defaultReflectionMode = originalReflectionMode;

        RenderSettings.ambientIntensity = originalAmbientIntensity;
        RenderSettings.reflectionIntensity = originalReflectionIntensity;
        RenderSettings.reflectionBounces = originalReflectionBounces;

        RenderSettings.fog = originalFog;
        RenderSettings.fogMode = originalFogMode;
        RenderSettings.fogColor = originalFogColor;
        RenderSettings.fogDensity = originalFogDensity;

        if (runtimeDaySkybox != null)
        {
            Destroy(runtimeDaySkybox);
            runtimeDaySkybox = null;
        }

        if (runtimeNightSkybox != null)
        {
            Destroy(runtimeNightSkybox);
            runtimeNightSkybox = null;
        }

        if (directionalSunLight != null)
        {
            directionalSunLight.color = originalSunColor;
            directionalSunLight.intensity = originalSunIntensity;
            directionalSunLight.transform.rotation = originalSunRotation;
        }

        DynamicGI.UpdateEnvironment();
    }
}