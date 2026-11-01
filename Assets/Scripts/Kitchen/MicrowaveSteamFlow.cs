using UnityEngine;

namespace Leftovers
{
    [DefaultExecutionOrder(100)]
    public class MicrowaveSteamFlow : MonoBehaviour
    {
        public Microwave microwave;
        ParticleSystem steam;
        ParticleSystemRenderer steamRenderer;
        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[400];
        MaterialPropertyBlock properties;

        void Awake()
        {
            steam = GetComponent<ParticleSystem>();
            steamRenderer = GetComponent<ParticleSystemRenderer>();
            properties = new MaterialPropertyBlock();
        }

        void LateUpdate()
        {
            if (microwave == null || steam == null) return;
            Transform body = microwave.transform;
            if (microwave.vibration != null) body = microwave.vibration.transform;
            steamRenderer.GetPropertyBlock(properties);
            properties.SetFloat("_MicrowaveSteam", 1f);
            properties.SetMatrix("_SteamWorldToLocal", body.worldToLocalMatrix);
            steamRenderer.SetPropertyBlock(properties);
            int count = steam.GetParticles(particles);
            for (int i = 0; i < count; i++)
            {
                Vector3 position = body.InverseTransformPoint(particles[i].position);
                if (position.z > .30f || position.z < -.22f || position.x < -.33f || position.x > .34f) continue;
                float roofFlow = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.22f, .33f, position.y));
                position.y = Mathf.Min(position.y, .34f);
                position.x = Mathf.Clamp(position.x, -.16f, .27f);
                Vector3 velocity = body.InverseTransformVector(particles[i].velocity);
                if (microwave.door.Angle > 8f)
                {
                    float forward = Mathf.Lerp(.035f, .22f, roofFlow);
                    velocity.z = Mathf.MoveTowards(velocity.z, forward, Time.deltaTime * .8f);
                }
                else
                {
                    position.z = Mathf.Min(position.z, .16f);
                    velocity.z = 0f;
                }
                particles[i].position = body.TransformPoint(position);
                particles[i].velocity = body.TransformVector(velocity);
            }
            steam.SetParticles(particles, count);
        }
    }
}
