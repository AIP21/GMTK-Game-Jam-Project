using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class SoccerPlayer : MonoBehaviour
{
    #region Navigation
    [Header("Navigation")]
    [Space(10)]
    public int WaitTime = 2; // Max time to wait before going after the ball

    [Space(10)]
    public float MovementSpeed = 3f;


    private NavMeshAgent agent;
    private Transform ballTransform;
    // private Rigidbody rb;
    private Rigidbody ballRb;

    private List<SoccerPlayer> allPlayers = new List<SoccerPlayer>();
    #endregion

    #region Soccer
    [Header("Soccer")]
    public float NearbyDistance = 5.0f; // The distance at which the player will try to dogpile the ball
    public int PlayersForDogpile = 3; // The number of players required to dogpile the ball
    public float DogpileRagdollDistance = 1.0f; // The distance at which the player will ragdoll when dogpiling the ball
    public float RecoveryTime = 2.0f; // The time it takes for the player to recover from ragdolling

    [Space(10)]
    public float KickForce = 10f;
    public float DogpileForce = 10f;

    public AudioClip KickSound;

    private List<Rigidbody> ragdollParts = new List<Rigidbody>();
    private AudioSource audioSource;
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
        // rb = GetComponent<Rigidbody>();
        ballTransform = GameObject.FindGameObjectWithTag("Ball").transform;
        ballRb = ballTransform.gameObject.GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();

        // Don’t update position automatically
        agent.updatePosition = false;

        ragdollParts = gameObject.GetComponentsInChildren<Rigidbody>().ToList();
    }

    // Called on start to slightly randomize the stats (makes it look more natural when there are lots of players)
    public void RandomizeStats(int difficulty)
    {
        MovementSpeed += Utils.WeightedRandom(-1f, difficulty, 0);

        // Randomize scale
        float scaleMultiplier = Utils.WeightedRandom(0.8f, 1.2f, 1);
        transform.localScale *= scaleMultiplier;
        agent.radius *= scaleMultiplier;

        // Set agent speed to match this one
        agent.speed = MovementSpeed;
    }

    public void StartPlaying(List<SoccerPlayer> players)
    {
        allPlayers.AddRange(players);
        allPlayers.Remove(this);
    }

    // Movement logic
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
        anim.SetFloat("InputHorizontal", velocity.x);
        anim.SetFloat("InputVertical", velocity.y);
        anim.SetFloat("InputMagnitude", shouldMove ? velocity.magnitude / 20 : 0);

        // Make the player look at the target
        if (headControl)
            headControl.lookAtTargetPosition = agent.steeringTarget + transform.forward;

        // Pull player towards agent to prevent drifting
        if (worldDeltaPosition.magnitude > agent.radius)
            transform.position = agent.nextPosition - 0.9f * worldDeltaPosition;

        if (ballTransform != null)
            agent.SetDestination(ballTransform.position);

        // Perform soccer logic
        soccerLogic();
    }

    int kickCooldown = 0;

    // ALl the logic related to soccer
    private void soccerLogic()
    {
        // If the player is close enough to the ball, kick it
        if (kickCooldown == 0 && Vector3.Distance(transform.position, ballTransform.position) < 1.5f)
        {
            kickCooldown = 1000;

            // Kick the ball
            ballRb.AddForce((ballTransform.position - transform.position).normalized * KickForce);

            // Play kick sound
            audioSource.PlayOneShot(KickSound);

            // Wait a bit before going after the ball again
            StartCoroutine(waitBeforeGoingAfterBall());

        }
        else if (kickCooldown > 0)
        {
            kickCooldown--;
        }

        int nearby = nearbyPlayers();

        // Dogpile the ball if there are enough players nearby
        if (nearby >= PlayersForDogpile)
        {
            // Dogpile the ball
            StartCoroutine(dogPile());
        }
    }

    // Increases the speed, runs up to the ball, enters ragdoll mode, and throws itself at the ball
    private IEnumerator dogPile()
    {
        // Increase speed
        agent.speed = MovementSpeed * 2;

        // Run up to the ball
        agent.SetDestination(ballTransform.position);

        // Wait until the player is close enough to the ball
        while (Vector3.Distance(transform.position, ballTransform.position) > DogpileRagdollDistance)
            yield return null;

        // Enter ragdoll mode
        enterRagdoll();

        // Throw itself at the ball
        // rb.AddForce(-(ballTransform.position - transform.position).normalized * DogpileForce);

        // Wait a bit before going after the ball again
        StartCoroutine(getUp());
    }

    private void enterRagdoll()
    {
        // Disable the animator
        anim.enabled = false;

        // Enable the rigidbody
        // rb.isKinematic = false;

        // Disable the navmesh agent
        agent.isStopped = true;

        for (int i = 0; i < ragdollParts.Count; i++)
        {
            ragdollParts[i].isKinematic = false;
        }
    }

    // Gets up and goes after the ball again
    private IEnumerator getUp()
    {
        // Wait a bit before going after the ball again
        yield return new WaitForSeconds(RecoveryTime);

        // Get up
        exitRagdoll();

        // Trigger get up animation
        // anim.SetTrigger("GetUp");

        // Start the navmesh agent
        agent.isStopped = false;

        // Go after the ball again
        agent.SetDestination(ballTransform.position);
    }

    private void exitRagdoll()
    {
        // Disable the rigidbody
        // rb.isKinematic = true;

        for (int i = 0; i < ragdollParts.Count; i++)
        {
            ragdollParts[i].isKinematic = true;
        }

        // Enable the animator
        anim.enabled = true;
    }

    // Waits a bit before going after the ball again
    private IEnumerator waitBeforeGoingAfterBall()
    {
        agent.SetDestination(transform.position);

        // Wait a bit before going after the ball again
        yield return new WaitForSeconds(WaitTime);

        // Go after the ball again
        agent.SetDestination(ballTransform.position);
    }

    private int nearbyPlayers()
    {
        int count = 0;
        foreach (SoccerPlayer player in allPlayers)
        {
            if (Vector3.Distance(transform.position, player.transform.position) < NearbyDistance)
                count++;
        }
        return count;
    }

    // Returns true if the player has reached its destination
    private bool atDestination()
    {
        return agent.remainingDistance < agent.radius;
    }
}