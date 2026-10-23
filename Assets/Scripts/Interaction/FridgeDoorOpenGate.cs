
using UnityEngine;

namespace Leftovers
{
    public class FridgeDoorOpenGate : MonoBehaviour
    {
        [Header("Door")]
        [SerializeField] private Transform upperDoor;

        [Header("Settings")]
        [SerializeField] private float openAngle = 35f;

        private Quaternion closedRotation;

        public bool IsOpen
        {
            get
            {
                if (upperDoor == null)
                    return false;

                float angle = Quaternion.Angle(
                    closedRotation,
                    upperDoor.localRotation
                );

                return angle >= openAngle;
            }
        }

        private void Awake()
        {
            if (upperDoor == null)
                upperDoor = transform;

            closedRotation = upperDoor.localRotation;
        }
    }
}

