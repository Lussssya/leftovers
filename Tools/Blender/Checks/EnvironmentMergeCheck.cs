using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections;
using Leftovers;
public class EnvironmentMergeCheck : MonoBehaviour
{
    bool failed;
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Environment.unity");
        new GameObject("Merge play check").AddComponent<EnvironmentMergeCheck>();
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
        ProductInteractable[] products = FindObjectsOfType<ProductInteractable>();
        AssemblyTable table = FindObjectOfType<AssemblyTable>();
        if (camera == null || microwave == null || food == null || products.Length < 1 || table == null || FindObjectOfType<Mover>() == null) { Fail("Merged gameplay missing"); yield break; }
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
        if (!microwave.IsFinished || !food.IsCooked) { Fail("Cooking failed after merge"); yield break; }
        microwave.door.BeginHold(); microwave.door.DragBy(105f);
        yield return new WaitForSeconds(.7f);
        microwave.door.EndHold(true);
        if (!microwave.chamberSteam.isPlaying || !microwave.insideLight.enabled) { Fail("Steam or light failed"); yield break; }
        if (!microwave.TakeFood(food)) { Fail("Food removal failed"); yield break; }
        Debug.Log("PASS Microwave cooks, opens, lights and releases cooked food after merge");
        ProductInteractable product = products[0];
        Transform hold = new GameObject("Temporary hold").transform;
        product.PickUp(hold);
        if (!product.IsHeld || !table.TryPlace(product) || product.IsHeld) { Fail("Team product pickup or assembly failed"); yield break; }
        product.PickUp(hold);
        product.Drop(hold.position, Quaternion.identity);
        if (product.IsHeld) { Fail("Team product drop failed"); yield break; }
        Debug.Log("PASS Team product pickup, table placement, pickup again and drop");
        if (FindObjectsOfType<FridgeDoorAlarm>().Length != 2) { Fail("Fridge alarm components lost"); yield break; }
        if (FindObjectsOfType<CabinetPart>().Length < 3) { Fail("Cabinet controls lost"); yield break; }
        camera.transform.position = microwave.transform.position + new Vector3(1.8f, .7f, -4f);
        camera.transform.LookAt(microwave.transform.position + new Vector3(1f,.4f,0f));
        RenderTexture target = new RenderTexture(1200,800,24);
        camera.targetTexture = target;
        camera.Render();
        RenderTexture.active = target;
        Texture2D image = new Texture2D(1200,800,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,1200,800),0,0);
        image.Apply();
        System.IO.File.WriteAllBytes("C:/Users/Mompas/AppData/Local/Temp/leftovers-environment-preview.png", image.EncodeToPNG());
        camera.targetTexture = null;
        RenderTexture.active = null;
        yield return null;
        Debug.Log("PASS Environment advances through ordinary frames with cabinets, player controls and both fridge alarms");
        EditorApplication.Exit(failed ? 1 : 0);
    }
    void Fail(string message)
    {
        Debug.LogError(message);
        EditorApplication.Exit(1);
    }
}
