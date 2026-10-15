using UnityEngine;

//Spawner de zombies para la noche. Escucha eventos de GameProgressController.cs.
//IMPORTANTE QUE LOS ZOMBIES TENGAN EL TAG DE ENEMY!!!

public class ZombieSpawner : MonoBehaviour
{
    [Header("Zombies")]
    [SerializeField] private GameObject[] zombiePrefabs;

    [Header("Spawns")]
    [SerializeField] private Transform spawnPointsParent;
    [SerializeField] private float timeBetweenSpawns = 4f;
    [SerializeField] private int maxZombiesAlive = 10;
    [SerializeField] private float initialSpawnDelay = 15f;

    [Header("Game Progress Manager")]
    [SerializeField] private GameProgressController gameProgressController;

    private float nextSpawnTime; //momento (Time.time) del proximo spawn permitido
    private bool nightActive; //se explica solo: true = noche, false = dia o demo terminada.
    private Transform[] spawnPoints;

    private void Awake()
    {
        LoadSpawnPoints();
    }

    //condiciones de no spawn: no es de noche, no hay prefab o spawn points, o ya hay demasiados zombies vivos.
    private void Update()
    {
        if (!nightActive) return;

        if (gameProgressController == null)
        {
            StopEnemiesSpawn();
            return;
        }

        if (zombiePrefabs == null || zombiePrefabs.Length == 0 || spawnPoints == null || spawnPoints.Length == 0)
            return;

        if (Time.time < nextSpawnTime)
            return;

        int currentZombies = GameObject.FindGameObjectsWithTag("Enemy").Length;

        if (currentZombies >= maxZombiesAlive)
        {
            nextSpawnTime = Time.time + timeBetweenSpawns;
            return;
        }

        SpawnZombie();
        nextSpawnTime = Time.time + timeBetweenSpawns;
    }

    private void OnEnable()
    {
        if (gameProgressController == null)
        {
            Debug.LogError("[ZombieSpawner] No se asigno GameProgressController.", this);
            return;
        }

        gameProgressController.OnPhaseChanged += HandlePhaseChanged;
        gameProgressController.OnGameEnded += HandleGameEnded;

        // Comprobar si la partida ya terminó.
        if (gameProgressController.IsGameEnded)
        {
            StopEnemiesSpawn();
            return;
        }

        // Comprobar el estado actual por si el spawner se activa durante la noche.
        HandlePhaseChanged(gameProgressController.CurrentPhase);
    }

    private void OnDisable()
    {
        if (gameProgressController == null)
            return;

        gameProgressController.OnPhaseChanged -= HandlePhaseChanged;
        gameProgressController.OnGameEnded -= HandleGameEnded; // detiene el spawn de enemigos al terminar la partida
    }

    private void LoadSpawnPoints()
    {
        if (spawnPointsParent == null)
        {
            Debug.LogError("[ZombieSpawner] No se asigno el objeto padre de los Spawn Points.", this);

            spawnPoints = new Transform[0];
            return;
        }

        int count = spawnPointsParent.childCount;
        spawnPoints = new Transform[count];

        for (int i = 0; i < count; i++)
        {
            spawnPoints[i] = spawnPointsParent.GetChild(i);
        }

        Debug.Log($"[ZombieSpawner] Se cargaron {spawnPoints.Length} puntos de aparicion.", this);
    }

    // revisa la fase en la que nos encontramos
    private void HandlePhaseChanged(DayNightPhase phase)
    {
        if (phase == DayNightPhase.Night && !gameProgressController.IsGameEnded)
        {
            StartEnemiesSpawn();
        }
        else
        {
            StopEnemiesSpawn();
        }
    }

    // detiene los zombies si detecta el evento de fin de juego
    private void HandleGameEnded(GameEnding ending)
    {
        StopEnemiesSpawn();
    }

    //se ejecuta al arrancar cada noche (noche 1, 2 o 3): habilita el spawn, pero el primer zombie recien aparece despues de initialSpawnDelay (ver comentario en la variable).
    private void StartEnemiesSpawn()
    {
        nightActive = true;
        nextSpawnTime = Time.time + initialSpawnDelay;
    }

    // desactiva los spawns, es para controlar los ciclos de combate
    private void StopEnemiesSpawn()
    {
        nightActive = false;
    }

    private void SpawnZombie()
    {
        //elige un spawn point al azar e instancia el prefab ahi
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        Debug.Log(Time.time);

        GameObject prefab = zombiePrefabs[Random.Range(0, zombiePrefabs.Length)];
        Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);
    }
}