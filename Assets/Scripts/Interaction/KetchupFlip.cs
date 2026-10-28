using UnityEngine;
using UnityEngine.Events;

namespace Leftovers
{
    [RequireComponent(typeof(Rigidbody))]
    public class KetchupFlip : MonoBehaviour
    {
        [Header("Interaction")]
        [SerializeField] Camera interactionCamera;
        [SerializeField] Transform holdPoint;
        [SerializeField] float pickupReach = 10f;
        [SerializeField] float catchDistance = 1.3f;

        [Header("Toss")]
        [SerializeField] float upwardVelocity = 3.2f;
        [SerializeField] float forwardVelocity = 0.6f;
        [SerializeField] float spinVelocity = 7f;

        [Header("Flip Check")]
        [SerializeField] Transform orientationReference;

        [Range(0f, 1f)]
        [SerializeField] float upsideDownThreshold = 0.6f;

        [SerializeField] Vector3 uprightHeldRotation = Vector3.zero;

        [SerializeField] Vector3 upsideDownHeldRotation =
            new Vector3(0f, 0f, 180f);

        [Header("Drop")]
        [SerializeField] float minimumImpactSpeed = 1f;
        [SerializeField] float minimumAirTime = 0.15f;

        [Header("Audio")]
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip dropClip;
        [SerializeField] float pitchVariation = 0.04f;

        [Header("Events")]
        [SerializeField] UnityEvent onFlipSuccess;
        [SerializeField] UnityEvent onDropped;

        Rigidbody body;
        Collider[] bottleColliders;

        bool isHeld;
        bool isAirborne;
        bool isFlipped;
        bool hasFailed;

        float tossTime;

        public bool IsHeld => isHeld;
        public bool IsAirborne => isAirborne;
        public bool IsFlipped => isFlipped;
        public bool HasFailed => hasFailed;

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            bottleColliders = GetComponentsInChildren<Collider>();

            if (interactionCamera == null)
            {
                interactionCamera = Camera.main;
            }

            if (orientationReference == null)
            {
                orientationReference = transform;
            }

            body.isKinematic = true;
            body.useGravity = false;
        }

        void Update()
        {
            if (hasFailed)
            {
                return;
            }

            if (!isHeld &&
                !isAirborne &&
                Input.GetKeyDown(KeyCode.E))
            {
                TryInitialPickup();
            }

            if (isHeld &&
                !isFlipped &&
                Input.GetKeyDown(KeyCode.F))
            {
                Toss();
            }

            if (isAirborne &&
                Input.GetKeyDown(KeyCode.E))
            {
                TryCatch();
            }
        }

        void TryInitialPickup()
        {
            Debug.Log("E pressed - trying to pick up ketchup");

            if (interactionCamera == null)
            {
                Debug.LogError("Interaction Camera is missing!");
                return;
            }

            if (holdPoint == null)
            {
                Debug.LogError("Hold Point is missing!");
                return;
            }

            float distance = Vector3.Distance(
                interactionCamera.transform.position,
                transform.position
            );

            Debug.Log("Distance to ketchup: " + distance);

            if (distance > pickupReach)
            {
                Debug.Log("Ketchup is too far away.");
                return;
            }

            Debug.Log("Ketchup is close enough - picking it up!");

            HoldBottle(false);
        }

        void HoldBottle(bool flipped)
        {
            isHeld = true;
            isAirborne = false;
            isFlipped = flipped;

            if (!body.isKinematic)
            {
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }

            body.isKinematic = true;
            body.useGravity = false;

            SetCollidersEnabled(false);

            transform.SetParent(holdPoint);
            transform.localPosition = Vector3.zero;

            if (flipped)
            {
                transform.localRotation =
                    Quaternion.Euler(upsideDownHeldRotation);
            }
            else
            {
                transform.localRotation =
                    Quaternion.Euler(uprightHeldRotation);
            }
        }

        void Toss()
        {
            if (interactionCamera == null)
            {
                return;
            }

            transform.SetParent(null, true);

            SetCollidersEnabled(true);

            body.isKinematic = false;
            body.useGravity = true;

            body.velocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;

            Vector3 launchVelocity =
                interactionCamera.transform.up * upwardVelocity +
                interactionCamera.transform.forward * forwardVelocity;

            body.AddForce(
                launchVelocity,
                ForceMode.VelocityChange
            );

            body.AddTorque(
                interactionCamera.transform.right * spinVelocity,
                ForceMode.VelocityChange
            );

            isHeld = false;
            isAirborne = true;

            tossTime = Time.time;
        }

        void TryCatch()
        {
            if (holdPoint == null)
            {
                return;
            }

            float distance =
                Vector3.Distance(
                    transform.position,
                    holdPoint.position
                );
            Debug.Log("Catch attempt distance: " + distance);

            if (distance > catchDistance)
            {
                return;
            }

            bool successfulFlip = IsBottleUpsideDown();
            Debug.Log("Upside down: " + successfulFlip);

            HoldBottle(successfulFlip);

            if (successfulFlip)
            {
                Debug.Log("Ketchup flip successful!");

                onFlipSuccess?.Invoke();
            }
            else
            {
                Debug.Log(
                    "Ketchup caught, but not upside down. Try again."
                );
            }
        }

        bool IsBottleUpsideDown()
        {
            Vector3 bottleUp = orientationReference.up;

            float alignment =
                Vector3.Dot(bottleUp, Vector3.down);

            return alignment >= upsideDownThreshold;
        }

        void OnCollisionEnter(Collision collision)
        {
            if (!isAirborne || hasFailed)
            {
                return;
            }

            if (Time.time - tossTime < minimumAirTime)
            {
                return;
            }

            if (collision.relativeVelocity.magnitude <
                minimumImpactSpeed)
            {
                return;
            }

            FailFromDrop();
        }

        void FailFromDrop()
        {
            hasFailed = true;
            isAirborne = false;

            PlayDropSound();

            Debug.Log(
                "Ketchup dropped - mission failed!"
            );

            onDropped?.Invoke();
        }

        void PlayDropSound()
        {
            if (audioSource == null || dropClip == null)
            {
                return;
            }

            float variation =
                Mathf.Abs(pitchVariation);

            audioSource.pitch =
                Random.Range(
                    1f - variation,
                    1f + variation
                );

            audioSource.PlayOneShot(dropClip);
        }

        void SetCollidersEnabled(bool enabled)
        {
            foreach (Collider bottleCollider in bottleColliders)
            {
                bottleCollider.enabled = enabled;
            }
        }
    }
}