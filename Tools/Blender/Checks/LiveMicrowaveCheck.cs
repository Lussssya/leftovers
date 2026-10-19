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
        RecoverySceneCheck.Verify();
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
        Quaternion rest = microwave.vibration.transform.localRotation;
        microwave.door.BeginHold();microwave.door.DragBy(-105);
        yield return CheckVibration(microwave.vibration, rest, .5f, "Slam");
        yield return new WaitForSeconds(.5f);
        microwave.door.EndHold(true);
        float timeout = Time.time + 8f;
        while (!microwave.IsFinished && Time.time < timeout) yield return null;
        yield return CheckAlarm(microwave, rest);
        if(!food.IsCooked || !microwave.IsFinished){Fail("Normal frame cooking");yield break;}
        if(microwave.chamberSteam.particleCount!=0 || food.steam.particleCount!=0){Fail("Steam visible with door shut");yield break;}
        Debug.Log("PASS No steam while cooking or while shut");
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
        if(highest-food.SteamPosition().y<.2f || sourceDistance>.19f){Fail("Normal frame steam rise or origin");yield break;}
        Debug.Log("PASS Steam rises in ordinary frames and newborn puffs originate on food");
        yield return new WaitForSeconds(4.1f);
        if(food.steam.emission.rateOverTime.constant>.001f){Fail("Food steam did not stop emitting after five seconds");yield break;}
        Debug.Log("PASS Food emission fades out five seconds after removal");
        yield return new WaitForSeconds(3f);
        if(food.steam.particleCount!=0){Fail("Food steam did not dissipate");yield break;}
        Debug.Log("PASS Remaining food puffs dissipate naturally");
        CabinetPart[] parts = FindObjectsOfType<CabinetPart>();
        if(parts.Length < 3){Fail("Cabinet parts missing"); yield break;}
        foreach(CabinetPart part in parts)
        {
            if(!part.slidesOut) continue;
            Vector3 before = part.transform.localPosition;
            part.BeginHold(); part.DragBy(part.maximumOpen);
            yield return new WaitForSeconds(.5f);
            if(Vector3.Distance(before, part.transform.localPosition) < .01f){Fail("Cabinet drawer did not move");yield break;}
            part.EndHold(true);
        }
        Debug.Log("PASS Cabinet doors and working drawer present");
        EditorApplication.Exit(0);
    }
    IEnumerator CheckAlarm(Microwave microwave, Quaternion rest)
    {
        int litFrames = 0;
        int darkFrames = 0;
        int pulses = 0;
        bool previous = false;
        float maximum = 0f;
        float deadline = Time.time + microwave.beep.length + 2f;
        MaterialPropertyBlock properties = new MaterialPropertyBlock();
        while (microwave.beepSource.isPlaying && Time.time < deadline)
        {
            yield return null;
            microwave.display.screen.GetPropertyBlock(properties);
            bool lit = properties.GetFloat("_DigitsVisible") > .5f;
            if(lit) litFrames++; else darkFrames++;
            if(lit && !previous) pulses++;
            previous = lit;
            Vector3 angles = (Quaternion.Inverse(rest) * microwave.vibration.transform.localRotation).eulerAngles;
            maximum = Mathf.Max(maximum, Mathf.Abs(Mathf.DeltaAngle(0f, angles.z)));
            if(Mathf.Abs(Mathf.DeltaAngle(0f,angles.y)) > .01f || Mathf.Abs(Mathf.DeltaAngle(0f,angles.x)) > .01f) Fail("Alarm wrong axis");
        }
        yield return new WaitForSeconds(.2f);
        microwave.display.screen.GetPropertyBlock(properties);
        if(litFrames == 0 || darkFrames == 0 || pulses < 2 || maximum < 1f) Fail("Alarm not flashing and shaking with multiple beeps");
        if(Quaternion.Angle(rest,microwave.vibration.transform.localRotation) > .01f || properties.GetFloat("_DigitsVisible") < .5f) Fail("Alarm did not reset");
        Debug.Log("PASS Alarm Z shake="+maximum+" beep flashes="+pulses+" lit frames="+litFrames+" dark frames="+darkFrames+" and reset");
    }
    IEnumerator CheckVibration(MicrowaveVibration vibration, Quaternion rest, float seconds, string label)
    {
        float until = Time.time + seconds;
        float maximum = 0f;
        while(Time.time < until)
        {
            Vector3 angles = (Quaternion.Inverse(rest) * vibration.transform.localRotation).eulerAngles;
            maximum = Mathf.Max(maximum, Mathf.Abs(Mathf.DeltaAngle(0f, angles.y)));
            if(Mathf.Abs(Mathf.DeltaAngle(0f, angles.x)) > .01f || Mathf.Abs(Mathf.DeltaAngle(0f, angles.z)) > .01f) Fail(label + " wrong shake axis");
            yield return null;
        }
        if(maximum < .01f) Fail(label + " did not vibrate");
        if(Quaternion.Angle(rest, vibration.transform.localRotation) > .01f) Fail(label + " did not settle");
        Debug.Log("PASS " + label + " Y vibration and reset: " + maximum);
    }
    void Fail(string message){Debug.LogError(message);EditorApplication.Exit(1);}
}
