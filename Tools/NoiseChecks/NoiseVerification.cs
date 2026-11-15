#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Leftovers;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NoiseVerification : MonoBehaviour
{
    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Begin()
    {
        Directory.CreateDirectory("Captures/Noise");
        File.WriteAllText("Captures/Noise/checks.txt", "");
        GameObject check = new GameObject("Temporary noise checks");
        DontDestroyOnLoad(check);
        check.AddComponent<NoiseVerification>();
    }

    IEnumerator Start()
    {
        yield return null;
        string[] scenes = { "Environment", "Kitchen" };
        foreach (string scene in scenes)
        {
            if (SceneManager.GetActiveScene().name != scene)
            {
                yield return SceneManager.LoadSceneAsync("Assets/Scenes/" + scene + ".unity");
                yield return null;
            }

            try
            {
                CheckApplianceNoise(scene);
            }
            catch (Exception error)
            {
                File.AppendAllText("Captures/Noise/checks.txt", "FAIL " + scene + ": " + error + "\n");
                EditorApplication.isPlaying = false;
                yield break;
            }

            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot("Captures/Noise/" + scene.ToLower() + "-quiet.png");
            yield return null;
            SetLoudNoise();
            yield return new WaitForEndOfFrame();
            ScreenCapture.CaptureScreenshot("Captures/Noise/" + scene.ToLower() + "-loud.png");
            yield return null;
        }

        File.AppendAllText("Captures/Noise/checks.txt", "PASS both scenes completed.\n");
        EditorApplication.isPlaying = false;
    }

    void CheckApplianceNoise(string scene)
    {
        FirstPersonController firstPerson = FindObjectOfType<FirstPersonController>();
        if (firstPerson != null)
        {
            firstPerson.SetPaused(false);
            firstPerson.cameraCanMove = false;
            firstPerson.playerCanMove = false;
        }

        CameraFollow cameraFollow = FindObjectOfType<CameraFollow>();
        if (cameraFollow != null)
        {
            cameraFollow.SetPaused(false);
            cameraFollow.enabled = false;
        }

        KitchenNoise meter = FindObjectOfType<KitchenNoise>();
        BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        Type type = meter.GetType();
        if (type.GetField("player", flags).GetValue(meter) == null)
        {
            throw new Exception("Player reference is missing.");
        }

        AudioSource[] sounds = (AudioSource[])type.GetField("kitchenSounds", flags).GetValue(meter);
        AudioSource testSource = null;
        foreach (AudioSource source in sounds)
        {
            if (source != null && source.isActiveAndEnabled && source.volume > 0f)
            {
                testSource = source;
                break;
            }
        }
        if (testSource == null)
        {
            throw new Exception("No kitchen sound sources are connected.");
        }
        AudioClip clip = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Microwave/Microwave_Beep.wav");
        testSource.PlayOneShot(clip);
        type.GetField("noise", flags).SetValue(meter, 0f);
        type.GetMethod("UpdateNoise", flags).Invoke(meter, new object[] { 1f });
        if (meter.NormalizedNoise <= 0f)
        {
            throw new Exception("Appliance playback did not raise noise.");
        }

        testSource.Stop();
        type.GetField("noise", flags).SetValue(meter, 0f);
        NoiseBarUI bar = FindObjectOfType<NoiseBarUI>();
        bar.GetType().GetField("displayedNoise", flags).SetValue(bar, 0f);
        File.AppendAllText("Captures/Noise/checks.txt", "PASS " + scene + ": player connected, appliance noise raises the bar.\n");
    }

    void SetLoudNoise()
    {
        KitchenNoise meter = FindObjectOfType<KitchenNoise>();
        meter.AddNoise(100f);
        KitchenHud hud = FindObjectOfType<KitchenHud>();
        hud.SendMessage("ShowFeedback", "Wrong product! Find the Cheese.");
        BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        NoiseBarUI bar = FindObjectOfType<NoiseBarUI>();
        bar.GetType().GetField("displayedNoise", flags).SetValue(bar, 1f);
    }
}
#endif
