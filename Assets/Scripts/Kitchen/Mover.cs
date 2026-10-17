using UnityEngine;

namespace Leftovers
{
    public class Mover : MonoBehaviour
    {
        [SerializeField] float moveSpeed = 3f;

        Rigidbody playerRigidbody;
        Vector3 moveDirection;

        void Awake()
        {
            playerRigidbody = GetComponent<Rigidbody>();
        }

        void Update()
        {
            float horizontalInput = 0f;
            float verticalInput = 0f;

            if (Input.GetKey(KeyCode.LeftArrow))
            {
                horizontalInput = horizontalInput - 1f;
            }

            if (Input.GetKey(KeyCode.RightArrow))
            {
                horizontalInput = horizontalInput + 1f;
            }

            if (Input.GetKey(KeyCode.DownArrow))
            {
                verticalInput = verticalInput - 1f;
            }

            if (Input.GetKey(KeyCode.UpArrow))
            {
                verticalInput = verticalInput + 1f;
            }

            moveDirection = new Vector3(horizontalInput, 0f, verticalInput).normalized;
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
