using UnityEngine;

public class ZombieSpawner : MonoBehaviour
{
    //config de spawn
    [SerializeField] private GameObject zombiePrefab; //prefab a instanciar
    [SerializeField] private Transform[] spawnPoints; //puntos posibles de aparicion
    [SerializeField] private float timeBetweenSpawns = 4f; //segundos entre spawns
    [SerializeField] private int maxZombiesAlive = 10; //limite de zombies vivos a la vez (podemos ampliarlo para que varie segun la oleada)

    //privada
    private float nextSpawnTime; //momento (Time.time) del proximo spawn permitido

    private void Update()
    {
        if (Time.time < nextSpawnTime) return; //todavia no toca spawnear
        if (zombiePrefab == null || spawnPoints == null || spawnPoints.Length == 0) return; //faltan referencias

        //cuenta cuantos zombies hay vivos usando el tag Enemy
        int currentZombies = GameObject.FindGameObjectsWithTag("Enemy").Length;
        if (currentZombies >= maxZombiesAlive) return; //maximo de zombies alcanzado (limite de spawns al mismo tiempo)

        SpawnZombie();
        nextSpawnTime = Time.time + timeBetweenSpawns;
    }

    private void SpawnZombie()
    {
        //elige un spawn point al azar e instancia el prefab ahi
        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        Instantiate(zombiePrefab, spawnPoint.position, spawnPoint.rotation);
    }
}