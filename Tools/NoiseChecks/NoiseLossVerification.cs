#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Reflection;
using Leftovers;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NoiseLossVerification : MonoBehaviour
{
    KitchenNoise noise;
    KitchenGameResult result;
    NoiseBarUI bar;
    BindingFlags privateFields = BindingFlags.Instance | BindingFlags.NonPublic;
    string output = "/tmp/leftovers-noise-loss/";

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    static void Begin()
    {
        GameObject checks = new GameObject("Temporary noise loss checks");
        DontDestroyOnLoad(checks);
        checks.AddComponent<NoiseLossVerification>();
    }

    IEnumerator Start()
    {
        Directory.CreateDirectory(output);
        File.WriteAllText(output + "checks.txt", "");
        yield return null;
        if (SceneManager.GetActiveScene().name != "Environment")
        {
            yield return SceneManager.LoadSceneAsync("Environment");
            yield return null;
        }
        yield return null;

        try
        {
            RunChecks();
        }
        catch (Exception error)
        {
            File.AppendAllText(output + "checks.txt", "FAIL " + error + "\n");
            EditorApplication.isPlaying = false;
            yield break;
        }

        float wait = 0f;
        while (!result.Finished && wait < 2f)
        {
            wait = wait + Time.unscaledDeltaTime;
            yield return null;
        }

        if (result.Finished && !result.Won && bar.IsFull && Time.timeScale == 0f)
        {
            File.AppendAllText(output + "checks.txt", "PASS the full bar shows the loss screen and pauses the game.\n");
            ScreenCapture.CaptureScreenshot(output + "loss.png");
        }
        else
        {
            File.AppendAllText(output + "checks.txt", "FAIL the noise limit did not trigger the expected loss.\n");
        }

        yield return new WaitForSecondsRealtime(0.2f);
        EditorApplication.isPlaying = false;
    }

    void RunChecks()
    {
        FirstPersonController player = FindObjectOfType<FirstPersonController>();
        player.SetPaused(false);
        player.cameraCanMove = false;
        player.playerCanMove = false;
        noise = FindObjectOfType<KitchenNoise>();
        result = FindObjectOfType<KitchenGameResult>();
        bar = noise.GetComponent<NoiseBarUI>();
        Require(result != null, "The existing game result component is present.");

        FridgeDoor door = FindObjectOfType<FridgeDoor>();
        Call(door, "Emit", DoorSoundKind.GentleClose, 1.2f);
        Call(result, "LateUpdate");
        Require(!result.Finished && !noise.LimitReached && noise.NormalizedNoise > 0f,
            "A gentle close raises noise without causing a loss.");

        Microwave microwave = FindObjectOfType<Microwave>();
        FieldInfo finished = microwave.GetType().GetField("<IsFinished>k__BackingField", privateFields);
        FieldInfo remaining = microwave.GetType().GetField("<RemainingSeconds>k__BackingField", privateFields);
        finished.SetValue(microwave, true);
        remaining.SetValue(microwave, 0f);
        Call(result, "LateUpdate");
        Require(!result.Finished, "The microwave timer reaching zero does not cause an instant loss.");
        finished.SetValue(microwave, false);
        remaining.SetValue(microwave, microwave.cookingSeconds);

        float beforePause = noise.NormalizedNoise;
        Time.timeScale = 0f;
        Call(door, "Emit", DoorSoundKind.Slam, 30f);
        Require(noise.NormalizedNoise == beforePause && !noise.LimitReached,
            "Paused noise does not trigger a loss.");
        Time.timeScale = 1f;

        Call(noise, "UpdateNoise", 0.1f);
        Require(noise.NormalizedNoise < beforePause && !result.Finished,
            "Noise below the limit still recovers.");

        Call(door, "Emit", DoorSoundKind.Slam, 30f);
        Require(noise.LimitReached && noise.NormalizedNoise == 1f,
            "One hard door slam fills the bar and reaches the loss limit.");
        Call(noise, "UpdateNoise", 1f);
        Require(noise.NormalizedNoise == 1f,
            "The limit stays reached even if the door event arrives between frames.");
        Call(result, "LateUpdate");
        Require(!result.Finished && !bar.IsFull,
            "The loss waits until the visible bar finishes filling.");
    }

    void Call(object target, string method, params object[] arguments)
    {
        target.GetType().GetMethod(method, privateFields).Invoke(target, arguments);
    }

    void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception(message);
        }
        File.AppendAllText(output + "checks.txt", "PASS " + message + "\n");
    }
}
#endif
