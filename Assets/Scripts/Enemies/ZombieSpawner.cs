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
    [SerializeField, Min(1)] private int maxZombiesPerNight = 30; // cantidad maxima de enemigos por noche
    [SerializeField, Min(1)] private int maxZombiesAlive = 10; // cantidad maxima de enemigos activos a la vez
    [SerializeField] private float initialSpawnDelay = 15f;

    [Header("Game Progress Manager")]
    [SerializeField] private GameProgressController gameProgressController;

    private int zombiesSpawnedThisNight;
    private bool canSpawnZombies;
    private float nextSpawnTime; //momento (Time.time) del proximo spawn permitido
    private bool nightActive; //se explica solo: true = noche, false = dia o demo terminada.

    private bool hordeCleared;
    private bool hasSpawnedZombie;

    private Transform[] spawnPoints;

    private void Awake()
    {
        LoadSpawnPoints();
    }

    //condiciones de no spawn: no es de noche, no hay prefab o spawn points, o ya hay demasiados zombies vivos.
    private void Update()
    {
        if (gameProgressController == null)
        {
            StopEnemiesSpawn();
            return;
        }

        CheckIfHordeIsCleared();

        if (!nightActive) return;

        if (zombiePrefabs == null || zombiePrefabs.Length == 0 || spawnPoints == null || spawnPoints.Length == 0)
            return;

        // ya se genero el maximo de zombies de la noche
        if (zombiesSpawnedThisNight >= maxZombiesPerNight)
        {
            canSpawnZombies = false;
            return;
        }

        if (!canSpawnZombies) return;

        if (Time.time < nextSpawnTime) return;

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
        canSpawnZombies = true;
        zombiesSpawnedThisNight = 0;

        hordeCleared = false;
        hasSpawnedZombie = false;

        nextSpawnTime = Time.time + initialSpawnDelay;
    }

    // desactiva los spawns, es para controlar los ciclos de combate
    private void StopEnemiesSpawn()
    {
        nightActive = false;
        canSpawnZombies = false;
    }

    private void SpawnZombie()
    {
        //elige un spawn point al azar e instancia el prefab ahi
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        GameObject prefab = zombiePrefabs[Random.Range(0, zombiePrefabs.Length)];
        Instantiate(prefab, spawnPoint.position, spawnPoint.rotation);

        zombiesSpawnedThisNight++;
        hasSpawnedZombie = true;
    }

    private void CheckIfHordeIsCleared()
    {
        if (hordeCleared || !hasSpawnedZombie)
            return;

        int currentZombies = GameObject.FindGameObjectsWithTag("Enemy").Length;

        if (currentZombies > 0)
            return;

        hordeCleared = true;
        canSpawnZombies = false;

        Debug.Log("[ZombieSpawner] Horda eliminada.");
    }
}