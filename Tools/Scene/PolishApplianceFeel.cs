using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Leftovers;
public class PolishApplianceFeel
{
    static Mesh puff;
    public static void Run()
    {
        Mesh original = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Effects/FridgePuff.asset");
        CombineInstance[] lobes = new CombineInstance[5];
        Vector3[] offsets = {Vector3.zero, new Vector3(-.3f,.05f,0),new Vector3(.28f,.12f,.05f),new Vector3(0,.3f,-.08f),new Vector3(.05f,-.12f,.25f)};
        for (int i=0;i<5;i++)
        {
            lobes[i].mesh = original;
            lobes[i].transform = Matrix4x4.TRS(offsets[i],Quaternion.Euler(i*31,i*67,0),Vector3.one*(i==0 ? .85f : .62f));
        }
        puff = new Mesh();
        puff.name = "Clustered fridge steam puff";
        puff.CombineMeshes(lobes);
        AssetDatabase.CreateAsset(puff,"Assets/Effects/SteamPuff.asset");
        GameObject microwaveRoot = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Microwave_Interactive.prefab");
        Microwave microwave = microwaveRoot.GetComponentInChildren<Microwave>();
        microwave.lightDelay = .42f;
        microwave.insideLight.intensity = 4.2f;
        microwave.insideLight.range = 2.1f;
        microwave.insideLight.renderMode = LightRenderMode.ForcePixel;
        microwave.insideLight.shadows = LightShadows.Soft;
        microwave.door.movementVolume = .022f;
        microwave.steamRate = 11;
        microwave.steamSeconds = 2.1f;
        FridgeSlamEffect shake = microwaveRoot.GetComponentInChildren<FridgeSlamEffect>();
        shake.shakeAngle = .65f;
        shake.shakeSeconds = .19f;
        SetupSteam(microwave.chamberSteam,false);
        SpatialAudio(microwaveRoot);
        PrefabUtility.SaveAsPrefabAsset(microwaveRoot,"Assets/Prefabs/Microwave_Interactive.prefab");
        PrefabUtility.UnloadPrefabContents(microwaveRoot);
        GameObject food = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Sandwich_Test.prefab");
        SetupSteam(food.GetComponentInChildren<MicrowaveFood>().steam,true);
        PrefabUtility.SaveAsPrefabAsset(food,"Assets/Prefabs/Sandwich_Test.prefab");
        PrefabUtility.UnloadPrefabContents(food);
        GameObject fridge = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Fridge_FlowTest.prefab");
        foreach (FridgeDoor door in fridge.GetComponentsInChildren<FridgeDoor>())
        {
            Bounds bounds = door.GetComponent<MeshFilter>().sharedMesh.bounds;
            Vector3 center = door.transform.TransformPoint(bounds.center);
            Vector3 back = door.transform.TransformDirection(Vector3.back);
            GameObject lamp = new GameObject(door.name + " interior light");
            lamp.transform.SetParent(fridge.transform);
            lamp.transform.position = center + back*.22f + Vector3.up*bounds.extents.y*.45f;
            Light light = lamp.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = new Color(1,.86f,.65f);
            light.intensity = 3.2f;
            light.range = 2.6f;
            light.shadows = LightShadows.Soft;
            light.renderMode = LightRenderMode.ForcePixel;
            light.enabled = false;
            DoorInteriorLight control = lamp.AddComponent<DoorInteriorLight>();
            control.door = door;
            control.insideLight = light;
        }
        SpatialAudio(fridge);
        PrefabUtility.SaveAsPrefabAsset(fridge,"Assets/Prefabs/Fridge_FlowTest.prefab");
        PrefabUtility.UnloadPrefabContents(fridge);
        EditorSceneManager.OpenScene("Assets/Scenes/Microwave Fridge Flow.unity");
        foreach (AudioSource source in Object.FindObjectsOfType<AudioSource>(true)) SetSpatial(source);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("APPLIANCE_FEEL_INSTALLED");
    }
    static void SpatialAudio(GameObject root)
    {
        foreach(AudioSource source in root.GetComponentsInChildren<AudioSource>(true)) SetSpatial(source);
    }
    static void SetSpatial(AudioSource source)
    {
        source.spatialBlend = 1;
        source.rolloffMode = AudioRolloffMode.Linear;
        source.minDistance = 1.5f;
        source.maxDistance = 22;
        source.spread = 0;
        source.dopplerLevel = 0;
    }
    static void SetupSteam(ParticleSystem steam, bool food)
    {
        steam.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = steam.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Shape;
        main.startSize3D = false;
        main.startSize = new ParticleSystem.MinMaxCurve(food ? .07f : .13f,food ? .12f : .22f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.3f,2.2f);
        main.startSpeed = 0;
        main.startRotation3D = true;
        main.startRotationX = new ParticleSystem.MinMaxCurve(0,6.28f);
        main.startRotationY = new ParticleSystem.MinMaxCurve(0,6.28f);
        main.startColor = new Color(.88f,.9f,.86f,.55f);
        main.gravityModifier = 0;
        main.maxParticles = 120;
        ParticleSystem.EmissionModule emission = steam.emission;
        emission.rateOverTime = food ? 5 : 0;
        ParticleSystem.VelocityOverLifetimeModule velocity = steam.velocityOverLifetime;
        velocity.enabled = true;
        velocity.space = ParticleSystemSimulationSpace.World;
        velocity.x = new ParticleSystem.MinMaxCurve(-.035f,.035f);
        velocity.y = new ParticleSystem.MinMaxCurve(.22f,.38f);
        velocity.z = new ParticleSystem.MinMaxCurve(food ? 0 : .08f,food ? .02f : .16f);
        ParticleSystem.NoiseModule noise = steam.noise;
        noise.enabled = true;
        noise.strength = .055f;
        noise.frequency = .7f;
        noise.scrollSpeed = .18f;
        ParticleSystem.SizeOverLifetimeModule size = steam.sizeOverLifetime;
        size.enabled = true;
        size.separateAxes = false;
        size.size = new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.45f),new Keyframe(.25f,.8f),new Keyframe(1,1.5f)));
        ParticleSystem.ColorOverLifetimeModule color = steam.colorOverLifetime;
        color.enabled = true;
        Gradient fade = new Gradient();
        fade.SetKeys(new GradientColorKey[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new GradientAlphaKey[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.12f),new GradientAlphaKey(.65f,.55f),new GradientAlphaKey(0,1)});
        color.color = fade;
        ParticleSystem.CollisionModule collision = steam.collision;
        collision.enabled = true;
        collision.type = ParticleSystemCollisionType.World;
        collision.mode = ParticleSystemCollisionMode.Collision3D;
        collision.quality = ParticleSystemCollisionQuality.High;
        collision.enableDynamicColliders = true;
        collision.bounce = .05f;
        collision.dampen = .15f;
        collision.lifetimeLoss = .05f;
        collision.radiusScale = .35f;
        ParticleSystemRenderer renderer = steam.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.mesh = puff;
        renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Effects/FridgePuff.mat");
    }
}
