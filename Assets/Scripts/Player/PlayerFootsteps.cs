using UnityEngine;

//Pasos del player. Un paso cada cierta distancia recorrida (mas corta caminando, mas larga corriendo), asi que no depende de las animaciones.
//El sonido depende del piso que pisa: el terreno se detecta por su textura dominante (pasto o tierra) y los demas pisos con un FootstepSurface.
//De cada piso elige una variante al azar pero nunca repite la anterior. Sin movimiento, en el aire o sin control del player no suena nada.
[RequireComponent(typeof(CharacterController))]
public class PlayerFootsteps : MonoBehaviour
{
    [System.Serializable]
    private class SurfaceSounds
    {
        public FootstepSurfaceType surface;
        public AudioClip[] clips;
        [Range(0f, 1f)] public float volume = 0.5f; //cada piso tiene su volumen porque los clips vienen muy distintos de fuertes
        [System.NonSerialized] public int lastIndex = -1;
    }

    [Header("Referencias")]
    [SerializeField] private AudioSource sfxSource; //Output en el grupo SFX del MainMixer, si esta vacio usa el del player
    [SerializeField] private PlayerController playerController;

    [Header("Sonidos por piso")]
    [SerializeField] private SurfaceSounds[] surfaces;
    [SerializeField] private FootstepSurfaceType fallbackSurface = FootstepSurfaceType.Dirt; //si no se reconoce el piso
    [SerializeField] private string[] grassKeywords = { "grass", "pasto" }; //si la textura dominante del terreno tiene alguno de estos nombres es pasto, si no es tierra

    [Header("Ritmo")]
    [SerializeField] private float walkSpeed = 2f; //mismas velocidades que PlayerController
    [SerializeField] private float runSpeed = 6f;
    [SerializeField] private float walkStride = 0.9f; //metros entre pasos caminando
    [SerializeField] private float runStride = 2f; //metros entre pasos corriendo
    [SerializeField] private float minSpeed = 0.3f; //por debajo de esto se considera parado
    [SerializeField, Range(0f, 1f)] private float walkVolume = 0.6f; //caminando suena mas bajo que corriendo (corriendo = 1)

    [Header("Deteccion del piso")]
    [SerializeField] private LayerMask groundMask = ~0;
    [SerializeField] private float rayLength = 1.5f;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private CharacterController controller;
    private float stepDistance;
    private bool wasMoving;
    private string lastGroundName = "(nada)"; //solo para el log: que collider se detecto debajo

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        if (!sfxSource) sfxSource = GetComponent<AudioSource>();
        if (!playerController) playerController = GetComponent<PlayerController>();
    }

    private void Update()
    {
        //muerto, en la intro o sin control: PlayerController apagado, no hay pasos (y evita usar una velocidad vieja)
        if (playerController != null && !playerController.enabled)
        {
            wasMoving = false;
            return;
        }

        Vector3 velocity = controller.velocity;
        velocity.y = 0f;
        float speed = velocity.magnitude;

        if (speed < minSpeed)
        {
            wasMoving = false;
            return;
        }

        if (!controller.isGrounded) return; //en el aire no cuenta distancia, pero tampoco reinicia el ritmo

        float run = Mathf.InverseLerp(walkSpeed, runSpeed, speed); //0 = caminando, 1 = corriendo
        float stride = Mathf.Lerp(walkStride, runStride, run);

        //al empezar a moverse el primer paso llega a mitad de zancada, no instantaneo ni despues de una zancada completa
        if (!wasMoving)
        {
            wasMoving = true;
            stepDistance = stride * 0.5f;
        }

        stepDistance += speed * Time.deltaTime;

        if (stepDistance >= stride)
        {
            stepDistance -= stride;
            PlayStep(run);
        }
    }

    private void PlayStep(float run)
    {
        if (sfxSource == null) return;

        FootstepSurfaceType surface = DetectSurface();
        SurfaceSounds sounds = FindSounds(surface);

        if (sounds == null || sounds.clips == null || sounds.clips.Length == 0) return;

        int index = PickIndex(sounds);
        AudioClip clip = sounds.clips[index];
        if (clip == null) return;

        float volume = sounds.volume * Mathf.Lerp(walkVolume, 1f, run);
        sfxSource.PlayOneShot(clip, volume);

        if (debugLog) Debug.Log($"[Footsteps] {surface} -> {clip.name} (vol {volume:0.00}, corriendo {run:0.00}) sobre '{lastGroundName}'", this);
    }

    //variante al azar, sin repetir la anterior (con una sola variante suena siempre esa)
    private int PickIndex(SurfaceSounds sounds)
    {
        int count = sounds.clips.Length;
        int index = Random.Range(0, count);

        if (count > 1 && index == sounds.lastIndex)
            index = (index + Random.Range(1, count)) % count;

        sounds.lastIndex = index;
        return index;
    }

    private SurfaceSounds FindSounds(FootstepSurfaceType surface)
    {
        if (surfaces == null) return null;

        foreach (SurfaceSounds sounds in surfaces)
            if (sounds.surface == surface) return sounds;

        return null;
    }

    //rayo hacia abajo desde los pies: un FootstepSurface manda, si es terreno se mira su textura dominante, si no se usa el piso por defecto
    private FootstepSurfaceType DetectSurface()
    {
        Vector3 origin = transform.position + Vector3.up * 0.2f;

        if (!Physics.Raycast(origin, Vector3.down, out RaycastHit hit, rayLength, groundMask, QueryTriggerInteraction.Ignore))
            return fallbackSurface;

        lastGroundName = hit.collider.name;

        FootstepSurface marker = hit.collider.GetComponentInParent<FootstepSurface>();
        if (marker != null) return marker.Surface;

        Terrain terrain = hit.collider.GetComponent<Terrain>();
        if (terrain != null) return SurfaceFromTerrain(terrain, hit.point);

        return fallbackSurface;
    }

    private FootstepSurfaceType SurfaceFromTerrain(Terrain terrain, Vector3 worldPoint)
    {
        TerrainData data = terrain.terrainData;
        if (data == null || data.alphamapLayers == 0) return fallbackSurface;

        //punto del mundo -> celda del mapa de texturas del terreno
        Vector3 local = worldPoint - terrain.transform.position;
        int x = Mathf.Clamp((int)(local.x / data.size.x * data.alphamapWidth), 0, data.alphamapWidth - 1);
        int z = Mathf.Clamp((int)(local.z / data.size.z * data.alphamapHeight), 0, data.alphamapHeight - 1);

        float[,,] weights = data.GetAlphamaps(x, z, 1, 1);
        int dominant = 0;

        for (int i = 1; i < data.alphamapLayers; i++)
            if (weights[0, 0, i] > weights[0, 0, dominant]) dominant = i;

        TerrainLayer[] layers = data.terrainLayers;
        if (dominant >= layers.Length || layers[dominant] == null) return fallbackSurface;

        string layerName = layers[dominant].name.ToLowerInvariant();
        string textureName = layers[dominant].diffuseTexture != null ? layers[dominant].diffuseTexture.name.ToLowerInvariant() : "";

        foreach (string keyword in grassKeywords)
        {
            string key = keyword.ToLowerInvariant();
            if (layerName.Contains(key) || textureName.Contains(key)) return FootstepSurfaceType.Grass;
        }

        if (debugLog) Debug.Log($"[Footsteps] terreno '{terrain.name}': textura dominante '{layers[dominant].name}' -> tierra", this);
        return FootstepSurfaceType.Dirt;
    }
}
