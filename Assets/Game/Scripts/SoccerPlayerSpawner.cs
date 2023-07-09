using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Chronos;

public class SoccerPlayerSpawner : MonoBehaviour
{
    #region Spawning
    [Header("Spawning")]
    public SoccerPlayer SoccerPlayerPrefab;

    public List<SoccerPlayer> SoccerPlayers;

    [Space(10)]
    public int MaxPlayers = 10;
    public int SpawnAttempts = 10;
    public int MinSpawnCooldown = 2;
    public int MaxSpawnCooldown = 15;

    public Bounds SpawnBounds;

    private bool spawning = false;
    private int difficulty = 1;
    #endregion

    public void StartSpawning()
    {
        spawning = true;
    }

    int spawnCooldown;

    // Update is called once per frame
    public void Update()
    {
        // Spawn players if the spawning process is active
        if (spawning)
        {
            if (spawnCooldown > 0)
            {
                spawnCooldown--;
                return;
            }

            // Stop spawning if the maximum number of players has been reached
            if (SoccerPlayers.Count > MaxPlayers + difficulty)
            {
                spawning = false;

                // Update player list for all players
                foreach (SoccerPlayer p in SoccerPlayers)
                {
                    p.StartPlaying(SoccerPlayers);
                }

                return;
            }

            // Attempt to spawn a player at a random position
            for (int a = 0; a < SpawnAttempts; a++)
            {
                // Attempt to spawn a player at a random position
                // Get a random position within the spawn bounds
                Vector3 position = new Vector3(
                    Random.Range(SpawnBounds.min.x, SpawnBounds.max.x),
                    Random.Range(SpawnBounds.min.y, SpawnBounds.max.y),
                    Random.Range(SpawnBounds.min.z, SpawnBounds.max.z)
                );

                // Check if we can spawn a player there
                object validPosition = getValidSpawnPosition(position);

                if (validPosition is Vector3 && validPosition != null) // The function only returns a Vector3 if the position is valid
                {
                    // Spawn a player at the position
                    spawnPlayer((Vector3)validPosition);
                    break;
                }
            }

            spawnCooldown = Random.Range(MinSpawnCooldown, MaxSpawnCooldown);
        }
    }

    public void Reset(int difficulty)
    {
        this.difficulty = difficulty;
        ClearAllPlayers();
        StartSpawning();
    }

    // Spawns a new player at the given position
    private void spawnPlayer(Vector3 position)
    {
        SoccerPlayer player = Instantiate(SoccerPlayerPrefab, position, Quaternion.identity, transform);
        SoccerPlayers.Add(player);
        player.RandomizeStats(difficulty);

        Timeline timeline = player.gameObject.AddComponent<Timeline>();
        timeline.mode = TimelineMode.Global;
        timeline.globalClockKey = "Root";


        player.transform.parent = transform;
    }

    // Clears all players from the scene
    public void ClearAllPlayers()
    {
        for (int i = 0; i < SoccerPlayers.Count; i++)
        {
            DestroyImmediate(SoccerPlayers[i].gameObject);
        }

        SoccerPlayers.Clear();
    }

    // Returns a valid spawn position for a player given an input position
    // Returns false if no valid position could be found
    private object getValidSpawnPosition(Vector3 position)
    {
        // Check if the position is too close to another player
        foreach (SoccerPlayer player in SoccerPlayers)
        {
            if (Vector3.Distance(player.transform.position, position) < 2f)
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

    public void OnDrawGizmosSelected()
    {
        // Draw spawn box
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(transform.position + SpawnBounds.center, SpawnBounds.size);
    }
}
