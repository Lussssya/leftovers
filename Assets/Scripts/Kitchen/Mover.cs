using UnityEngine;

namespace Leftovers
{
    public class Mover : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 3f;
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

            if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.A))
            {
                horizontalInput = horizontalInput - 1f;
            }

            if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.D))
            {
                horizontalInput = horizontalInput + 1f;
            }

            if (Input.GetKey(KeyCode.DownArrow) || Input.GetKey(KeyCode.S))
            {
                verticalInput = verticalInput - 1f;
            }

            if (Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.W))
            {
                verticalInput = verticalInput + 1f;
            }

            moveDirection = new Vector3(horizontalInput, 0f, verticalInput).normalized;

            if (movementCamera != null)
            {
                Quaternion facingRotation = Quaternion.Euler(0f, movementCamera.eulerAngles.y, 0f);
                moveDirection = facingRotation * moveDirection;
            }
        }

        void FixedUpdate()
        {
            if (playerRigidbody == null)
            {
                return;
            }

            Vector3 movement = moveDirection * moveSpeed * Time.deltaTime;
            playerRigidbody.MovePosition(playerRigidbody.position + movement);
        }
    }
}
