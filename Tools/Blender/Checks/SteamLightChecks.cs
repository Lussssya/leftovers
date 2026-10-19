using UnityEngine;
using UnityEditor;
using Leftovers;
public static class SteamLightChecks
{
    public static void Run()
    {
        MicrowaveFood food = Object.FindObjectOfType<MicrowaveFood>();
        Microwave microwave = Object.FindObjectOfType<Microwave>();
        food.Place();
        food.transform.position = new Vector3(2.2f,1.7f,1.5f);
        food.steam.transform.position = food.SteamPosition();
        if (Vector3.Distance(food.steam.transform.position,food.transform.TransformPoint(food.steamOrigin))>.001f) throw new System.Exception("Steam origin is not on food");
        Debug.Log("PASS Steam origin follows sandwich top surface");
        if (food.steam.shape.position != Vector3.zero || food.steam.shape.scale.y>.002f) throw new System.Exception("Steam shape offset");
        Debug.Log("PASS Steam emitter is a narrow surface, without floating offset");
        foreach (DoorInteriorLight light in Object.FindObjectsOfType<DoorInteriorLight>())
        {
            if(light.insideLight.shadowStrength!=1) throw new System.Exception("Fridge light leaks");
        }
        if(microwave.insideLight.shadowStrength!=1) throw new System.Exception("Microwave light leaks");
        Debug.Log("PASS Interior lights fully shadowed");
        food.Cook();
        food.steam.Simulate(.65f,true,true);
        Camera camera = Camera.main;
        camera.transform.position = food.bread.bounds.center+new Vector3(-.45f,.38f,1.1f);
        camera.transform.LookAt(food.bread.bounds.center+Vector3.up*.12f);
        InspectAppliances.Capture(camera,"sandwich-steam-preview.png");
        ParticleSystem.EmissionModule emission=food.steam.emission;
        emission.enabled=false;
        food.steam.Stop(true,ParticleSystemStopBehavior.StopEmittingAndClear);
        food.steam.Emit(12);
        food.steam.Simulate(.1f,true,false);
        ParticleSystem.Particle[] particles=new ParticleSystem.Particle[64];
        int count=food.steam.GetParticles(particles);
        float first=0;
        for(int i=0;i<count;i++) first+=particles[i].position.y;
        first/=Mathf.Max(1,count);
        food.steam.Simulate(1f,true,false);
        count=food.steam.GetParticles(particles);
        float second=0;
        for(int i=0;i<count;i++) second+=particles[i].position.y;
        second/=Mathf.Max(1,count);
        if(count<6 || second-first<.3f) throw new System.Exception("Steam is not rising: "+(second-first));
        Debug.Log("PASS Steam rises "+(second-first)+" metres in one second");
        if(GameObject.Find("Beveled wood floor").transform.childCount<100) throw new System.Exception("Missing plank floor");
        Debug.Log("PASS Physical beveled plank floor present");
    }
}


