using System;
using UnityEngine;
using UnityEngine.Events;

namespace Leftovers
{
    public enum DoorSoundKind
    {
        Opening,
        Movement,
        GentleClose,
        Slam,
        Stop
    }

    [Serializable]
    public class DoorNoiseEvent : UnityEvent<float>
    {
    }

    public class FridgeDoor : MonoBehaviour
    {
        public float maximumAngle = 110f;
        public float openingDirection = -1f;
        public float responseSeconds = 0.008f;
        public float maximumSpeed = 720f;
        public float releasedFriction = 12f;
        public float releaseMomentum = 0.65f;
        public float slamSpeed = 100f;
        public float fullImpactSpeed = 320f;
        public float movementNoisePerDegree = 0.012f;
        public float openingNoise = 0.7f;
        public float gentleCloseNoise = 1.2f;
        public float slamNoise = 24f;
        [Range(0f, 2f)]
        public float slamVolume = 1f;
        public ParticleSystem slamPuff;
        public AudioSource oneShotSource;
        public AudioSource movementSource;
        public AudioClip[] openingClips;
        public AudioClip[] closingClips;
        public AudioClip[] slamClips;
        public AudioClip movementLoop;
        public float movementVolume = 0.1f;
        public DoorNoiseEvent noiseEmitted = new DoorNoiseEvent();

        public event Action<FridgeDoor, DoorSoundKind, float> SoundEmitted;
        public float Angle { get; private set; }
        public float AngularSpeed { get; private set; }
        public float LastClosingSpeed { get; private set; }
        public bool IsHeld { get; private set; }
        public float MaximumAngle { get { return maximumAngle; } }
        public float SlamSpeed { get { return slamSpeed; } }

        Quaternion closedRotation;
        float targetAngle;
        float velocity;
        float closingSpeed;
        float movementLoudness;
        bool opened;
        bool closeArmed;
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
            closedRotation = transform.localRotation;

            if (movementSource != null)
            {
                movementSource.clip = movementLoop;
                movementSource.loop = true;
                movementSource.volume = 0f;
            }
        }

        public void BeginHold()
        {
            Initialize();
            IsHeld = true;
            targetAngle = Angle;
            velocity = 0f;
            closingSpeed = 0f;
        }

        public void DragBy(float degrees)
        {
            if (IsHeld)
            {
                targetAngle = Mathf.Clamp(targetAngle + degrees, 0f, maximumAngle);
            }
        }

        public void EndHold(bool cancelMomentum = false)
        {
            IsHeld = false;
            targetAngle = Angle;
            closingSpeed = 0f;
            velocity = velocity * releaseMomentum;

            if (cancelMomentum)
            {
                velocity = 0f;
            }
        }

        public Vector3 PointAtAngle(Vector3 localPoint, float angle)
        {
            Initialize();
            Quaternion rotation = closedRotation * Quaternion.AngleAxis(openingDirection * angle, Vector3.up);
            Vector3 point = transform.localPosition + rotation * Vector3.Scale(localPoint, transform.localScale);

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
                closingSpeed = 0f;
                targetAngle = Angle;
                return;
            }

            float previousAngle = Angle;
            float impactSpeed;

            if (IsHeld)
            {
                impactSpeed = MoveHeldDoor(deltaTime);
            }
            else
            {
                impactSpeed = MoveReleasedDoor(deltaTime);
            }

            Angle = Mathf.Clamp(Angle, 0f, maximumAngle);
            AngularSpeed = Mathf.Abs(Angle - previousAngle) / deltaTime;
            CheckOpening();
            CheckClosing(previousAngle, impactSpeed);
            CheckOpenStop(previousAngle);
            transform.localRotation = closedRotation * Quaternion.AngleAxis(openingDirection * Angle, Vector3.up);

            float distance = Mathf.Abs(Angle - previousAngle);
            if (distance > 0.0001f)
            {
                Emit(DoorSoundKind.Movement, distance * movementNoisePerDegree);
            }

