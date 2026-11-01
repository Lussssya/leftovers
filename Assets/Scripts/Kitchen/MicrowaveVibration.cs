using UnityEngine;

namespace Leftovers
{
    public class MicrowaveVibration : MonoBehaviour
    {
        public FridgeDoor door;
        public float slamAmount = 3f;
        public float timerAmount = 2.5f;
        public float slamSeconds = 0.35f;
        public float timerFadeSeconds = 0.06f;
        public float frequency = 18f;
        Quaternion restingRotation;
        float remaining;
        float duration;
        float amount;
        bool timerBeeping;
        float timerStrength;

        void OnEnable()
        {
            restingRotation = transform.localRotation;
            if (door != null) door.SoundEmitted += DoorSound;
        }

        void DoorSound(FridgeDoor source, DoorSoundKind kind, float noise)
        {
            if (kind == DoorSoundKind.Slam) StartVibration(slamAmount, slamSeconds);
        }

        public void SetTimerBeep(bool beeping)
        {
            timerBeeping = beeping;
        }

        void StartVibration(float strength, float seconds)
        {
            amount = strength;
            duration = Mathf.Max(0.01f, seconds);
            remaining = duration;
        }

        void LateUpdate()
        {
            float slamAngle = 0f;
            if (remaining > 0f)
            {
                remaining = Mathf.Max(0f, remaining - Time.deltaTime);
                float elapsed = duration - remaining;
                slamAngle = Mathf.Sin(elapsed * frequency * Mathf.PI * 2f) * amount * remaining / duration;
            }
            float target = 0f;
            if (timerBeeping) target = 1f;
            timerStrength = Mathf.MoveTowards(timerStrength, target, Time.deltaTime / Mathf.Max(0.01f, timerFadeSeconds));
            float timerAngle = Mathf.Sin(Time.time * 12f * Mathf.PI * 2f) * timerAmount * timerStrength;
            transform.localRotation = restingRotation * Quaternion.Euler(0f, slamAngle, timerAngle);
        }

        void OnDisable()
        {
            if (door != null) door.SoundEmitted -= DoorSound;
            remaining = 0f;
            timerBeeping = false;
            timerStrength = 0f;
            transform.localRotation = restingRotation;
        }
    }
}
