using UnityEngine;

namespace Leftovers
{
    public class CabinetPart : MonoBehaviour
    {
        public bool slidesOut;
        public float openingDirection = -1f;
        public float maximumOpen = 100f;
        public float moveSpeed = 360f;
        public FridgeDoor hingedDoor;

        public bool IsHeld
        {
            get
            {
                if (hingedDoor != null)
                {
                    return hingedDoor.IsHeld;
                }

                return isHeld;
            }
        }

        bool isHeld;
        Vector3 closedPosition;
        Quaternion closedRotation;
        float currentOpen;
        float targetOpen;
        bool initialized;

        void Awake()
        {
            Initialize();
        }

        void Initialize()
        {
            if (initialized)
            {
                return;
            }

            closedPosition = transform.localPosition;
            closedRotation = transform.localRotation;
            initialized = true;
        }

        public void BeginHold()
        {
            if (hingedDoor != null)
            {
                hingedDoor.BeginHold();
                return;
            }

            Initialize();
            isHeld = true;
            targetOpen = currentOpen;
        }

        public void DragBy(float amount)
        {
            if (hingedDoor != null)
            {
                hingedDoor.DragBy(amount);
                return;
            }

            if (IsHeld)
            {
                targetOpen = Mathf.Clamp(targetOpen + amount, 0f, maximumOpen);
            }
        }

        public void EndHold(bool cancelMomentum = false)
        {
            if (hingedDoor != null)
            {
                hingedDoor.EndHold(cancelMomentum);
                return;
            }

            isHeld = false;
            targetOpen = currentOpen;
        }

        public Vector3 PointAtOpen(Vector3 localPoint, float amount)
        {
            if (hingedDoor != null)
            {
                return hingedDoor.PointAtAngle(localPoint, amount);
            }

            Initialize();

            if (slidesOut)
            {
                Vector3 position = closedPosition + Vector3.forward * amount;
                return transform.parent.TransformPoint(position + closedRotation * localPoint);
            }

            Quaternion rotation = closedRotation * Quaternion.AngleAxis(openingDirection * amount, Vector3.up);
            return transform.parent.TransformPoint(closedPosition + rotation * localPoint);
        }

        void LateUpdate()
        {
            if (hingedDoor != null)
            {
                return;
            }

            currentOpen = Mathf.MoveTowards(currentOpen, targetOpen, moveSpeed * Time.deltaTime);

            if (slidesOut)
            {
                transform.localPosition = closedPosition + Vector3.forward * currentOpen;
            }
            else
            {
                transform.localRotation = closedRotation * Quaternion.AngleAxis(openingDirection * currentOpen, Vector3.up);
            }
        }

        void OnDisable()
        {
            EndHold(true);
        }
    }
}
