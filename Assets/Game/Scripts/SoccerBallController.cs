using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SoccerBallController : MonoBehaviour
{
    #region Movement
    [Header("Movement")]
    public float MovementSpeed;
    public float JumpForce;

    [HideInInspector]
    public Rigidbody rb;

    public float LoseSensitivity = 0.05f;

    private List<float> latestSpeeds = new List<float>();
    #endregion

    #region Visuals
    [Header("Visuals")]
    public TrailRenderer Trail;
    #endregion

    #region Audio
    [Header("Audio")]
    public AudioClip JumpSound;
    #endregion

    #region Input
    [Header("Input")]
    public Transform Camera;
    public Transform CameraTarget;

    private PlayerInput playerInput;
    private Vector3 moveInput;

    [HideInInspector]
    public bool isStopped = true;

    [Space(10)]
    public int MaxJumpCooldown = 240;
    public int JumpCooldown = 0;
    #endregion

    // Awake is called when the script instance is being loaded
    public void Awake()
    {
        // Assign references
        playerInput = new PlayerInput();
        rb = GetComponent<Rigidbody>();
        Trail = GetComponent<TrailRenderer>();
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

    public float smoothTime = 0.05f;
    int tick = 0;
    bool spacebarDown = false;

    public void FixedUpdate()
    {
        if (isStopped)
            return;

        if (playerInput == null)
        {
            playerInput = new PlayerInput();
            playerInput.Player.Enable();
        }

        if (grounded() && playerInput.Player.Jump.IsPressed() && JumpCooldown < MaxJumpCooldown)
        {
            JumpCooldown++;
            spacebarDown = true;
        }

        // Spacebar let go
        if (grounded() && spacebarDown && !playerInput.Player.Jump.IsPressed())
        {
            Vector3 jump = new Vector3(0.0f, JumpCooldown * JumpForce, 0.0f);
            rb.AddForce(jump);

            JumpCooldown = 0;

            AudioSource.PlayClipAtPoint(JumpSound, transform.position);
        }

        // Get the direction the camera is facing
        Vector3 cameraForward = Camera.forward;
        cameraForward.y = 0.0f;
        cameraForward.Normalize();

        // Move the attractor in the direction of the input using the camera's forward direction
        moveInput = playerInput.Player.Move.ReadValue<Vector2>();
        Vector3 move = cameraForward * moveInput.y + Camera.right * moveInput.x;
        move.y = 0.0f;
        move.Normalize();
        move *= MovementSpeed;
        rb.AddForce(move);

        Debug.DrawRay(transform.position, move * 2, Color.red);
        Debug.DrawRay(transform.position, rb.velocity * 2, Color.blue);
        Debug.DrawRay(transform.position, CameraTarget.forward * 2, Color.green);

        CameraTarget.position = transform.position;

        // Rotate the target y to face where the velocity is going
        Vector3 velocity = rb.velocity;
        velocity.y = 0.0f;
        velocity.Normalize();

        Vector3 currentForward = CameraTarget.forward;
        currentForward.y = 0.0f;
        currentForward.Normalize();

        Vector3 newForward = Vector3.SmoothDamp(currentForward, velocity, ref velocity, smoothTime);
        CameraTarget.forward = newForward;

        if (tick < 10)
        {
            tick++;
            return;
        }

        tick = 0;

        // Keep track of the speed
        latestSpeeds.Add(rb.velocity.magnitude);

        if (latestSpeeds.Count > 100)
            latestSpeeds.RemoveAt(0);

        float averageSpeed = 0.0f;
        foreach (float speed in latestSpeeds)
            averageSpeed += speed;

        averageSpeed /= latestSpeeds.Count;

        if (latestSpeeds.Count == 100 && averageSpeed < LoseSensitivity)
            GameManager.Instance.Lose();
    }

    // public void Jump(InputAction.CallbackContext context)
    // {
    //     // if (JumpCooldown < MaxJumpCooldown)
    //     // {
    //     //     return;
    //     // }

    //     if (!grounded())
    //     {
    //         return;
    //     }

    //     Vector3 jump = new Vector3(0.0f, JumpCooldown, 0.0f);
    //     rb.AddForce(jump);

    //     JumpCooldown = 0;
    // }

    private bool grounded()
    {
        return Physics.Raycast(transform.position, Vector3.down, 1.0f);
    }

    public void Stop()
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = true;
        isStopped = true;
    }

    public void Begin()
    {
        rb.velocity = Vector3.zero;
        rb.angularVelocity = Vector3.zero;
        rb.isKinematic = false;
        isStopped = false;

        latestSpeeds.Clear();
    }
}