using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Leftovers;
public class FixLiveMicrowave
{
    public static void Run()
    {
        GameObject root=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Microwave_Interactive.prefab");
        Microwave microwave=root.GetComponentInChildren<Microwave>();
        microwave.beepSource.volume=.34f;
        microwave.cookingSound.volume=.5f;
        foreach(AudioSource source in new AudioSource[]{microwave.beepSource,microwave.cookingSound})
        {
            source.spatialBlend=1;
            source.minDistance=1;
            source.maxDistance=16;
            source.rolloffMode=AudioRolloffMode.Linear;
            source.reverbZoneMix=0;
        }
        microwave.insideLight.transform.localPosition=new Vector3(.04f,.34f,.09f);
        microwave.insideLight.color=new Color(1,.94f,.83f);
        microwave.insideLight.intensity=1.25f;
        microwave.insideLight.range=1.6f;
        GameObject fill=new GameObject("Interior bounced light");
        fill.transform.SetParent(microwave.insideLight.transform.parent,false);
        fill.transform.localPosition=new Vector3(-.12f,.23f,-.08f);
        microwave.interiorFill=fill.AddComponent<Light>();
        microwave.interiorFill.color=new Color(1,.94f,.84f);
        microwave.interiorFill.intensity=.35f;
        microwave.interiorFill.range=1.25f;
        microwave.interiorFill.shadows=LightShadows.Soft;
        microwave.interiorFill.shadowStrength=1;
        microwave.interiorFill.shadowBias=.005f;
        microwave.interiorFill.shadowNormalBias=.015f;
        microwave.interiorFill.shadowNearPlane=.03f;
        microwave.interiorFill.enabled=false;
        microwave.lampOn.SetColor("_EmissionColor",new Color(1,.91f,.72f)*.65f);
        EditorUtility.SetDirty(microwave.lampOn);
        Steam(microwave.chamberSteam);
        PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/Microwave_Interactive.prefab");
        PrefabUtility.UnloadPrefabContents(root);
        root=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Sandwich_Test.prefab");
        Steam(root.GetComponent<MicrowaveFood>().steam);
        PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/Sandwich_Test.prefab");
        PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.SaveAssets();
    }
    static void Steam(ParticleSystem steam)
    {
        ParticleSystem.MainModule main=steam.main;
        main.cullingMode=ParticleSystemCullingMode.AlwaysSimulate;
        main.simulationSpace=ParticleSystemSimulationSpace.World;
        main.simulationSpeed=1;
        ParticleSystem.CollisionModule collision=steam.collision;
        collision.enableDynamicColliders=false;
        ParticleSystem.SizeOverLifetimeModule size=steam.sizeOverLifetime;
        size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.65f),new Keyframe(.4f,1),new Keyframe(1,1.7f)));
    }
}
