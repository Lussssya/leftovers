using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections;
using Leftovers;
public class SteamRoofCheck : MonoBehaviour
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Microwave Fridge Flow.unity");
        new GameObject("Roof check").AddComponent<SteamRoofCheck>();
        EditorApplication.isPlaying = true;
    }
    IEnumerator Start()
    {
        yield return null;
        Microwave microwave = FindObjectOfType<Microwave>();
        microwave.door.BeginHold(); microwave.door.DragBy(105);
        yield return new WaitForSeconds(.6f);
        microwave.door.EndHold(true);
        ParticleSystem steam = microwave.chamberSteam;
        steam.Play();
        ParticleSystem.EmitParams puff = new ParticleSystem.EmitParams();
        Transform body = microwave.vibration.transform;
        puff.position = body.TransformPoint(new Vector3(.05f,.36f,.04f));
        puff.velocity = Vector3.up * .3f;
        puff.startLifetime = 4f;
        puff.startSize = .5f;
        steam.Emit(puff,1);
        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[400];
        float until = Time.time + 2.2f;
        bool escaped = false;
        while(Time.time < until)
        {
            yield return null;
            int count = steam.GetParticles(particles);
            for(int i=0;i<count;i++)
            {
                Vector3 p = body.InverseTransformPoint(particles[i].position);
                if(p.z < .215f && p.y > .345f){Debug.LogError("Steam crossed roof");EditorApplication.Exit(1);yield break;}
                if(p.z > .30f && p.y > .35f) escaped = true;
            }
        }
        MaterialPropertyBlock properties = new MaterialPropertyBlock();
        steam.GetComponent<ParticleSystemRenderer>().GetPropertyBlock(properties);
        if(!escaped || properties.GetFloat("_MicrowaveSteam") != 1f){Debug.LogError("Steam failed to glide out and rise");EditorApplication.Exit(1);yield break;}
        AudioClip close = AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Microwave/Microwave_Close.wav");
        if(close == null || close.length < .5f){Debug.LogError("Close clip import failed");EditorApplication.Exit(1);yield break;}
        Debug.Log("PASS Steam stays below roof, exits opening, then rises; shell edge masking enabled; close clip imported "+close.length);
        EditorApplication.Exit(0);
    }
}
