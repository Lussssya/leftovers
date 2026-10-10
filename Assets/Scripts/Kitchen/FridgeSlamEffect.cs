using UnityEngine;

namespace Leftovers
{
    public class FridgeSlamEffect : MonoBehaviour
    {
        public float shakeAngle = 0.6f;
        public float shakeSeconds = 0.25f;

        FridgeDoor[] doors;
        Quaternion restingRotation;
        float remainingTime;

        void OnEnable()
        {
            restingRotation = transform.localRotation;
            doors = GetComponentsInChildren<FridgeDoor>(true);
            foreach (FridgeDoor door in doors)
            {
                door.SoundEmitted += OnDoorSound;
            }
        }

        void OnDoorSound(FridgeDoor door, DoorSoundKind kind, float noise)
        {
            if (kind == DoorSoundKind.Slam)
            {
                remainingTime = Mathf.Max(0f, shakeSeconds);
                if (door.slamPuff != null)
                {
                    EmitPuff(door);
                }
            }
        }

        void EmitPuff(FridgeDoor door)
        {
            Bounds bounds = door.GetComponent<MeshFilter>().sharedMesh.bounds;
            for (int i = 0; i < 72; i++)
            {
                float distance = (i % 18 + Random.Range(0.15f, 0.85f)) / 18f;
                Vector3 position = bounds.center;
                Vector3 direction = Vector3.zero;

                if (i < 36)
                {
                    position.x = Mathf.Lerp(bounds.min.x, bounds.max.x, distance);
                    direction.y = 1f;
                    if (i < 18)
                    {
                        direction.y = -1f;
                    }
                    position.y += direction.y * (bounds.extents.y - 0.006f);
                }
                else
                {
                    position.y = Mathf.Lerp(bounds.min.y, bounds.max.y, distance);
                    direction.x = 1f;
                    if (i < 54)
                    {
                        direction.x = -1f;
                    }
                    position.x += direction.x * (bounds.extents.x - 0.006f);
                }

                position.z = bounds.min.z + 0.015f;
                direction.z = 0.15f;
                Vector3 start = door.PointAtAngle(position, 0f);
                Vector3 end = door.PointAtAngle(position + direction, 0f);
                ParticleSystem.EmitParams puff = new ParticleSystem.EmitParams();
                float width = Random.Range(0.065f, 0.1f);
                puff.startSize3D = new Vector3(width, 0.025f, 0.025f);
                if (i >= 36)
                {
                    puff.startSize3D = new Vector3(0.025f, width, 0.025f);
                }
                puff.position = door.slamPuff.transform.InverseTransformPoint(start);
                puff.velocity = door.slamPuff.transform.InverseTransformVector(end - start) * Random.Range(0.3f, 0.5f);
                door.slamPuff.Emit(puff, 1);
            }
        }

        void LateUpdate()
        {
            UpdateShake(Time.deltaTime);
        }

        public void UpdateShake(float deltaTime)
        {
            if (remainingTime <= 0f)
            {
                return;
            }

            remainingTime = Mathf.Max(0f, remainingTime - deltaTime);
            if (remainingTime == 0f)
            {
                transform.localRotation = restingRotation;
                return;
            }

            float elapsedTime = shakeSeconds - remainingTime;
            float fade = remainingTime / shakeSeconds;
            float angle = Mathf.Sin(elapsedTime * 80f) * shakeAngle * fade;
            transform.localRotation = restingRotation * Quaternion.Euler(angle, 0f, 0f);
        }

        void OnDisable()
        {
            foreach (FridgeDoor door in doors)
            {
                if (door != null)
                {
                    door.SoundEmitted -= OnDoorSound;
                }
            }

            remainingTime = 0f;
            transform.localRotation = restingRotation;
        }
    }
}
