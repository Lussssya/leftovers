using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections;
using Leftovers;
public class LiveMicrowaveCheck : MonoBehaviour
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Microwave Fridge Flow.unity");
        new GameObject("Live cook check").AddComponent<LiveMicrowaveCheck>();
        EditorApplication.isPlaying=true;
    }
    IEnumerator Start()
    {
        yield return null;
        Microwave microwave=FindObjectOfType<Microwave>();
        MicrowaveFood food=FindObjectOfType<MicrowaveFood>();
        FridgeDoor[] doors=FindObjectsOfType<FridgeDoor>();
        foreach(FridgeDoor door in doors){door.BeginHold();door.DragBy(105);}
        yield return new WaitForSeconds(.6f);
        foreach(FridgeDoor door in doors)door.EndHold(true);
        food.transform.SetParent(null,true);
        food.Hold();
        food.MoveTo(food.transform.position+Vector3.forward*2);
        food.MoveTo(new Vector3(microwave.transform.position.x,food.transform.position.y,3));
        Vector3 tray=microwave.turntable.TransformPoint(new Vector3(0,.002f,0));
        food.MoveTo(new Vector3(tray.x,tray.y,3));
        if(!microwave.PutFood(food)){Fail("Food insertion");yield break;}
        microwave.door.BeginHold();microwave.door.DragBy(-105);
        yield return new WaitForSeconds(.5f);
        microwave.door.EndHold(true);
        yield return new WaitForSeconds(5.6f);
        if(!food.IsCooked || !microwave.IsFinished){Fail("Normal frame cooking");yield break;}
        if(microwave.chamberSteam.particleCount<5){Fail("No accumulated chamber steam");yield break;}
        Debug.Log("PASS Closed chamber accumulates steam");
        microwave.door.BeginHold();microwave.door.DragBy(105);
        yield return new WaitForSeconds(.5f);
        microwave.door.EndHold(true);
        if(!microwave.chamberSteam.isPlaying){Fail("Chamber playback");yield break;}
        Debug.Log("PASS Normal frame cooking and steam playback");
        ParticleSystem.Particle[] escapedPuffs=new ParticleSystem.Particle[400];
        yield return new WaitForSeconds(.4f);
        int escapedCount=microwave.chamberSteam.GetParticles(escapedPuffs);
        int outside=0;
        for(int i=0;i<escapedCount;i++) if(microwave.transform.InverseTransformPoint(escapedPuffs[i].position).z>.22f)outside++;
        if(outside<1){Fail("Steam did not escape opening");yield break;}
        Debug.Log("PASS Steam escapes the front opening: "+outside+" particles");
        if(microwave.GetComponentsInChildren<Light>().Length!=1){Fail("Interior is not single source");yield break;}
        Debug.Log("PASS Single interior light source");
        Camera camera=Camera.main;
        camera.transform.position=microwave.transform.position+new Vector3(-.4f,.65f,2.8f);
        camera.transform.LookAt(microwave.transform.position+Vector3.up*.55f);
        InspectAppliances.Capture(camera,"live-open-light.png");
        if(!microwave.TakeFood(food)){Fail("Food removal");yield break;}
        food.Hold();food.MoveTo(new Vector3(tray.x,tray.y,3));
        camera.transform.position=food.bread.bounds.center+new Vector3(-.45f,.4f,1.35f);
        camera.transform.LookAt(food.bread.bounds.center+Vector3.up*.2f);
        yield return new WaitForSeconds(.15f);
        InspectAppliances.Capture(camera,"live-steam-start.png");
        yield return new WaitForSeconds(1f);
        ParticleSystem.Particle[] particles=new ParticleSystem.Particle[400];
        int count=food.steam.GetParticles(particles);
        float highest=food.SteamPosition().y;
        float youngest=float.MaxValue;
        float sourceDistance=100;
        for(int i=0;i<count;i++)
        {
            highest=Mathf.Max(highest,particles[i].position.y);
            float age=particles[i].startLifetime-particles[i].remainingLifetime;
            if(age<youngest){youngest=age;sourceDistance=Vector3.Distance(particles[i].position,food.SteamPosition());}
        }
        Debug.Log("LIVE_STEAM count="+count+" rise="+(highest-food.SteamPosition().y)+" newbornDistance="+sourceDistance+" age="+youngest);
        InspectAppliances.Capture(camera,"live-steam-rise.png");
        if(highest-food.SteamPosition().y<.3f || sourceDistance>.19f){Fail("Normal frame steam rise or origin");yield break;}
        Debug.Log("PASS Steam rises in ordinary frames and newborn puffs originate on food");
        yield return new WaitForSeconds(4.1f);
        if(food.steam.emission.rateOverTime.constant>.001f){Fail("Food steam did not stop emitting after five seconds");yield break;}
        Debug.Log("PASS Food emission fades out five seconds after removal");
        yield return new WaitForSeconds(3f);
        if(food.steam.particleCount!=0){Fail("Food steam did not dissipate");yield break;}
        Debug.Log("PASS Remaining food puffs dissipate naturally");
        EditorApplication.Exit(0);
    }
    void Fail(string message){Debug.LogError(message);EditorApplication.Exit(1);}
}

