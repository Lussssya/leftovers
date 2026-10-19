using UnityEditor;
using UnityEngine;
using Leftovers;
public class FinalMicrowaveFlow
{
    public static void Run()
    {
        GameObject root=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Microwave_Interactive.prefab");
        Microwave microwave=root.GetComponentInChildren<Microwave>();
        if(microwave.interiorFill!=null)Object.DestroyImmediate(microwave.interiorFill.gameObject);
        microwave.interiorFill=null;
        microwave.insideLight.transform.localPosition=new Vector3(-.12f,.30f,.03f);
        microwave.insideLight.intensity=2.1f;
        microwave.insideLight.color=new Color(1,.93f,.80f);
        microwave.insideLight.range=1.8f;
        microwave.door.movementFadeSpeed=8f;
        ParticleSystem.VelocityOverLifetimeModule velocity=microwave.chamberSteam.velocityOverLifetime;
        velocity.y=new ParticleSystem.MinMaxCurve(.29f,.37f);
        ParticleSystem.MainModule main=microwave.chamberSteam.main;
        main.startLifetime=new ParticleSystem.MinMaxCurve(2.4f,3.2f);
        Material interior=AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/Microwave/Polished/Interior.mat");
        interior.SetFloat("_Glossiness",.12f);
        EditorUtility.SetDirty(interior);
        PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/Microwave_Interactive.prefab");
        PrefabUtility.UnloadPrefabContents(root);
        root=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Sandwich_Test.prefab");
        MicrowaveFood food=root.GetComponent<MicrowaveFood>();
        ParticleSystem.ShapeModule shape=food.steam.shape;
        shape.scale=new Vector3(.105f,.001f,.075f);
        velocity=food.steam.velocityOverLifetime;
        velocity.y=new ParticleSystem.MinMaxCurve(.34f,.43f);
        PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/Sandwich_Test.prefab");
        PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.SaveAssets();
    }
}
