// To rebuild in a disposable Unity project, copy this file to Assets/Editor.
// Run Unity -executeMethod FridgePrototypeTools.BuildAndCheck. Never auto-runs.
using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Leftovers;

public static class FridgePrototypeTools
{
    const string Model = "Assets/Models/Fridge/Fridge_LowPoly_Rigged.fbx";
    const string Prefab = "Assets/Prefabs/Fridge_Interactive.prefab";
    const string Scene = "Assets/Scenes/FridgeInteraction.unity";

    static void Set(UnityEngine.Object obj, string key, UnityEngine.Object value)
    {
        var so = new SerializedObject(obj);
        so.FindProperty(key).objectReferenceValue = value;
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    static void Clips(FridgeDoor door, string field, string prefix)
    {
        var so = new SerializedObject(door); var p = so.FindProperty(field); p.arraySize = 2;
        for (int i = 0; i < 2; i++) p.GetArrayElementAtIndex(i).objectReferenceValue =
            AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Fridge/Fridge_" + prefix + "_0" + (i + 1) + ".wav");
        so.ApplyModifiedPropertiesWithoutUndo();
    }
    static AudioSource Source(GameObject obj)
    {
        var s = obj.AddComponent<AudioSource>(); s.playOnAwake = false; s.spatialBlend = 0.85f;
        s.minDistance = 2f; s.maxDistance = 12f; s.dopplerLevel = 0f; s.rolloffMode = AudioRolloffMode.Logarithmic;
        return s;
    }

    public static void BuildAndCheck()
    {
        Directory.CreateDirectory("Assets/Prefabs");
        AssetDatabase.Refresh();
        foreach (string path in Directory.GetFiles("Assets/Audio/Fridge", "*.wav"))
        {
            var importer = (AudioImporter)AssetImporter.GetAtPath(path.Replace('\\', '/'));
            var sample = importer.defaultSampleSettings;
            sample.loadType = AudioClipLoadType.DecompressOnLoad;
            sample.compressionFormat = AudioCompressionFormat.PCM;
            sample.preloadAudioData = true;
            importer.defaultSampleSettings = sample; importer.forceToMono = true;
            importer.SaveAndReimport();
        }
        var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        var fridge = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Model));
        PrefabUtility.UnpackPrefabInstance(fridge, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
        fridge.name = "Fridge_Interactive";
        var body = fridge.GetComponentsInChildren<MeshFilter>().Single(x => x.name == "Body");
        body.gameObject.AddComponent<MeshCollider>().sharedMesh = body.sharedMesh;
        foreach (string name in new[] { "UpperDoor", "LowerDoor" })
        {
            var part = fridge.GetComponentsInChildren<MeshFilter>().Single(x => x.name == name);
            var door = part.gameObject.AddComponent<FridgeDoor>();
            var rb = part.gameObject.AddComponent<Rigidbody>(); rb.isKinematic = true; rb.useGravity = false;
            part.gameObject.AddComponent<MeshCollider>().sharedMesh = part.sharedMesh;
            var handle = new GameObject("Handle"); handle.transform.SetParent(part.transform, false);
            handle.transform.localPosition = new Vector3(0.807f, name == "UpperDoor" ? 0.190f : -0.226f, 0.295f);
            var box = handle.AddComponent<BoxCollider>(); box.isTrigger = true; box.size = new Vector3(0.34f, 0.145f, 0.18f);
            // The inner lip can also be gripped when an open door hides its outside handle.
            var innerGrip = handle.AddComponent<BoxCollider>(); innerGrip.isTrigger = true;
            innerGrip.center = new Vector3(0, 0, -0.32f); innerGrip.size = new Vector3(0.34f, 0.145f, 0.16f);
            Set(handle.AddComponent<FridgeDoorHandle>(), "door", door);
            Set(door, "oneShotSource", Source(handle));
            var movement = new GameObject("MovementAudio"); movement.transform.SetParent(part.transform, false);
            Set(door, "movementSource", Source(movement));
            Set(door, "movementLoop", AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Fridge/Fridge_Movement.wav"));
            Clips(door, "openingClips", "Open"); Clips(door, "closingClips", "Close"); Clips(door, "slamClips", "Slam");
        }
        PrefabUtility.SaveAsPrefabAssetAndConnect(fridge, Prefab, InteractionMode.AutomatedAction);
        var cameraObject = new GameObject("Main Camera"); cameraObject.tag = "MainCamera";
        var camera = cameraObject.AddComponent<Camera>(); cameraObject.AddComponent<AudioListener>();
        camera.transform.position = new Vector3(2.25f, 1.72f, 3.4f);
        camera.transform.LookAt(new Vector3(0, 0.98f, 0.1f));
        camera.fieldOfView = 39f; camera.nearClipPlane = 0.05f;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.045f, 0.063f, 0.086f);
        var interactor = cameraObject.AddComponent<MouseDoorInteractor>(); Set(interactor, "interactionCamera", camera);
        var floor = GameObject.CreatePrimitive(PrimitiveType.Cube); floor.name = "Floor";
        floor.transform.position = new Vector3(0, -.07f, 0); floor.transform.localScale = new Vector3(8, .14f, 8);
        floor.GetComponent<Renderer>().sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/KitchenFloor.mat");
        var light = new GameObject("Soft kitchen light").AddComponent<Light>(); light.type = LightType.Directional;
        light.intensity = 1.25f; light.transform.rotation = Quaternion.Euler(38, -135, 0); light.shadows = LightShadows.Soft;
        RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(.42f, .47f, .55f);
        EditorSceneManager.SaveScene(scene, Scene);
        AssetDatabase.SaveAssets();
        Check();
        // Standalone build allows real mouse testing without touching the user's open Editor.
        var result = BuildPipeline.BuildPlayer(new BuildPlayerOptions {
            scenes = new[] { Scene }, locationPathName = "BuildVerified/FridgeInteraction.exe",
            target = BuildTarget.StandaloneWindows64, options = BuildOptions.Development
        });
        if (result.summary.result != UnityEditor.Build.Reporting.BuildResult.Succeeded)
            throw new Exception("Prototype build failed: " + result.summary.result);
        File.WriteAllText("fridge-check-success.txt", "Compile, deterministic interaction tests, and Windows development build passed.");
    }

