using UnityEngine;

namespace Leftovers
{
    public class Drawer : MonoBehaviour
    {
        public Vector3 openingDirection = Vector3.back;
        public float maximumOpeningDistance = 0.45f;
        public float responseSeconds = 0.008f;
        public float maximumSpeed = 3f;
        public float releasedFriction = 12f;
        public float releaseMomentum = 0.65f;

        public float Distance { get; private set; }
        public float MovementSpeed { get; private set; }
        public bool IsHeld { get; private set; }
        public Vector3 ClosedPosition { get { return closedPosition; } }
        public float MaximumOpeningDistance { get { return Mathf.Max(0f, maximumOpeningDistance); } }

        Vector3 closedPosition;
        float targetDistance;
        float velocity;
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

            initialized = true;
            closedPosition = transform.localPosition;
        }

        public void BeginHold()
        {
            Initialize();
            IsHeld = true;
            targetDistance = Distance;
            velocity = 0f;
        }

        public void DragBy(float distance)
        {
            if (IsHeld)
            {
                targetDistance = Mathf.Clamp(targetDistance + distance, 0f, MaximumOpeningDistance);
            }
        }

        public void EndHold(bool cancelMomentum = false)
        {
            IsHeld = false;
            targetDistance = Distance;
            velocity = velocity * releaseMomentum;

            if (cancelMomentum)
            {
                velocity = 0f;
            }
        }

        public Vector3 PointAtDistance(Vector3 localPoint, float distance)
        {
            Initialize();
            float clampedDistance = Mathf.Clamp(distance, 0f, MaximumOpeningDistance);
            Vector3 position = closedPosition + GetOpeningDirection() * clampedDistance;
            Vector3 point = position + transform.localRotation * Vector3.Scale(localPoint, transform.localScale);

            if (transform.parent != null)
            {
                point = transform.parent.TransformPoint(point);
            }

            return point;
        }

        void LateUpdate()
        {
            Step(Time.deltaTime);
        }

        public void Step(float deltaTime)
        {
            Initialize();

            if (deltaTime <= 0f)
            {
                return;
            }

            if (deltaTime > 0.12f)
            {
                velocity = 0f;
                targetDistance = Distance;
                return;
            }

            float previousDistance = Distance;

            if (IsHeld)
            {
                MoveHeldDrawer(deltaTime);
            }
            else
            {
                MoveReleasedDrawer(deltaTime);
            }

            Distance = Mathf.Clamp(Distance, 0f, MaximumOpeningDistance);
            StopAtLimits();
            MovementSpeed = Mathf.Abs(Distance - previousDistance) / deltaTime;
            transform.localPosition = closedPosition + GetOpeningDirection() * Distance;
        }

        void MoveHeldDrawer(float deltaTime)
        {
            float smoothing = 1f;
            if (responseSeconds > 0f)
            {
                smoothing = 1f - Mathf.Exp(-deltaTime / responseSeconds);
            }

            float nextDistance = Mathf.Lerp(Distance, targetDistance, smoothing);
            if (targetDistance == 0f || targetDistance == MaximumOpeningDistance)
            {
                nextDistance = targetDistance;
            }

            float maxMovement = Mathf.Max(0f, maximumSpeed) * deltaTime;
            float movement = Mathf.Clamp(nextDistance - Distance, -maxMovement, maxMovement);
            velocity = movement / deltaTime;
            Distance = Distance + movement;

            if (targetDistance == 0f && Distance < 0.0001f)
            {
                Distance = 0f;
            }
        }

        void MoveReleasedDrawer(float deltaTime)
        {
            if (releasedFriction <= 0f)
            {
                Distance = Distance + velocity * deltaTime;
                return;
            }

            float slowing = Mathf.Exp(-releasedFriction * deltaTime);
            Distance = Distance + velocity * (1f - slowing) / releasedFriction;
            velocity = velocity * slowing;

            if (Mathf.Abs(velocity) < 0.001f)
            {
                velocity = 0f;
            }
        }

        void StopAtLimits()
        {
            if (Distance == 0f && velocity < 0f)
            {
                velocity = 0f;
            }

            if (Distance == MaximumOpeningDistance && velocity > 0f)
            {
                velocity = 0f;
            }
        }

        Vector3 GetOpeningDirection()
        {
            if (openingDirection.sqrMagnitude < 0.0001f)
            {
                return Vector3.back;
            }

            return openingDirection.normalized;
        }

        void OnDisable()
        {
            EndHold(true);
            MovementSpeed = 0f;
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused)
            {
                EndHold(true);
            }
        }
    }
}
