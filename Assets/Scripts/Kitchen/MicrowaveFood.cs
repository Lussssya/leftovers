using UnityEngine;

namespace Leftovers
{
    public class MicrowaveFood : MonoBehaviour
    {
        public ParticleSystem steam;
        public Renderer bread;
        public Material cookedMaterial;
        public float heatedSeconds;
        public Vector3 steamOrigin;
        public bool IsCooked { get; private set; }
        public bool IsHeld { get; set; }

        float coolingTime = -1f;
        float steamRate = 7f;

        Rigidbody body;
        BoxCollider[] solidParts;
        RaycastHit[] hits = new RaycastHit[64];

        void Awake()
        {
            body = GetComponent<Rigidbody>();
            solidParts = GetComponentsInChildren<BoxCollider>();
        }

        public Vector3 SteamPosition()
        {
            return transform.TransformPoint(steamOrigin);
        }

        void LateUpdate()
        {
            if (steam != null && bread != null)
            {
                steam.transform.position = SteamPosition();
                if (coolingTime >= 0f)
                {
                    coolingTime = Mathf.Max(0f, coolingTime - Time.deltaTime);
                    ParticleSystem.EmissionModule emission = steam.emission;
                    emission.rateOverTime = steamRate * Mathf.Clamp01(coolingTime / 2f);
                    if (coolingTime == 0f) steam.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                }
            }
        }

        public void StartCooling()
        {
            if (IsCooked) coolingTime = 5f;
        }

        public void StopCooling()
        {
            coolingTime = -1f;
            if (steam != null && IsCooked)
            {
                ParticleSystem.EmissionModule emission = steam.emission;
                emission.rateOverTime = steamRate;
                steam.Play();
            }
        }

        public void ShowSteam()
        {
            if (steam == null || !IsCooked) return;
            steam.transform.position = SteamPosition();
            ParticleSystem.EmissionModule emission = steam.emission;
            emission.rateOverTime = steamRate;
            if (!steam.isPlaying) steam.Play();
        }

        public void HideSteam()
        {
            if (steam != null) steam.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        public void Hold()
        {
            IsHeld = true;
            if (body != null)
            {
                body.isKinematic = true;
                body.useGravity = false;
            }
        }

        public void Place()
        {
            IsHeld = false;
            if (body != null)
            {
                body.isKinematic = true;
                body.useGravity = false;
            }
        }

        public void Drop()
        {
            IsHeld = false;
            transform.SetParent(null, true);
            if (body != null)
            {
                body.isKinematic = false;
                body.useGravity = true;
            }
        }

        public bool CanReach(Vector3 position)
        {
            Vector3 movement = position - transform.position;
            Vector3 normal;
            return SafeDistance(movement, out normal) >= movement.magnitude - 0.001f;
        }

        public void MoveTo(Vector3 position)
        {
            Vector3 movement = position - transform.position;
            Vector3 normal;
            float distance = SafeDistance(movement, out normal);
            transform.position += movement.normalized * distance;
            if (distance < movement.magnitude && normal.sqrMagnitude > 0f)
            {
                Vector3 slide = Vector3.ProjectOnPlane(position - transform.position, normal);
                float remaining = SafeDistance(slide, out normal);
                transform.position += slide.normalized * remaining;
            }
        }

        float SafeDistance(Vector3 movement, out Vector3 normal)
        {
            normal = Vector3.zero;
            float distance = movement.magnitude;
            if (distance < 0.0001f)
            {
                return distance;
            }
            if (solidParts == null)
            {
                solidParts = GetComponentsInChildren<BoxCollider>();
            }
            Physics.SyncTransforms();
            foreach (BoxCollider part in solidParts)
            {
                if (part.isTrigger || !part.enabled)
                {
                    continue;
                }
                Vector3 scale = part.transform.lossyScale;
                scale = new Vector3(Mathf.Abs(scale.x), Mathf.Abs(scale.y), Mathf.Abs(scale.z));
                Vector3 half = Vector3.Scale(part.size * 0.5f, scale) - Vector3.one * 0.001f;
                Vector3 center = part.transform.TransformPoint(part.center);
                int count = Physics.BoxCastNonAlloc(center, half, movement.normalized, hits, part.transform.rotation, movement.magnitude, ~0, QueryTriggerInteraction.Ignore);
                for (int i = 0; i < count; i++)
                {
                    if (hits[i].collider.transform.IsChildOf(transform))
                    {
                        continue;
                    }
                    float safe = Mathf.Max(0f, hits[i].distance - 0.002f);
                    if (safe < distance)
                    {
                        distance = safe;
                        normal = hits[i].normal;
                    }
                }
            }
            return distance;
        }

        public void Cook()
        {
            if (IsCooked)
            {
                return;
            }

            IsCooked = true;
            if (bread != null && cookedMaterial != null)
            {
                bread.sharedMaterial = cookedMaterial;
            }
        }
    }
}
