using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Cinemachine;

public class ZombieTargetController : MonoBehaviour
{
    #region Stats
    [Header("Stats")]
    public int MaxZombies = 10; // Max zombies the player can control
    public float MaxZombieDistance = 10.0f; // Max distance the player can control zombies from
    public float SafeThreshold = 10.0f; // Extra buffer distance to keep zombies from instantly being lost if they step outside the control radius

    public float MoveSpeed = 1.0f; // Speed at which the player moves
    public float UpDownSpeed = 1.0f; // Speed at which the player moves up and down
    #endregion

    #region Visuals
    [Header("Visuals")]
    public CinemachineVirtualCamera Camera;

    private CinemachineFramingTransposer cameraFramingTransposer;
    private GameObject sphereVisual; // Visual representation of the max zombie distance
    #endregion

    #region Input
    private PlayerInput playerInput;
    private Vector3 moveInput;
    #endregion

    #region Attraction
    [Header("Attraction")]
    public LayerMask ZombieLayer; // Layer mask for zombies

    [SerializeField]
    private List<ZombieAgent> controlledZombies = new List<ZombieAgent>(); // List of zombies the player is controlling
    #endregion

    // Awake is called when the script instance is being loaded
    public void Awake()
    {
        // Assign references
        playerInput = new PlayerInput();
    }

    public void OnEnable()
    {
        // Enable player input
        playerInput.Player.Enable();
    }

    public void OnDisable()
    {
        // Disable player input
        playerInput.Player.Disable();
    }

    // Start is called before the first frame update
    public void Start()
    {
        // Assign references
        sphereVisual = transform.Find("SphereVisual").gameObject;

        cameraFramingTransposer = Camera.GetCinemachineComponent<CinemachineFramingTransposer>();
    }

    // Update is called once per frame
    public void Update()
    {
        // Set the size of the sphere visual to match the max zombie distance
        sphereVisual.transform.localScale = new Vector3(MaxZombieDistance, MaxZombieDistance, MaxZombieDistance);

        // Get input from the player input system
        move();
    }

    // FixedUpdate is called once per physics update
    public void FixedUpdate()
    {
        // Gain control of new zombies
        collectZombies();

        // Lose control of zombies that are too far away
        loseZombies();
    }

    // Gain control of new zombies
    private void collectZombies()
    {
        // Find zombies within the max zombie distance
        Collider[] zombies = Physics.OverlapSphere(transform.position, MaxZombieDistance, ZombieLayer);

        // Loop through all detected zombies
        foreach (Collider zombie in zombies)
        {
            // Get the zombie agent component
            ZombieAgent zombieAgent = zombie.GetComponent<ZombieAgent>();

            // If the zombie is not already controlled and the player has not reached the max number of controlled zombies
            if (!controlledZombies.Contains(zombieAgent) && controlledZombies.Count < MaxZombies)
            {
                // Add the zombie to the list of controlled zombies
                controlledZombies.Add(zombieAgent);

                // Make the zombie track this
                zombieAgent.SetTarget(transform);

                // Change the color to hint its state to the player
                zombieAgent.BodyRenderer.material.color = Color.green;
            }
        }
    }

    // Lose control of zombies that are too far away
    private void loseZombies()
    {
        List<ZombieAgent> removalQueue = new List<ZombieAgent>();

        // Loop through all controlled zombies
        foreach (ZombieAgent zombie in controlledZombies)
        {
            // If the zombie is too far away
            if (Vector3.Distance(transform.position, zombie.transform.position) > MaxZombieDistance + SafeThreshold)
            {
                // Queue up the zombie for removal
                removalQueue.Add(zombie);

                // Make the zombie stop tracking this
                zombie.SetTarget(null);

                // Change the color to hint its state to the player
                zombie.BodyRenderer.material.color = Color.white;
            }
            else if (Vector3.Distance(transform.position, zombie.transform.position) > MaxZombieDistance)
            {
                // Change the color to hint its state to the player
                zombie.BodyRenderer.material.color = Color.red;
            }
        }

        // Remove the zombies queued up for removal
        controlledZombies.RemoveAll(zombie => removalQueue.Contains(zombie));
    }

    #region Input
    public Vector3 velRef;
    public float velRefY;

    private void move()
    {
        // Move the attractor in the direction of the input
        moveInput = playerInput.Player.Move.ReadValue<Vector2>();
        float scroll = playerInput.Player.UpDown.ReadValue<float>();

        transform.position = Vector3.SmoothDamp(transform.position, transform.position + new Vector3(moveInput.x, 0, moveInput.y) * MoveSpeed * Time.deltaTime, ref velRef, 0);

        if (scroll > 0)
        {
            moveInput.z = 1.0f * UpDownSpeed;
        }
        else if (scroll < 0)
        {
            moveInput.z = -1.0f * UpDownSpeed;
        }

        // Move the camera up and down (Cinemachine follow offset)
        cameraFramingTransposer.m_TrackedObjectOffset.z = -(5 + MaxZombieDistance);
        cameraFramingTransposer.m_TrackedObjectOffset.y = Mathf.Clamp(cameraFramingTransposer.m_TrackedObjectOffset.y + moveInput.z, 1.0f, 40.0f);

        // Keep the visual in frame
        cameraFramingTransposer.m_CameraDistance = MaxZombieDistance;
    }

    public void Focus(InputAction.CallbackContext context)
    {
        Debug.Log("Focus!");
    }

    public void Attack(InputAction.CallbackContext context)
    {
        Debug.Log("Attack!");
    }
    #endregion
}