    public static void Check()
    {
        EditorSceneManager.OpenScene(Scene);
        foreach (int fps in new[] { 30, 60, 144 })
        {
            Scenario(fps, 20f, false, false);
            Scenario(fps, 240f, true, false);
            Scenario(fps, 240f, false, true);
        }
        var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
        var d = root.GetComponentsInChildren<FridgeDoor>()[0];
        int closes = 0; d.SoundEmitted += (door, kind, noise) => { if (kind == DoorSoundKind.Slam || kind == DoorSoundKind.GentleClose) closes++; };
        d.BeginHold(); d.DragBy(500); for (int i = 0; i < 60; i++) d.Step(1f / 60);
        if (d.Angle > d.MaximumAngle) throw new Exception("Open limit failed");
        d.EndHold(true); var angle = d.Angle; d.BeginHold(); d.Step(1f / 60);
        if (Mathf.Abs(angle - d.Angle) > .001f) throw new Exception("Regrab snapped");
        d.DragBy(-500); for (int i = 0; i < 180; i++) d.Step(1f / 60);
        if (closes != 1 || d.Angle != 0) throw new Exception("Contact repeated or closed limit failed");
        for (int i = 0; i < 60; i++) { d.DragBy(-1); d.Step(1f / 60); }
        if (closes != 1) throw new Exception("Noise spam at stop");
        d.DragBy(60); for (int i = 0; i < 30; i++) d.Step(1f / 60);
        d.EndHold(true); angle = d.Angle; d.Step(.5f); d.Step(1f / 60);
        if (Mathf.Abs(angle - d.Angle) > .001f) throw new Exception("Focus cancellation/hitch drift");
        UnityEngine.Object.DestroyImmediate(root);
        CheckGrips();
        EditorSceneManager.OpenScene(Scene); // Restore the saved, closed pose after checks.
        Debug.Log("FRIDGE_INTERACTION_CHECKS_PASSED");
    }

