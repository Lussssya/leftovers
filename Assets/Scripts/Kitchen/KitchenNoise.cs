using UnityEngine;

namespace Leftovers
{
    public class KitchenNoise : MonoBehaviour
    {
        [SerializeField] Transform player;
        [SerializeField] AudioSource snoringSource;
        [SerializeField] AudioSource[] kitchenSounds;
        [SerializeField] float maximumNoise = 100f;
        [SerializeField] float recoveryPerSecond = 8f;
        [SerializeField] float movementNoisePerMeter = 4f;
        [SerializeField] float itemDropNoise = 18f;
        [SerializeField] float soundNoisePerSecond = 24f;
        [SerializeField] float soundSensitivity = 2f;
        [SerializeField, Range(0f, 1f)] float quietSnoringVolume = 0.55f;
        [SerializeField, Range(0f, 1f)] float loudSnoringVolume = 0.95f;
        [SerializeField, Range(0f, 1f)] float disturbanceStartsAt = 0.25f;
        [SerializeField, Range(0f, 1f)] float disturbedVolumeDip = 0.7f;
        [SerializeField, Range(0f, 0.3f)] float disturbedPitchChange = 0.12f;
        [SerializeField] float volumeFadeSpeed = 0.5f;

        FridgeDoor[] doors;
        Vector3 previousPlayerPosition;
        float noise;
        float breathingTime;
        bool snoringPaused;

        public bool LimitReached { get; private set; }

        public float NormalizedNoise
        {
            get
            {
                return Mathf.Clamp01(noise / Mathf.Max(1f, maximumNoise));
            }
        }

        void OnEnable()
        {
            doors = FindObjectsOfType<FridgeDoor>();
            foreach (FridgeDoor door in doors)
            {
                if (door.gameObject.scene == gameObject.scene)
                {
                    door.SoundEmitted += OnDoorSound;
                }
            }

            RememberPlayerPosition();
        }

        void Start()
        {
            if (snoringSource != null && snoringSource.clip != null)
            {
                snoringSource.volume = quietSnoringVolume;
                snoringSource.Play();
            }
        }

        void Update()
        {
            if (Time.timeScale == 0f)
            {
                PauseSnoring();
                RememberPlayerPosition();
                return;
            }

            ResumeSnoring();
            UpdateNoise(Time.deltaTime);
            UpdateSnoring(Time.deltaTime);
        }

        void UpdateNoise(float seconds)
        {
            if (LimitReached)
            {
                return;
            }

            noise = Mathf.Max(0f, noise - Mathf.Max(0f, recoveryPerSecond) * seconds);
            AddMovementNoise();
            AddNoise(MeasureKitchenSounds() * Mathf.Max(0f, soundNoisePerSecond) * seconds);
        }

        void AddMovementNoise()
        {
            if (player == null)
            {
                return;
            }

            Vector3 movement = player.position - previousPlayerPosition;
            movement.y = 0f;
            float distance = movement.magnitude;
            if (distance < 3f)
            {
                AddNoise(distance * Mathf.Max(0f, movementNoisePerMeter));
            }

            RememberPlayerPosition();
        }

        float MeasureKitchenSounds()
        {
            float loudness = 0f;
            if (kitchenSounds == null)
            {
                return loudness;
            }

            foreach (AudioSource source in kitchenSounds)
            {
                if (source == null || source == snoringSource || !source.isPlaying || source.mute)
                {
                    continue;
                }

                loudness = Mathf.Max(loudness, source.volume);
            }

            return Mathf.Clamp01(loudness * Mathf.Max(0f, soundSensitivity));
        }

        void OnDoorSound(FridgeDoor door, DoorSoundKind kind, float amount)
        {
            if (kind == DoorSoundKind.Slam)
            {
                AddNoise(Mathf.Max(1f, maximumNoise));
                return;
            }

            AddNoise(amount);
        }

        public void AddItemDropNoise()
        {
            AddNoise(itemDropNoise);
        }

        public void AddNoise(float amount)
        {
            if (!isActiveAndEnabled || Time.timeScale == 0f || amount <= 0f)
            {
                return;
            }

            noise = Mathf.Clamp(noise + amount, 0f, Mathf.Max(1f, maximumNoise));
            if (NormalizedNoise >= 1f)
            {
                LimitReached = true;
            }
        }

        void UpdateSnoring(float seconds)
        {
            if (snoringSource == null || snoringSource.clip == null)
            {
                return;
            }

            if (!snoringSource.isPlaying)
            {
                snoringSource.Play();
            }

            breathingTime = breathingTime + seconds;
            float disturbance = Mathf.InverseLerp(disturbanceStartsAt, 1f, NormalizedNoise);
            float breathing = Mathf.PerlinNoise(breathingTime * 1.7f, 0.6f);
            float dip = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.3f, 0.65f, breathing));
            float volume = Mathf.Lerp(quietSnoringVolume, loudSnoringVolume, NormalizedNoise);
            volume = Mathf.Lerp(volume, quietSnoringVolume,
                dip * disturbance * disturbedVolumeDip);
            snoringSource.volume = Mathf.MoveTowards(snoringSource.volume, volume,
                Mathf.Max(0f, volumeFadeSpeed) * seconds);

            float pitchChange = Mathf.PerlinNoise(breathingTime * 1.3f, 8f) * 2f - 1f;
            float pitch = 1f + pitchChange * disturbance * disturbedPitchChange;
            snoringSource.pitch = Mathf.MoveTowards(snoringSource.pitch, pitch, seconds * 0.25f);
        }

        void PauseSnoring()
        {
            if (snoringSource != null && snoringSource.isPlaying)
            {
                snoringSource.Pause();
                snoringPaused = true;
            }
        }

        void ResumeSnoring()
        {
            if (snoringSource != null && snoringPaused)
            {
                snoringSource.UnPause();
                snoringPaused = false;
            }
        }

        void RememberPlayerPosition()
        {
            if (player != null)
            {
                previousPlayerPosition = player.position;
            }
        }

        void OnDisable()
        {
            if (doors != null)
            {
                foreach (FridgeDoor door in doors)
                {
                    if (door != null)
                    {
                        door.SoundEmitted -= OnDoorSound;
                    }
                }
            }

            if (snoringSource != null)
            {
                snoringSource.Stop();
            }

            snoringPaused = false;
        }
    }
}
