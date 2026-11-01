using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Leftovers;
public class RefineSteamAndDisplay
{
    public static void Run()
    {
        GameObject root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Microwave_Interactive.prefab");
        Microwave microwave = root.GetComponentInChildren<Microwave>();
        microwave.steamRate = 32;
        microwave.steamSeconds = 2.5f;
        microwave.beepSource.volume *= .82f;
        microwave.cookingSound.volume *= .85f;
        microwave.display.door = microwave.door;
        Material timer = microwave.display.screen.sharedMaterial;
        timer.shader = Shader.Find("Leftovers/Microwave Display");
        EditorUtility.SetDirty(timer);
        Steam(microwave.chamberSteam);
        PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/Microwave_Interactive.prefab");
        PrefabUtility.UnloadPrefabContents(root);
        root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Sandwich_Test.prefab");
        ParticleSystem steam = root.GetComponentInChildren<MicrowaveFood>().steam;
        Steam(steam);
        ParticleSystem.EmissionModule emission = steam.emission;
        emission.rateOverTime = 16;
        PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/Sandwich_Test.prefab");
        PrefabUtility.UnloadPrefabContents(root);
        root = PrefabUtility.LoadPrefabContents("Assets/Prefabs/Fridge_FlowTest.prefab");
        foreach (DoorInteriorLight control in root.GetComponentsInChildren<DoorInteriorLight>())
        {
            Light light = control.insideLight;
            light.intensity = 1.35f;
            light.color = new Color(1,.94f,.83f);
            light.shadowStrength = .35f;
            light.shadowBias = .025f;
            light.shadowNormalBias = .12f;
            light.shadowResolution = UnityEngine.Rendering.LightShadowResolution.High;
            light.transform.localPosition += new Vector3(0,.08f,-.05f);
        }
        PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/Fridge_FlowTest.prefab");
        PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/Scenes/Microwave Fridge Flow.unity");
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("DISPLAY_STEAM_REFINED");
    }
    static void Steam(ParticleSystem steam)
    {
        ParticleSystem.MainModule main = steam.main;
        main.startDelay = 0;
        main.maxParticles = 240;
        ParticleSystem.ColorOverLifetimeModule color = steam.colorOverLifetime;
        Gradient fade = new Gradient();
        fade.SetKeys(new GradientColorKey[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new GradientAlphaKey[]{new GradientAlphaKey(.65f,0),new GradientAlphaKey(1,.06f),new GradientAlphaKey(.65f,.55f),new GradientAlphaKey(0,1)});
        color.color = fade;
    }
}
