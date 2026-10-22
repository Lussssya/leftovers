using UnityEngine;

namespace Leftovers
{
    public class KetchupDoorMount : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private KetchupFlip ketchup;
        [SerializeField] private Transform doorPivot;

        [Header("Position on Door")]
        [SerializeField] private Vector3 localPosition;
        [SerializeField] private Vector3 localRotation;

        private bool released;

        void Start()
        {
            if (ketchup == null || doorPivot == null)
            {
                Debug.LogWarning(
                    "KetchupDoorMount: Missing ketchup or door pivot."
                );
                return;
            }

            // Attach bottle to moving refrigerator door.
            ketchup.transform.SetParent(doorPivot, false);

            ketchup.transform.localPosition = localPosition;

            ketchup.transform.localRotation =
                Quaternion.Euler(localRotation);

            Rigidbody rb = ketchup.GetComponent<Rigidbody>();

            if (rb != null)
            {
                rb.isKinematic = true;
                rb.useGravity = false;
            }
        }

        void LateUpdate()
        {
            if (released || ketchup == null)
                return;

            // Your existing KetchupFlip handles pickup and tossing.
            // Once picked up, stop managing the door attachment.
            if (ketchup.IsHeld || ketchup.IsAirborne)
            {
                released = true;
            }
        }
    }
}

