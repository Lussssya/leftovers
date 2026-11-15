using System.IO;
using UnityEngine;

namespace Leftovers
{
    public class IntroCloudLogo : MonoBehaviour
    {
        public RenderTexture texture;
        public const float RenderPadding = 3f;
        public KitchenIntro settings;
        float[] movementLimits;
        Camera logoCamera;
        ParticleSystem clouds;
        ParticleSystem.Particle[] particles;
        Vector3[] positions;
        Vector3[] drift;
        float[] sizes;
        Material material;

        public void Build()
        {
            texture = new RenderTexture(3072, 1024, 16, RenderTextureFormat.ARGB32);
            texture.Create();
            GameObject cameraObject = new GameObject("Cloud logo camera");
            cameraObject.transform.SetParent(transform, false);
            cameraObject.transform.localPosition = new Vector3(0, 0, -15);
            logoCamera = cameraObject.AddComponent<Camera>();
            logoCamera.orthographic = true;
            logoCamera.orthographicSize = 2f * RenderPadding;
            logoCamera.clearFlags = CameraClearFlags.SolidColor;
            logoCamera.backgroundColor = Color.clear;
            logoCamera.cullingMask = 1 << 30;
            logoCamera.targetTexture = texture;
            logoCamera.enabled = false;
            GameObject cloudObject = new GameObject("Letters made of cloud");
            cloudObject.transform.SetParent(transform, false);
            cloudObject.layer = 30;
            cloudObject.transform.localRotation = Quaternion.Euler(settings.cloudViewAngle);
            clouds = cloudObject.AddComponent<ParticleSystem>();
            clouds.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleSystem.MainModule main = clouds.main;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.Local;
            main.maxParticles = 10000;
            ParticleSystem.EmissionModule emission = clouds.emission;
            emission.enabled = false;
            ParticleSystem.ShapeModule shape = clouds.shape;
            shape.enabled = false;
            material = new Material(Resources.Load<Shader>("IntroLogo/IntroCloud"));
            ParticleSystemRenderer renderer = clouds.GetComponent<ParticleSystemRenderer>();
            renderer.material = material;
            renderer.sortMode = ParticleSystemSortMode.Distance;
            TextAsset data = Resources.Load<TextAsset>("IntroLogo/CloudPoints");
            using (BinaryReader reader = new BinaryReader(new MemoryStream(data.bytes)))
            {
                int count = reader.ReadInt32();
                particles = new ParticleSystem.Particle[count];
                positions = new Vector3[count];
                drift = new Vector3[count];
                sizes = new float[count];
                for (int i = 0; i < count; i++)
                {
                    positions[i] = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle() * settings.cloudDepth);
                    sizes[i] = reader.ReadSingle();
                    float angle = i * 2.39996f;
                    drift[i] = new Vector3(Mathf.Cos(angle) * .9f, Mathf.Sin(angle) * .9f, Mathf.Sin(angle * 2f) * .4f);
                }
            }
            byte[] motion = Resources.Load<TextAsset>("IntroLogo/CloudMotion").bytes;
            movementLimits = new float[particles.Length];
            System.Buffer.BlockCopy(motion, 0, movementLimits, 0, motion.Length);
            Show(0f, 0f);
        }

        public void Show(float visibility, float dissolve)
        {
            float time = Time.unscaledTime * settings.cloudMovementSpeed;
            material.SetFloat("_CloudTime", Time.unscaledTime * settings.cloudTextureMotionSpeed);
            material.SetFloat("_MotionAmount", settings.cloudTextureMotionAmount);
            for (int i = 0; i < particles.Length; i++)
            {
                float stagger = (Mathf.Sin(i * 17.13f) * .5f + .5f) * settings.cloudPoofStagger;
                float progress = Mathf.Clamp01((dissolve - stagger) / (1f - stagger));
                Vector3 curl = new Vector3(Mathf.Sin(progress * 5f + i), Mathf.Cos(progress * 4f + i), 0f) * progress * settings.cloudPoofCurl;
                float radius = movementLimits[i] * settings.cloudMovementAmount;
                if (i % 7 == 0) radius += settings.cloudEdgeDrift;
                Vector3 motion = new Vector3(Mathf.Sin(time + i), Mathf.Cos(time * .8f + i), 0f) * radius * .7f;
                particles[i].position = positions[i] + motion + drift[i] * progress * settings.cloudPoofDistance + curl;
                particles[i].startSize = sizes[i] * settings.cloudSize * (1f + progress * settings.cloudPoofExpansion);
                float shade = Mathf.Lerp(.58f, 1f, Mathf.InverseLerp(.5f, -.5f, positions[i].z));
                particles[i].startColor = new Color(settings.cloudColor.r * shade, settings.cloudColor.g * shade, settings.cloudColor.b * shade, settings.cloudColor.a * visibility * (1f - progress) * (1f - progress));
                particles[i].startLifetime = 100f;
                particles[i].remainingLifetime = 100f;
                particles[i].rotation = i * 137.5f + Time.unscaledTime * settings.cloudTurnDegreesPerSecond;
            }
            clouds.SetParticles(particles, particles.Length);
            logoCamera.Render();
        }

        void OnDestroy()
        {
            if (texture != null) { texture.Release(); Destroy(texture); }
            if (material != null) Destroy(material);
        }
    }
}

