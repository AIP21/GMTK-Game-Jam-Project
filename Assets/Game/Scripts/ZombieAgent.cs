using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class ZombieAgent : MonoBehaviour
{
    #region Navigation
    public float WanderRadius = 20f;
    public float DetectionRadius = 10f;

    [Space(10)]
    public int SetTargetInterval = 30;
    public int CharacterSearchInterval = 60;

    [Space(10)]
    public bool Searching = false;

    [Space(10)]
    public int MinWanderCooldown = 2;
    public int MaxWanderCooldown = 15;

    [Space(15)]
    public LayerMask CharacterLayer;

    private NavMeshAgent agent;
    private Transform characterTransform;
    private Vector3 targetPosition;
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

    // Called on start to slightly randomize the stats
    private void randomizeStats()
    {
        WanderRadius += Random.Range(-5f, 5f);
        DetectionRadius += Random.Range(-5f, 5f);
        SetTargetInterval += Random.Range(-10, 10);
        CharacterSearchInterval += Random.Range(-10, 10);
    }

    int tick;

    // Called every physics update (for uniform update time)
    public void FixedUpdate()
    {
        tick++;

        // If no character is being tracked or we lost sight of the character, start searching
        if (characterTransform == null || !canSeeCharacter())
        {
            Searching = true;
        }
        else
        {
            // We are tracking a character, so chase it
            targetPosition = characterTransform.position;
        }

        // Set the agent destination or search for the character (every few frames for performance)
        if (tick == SetTargetInterval)
        {
            agent.SetDestination(targetPosition);
        }
        // Find the character (every few frames for performance)
        else if (tick == CharacterSearchInterval)
        {
            tick = 0;

            // Only if searching for character
            if (Searching)
                searchForCharacter();
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

        bool shouldMove = velocity.magnitude > 0.5f && agent.remainingDistance > agent.radius;

        // Update animation parameters
        anim.SetBool("Searching", Searching);
        anim.SetFloat("InputHorizontal", velocity.x);
        anim.SetFloat("InputVertical", velocity.y);
        anim.SetFloat("InputMagnitude", velocity.magnitude);

        // Make the zombie look at the target
        if (headControl)
            headControl.lookAtTargetPosition = agent.steeringTarget + transform.forward;

        // Pull zombie towards agent to prevent drifting
        if (worldDeltaPosition.magnitude > agent.radius)
            transform.position = agent.nextPosition - 0.9f * worldDeltaPosition;
    }

    public void OnAnimatorMove()
    {
        // Update position based on animation movement using navigation surface height
        Vector3 position = anim.rootPosition;
        position.y = agent.nextPosition.y;
        transform.position = position;
    }

    // Search for the character within the detection radius
    private void searchForCharacter()
    {
        // Get all objects within the detection sphere (only check the layer that the character is on)
        Collider[] detectedColliders = Physics.OverlapSphere(transform.position, DetectionRadius, CharacterLayer);

        // If any objects were detected, then we found the character (because there will only ever be one character in the game world)
        if (detectedColliders.Length > 0)
        {
            // Set the target to the first detected character
            characterTransform = detectedColliders[0].transform;
            Searching = false;
        }
        else
        { // Nothing found, wander around and keep searching
            wander();
        }
    }

    int wanderCooldown = 0;

    // Wander around randomly while searching for a target
    private void wander()
    {
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

        // If the zombie hasn't yet reached its wander destination, then don't wander again
        if (agent.remainingDistance > agent.stoppingDistance)
            return;

        bool foundValidTarget = false;

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
                foundValidTarget = true;
                Debug.DrawLine(transform.position, hit.position, Color.green, 2);
                break;
            }

            Debug.DrawLine(transform.position, wanderTarget, Color.red, 2);
        }

        print(foundValidTarget);
    }

    // Returns true if the zombie can see the character (distance)
    private bool canSeeCharacter()
    {
        return Vector3.SqrMagnitude(transform.position - characterTransform.position) < DetectionRadius * DetectionRadius;
    }

    public void OnDrawGizmosSelected()
    {
        // Draw a yellow sphere to indicate the detection radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, DetectionRadius);

        // Draw a green sphere to indicate the wander radius
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, WanderRadius);

        if (targetPosition != null)
        {
            // Draw a small red sphere at the target position
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(targetPosition, 1);
        }
    }
}