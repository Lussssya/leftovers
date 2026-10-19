using UnityEngine;

namespace Leftovers
{
    public class MicrowaveDisplay : MonoBehaviour
    {
        public GameObject[] tens;
        public GameObject[] ones;
        public Renderer screen;
        public FridgeDoor door;
        public float glitchSeconds = 0.5f;
        MaterialPropertyBlock screenProperties;
        string[] numbers = { "1111110", "0110000", "1101101", "1111001", "0110011", "1011011", "1011111", "1110000", "1111111", "1111011" };
        int lastNumber = -1;
        int actualNumber;
        float glitchTime;
        float nextFrame;

        void OnEnable()
        {
            if (door != null) door.SoundEmitted += OnDoorSound;
        }

        void OnDoorSound(FridgeDoor source, DoorSoundKind kind, float noise)
        {
            if (kind == DoorSoundKind.Slam)
            {
                glitchTime = glitchSeconds;
                nextFrame = 0f;
            }
        }

        void LateUpdate()
        {
            StepGlitch(Time.deltaTime);
        }

        public void StepGlitch(float seconds)
        {
            if (glitchTime <= 0f) return;
            glitchTime = Mathf.Max(0f, glitchTime - seconds);
            nextFrame -= seconds;
            if (glitchTime == 0f)
            {
                SetGlitch(0f);
                DrawNumber(actualNumber);
            }
            else if (nextFrame <= 0f)
            {
                nextFrame = 0.045f;
                DrawNumber(Random.Range(0,100));
                SetGlitch(Random.Range(0.4f,1f));
            }
        }

        void SetGlitch(float amount)
        {
            if (screen == null) return;
            if (screenProperties == null) screenProperties = new MaterialPropertyBlock();
            screen.GetPropertyBlock(screenProperties);
            screenProperties.SetFloat("_Glitch",amount);
            screenProperties.SetFloat("_GlitchFrame",Random.Range(0f,100f));
            screen.SetPropertyBlock(screenProperties);
        }

        public void SetDigitsVisible(bool visible)
        {
            if (screen == null) return;
            if (screenProperties == null) screenProperties = new MaterialPropertyBlock();
            screen.GetPropertyBlock(screenProperties);
            float brightness = 0f;
            if (visible) brightness = 1f;
            screenProperties.SetFloat("_DigitsVisible", brightness);
            screen.SetPropertyBlock(screenProperties);
        }

        public void Show(float seconds)
        {
            actualNumber = Mathf.Clamp(Mathf.CeilToInt(seconds),0,99);
            if (glitchTime <= 0f) DrawNumber(actualNumber);
        }

        void DrawNumber(int number)
        {
            if (number == lastNumber) return;
            lastNumber = number;
            if (screen != null)
            {
                if (screenProperties == null) screenProperties = new MaterialPropertyBlock();
                float column = number % 10;
                float row = number / 10;
                screen.GetPropertyBlock(screenProperties);
                screenProperties.SetVector("_MainTex_ST",new Vector4(.1f,.1f,column*.1f,.9f-row*.1f));
                screen.SetPropertyBlock(screenProperties);
                return;
            }
            SetDigit(tens,number/10);
            SetDigit(ones,number%10);
        }

        void SetDigit(GameObject[] segments,int number)
        {
            for (int i=0;i<segments.Length;i++) segments[i].SetActive(numbers[number][i]=='1');
        }

        void OnDisable()
        {
            if (door != null) door.SoundEmitted -= OnDoorSound;
            glitchTime = 0f;
            SetDigitsVisible(true);
            SetGlitch(0f);
            DrawNumber(actualNumber);
        }
    }
}
