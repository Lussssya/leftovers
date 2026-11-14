using UnityEngine;

namespace Leftovers
{
    public class Mover : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 3f;
        [SerializeField] float sprintMultiplier = 1.5f;
        [SerializeField] float collisionSkin = 0.02f;
        [SerializeField] Transform movementCamera;
        [SerializeField] AudioSource footstepSource;
        [SerializeField] AudioClip footstepClip;
        [SerializeField] float stepDistance = 1.4f;
        [SerializeField] [Range(0f, 1f)] float footstepVolume = 0.15f;

        Rigidbody playerRigidbody;
        Vector3 moveDirection;
        CameraFollow viewController;
        bool isSprinting;
        Vector3 previousStepPosition;
        float distanceSinceStep;

        void Awake()
        {
            playerRigidbody = GetComponent<Rigidbody>();
            previousStepPosition = transform.position;
            if (movementCamera != null)
            {
                viewController = movementCamera.GetComponent<CameraFollow>();
            }
        }

        void Update()
        {
            if (Time.timeScale == 0f || (viewController != null && viewController.IsDragging))
            {
                moveDirection = Vector3.zero;
                isSprinting = false;
                return;
            }

            isSprinting = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);

            float horizontalInput = 0f;
            float verticalInput = 0f;

            if (Input.GetKey(KeyCode.A))
            {
                horizontalInput = horizontalInput - 1f;
            }

            if (Input.GetKey(KeyCode.D))
            {
                horizontalInput = horizontalInput + 1f;
            }

            if (Input.GetKey(KeyCode.S))
            {
                verticalInput = verticalInput - 1f;
            }

            if (Input.GetKey(KeyCode.W))
            {
                verticalInput = verticalInput + 1f;
            }

            Vector3 horizontalForward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
            Vector3 horizontalRight = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
            moveDirection = horizontalRight * horizontalInput + horizontalForward * verticalInput;
            moveDirection = Vector3.ClampMagnitude(moveDirection, 1f);
        }

        void FixedUpdate()
        {
            if (playerRigidbody == null)
            {
                return;
            }

            float currentSpeed = moveSpeed;
            if (isSprinting)
            {
                currentSpeed = currentSpeed * sprintMultiplier;
            }

            Vector3 movement = moveDirection * currentSpeed * Time.fixedDeltaTime;
            movement = GetCollisionSafeMovement(movement);
            playerRigidbody.MovePosition(playerRigidbody.position + movement);
        }

        Vector3 GetCollisionSafeMovement(Vector3 movement)
        {
            float distance = movement.magnitude;
            if (distance <= 0f)
            {
                return Vector3.zero;
            }

            Vector3 direction = movement / distance;
            RaycastHit hit;
            if (!playerRigidbody.SweepTest(
                direction,
                out hit,
                distance,
                QueryTriggerInteraction.Ignore))
            {
                return movement;
            }

            float safeDistance = Mathf.Max(0f, hit.distance - collisionSkin);
            return direction * safeDistance;
        }

        void LateUpdate()
        {
            Vector3 displacement = transform.position - previousStepPosition;
            previousStepPosition = transform.position;
            displacement.y = 0f;
            RaycastHit ground;
            if (playerRigidbody == null || Time.timeScale == 0f ||
                moveDirection.sqrMagnitude < 0.01f || displacement.magnitude > 1f ||
                (viewController != null && viewController.IsDragging) ||
                !playerRigidbody.SweepTest(Vector3.down, out ground, 0.15f, QueryTriggerInteraction.Ignore))
            {
                distanceSinceStep = 0f;
                return;
            }

            distanceSinceStep += displacement.magnitude;
            if (distanceSinceStep >= Mathf.Max(0.2f, stepDistance))
            {
                distanceSinceStep = 0f;
                if (footstepSource != null && footstepClip != null)
                {
                    footstepSource.PlayOneShot(footstepClip, footstepVolume);
                }
            }
        }
    }
}