            UpdateMovementAudio(deltaTime);
        }

        float MoveHeldDoor(float deltaTime)
        {
            float smoothing = 1f - Mathf.Exp(-deltaTime / responseSeconds);
            float nextAngle = Mathf.Lerp(Angle, targetAngle, smoothing);

            if (targetAngle == 0f || targetAngle == maximumAngle)
            {
                nextAngle = targetAngle;
            }

            float maxMovement = maximumSpeed * deltaTime;
            float movement = Mathf.Clamp(nextAngle - Angle, -maxMovement, maxMovement);
            velocity = movement / deltaTime;
            float smoothingSpeed = 1f - Mathf.Exp(-deltaTime / 0.06f);
            closingSpeed = Mathf.Lerp(closingSpeed, Mathf.Max(0f, -velocity), smoothingSpeed);
            Angle = Angle + movement;

            if (targetAngle == 0f && Angle < 0.04f)
            {
                Angle = 0f;
            }

            return closingSpeed;
        }

        float MoveReleasedDoor(float deltaTime)
        {
            float impactSpeed = Mathf.Max(0f, -velocity);
            float slowing = Mathf.Exp(-releasedFriction * deltaTime);
            Angle = Angle + velocity * (1f - slowing) / releasedFriction;
            velocity = velocity * slowing;

            if (Mathf.Abs(velocity) < 0.15f)
            {
                velocity = 0f;
            }

            return impactSpeed;
        }

        void CheckOpening()
        {
            if (Angle < 0.8f)
            {
                return;
            }

            closeArmed = true;
            if (!opened)
            {
                opened = true;
                float force = Mathf.Clamp01(AngularSpeed / 220f);
                Play(openingClips, Mathf.Lerp(0.18f, 0.65f, force));
                Emit(DoorSoundKind.Opening, openingNoise * (1f + force));
            }
        }

        void CheckClosing(float previousAngle, float impactSpeed)
        {
            if (Angle != 0f)
            {
                return;
            }

            if (closeArmed && previousAngle > 0f)
            {
                LastClosingSpeed = impactSpeed;
                float force = Mathf.Clamp01(impactSpeed / fullImpactSpeed);

                if (impactSpeed >= slamSpeed)
                {
                    Play(slamClips, slamVolume * Mathf.Lerp(0.65f, 1f, force));
                    Emit(DoorSoundKind.Slam, slamNoise * Mathf.Lerp(0.65f, 1.35f, force));
                }
                else
                {
                    Play(closingClips, Mathf.Lerp(0.14f, 0.45f, force));
                    Emit(DoorSoundKind.GentleClose, gentleCloseNoise * (1f + force));
                }

                closeArmed = false;
            }

            if (previousAngle > 0f)
            {
                opened = false;
            }

            velocity = 0f;
        }

        void CheckOpenStop(float previousAngle)
        {
            if (Angle < maximumAngle || previousAngle >= maximumAngle)
            {
                return;
            }

            if (AngularSpeed > 70f)
            {
                Play(closingClips, 0.12f);
                Emit(DoorSoundKind.Stop, Mathf.Clamp(AngularSpeed / 100f, 0.5f, 4f));
            }

            velocity = 0f;
        }

        void UpdateMovementAudio(float deltaTime)
        {
            if (movementSource == null || movementLoop == null)
            {
                return;
            }

            float volume = 0f;
            if (AngularSpeed > 0.2f)
            {
                volume = movementVolume * Mathf.Lerp(0.18f, 1f, Mathf.Clamp01(AngularSpeed / 150f));
            }

            float smoothing = 1f - Mathf.Exp(-deltaTime * 35f);
            movementLoudness = Mathf.Lerp(movementLoudness, volume, smoothing);
            movementSource.volume = movementLoudness;
            movementSource.pitch = Mathf.Lerp(0.86f, 1.12f, Mathf.Clamp01(AngularSpeed / 180f));

            if (volume > 0f && !movementSource.isPlaying)
            {
                movementSource.Play();
            }

            if (volume == 0f && movementLoudness < 0.001f)
            {
                movementSource.Stop();
            }
        }

        void Play(AudioClip[] clips, float volume)
        {
            if (oneShotSource == null || clips == null || clips.Length == 0)
            {
                return;
            }

            int index = UnityEngine.Random.Range(0, clips.Length);
            AudioClip clip = clips[index];
            if (clip != null)
            {
                oneShotSource.pitch = UnityEngine.Random.Range(0.98f, 1.02f);
                oneShotSource.PlayOneShot(clip, volume);
            }
        }

        void Emit(DoorSoundKind kind, float noise)
        {
            noiseEmitted.Invoke(noise);
            if (SoundEmitted != null)
            {
                SoundEmitted(this, kind, noise);
            }
        }

        void OnDisable()
        {
            EndHold(true);
            AngularSpeed = 0f;
            movementLoudness = 0f;

            if (movementSource != null)
            {
                movementSource.Stop();
            }
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