    static void CheckGrips()
    {
        var camera = Camera.main;
        foreach (var door in UnityEngine.Object.FindObjectsOfType<FridgeDoor>())
        {
            var handle = door.GetComponentInChildren<FridgeDoorHandle>();
            foreach (float angle in new[] { 0f, 55f, 110f })
            {
                door.BeginHold(); door.DragBy(angle - door.Angle);
                for (int i = 0; i < 90; i++) door.Step(1f / 60);
                door.EndHold(true); Physics.SyncTransforms();
                bool inside = Vector3.Dot(camera.transform.position - handle.transform.position, -handle.transform.forward) > 0;
                Vector3 point = handle.transform.TransformPoint(inside ? new Vector3(0, 0, -.32f) : Vector3.zero);
                Vector3 screen = camera.WorldToScreenPoint(point);
                if (screen.z <= 0 || !Physics.Raycast(camera.ScreenPointToRay(screen), out RaycastHit hit, 4.5f, ~0, QueryTriggerInteraction.Collide) ||
                    hit.collider.GetComponent<FridgeDoorHandle>() != handle)
                    throw new Exception("Unreachable grip: " + door.name + " angle=" + angle);
                Debug.Log("FRIDGE_GRIP " + door.name + " angle=" + angle + " inside=" + inside);
            }
            door.BeginHold(); door.DragBy(-500); for (int i = 0; i < 90; i++) door.Step(1f / 60); door.EndHold(true);
        }
    }

    static void Scenario(int fps, float closingSpeed, bool expectSlam, bool brake)
    {
        var root = UnityEngine.Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab));
        var doors = root.GetComponentsInChildren<FridgeDoor>();
        foreach (var d in doors)
        {
            int slams = 0, closes = 0; float totalNoise = 0;
            d.SoundEmitted += (door, kind, noise) => { totalNoise += noise; if (kind == DoorSoundKind.Slam) slams++; if (kind == DoorSoundKind.GentleClose) closes++; };
            float dt = 1f / fps;
            d.BeginHold();
            for (int i = 0; i < fps * 2; i++) { d.DragBy(30f * dt); d.Step(dt); }
            for (int i = 0; i < fps; i++) d.Step(dt);
            if (Mathf.Abs(d.Angle - 60f) > .1f) throw new Exception("Hold tracking failed at " + fps);
            if (brake)
            {
                while (d.Angle > 15f) { d.DragBy(-closingSpeed * dt); d.Step(dt); }
                for (int i = 0; i < fps; i++) d.Step(dt);
                closingSpeed = 10f;
            }
            for (int i = 0; i < fps * 8; i++) { d.DragBy(-closingSpeed * dt); d.Step(dt); }
            if (slams != (expectSlam ? 1 : 0) || closes != (expectSlam ? 0 : 1) || d.Angle != 0)
                throw new Exception("Classification failed fps=" + fps + " slam=" + slams + " close=" + closes);
            if (totalNoise <= 0) throw new Exception("Missing noise events");
            if (Mathf.Abs(d.LastClosingSpeed - closingSpeed) > closingSpeed * .1f)
                throw new Exception("Frame-rate-dependent contact speed " + d.LastClosingSpeed + " expected " + closingSpeed);
            Debug.Log("FRIDGE_CASE " + d.name + " fps=" + fps + " brake=" + brake + " slam=" + slams + " contact=" + d.LastClosingSpeed);
        }
        UnityEngine.Object.DestroyImmediate(root);
    }
}
