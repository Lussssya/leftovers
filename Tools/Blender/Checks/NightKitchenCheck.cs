using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections;
using Leftovers;
public class NightKitchenCheck : MonoBehaviour
{
    bool failed;
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Environment.unity");
        new GameObject("Merge play check").AddComponent<NightKitchenCheck>();
        EditorApplication.isPlaying = true;
    }
    void Awake()
    {
        Application.logMessageReceived += Log;
    }
    void Log(string message, string trace, LogType type)
    {
        if (type == LogType.Exception || type == LogType.Error) failed = true;
    }
    IEnumerator Start()
    {
        yield return null;
        CameraFollow cameraControls = FindObjectOfType<CameraFollow>();
        cameraControls.SetPaused(false);
        Camera camera = Camera.main;
        Microwave microwave = FindObjectOfType<Microwave>();
        MicrowaveFood food = FindObjectOfType<MicrowaveFood>();


        if (camera == null || microwave == null || food == null || FindObjectOfType<Mover>() == null) { Fail("Merged gameplay missing"); yield break; }
        foreach (FridgeDoor door in FindObjectsOfType<FridgeDoor>()) { door.BeginHold(); door.DragBy(105f); }
        yield return new WaitForSeconds(.7f);
        foreach (FridgeDoor door in FindObjectsOfType<FridgeDoor>()) door.EndHold(true);
        food.transform.SetParent(null, true);
        food.Hold();
        food.transform.position = microwave.turntable.position + microwave.transform.forward * 1.6f;
        Physics.SyncTransforms();
        if (!microwave.PutFood(food)) { Fail("Microwave food insertion blocked"); yield break; }
        microwave.door.BeginHold(); microwave.door.DragBy(-105f);
        yield return new WaitForSeconds(.7f);
        microwave.door.EndHold(true);
        yield return new WaitForSeconds(microwave.cookingSeconds + .8f);
        if (!microwave.IsFinished || !food.IsCooked) { Fail("Cooking failed in the night kitchen"); yield break; }
        microwave.door.BeginHold(); microwave.door.DragBy(105f);
        yield return new WaitForSeconds(.7f);
        microwave.door.EndHold(true);
        if (!microwave.chamberSteam.isPlaying || !microwave.insideLight.enabled) { Fail("Steam or light failed"); yield break; }
        if (!microwave.TakeFood(food)) { Fail("Food removal failed"); yield break; }
        Debug.Log("PASS Microwave cooks, opens, lights and releases cooked food in the night kitchen");
        if (FindObjectsOfType<FridgeDoor>().Length < 3) { Fail("Appliance doors missing"); yield break; }
        if (FindObjectsOfType<CabinetPart>().Length < 3) { Fail("Cabinet controls lost"); yield break; }
        yield return null;
        Debug.Log("PASS Environment advances through ordinary frames with cabinets, player controls and appliance doors");
        EditorApplication.Exit(failed ? 1 : 0);
    }
    void Fail(string message)
    {
        Debug.LogError(message);
        EditorApplication.Exit(1);
    }
}


