using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class ZombieAgent : MonoBehaviour
{
    #region Navigation
    [Header("Navigation")]
    public float WanderRadius = 20f;
    // public float DetectionRadius = 10f;

    [Space(10)]
    public int SetTargetInterval = 30;
    public int TargetSearchInterval = 60;

    [Space(10)]
    public float MovementSpeed = 3f;


    [Space(10)]
    public bool Searching = false;

    [Space(10)]
    public int MinWanderCooldown = 2;
    public int MaxWanderCooldown = 15;

    [Space(15)]
    [HideInInspector]
    public LayerMask TargetLayer;

    private NavMeshAgent agent;
    private Transform targetTransform;
    private Vector3 targetPosition;
    #endregion

    #region Visuals
    [Header("Visuals")]
    public Renderer BodyRenderer;
    #endregion

    #region Animation
    private Animator anim;
    private LookAt headControl;
    private Vector2 smoothDeltaPosition = Vector2.zero;
    private Vector2 velocity = Vector2.zero;
    #endregion

    public void Start()
    {
        // Assign references to scripts on this game object
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        headControl = GetComponent<LookAt>();

        // Don’t update position automatically
        agent.updatePosition = false;

        targetPosition = transform.position;

        randomizeStats();
    }

    // Called on start to slightly randomize the stats (makes it look more natural when there are hordes zombies)
    private void randomizeStats()
    {
        WanderRadius += Utils.WeightedRandom(-5f, 5f, 0);
        // DetectionRadius += Utils.WeightedRandom(-5f, 5f, 0);
        SetTargetInterval += Utils.WeightedRandom(-10, 10, 0);
        TargetSearchInterval += Utils.WeightedRandom(-10, 10, 0);
        MinWanderCooldown = Mathf.Max(1, MinWanderCooldown + Utils.WeightedRandom(-3, 4, 0));
        MaxWanderCooldown = Mathf.Max(MinWanderCooldown + 3, MaxWanderCooldown + Utils.WeightedRandom(-5, 10, 0));
        MovementSpeed += Utils.WeightedRandom(-1f, 1f, 0);

        // Randomize scale
        float scaleMultiplier = Utils.WeightedRandom(0.8f, 1.2f, 1);
        transform.localScale *= scaleMultiplier;
        agent.radius *= scaleMultiplier;

        // Set agent speed to match this one
        agent.speed = MovementSpeed;
    }

    int tick;

    // Called every physics update (for uniform update time)
    public void FixedUpdate()
    {
        tick++;

        // If no target is being tracked or we lost sight of the target, start searching
        if (targetTransform == null) //|| !canSeeTarget())
        {
            Searching = true;
        }
        else
        {
            // We are tracking a target, so chase it
            targetPosition = targetTransform.position;
            Searching = false;
        }

        // Set the agent destination or search for the target (every few frames for performance)
        if (tick == SetTargetInterval)
        {
            agent.SetDestination(targetPosition);
        }
        // Find the target (every few frames for performance)
        else if (tick == TargetSearchInterval)
        {
            tick = 0;

            // Only if searching for target
            if (Searching)
                wander();
        }

        if (Searching)
        {
            BodyRenderer.material.color = Color.white;
            agent.speed = MovementSpeed;
        }
        else
        {
            agent.speed = MovementSpeed * 2f;
        }
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

        bool shouldMove = velocity.magnitude > 0.5f && !atDestination();

        // Update animation parameters
        anim.SetBool("Searching", Searching);
        anim.SetFloat("InputHorizontal", velocity.x);
        anim.SetFloat("InputVertical", velocity.y);
        anim.SetFloat("InputMagnitude", shouldMove ? velocity.magnitude / 20 : 0);

        // Make the zombie look at the target
        if (headControl)
            headControl.lookAtTargetPosition = agent.steeringTarget + transform.forward;

        // Pull zombie towards agent to prevent drifting
        if (worldDeltaPosition.magnitude > agent.radius)
            transform.position = agent.nextPosition - 0.9f * worldDeltaPosition;
    }

    public void OnAnimatorMove()
    {
        //     // Update position based on animation movement using navigation surface height
        //     Vector3 position = anim.rootPosition;
        //     position.y = agent.nextPosition.y;
        //     // transform.position = position;
    }

    // (UNUSED) Search for the target within the detection radius
    // private void searchForTarget()
    // {
    //     // Get all objects within the detection sphere (only check the layer that the target is on)
    //     Collider[] detectedColliders = Physics.OverlapSphere(transform.position, DetectionRadius, TargetLayer);

    //     // If any objects were detected, then we found the target (because there will only ever be one target in the game world)
    //     if (detectedColliders.Length > 0)
    //     {
    //         // Set the target to the first detected target
    //         targetTransform = detectedColliders[0].transform;
    //         Searching = false;
    //     }
    //     else
    //     { // Nothing found, wander around and keep searching
    //         wander();
    //     }
    // }

    int wanderCooldown = 0;

    // Wander around randomly while searching for a target
    private void wander()
    {
        // If the zombie hasn't yet reached its wander destination, then don't wander again
        // if (!atDestination())
        //     return;

        // If the wander cooldown is still active, then don't wander
        if (wanderCooldown > 0)
        {
            wanderCooldown--;
            return;
        }
        else
        {
            wanderCooldown = Random.Range(MinWanderCooldown, MaxWanderCooldown);
        }

        // Max 10 attempts to find a valid place to wander to
        for (int i = 0; i < 10; i++)
        {
            // Get a random direction to wander to
            Vector3 wanderDirection = Random.insideUnitSphere * (1.0f + (i / 10.0f));

            // Multiply the direction vector by 10 and add to the current position to get a point in the wander direction
            Vector3 wanderTarget = transform.position + wanderDirection * WanderRadius;

            // Check if the target position is valid (sample navigation mesh to see if the zombie can reach that point)
            NavMeshHit hit;
            if (NavMesh.SamplePosition(wanderTarget, out hit, 2, NavMesh.AllAreas))
            {
                // Set the target to the wander target
                targetPosition = hit.position;
                // Debug.DrawLine(transform.position, hit.position, Color.green, 2);
                break;
            }

            // Debug.DrawLine(transform.position, wanderTarget, Color.red, 2);
        }
    }

    public void SetTarget(Transform target)
    {
        targetTransform = target;
    }

    // Returns true if the zombie can see the target (distance)
    // private bool canSeeTarget()
    // {
    //     return Vector3.SqrMagnitude(transform.position - targetTransform.position) < DetectionRadius * DetectionRadius;
    // }

    // Returns true if the zombie has reached its destination
    private bool atDestination()
    {
        return agent.remainingDistance < agent.radius;
    }

    public void OnDrawGizmosSelected()
    {
        // Draw a yellow sphere to indicate the detection radius
        // Gizmos.color = Color.yellow;
        // Gizmos.DrawWireSphere(transform.position, DetectionRadius);

        // Draw a green sphere to indicate the wander radius
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, WanderRadius);

        if (agent != null)
        {
            // Draw a green sphere to indicate the agent next position
            Gizmos.color = Color.white;
            Gizmos.DrawWireSphere(agent.nextPosition, 1);
        }

        if (targetPosition != null)
        {
            // Draw a small red sphere at the target position
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetPosition, 1);
        }
    }
}