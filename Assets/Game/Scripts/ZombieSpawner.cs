using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ZombieSpawner : MonoBehaviour
{
    #region Spawning
    [Header("Spawning")]
    public ZombieAgent ZombiePrefab;

    public List<ZombieAgent> Zombies;

    [Space(10)]
    public int MaxZombies = 10;
    public int SpawnAttempts = 10;
    public int MinSpawnCooldown = 2;
    public int MaxSpawnCooldown = 15;

    public Bounds SpawnBounds;

    private bool spawning = false;
    #endregion

    // Start is called before the first frame update
    public void Start()
    {
        StartSpawning();
    }

    public void StartSpawning()
    {
        spawning = true;
    }

    int spawnCooldown;

    // Update is called once per frame
    public void Update()
    {
        // Spawn zombies if the spawning process is active
        if (spawning)
        {
            if (spawnCooldown > 0)
            {
                spawnCooldown--;
                return;
            }

            // Stop spawning if the maximum number of zombies has been reached
            if (Zombies.Count > MaxZombies)
            {
                spawning = false;
                return;
            }

            // Attempt to spawn a zombie at a random position
            for (int a = 0; a < SpawnAttempts; a++)
            {
                // Attempt to spawn a zombie at a random position
                // Get a random position within the spawn bounds
                Vector3 position = new Vector3(
                    Random.Range(SpawnBounds.min.x, SpawnBounds.max.x),
                    Random.Range(SpawnBounds.min.y, SpawnBounds.max.y),
                    Random.Range(SpawnBounds.min.z, SpawnBounds.max.z)
                );

                // Check if we can spawn a zombie there
                object validPosition = getValidSpawnPosition(position);

                if (validPosition is Vector3) // The function only returns a Vector3 if the position is valid
                {
                    // Spawn a zombie at the position
                    spawnZombie((Vector3)validPosition);
                    break;
                }
            }

            spawnCooldown = Random.Range(MinSpawnCooldown, MaxSpawnCooldown);
        }
    }

    // Spawns a new zombie at the given position
    private void spawnZombie(Vector3 position)
    {
        ZombieAgent zombie = Instantiate(ZombiePrefab, position, Quaternion.identity);
        zombie.transform.parent = transform;
        Zombies.Add(zombie);
    }

    // Clears all zombies from the scene
    public void ClearAllZombies()
    {
        for (int i = 0; i < Zombies.Count; i++)
        {
            Destroy(Zombies[i]);
        }

        Zombies.Clear();
    }

    // Returns a valid spawn position for a zombie given an input position
    // Returns false if no valid position could be found
    private object getValidSpawnPosition(Vector3 position)
    {
        // Check if the position is too close to another zombie
        foreach (ZombieAgent zombie in Zombies)
        {
            if (Vector3.Distance(zombie.transform.position, position) < 2f)
            {
                return false;
            }
        }

        // Check if the position is valid on the navmesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(position, out hit, 2f, NavMesh.AllAreas))
        {
            return hit.position; // Return that navmesh position
        }

        return false;
    }
}
