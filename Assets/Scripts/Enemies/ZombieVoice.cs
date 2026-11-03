using UnityEngine;

// Voz del zombie: sale por el grupo Voz del MainMixer, con un AudioSource 3D hijo del zombie (asi se oye desde donde esta).
//  - Acercarse: mientras esta cerca del player lo persigue y gruñe de vez en cuando (intervalo al azar, variante al azar sin repetir la anterior)
//  - Atacar: gruñido cuando empieza el golpe (ZombieAttack.IsWindingUp pasa a true), corta al de acercarse
// Al morir se calla. Se pone en Zombie1; las variantes (Male2, Male3, Police...) lo heredan.
[RequireComponent(typeof(Health))]
public class ZombieVoice : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private AudioSource voiceSource; //AudioSource 3D (hijo del zombie) con Output en el grupo Voz
    [SerializeField] private ZombieControllerNavMesh zombieController; //si estan vacias se buscan solas
    [SerializeField] private ZombieAttack zombieAttack;

    [Header("Voces")]
    [SerializeField] private VoiceLine[] approachLines;
    [SerializeField] private VoiceLine[] attackLines;

    [Header("Acercarse")]
    [SerializeField] private float approachDistance = 12f; //metros al player a partir de los cuales gruñe
    [SerializeField] private float minInterval = 4f; //segundos entre un gruñido y el siguiente (al azar entre min y max)
    [SerializeField] private float maxInterval = 8f;

    [Header("Debug")]
    [SerializeField] private bool debugLog = false;

    private Health health;
    private float nextApproachTime;
    private bool wasWindingUp;
    private int lastApproachIndex = -1;
    private int lastAttackIndex = -1;

    private void Awake()
    {
        health = GetComponent<Health>();
        if (!zombieController) zombieController = GetComponent<ZombieControllerNavMesh>();
        if (!zombieAttack) zombieAttack = GetComponent<ZombieAttack>();

        //el primer gruñido sale en un momento al azar, asi los zombies que aparecen juntos no suenan sincronizados
        nextApproachTime = Time.time + Random.Range(0f, maxInterval);
    }

    private void Update()
    {
        if (health.IsDead)
        {
            if (voiceSource != null && voiceSource.isPlaying) voiceSource.Stop();
            enabled = false;
            return;
        }

        if (voiceSource == null || zombieController == null || zombieController.Player == null || zombieController.IsPlayerDead)
            return;

        //ataque: se dispara una vez cuando arranca el golpe
        bool windingUp = zombieAttack != null && zombieAttack.IsWindingUp;

        if (windingUp && !wasWindingUp)
            PlayRandom(attackLines, ref lastAttackIndex, "ataque");

        wasWindingUp = windingUp;

        //acercarse: cerca del player, cada tanto, y sin pisar otro gruñido
        if (Time.time < nextApproachTime || zombieController.IsStunned)
            return;

        float distance = Vector3.Distance(transform.position, zombieController.Player.position);

        if (distance > approachDistance)
            return; //lejos no gasta el turno, vuelve a mirar el proximo frame

        if (!voiceSource.isPlaying)
            PlayRandom(approachLines, ref lastApproachIndex, "acercarse");

        nextApproachTime = Time.time + Random.Range(minInterval, Mathf.Max(minInterval, maxInterval));
    }

    //variante al azar sin repetir la anterior (con una sola suena siempre esa)
    private void PlayRandom(VoiceLine[] lines, ref int lastIndex, string reason)
    {
        if (lines == null || lines.Length == 0)
            return;

        int index = Random.Range(0, lines.Length);

        if (lines.Length > 1 && index == lastIndex)
            index = (index + Random.Range(1, lines.Length)) % lines.Length;

        lastIndex = index;

        VoiceLine line = lines[index];
        if (line == null || line.clip == null)
            return;

        voiceSource.Stop(); //el ataque corta al gruñido de acercarse
        voiceSource.clip = line.clip;
        voiceSource.volume = line.volume;
        voiceSource.Play();

        if (debugLog) Debug.Log($"[ZombieVoice] {reason} -> {line.clip.name} (vol {line.volume:0.00})", this);
    }
}
