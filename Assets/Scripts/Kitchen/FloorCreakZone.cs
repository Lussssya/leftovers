using UnityEngine;

namespace Leftovers
{
    public class FloorCreakZone : MonoBehaviour
    {
        [SerializeField] AudioSource audioSource;
        [SerializeField] AudioClip creakClip;
        [SerializeField] float cooldown = 1.75f;
        [SerializeField] float pitchVariation = 0.04f;

        float nextAllowedTime;

        void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag("Player"))
            {
                return;
            }

            if (Time.time < nextAllowedTime)
            {
                return;
            }

            PlayCreak();
        }

        void PlayCreak()
        {
            if (audioSource == null || creakClip == null)
            {
                return;
            }

            float variation = Mathf.Abs(pitchVariation);
            audioSource.pitch = Random.Range(1f - variation, 1f + variation);
            audioSource.PlayOneShot(creakClip);
            nextAllowedTime = Time.time + Mathf.Max(0f, cooldown);
        }
    }
}
