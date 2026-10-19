using UnityEngine;

namespace Leftovers
{
    public class DoorCollision : MonoBehaviour
    {
        public FridgeDoor door;
        public Transform applianceRoot;
        public BoxCollider[] parts;

        Collider[] nearby = new Collider[64];

        public float LimitAngle(float previous, float wanted)
        {
            if (parts == null || parts.Length == 0 || Mathf.Abs(wanted - previous) < 0.0001f)
            {
                return wanted;
            }

            Physics.SyncTransforms();
            int steps = Mathf.CeilToInt(Mathf.Abs(wanted - previous) / 0.25f);
            float safe = previous;
            for (int i = 1; i <= steps; i++)
            {
                float angle = Mathf.Lerp(previous, wanted, (float)i / steps);
                if (BlockedAt(angle))
                {
                    float blocked = angle;
                    for (int j = 0; j < 4; j++)
                    {
                        float middle = (safe + blocked) * 0.5f;
                        if (BlockedAt(middle))
                        {
                            blocked = middle;
                        }
                        else
                        {
                            safe = middle;
                        }
                    }
                    return safe;
                }
                safe = angle;
            }
            return wanted;
        }

        bool BlockedAt(float angle)
        {
            foreach (BoxCollider part in parts)
            {
                if (part == null || !part.enabled)
                {
                    continue;
                }

                Vector3 center = transform.InverseTransformPoint(part.transform.TransformPoint(part.center));
                center = door.PointAtAngle(center, angle);
                Quaternion relative = Quaternion.Inverse(transform.rotation) * part.transform.rotation;
                Quaternion rotation = door.RotationAtAngle(angle) * relative;
                Vector3 scale = part.transform.lossyScale;
                scale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                Vector3 half = Vector3.Scale(part.size * 0.5f, scale) - Vector3.one * 0.001f;
                int count = Physics.OverlapBoxNonAlloc(center, half, nearby, rotation, ~0, QueryTriggerInteraction.Ignore);
                if (count == nearby.Length)
                {
                    return true;
                }
                for (int i = 0; i < count; i++)
                {
                    if (BlocksDoor(nearby[i]))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        bool BlocksDoor(Collider other)
        {
            if (other.transform.IsChildOf(transform))
            {
                return false;
            }
            if (other.GetComponentInParent<MicrowaveFood>() != null)
            {
                return true;
            }
            if (applianceRoot != null && other.transform.IsChildOf(applianceRoot))
            {
                return other.GetComponentInParent<FridgeDoor>() != null;
            }
            return true;
        }
    }
}
