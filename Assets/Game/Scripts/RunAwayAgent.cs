using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class RunAwayAgent : MonoBehaviour
{
    #region Navigation
    private NavMeshAgent agent;

    public float DetectionRadius = 30f;

    public float Randomness = 1;

    public int NearestZombieCount = 5;

    public int ZombieCheckInterval = 30;

    public List<ZombieAgent> Zombies;
    public List<ZombieAgent> NearestZombies;

    public Vector3 RunAwayPosition;
    #endregion

    #region Animation
    private Animator anim;
    private LookAt headControl;
    private Vector2 smoothDeltaPosition = Vector2.zero;
    private Vector2 velocity = Vector2.zero;
    #endregion

    // Called when the game starts
    public void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        headControl = GetComponent<LookAt>();

        RunAwayPosition = transform.position;

        // Don’t update position automatically
        agent.updatePosition = false;
    }


    public void Update()
    {
        Vector3 worldDeltaPosition = agent.nextPosition - transform.position;

        // Map 'worldDeltaPosition' to local space
        float dx = Vector3.Dot(transform.right, worldDeltaPosition);
        float dy = Vector3.Dot(transform.forward, worldDeltaPosition);
        Vector2 deltaPosition = new Vector2(dx, dy);

        // Low-pass filter the deltaMove
        float smooth = Mathf.Min(1.0f, Time.deltaTime / 0.15f);
        smoothDeltaPosition = Vector2.Lerp(smoothDeltaPosition, deltaPosition, smooth);

        // Update velocity if time advances
        if (Time.deltaTime > 1e-5f)
            velocity = smoothDeltaPosition / Time.deltaTime;

        bool shouldMove = velocity.magnitude > 0.5f && agent.remainingDistance > agent.radius;

        // Update animation parameters
        // anim.SetBool("Moving", shouldMove);
        anim.SetFloat("InputHorizontal", velocity.x);
        anim.SetFloat("InputVertical", velocity.y);
        anim.SetFloat("InputMagnitude", velocity.magnitude);

        // Make the character look at the target
        if (headControl)
            headControl.lookAtTargetPosition = agent.steeringTarget + transform.forward;

        // Pull agent towards character game object to prevent drifting
        if (worldDeltaPosition.magnitude > agent.radius)
            agent.nextPosition = transform.position + 0.9f * worldDeltaPosition;
    }

    int tick;

    // Called every physics update (for uniform update time)
    public void FixedUpdate()
    {
        tick++;

        // Find nearby zombies (every few frames for performance)
        if (tick == ZombieCheckInterval)
        {
            tick = 0;
            findNearbyZombies();
        }

        calculateTargetPosition();

        agent.SetDestination(RunAwayPosition);
    }

    public void OnAnimatorMove()
    {
        // Update position based on animation movement using navigation surface height
        Vector3 position = anim.rootPosition;
        position.y = agent.nextPosition.y;
        transform.position = position;
    }

    // Gets every zombie in the game world and finds the five nearest ones
    private void findNearbyZombies()
    {
        Zombies = new List<ZombieAgent>(GameObject.FindObjectsOfType<ZombieAgent>());
        NearestZombies = new List<ZombieAgent>();

        foreach (ZombieAgent zombie in Zombies)
        {
            if (NearestZombies.Count < NearestZombieCount)
            {
                NearestZombies.Add(zombie);
            }
            else
            {
                foreach (ZombieAgent nearestZombie in NearestZombies)
                {
                    if (Vector3.Distance(nearestZombie.transform.position, transform.position) > Vector3.Distance(zombie.transform.position, transform.position))
                    {
                        NearestZombies.Remove(nearestZombie);
                        NearestZombies.Add(zombie);
                    }
                }
            }
        }
    }

    // Calculates the target position to run away from the nearest zombies
    private void calculateTargetPosition()
    {
        Vector3 runAwayDirection = Vector3.zero;

        // Average direction to each nearby zombie
        foreach (ZombieAgent zombie in NearestZombies)
        {
            runAwayDirection += transform.position - zombie.transform.position;
        }

        runAwayDirection /= NearestZombies.Count;

        runAwayDirection.Normalize();

        // Max 10 attempts to find a valid spot to run to
        for (int i = 0; i < 10; i++)
        {
            // Now that we have the direction to run away to, we add some randomness
            Vector3 randomizedDirection = runAwayDirection + UnityEngine.Random.insideUnitSphere * Randomness;

            // Multiply the direction vector by 10 and add to the current position to get a point in the run direction
            Vector3 targetPosition = transform.position + runAwayDirection * 10;

            // Check if the target position is valid (sample navigation mesh to see if the character can reach that point)
            NavMeshHit hit;
            if (NavMesh.SamplePosition(targetPosition, out hit, 10, NavMesh.AllAreas))
            {
                RunAwayPosition = hit.position;
                break;
            }
        }
    }
}
