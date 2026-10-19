using UnityEngine;

namespace Leftovers
{
    public class Microwave : MonoBehaviour
    {
        public FridgeDoor door;
        public Transform turntable;
        public Light insideLight;
<<<<<<< Updated upstream
        public Light interiorFill;
=======
>>>>>>> Stashed changes
        public Renderer lamp;
        public Material lampOn;
        public Material lampOff;
        public MicrowaveDisplay display;
        public ParticleSystem chamberSteam;
        public AudioSource cookingSound;
        public AudioSource beepSource;
        public AudioClip beep;
        public AudioClip startBeep;
        public float startDelay = 0.22f;
        public float cookingSeconds = 5f;
        public float cookedWindow = 2f;
        public float lightDelay = 0.12f;
        public float plateSpeed = 38f;
        public float steamSeconds = 1.5f;
        public float steamRate = 110f;
        public MicrowaveFood food;

        public float RemainingSeconds { get; private set; } = 5f;
        public bool IsCooking { get; private set; }
        public bool IsOpen { get; private set; }
        public bool IsFinished { get; private set; }

        float lightWait;
        float steamTime;
<<<<<<< Updated upstream
        float trappedSteam;
        float motorVolume;
        ParticleSystem.Particle[] steamPuffs = new ParticleSystem.Particle[400];
=======
>>>>>>> Stashed changes
        float steamParticles;
        bool lightIsOn;
        float closedWait;

<<<<<<< Updated upstream
        void Awake()
        {
            if (Camera.main != null) Camera.main.depthTextureMode |= DepthTextureMode.Depth;
            if (cookingSound != null)
            {
                motorVolume = cookingSound.volume;
                cookingSound.volume = 0f;
            }
        }

=======
>>>>>>> Stashed changes
        void LateUpdate()
        {
            Step(Time.deltaTime);
        }

        public void Step(float seconds)
        {
            if (door == null || seconds <= 0f)
            {
                return;
            }

            bool opened = door.Angle > 0f;
            if (opened != IsOpen)
            {
                IsOpen = opened;
                if (IsOpen)
                {
                    OpenDoor();
                }
                else
                {
                    SetLight(false);
<<<<<<< Updated upstream
                    if (food != null) food.HideSteam();
                    if (chamberSteam != null) chamberSteam.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
=======
>>>>>>> Stashed changes
                }
            }

            if (IsOpen)
            {
                lightWait += seconds;
                if (lightWait >= lightDelay)
                {
                    SetLight(true);
                }
            }

            bool wasCooking = IsCooking;
            float previousWait = closedWait;
            if (IsOpen || food == null || food.IsHeld || food.IsCooked)
            {
                closedWait = 0f;
            }
            else
            {
                closedWait += seconds;
            }
            IsCooking = !IsOpen && food != null && !food.IsHeld && !food.IsCooked && !IsFinished && closedWait >= startDelay;
            if (IsCooking && !wasCooking && beepSource != null && startBeep != null)
            {
                beepSource.PlayOneShot(startBeep, 0.55f);
            }
            if (IsCooking)
            {
                float cookingTime = seconds;
                if (previousWait < startDelay)
                {
                    cookingTime = Mathf.Max(0f, closedWait - startDelay);
                }
                food.heatedSeconds += cookingTime;
                RemainingSeconds = Mathf.Max(0f, cookingSeconds - food.heatedSeconds);
                turntable.Rotate(Vector3.up, plateSpeed * cookingTime, Space.Self);
                if (RemainingSeconds == 0f)
                {
                    FinishCooking();
                }
            }
            else if (food == null)
            {
                RemainingSeconds = cookingSeconds;
            }

            if (cookingSound != null)
            {
                if (IsCooking && !cookingSound.isPlaying)
                {
                    cookingSound.Play();
                }
<<<<<<< Updated upstream
                float targetVolume = 0f;
                if (IsCooking) targetVolume = motorVolume;
                cookingSound.volume = Mathf.MoveTowards(cookingSound.volume, targetVolume, motorVolume * seconds / 0.25f);
                if (!IsCooking && cookingSound.volume <= 0.001f)
=======
                if (!IsCooking && cookingSound.isPlaying)
>>>>>>> Stashed changes
                {
                    cookingSound.Stop();
                }
            }

            if (display != null)
            {
                display.Show(RemainingSeconds);
            }
            UpdateSteam(seconds);
        }

