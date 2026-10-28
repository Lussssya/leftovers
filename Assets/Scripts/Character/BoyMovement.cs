using UnityEngine;

public class BoyMovement : MonoBehaviour
{
    public float moveSpeed = 3f;
    public float rotationSpeed = 10f;

    [Header("Gravity")]
    public float gravity = -20f;

    [Header("Footsteps")]
    public AudioClip footstepSound;
    public float soundStartTime = 1f;

    private AudioSource footstepSource;
    private CharacterController controller;

    private float verticalVelocity;
    
    public bool canMove = true;

    void Start()
    {
        controller = GetComponent<CharacterController>();
        footstepSource = GetComponent<AudioSource>();

        if (controller == null)
        {
            Debug.LogError("Character Controller is missing!");
        }

        if (footstepSource != null && footstepSound != null)
        {
            footstepSource.clip = footstepSound;
            footstepSource.loop = false;
            footstepSource.playOnAwake = false;
            footstepSource.volume = 1f;
        }
    }

    void Update()
    {
        float horizontal = 0f;
        float vertical = 0f;

        if (canMove)
        {
            horizontal = Input.GetAxisRaw("Horizontal");
            vertical = Input.GetAxisRaw("Vertical");
        }

        Vector3 movement =
            new Vector3(horizontal, 0f, vertical);

        if (movement.magnitude > 1f)
        {
            movement.Normalize();
        }

        bool isMoving =
            movement.sqrMagnitude > 0.01f;

        // -------------------------
        // GRAVITY
        // -------------------------

        if (controller.isGrounded &&
            verticalVelocity < 0f)
        {
            // Keeps player firmly on floor
            verticalVelocity = -2f;
        }
        else
        {
            verticalVelocity +=
                gravity * Time.deltaTime;
        }

        // Horizontal movement
        Vector3 velocity =
            movement * moveSpeed;

        // Vertical gravity movement
        velocity.y = verticalVelocity;

        controller.Move(
            velocity * Time.deltaTime
        );

        // -------------------------
        // ROTATION
        // -------------------------

        if (isMoving)
        {
            Quaternion targetRotation =
                Quaternion.LookRotation(movement);

            transform.rotation =
                Quaternion.Slerp(
                    transform.rotation,
                    targetRotation,
                    rotationSpeed * Time.deltaTime
                );
        }

        // -------------------------
        // FOOTSTEPS
        // -------------------------

        if (footstepSource != null &&
            footstepSound != null)
        {
            if (isMoving && controller.isGrounded)
            {
                if (!footstepSource.isPlaying)
                {
                    footstepSource.time =
                        Mathf.Clamp(
                            soundStartTime,
                            0f,
                            footstepSound.length - 0.01f
                        );

                    footstepSource.Play();
                }
            }
            else
            {
                if (footstepSource.isPlaying)
                {
                    footstepSource.Stop();
                }
            }
        }
    }
}