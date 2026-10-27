using UnityEngine;

namespace Leftovers
{
    public class Mover : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 3f;
        [SerializeField] float collisionSkin = 0.02f;
        [SerializeField] Transform movementCamera;

        Rigidbody playerRigidbody;
        Vector3 moveDirection;
        CameraFollow viewController;

        void Awake()
        {
            playerRigidbody = GetComponent<Rigidbody>();
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
                return;
            }

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

            Vector3 movement = moveDirection * moveSpeed * Time.fixedDeltaTime;
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
    }
}
