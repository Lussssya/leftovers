using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.Rendering;
using Leftovers;
public class FixSteamAndLeaks
{
    public static void Run()
    {
        GameObject root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Microwave_Interactive.prefab");
        Microwave microwave = root.GetComponentInChildren<Microwave>();
        microwave.door.closingVolume = .85f;
        microwave.steamRate = 38;
        Steam(microwave.chamberSteam,.055f,.09f,0);
        foreach (BoxCollider box in root.GetComponentsInChildren<BoxCollider>())
        {
            if (box.name != "Back solid" && box.name != "Top solid" && box.name != "Bottom solid" && box.name != "Left solid" && box.name != "Controls solid") continue;
            GameObject blocker = GameObject.CreatePrimitive(PrimitiveType.Cube);
            blocker.name = "Light blocker";
            Object.DestroyImmediate(blocker.GetComponent<Collider>());
            blocker.transform.SetParent(box.transform,false);
            blocker.transform.localPosition = box.center;
            blocker.transform.localScale = box.size;
            blocker.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.ShadowsOnly;
        }
        Lights(root);
        PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/Microwave_Interactive.prefab");
        PrefabUtility.UnloadPrefabContents(root);
        root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Sandwich_Test.prefab");
        MicrowaveFood food = root.GetComponentInChildren<MicrowaveFood>();
        food.steam.transform.position = food.SteamPosition();
        Steam(food.steam,.045f,.075f,34);
        PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/Sandwich_Test.prefab");
        PrefabUtility.UnloadPrefabContents(root);
        root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Fridge_FlowTest.prefab");
        Lights(root);
        PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/Fridge_FlowTest.prefab");
        PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/Scenes/Microwave Fridge Flow.unity");
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("STEAM_LIGHT_FIX_SAVED");
    }
    static void Lights(GameObject root)
    {
        foreach(Light light in root.GetComponentsInChildren<Light>(true))
        {
            light.shadows = LightShadows.Soft;
            light.shadowStrength = 1;
            light.shadowBias = .005f;
            light.shadowNormalBias = .015f;
            light.shadowNearPlane = .03f;
            light.shadowResolution = LightShadowResolution.High;
        }
        foreach(MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>())
        {
            if (renderer.shadowCastingMode == ShadowCastingMode.On) renderer.shadowCastingMode = ShadowCastingMode.TwoSided;
        }
    }
    static void Steam(ParticleSystem steam,float low,float high,float rate)
    {
        ParticleSystem.MainModule main = steam.main;
        main.startSize = new ParticleSystem.MinMaxCurve(low,high);
        main.startLifetime = new ParticleSystem.MinMaxCurve(.9f,1.5f);
        main.startSpeed = 0;
        ParticleSystem.ShapeModule shape = steam.shape;
        shape.enabled = true;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.position = Vector3.zero;
        shape.rotation = Vector3.zero;
        shape.scale = new Vector3(.045f,.001f,.035f);
        ParticleSystem.VelocityOverLifetimeModule velocity = steam.velocityOverLifetime;
        velocity.x = new ParticleSystem.MinMaxCurve(-.008f,.008f);
        velocity.y = new ParticleSystem.MinMaxCurve(.12f,.18f);
        velocity.z = new ParticleSystem.MinMaxCurve(-.008f,.008f);
        ParticleSystem.NoiseModule noise = steam.noise;
        noise.strength = .018f;
        ParticleSystem.EmissionModule emission = steam.emission;
        emission.rateOverTime = rate;
        ParticleSystem.CollisionModule collision = steam.collision;
        collision.radiusScale = .08f;
    }
}