        void OpenDoor()
        {
            lightWait = 0f;
            SetLight(false);
            if (food == null)
            {
                return;
            }

            if (food.heatedSeconds >= Mathf.Max(0f, cookingSeconds - cookedWindow))
            {
                food.Cook();
            }
            if (food.IsCooked)
            {
<<<<<<< Updated upstream
                food.ShowSteam();
                steamTime = steamSeconds;
                steamParticles = 0f;
                if (chamberSteam != null)
                {
                    chamberSteam.transform.position = food.SteamPosition();
                    chamberSteam.Play();
                    int count = chamberSteam.GetParticles(steamPuffs);
                    for (int i = 0; i < count; i++)
                    {
                        steamPuffs[i].velocity = transform.forward * 0.32f;
                    }
                    chamberSteam.SetParticles(steamPuffs, count);
                }
=======
                steamTime = steamSeconds;
                steamParticles = 0f;
>>>>>>> Stashed changes
            }
        }

        void FinishCooking()
        {
            IsCooking = false;
            IsFinished = true;
            food.Cook();
            SetLight(true);
            if (beepSource != null && beep != null)
            {
                beepSource.PlayOneShot(beep);
            }
        }

        void UpdateSteam(float seconds)
        {
<<<<<<< Updated upstream
            if (chamberSteam == null) return;
            if (!IsOpen)
            {
                if (food == null || food.heatedSeconds < 1f) return;
                trappedSteam = Mathf.Min(1f, trappedSteam + seconds * 0.2f);
                return;
            }
            else
            {
                if (trappedSteam <= 0f || door.Angle < 8f || food == null || !food.IsCooked) return;
                float released = Mathf.Min(trappedSteam, seconds * 0.5f);
                trappedSteam -= released;
                steamParticles += released * 46f;
            }
            if (!chamberSteam.isPlaying) chamberSteam.Play();
            int count = Mathf.FloorToInt(steamParticles);
            steamParticles -= count;
            for (int i = 0; i < count; i++)
            {
                ParticleSystem.EmitParams puff = new ParticleSystem.EmitParams();
                Vector3 position = new Vector3(Random.Range(-0.12f, 0.20f), Random.Range(0.22f, 0.30f), 0.02f);
                puff.velocity = Vector3.zero;
                if (IsOpen)
                {
                    position.z = 0.16f;
                    position.x = Mathf.Lerp(-0.12f, position.x, Mathf.Clamp01(door.Angle / 75f));
                    puff.velocity = transform.forward * 0.32f;
                }
                puff.position = transform.TransformPoint(position);
                chamberSteam.Emit(puff, 1);
            }
=======
            if (steamTime <= 0f || !IsOpen || chamberSteam == null)
            {
                return;
            }

            steamTime = Mathf.Max(0f, steamTime - seconds);
            steamParticles += seconds * steamRate;
            int count = Mathf.FloorToInt(steamParticles);
            steamParticles -= count;
            chamberSteam.Emit(count);
>>>>>>> Stashed changes
        }

        void SetLight(bool on)
        {
            if (insideLight != null)
            {
                insideLight.enabled = on;
<<<<<<< Updated upstream
                if (interiorFill != null) interiorFill.enabled = on;
=======
>>>>>>> Stashed changes
            }
            if (lamp != null && lightIsOn != on)
            {
                if (on)
                {
                    lamp.sharedMaterial = lampOn;
                }
                else
                {
                    lamp.sharedMaterial = lampOff;
                }
            }
            lightIsOn = on;
        }

        public bool PutFood(MicrowaveFood item)
        {
            if (!IsOpen || door.Angle < 45f || food != null || item == null)
            {
                return false;
            }
            Vector3 position = turntable.TransformPoint(new Vector3(0f, 0.002f, 0f));
            if (!item.CanReach(position))
            {
                return false;
            }

            food = item;
<<<<<<< Updated upstream
            item.StopCooling();
=======
>>>>>>> Stashed changes
            food.Place();
            food.transform.SetParent(turntable, false);
            food.transform.localPosition = new Vector3(0f, 0.002f, 0f);
            food.transform.localRotation = Quaternion.identity;
            food.transform.localScale = Vector3.one;
            IsFinished = food.IsCooked;
            RemainingSeconds = Mathf.Max(0f, cookingSeconds - food.heatedSeconds);
            return true;
        }

        public bool TakeFood(MicrowaveFood item)
        {
            if (!IsOpen || door.Angle < 45f || food != item)
            {
                return false;
            }

<<<<<<< Updated upstream
            item.StartCooling();
=======
>>>>>>> Stashed changes
            food = null;
            item.transform.SetParent(null, true);
            IsFinished = false;
            steamTime = 0f;
            return true;
        }

        void OnDisable()
        {
            IsCooking = false;
            if (cookingSound != null)
            {
                cookingSound.Stop();
            }
        }
    }
}
<<<<<<< Updated upstream



=======
>>>>>>> Stashed changes
