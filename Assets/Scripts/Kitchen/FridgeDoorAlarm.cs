using UnityEngine;

namespace Leftovers
{
    public class FridgeDoorAlarm : MonoBehaviour
    {
        [SerializeField] FridgeDoor door;
        [SerializeField] float openWarningDelay = 10f;
        [SerializeField] float beepInterval = 1f;
        [SerializeField] AudioClip beepClip;
        [SerializeField] AudioSource audioSource;
        [SerializeField] float openAngleThreshold = 1f;

        float openTime;
        float timeUntilNextBeep;
        bool warningActive;

        void Update()
        {
            UpdateAlarm(Time.deltaTime);
        }

        public void UpdateAlarm(float deltaTime)
        {
            if (!IsDoorOpen())
            {
                ResetAlarm();
                return;
            }

            openTime = openTime + Mathf.Max(0f, deltaTime);
            if (openTime < Mathf.Max(0f, openWarningDelay))
            {
                return;
            }

            if (!warningActive)
            {
                warningActive = true;
                timeUntilNextBeep = 0f;
            }

            timeUntilNextBeep = timeUntilNextBeep - Mathf.Max(0f, deltaTime);
            if (timeUntilNextBeep > 0f)
            {
                return;
            }

            PlayBeep();
            timeUntilNextBeep = Mathf.Max(0.01f, beepInterval);
        }

        bool IsDoorOpen()
        {
            if (door == null)
            {
                return false;
            }

            return door.Angle >= Mathf.Max(0f, openAngleThreshold);
        }

        void PlayBeep()
        {
            if (audioSource == null || beepClip == null)
            {
                return;
            }

            if (!audioSource.isPlaying)
            {
                audioSource.PlayOneShot(beepClip);
            }
        }

        void ResetAlarm()
        {
            openTime = 0f;
            timeUntilNextBeep = 0f;
            warningActive = false;

            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }
        }

        void OnDisable()
        {
            ResetAlarm();
        }
    }
}
