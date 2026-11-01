using UnityEngine;
using UnityEditor;
using Leftovers;
public class FogSteamSetup
{
    public static void Run()
    {
        Material fog=new Material(Shader.Find("Leftovers/Soft Steam"));
        AssetDatabase.CreateAsset(fog,"Assets/Effects/SoftSteam.mat");
        foreach(string path in new string[]{"Assets/Prefabs/Microwave_Interactive.prefab","Assets/Prefabs/Sandwich_Test.prefab"})
        {
            GameObject root=PrefabUtility.LoadPrefabContents(path);
            Microwave microwave=root.GetComponentInChildren<Microwave>();
            MicrowaveFood food=root.GetComponentInChildren<MicrowaveFood>();
            ParticleSystem steam;
            bool small=food!=null;
            if(small)steam=food.steam;
            else steam=microwave.chamberSteam;
            ParticleSystem.MainModule main=steam.main;
            main.startSize3D=false;
            main.startSize=new ParticleSystem.MinMaxCurve(small?.22f:.65f,small?.34f:1.05f);
            main.startLifetime=new ParticleSystem.MinMaxCurve(small?1.5f:2.4f,small?2.1f:3.3f);
            main.startColor=new Color(.91f,.93f,.94f,small?.22f:.50f);
            main.startRotation3D=false;
            main.startRotation=new ParticleSystem.MinMaxCurve(-.5f,.5f);
            main.playOnAwake=false;
            main.maxParticles=160;
            ParticleSystem.EmissionModule emission=steam.emission;
            emission.rateOverTime=small?7:0;
            ParticleSystem.VelocityOverLifetimeModule velocity=steam.velocityOverLifetime;
            velocity.y=new ParticleSystem.MinMaxCurve(small?.21f:.3f,small?.28f:.4f);
            velocity.x=new ParticleSystem.MinMaxCurve(-.025f,.025f);
            velocity.z=new ParticleSystem.MinMaxCurve(0,.025f);
            ParticleSystem.NoiseModule noise=steam.noise;
            noise.strength=.065f;
            noise.frequency=.65f;
            noise.scrollSpeed=.15f;
            ParticleSystem.SizeOverLifetimeModule size=steam.sizeOverLifetime;
            size.size=new ParticleSystem.MinMaxCurve(1,new AnimationCurve(new Keyframe(0,.55f),new Keyframe(.5f,1),new Keyframe(1,1.5f)));
            ParticleSystem.ColorOverLifetimeModule color=steam.colorOverLifetime;
            Gradient fade=new Gradient();
            fade.SetKeys(new GradientColorKey[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new GradientAlphaKey[]{new GradientAlphaKey(0,0),new GradientAlphaKey(1,.12f),new GradientAlphaKey(.65f,.55f),new GradientAlphaKey(0,1)});
            color.color=fade;
            ParticleSystemRenderer renderer=steam.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode=ParticleSystemRenderMode.Billboard;
            renderer.sharedMaterial=fog;
            renderer.maxParticleSize=1;
            renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            PrefabUtility.SaveAsPrefabAsset(root,path);
            PrefabUtility.UnloadPrefabContents(root);
        }
        AssetDatabase.SaveAssets();
    }
}
