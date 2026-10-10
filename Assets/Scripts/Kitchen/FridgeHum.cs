using UnityEngine;

namespace Leftovers
{
    public class FridgeHum : MonoBehaviour
    {
        public FridgeDoor upperDoor;
        public FridgeDoor lowerDoor;
        public AudioSource humSource;
        public float closedVolume = 0.035f;
        public float openVolume = 0.14f;
        public float fullVolumeAngle = 35f;
        public float fadeSpeed = 0.3f;

        void OnEnable()
        {
            if (humSource != null)
            {
                humSource.loop = true;
                humSource.volume = closedVolume;
                humSource.Play();
            }
        }

        void LateUpdate()
        {
            UpdateHum(Time.deltaTime);
        }

        public void UpdateHum(float deltaTime)
        {
            if (humSource == null)
            {
                return;
            }

            float angle = 0f;
            if (upperDoor != null)
            {
                angle = upperDoor.Angle;
            }

            if (lowerDoor != null)
            {
                angle = Mathf.Max(angle, lowerDoor.Angle);
            }

            float opening = Mathf.Clamp01(angle / Mathf.Max(1f, fullVolumeAngle));
            float volume = Mathf.Lerp(closedVolume, openVolume, opening);
            humSource.volume = Mathf.MoveTowards(humSource.volume, volume, fadeSpeed * deltaTime);
        }

        void OnDisable()
        {
            if (humSource != null)
            {
                humSource.Stop();
            }
        }
    }
}
