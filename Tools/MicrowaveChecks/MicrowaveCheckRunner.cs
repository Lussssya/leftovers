using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using Leftovers;
public class MicrowaveCheckRunner : MonoBehaviour
{
    Microwave microwave;
    FridgeDoor door;
    int slams;
    int closes;
    IEnumerator Start()
    {
        yield return null;
        try
        {
            RunChecks();
            File.WriteAllText("microwave-runtime-passed.txt", "Empty door; delayed light; plate placement; five-second countdown; paused early opening; early cooked window; completion beep/light; hot food removal and steam; half-open door deflects steam; handle picking; both appliances retained.");
            Debug.Log("MICROWAVE_RUNTIME_PASSED");
            UnityEditor.EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            UnityEditor.EditorApplication.Exit(1);
        }
    }
    void Check(bool passed, string message)
    {
        if (!passed) throw new Exception(message);
        Debug.Log("PASS " + message);
    }
    void RunChecks()
    {
        microwave = UnityEngine.Object.FindObjectOfType<Microwave>();
        door = microwave.door;
        microwave.enabled = false;
        door.enabled = false;
        door.SoundEmitted += OnSound;
        microwave.Step(.01f);
        Check(microwave.food == null && !microwave.IsCooking && !microwave.insideLight.enabled, "Empty and closed stays off");
        SetDoor(2);
        Check(!microwave.insideLight.enabled, "Light waits after opening");
        microwave.Step(.13f);
        Check(microwave.insideLight.enabled, "Light turns on after delay");
        SetDoor(85);
        CheckHandle();
        SetDoor(0, 20);
        Check(!microwave.IsCooking && !microwave.insideLight.enabled, "Empty closure switches light off");
        Check(closes == 1 && slams == 0, "Slow microwave close is gentle");
        Check(!microwave.beepSource.isPlaying, "Empty close does not beep");
        SetDoor(80);
        SetDoor(0, 240);
        Check(slams == 1, "Fast microwave close is a slam");
        SetDoor(110);
        CheckHandle();
        MicrowaveFood food = UnityEngine.Object.FindObjectOfType<MicrowaveFood>();
        CheckFoodRay(food);
        Check(microwave.PutFood(food), "Plate fits on turntable");
        CheckFoodRay(food);
        Check(Vector3.Distance(food.transform.lossyScale, Vector3.one * 2.6f) < .001f, "Food retains correct scale when placed");
        SetDoor(0);
        Check(microwave.beepSource.isPlaying, "Loading and closing plays one short start cue");
        Check(microwave.startBeep != null && microwave.startBeep.length >= .09f && microwave.startBeep.length < .25f, "Start cue is short");
        Check(microwave.beep.length > 3f, "Full downloaded completion cue is retained");
        Check(microwave.cookingSound.volume >= .65f, "Microwave motor is louder");
        microwave.beepSource.Stop();
        food.heatedSeconds = 0;
        Quaternion plateRotation = microwave.turntable.localRotation;
        microwave.Step(1);
        Check(microwave.IsCooking && Mathf.Abs(microwave.RemainingSeconds - 4) < .001f, "Five second countdown starts");
        Check(!microwave.beepSource.isPlaying, "Start cue does not repeat while cooking");
        Check(Quaternion.Angle(plateRotation, microwave.turntable.localRotation) > 20, "Plate spins while cooking");
        Check(!microwave.insideLight.enabled, "Cooking keeps interior light off");
        SetDoor(85);
        float remaining = microwave.RemainingSeconds;
        Quaternion paused = microwave.turntable.localRotation;
        microwave.Step(.5f);
        Check(!food.IsCooked && !microwave.IsCooking && microwave.RemainingSeconds == remaining, "Early opening pauses uncooked food");
        Check(Quaternion.Angle(paused, microwave.turntable.localRotation) < .001f, "Open door stops plate");
        SetDoor(0);
        food.heatedSeconds = 0;
        microwave.Step(3.5f);
        Check(microwave.RemainingSeconds > 0 && microwave.RemainingSeconds <= 2, "Timer reaches cooked window");
        SetDoor(85);
        Check(food.IsCooked && food.steam.isPlaying && !microwave.IsCooking, "Opening during window cooks food and steams");
        microwave.Step(.2f);
        microwave.chamberSteam.Simulate(.02f, true, false);
        Check(microwave.chamberSteam.particleCount > 0, "Cooked opening vents steam");
        Check(microwave.TakeFood(food), "Cooked plate can be removed");
        Check(food.steam.isPlaying, "Removed cooked food still steams");
        food.gameObject.SetActive(false);
        GameObject fresh = UnityEngine.Object.Instantiate(UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Sandwich_Test.prefab"));
        MicrowaveFood raw = fresh.GetComponent<MicrowaveFood>();
        Check(microwave.PutFood(raw), "Fresh uncooked food can replace cooked food");
        SetDoor(0);
        raw.heatedSeconds = 0;
        microwave.Step(5);
        Check(raw.IsCooked && microwave.IsFinished && microwave.RemainingSeconds == 0 && !microwave.IsCooking, "Timer zero finishes cooking");
        Check(microwave.insideLight.enabled && microwave.beepSource.isPlaying && !microwave.cookingSound.isPlaying, "Completion beeps, lights and stops motor");
        microwave.beepSource.Stop();
        microwave.Step(.1f);
        Check(!microwave.beepSource.isPlaying, "Completion cue does not repeat");
        Check(microwave.IsFinished && microwave.RemainingSeconds == 0, "Completed cycle does not restart");
        Check(microwave.chamberSteam.textureSheetAnimation.enabled && microwave.chamberSteam.textureSheetAnimation.numTilesX == 4, "Steam uses animated wisp atlas");
        Check(microwave.steamRate >= 100 && food.steam.emission.rateOverTime.constant >= 30, "Steam density increased");
        SetDoor(45);
        CheckSteamCollision();
        CheckHandle();
        int fridgeDoors = 0;
        foreach (FridgeDoor other in UnityEngine.Object.FindObjectsOfType<FridgeDoor>()) if (other != door) fridgeDoors++;
        Check(fridgeDoors == 2, "Existing fridge still has two working door components");
    }
    void SetDoor(float angle, float speed = 500)
    {
        door.BeginHold();
        for (int i = 0; i < 700 && Mathf.Abs(door.Angle - angle) > .002f; i++)
        {
            float change = Mathf.Clamp(angle - door.Angle, -speed / 60, speed / 60);
            door.DragBy(change);
            door.Step(1f/60);
            microwave.Step(1f/60);
        }
        door.EndHold(true);
        Physics.SyncTransforms();
    }
    void OnSound(FridgeDoor source, DoorSoundKind kind, float amount)
    {
        if (kind == DoorSoundKind.Slam) slams++;
        if (kind == DoorSoundKind.GentleClose) closes++;
    }
    void CheckFoodRay(MicrowaveFood food)
    {
        Physics.SyncTransforms();
        Vector3 origin = Camera.main.transform.position;
        Vector3 target = food.transform.TransformPoint(new Vector3(0, .07f, 0));
        RaycastHit hit;
        bool found = Physics.Raycast(origin, target - origin, out hit, 40, ~0, QueryTriggerInteraction.Collide);
        Check(found && hit.collider.GetComponentInParent<MicrowaveFood>() == food, "Mouse can pick food at " + food.transform.position);
    }
    void CheckHandle()
    {
        FridgeDoorHandle[] handles = door.GetComponentsInChildren<FridgeDoorHandle>();
        bool reachable = false;
        foreach (FridgeDoorHandle handle in handles)
        {
            Vector3 origin = Camera.main.transform.position;
            RaycastHit hit;
            if (Physics.Raycast(origin, handle.transform.position - origin, out hit, 40, ~0, QueryTriggerInteraction.Collide))
                if (hit.collider.GetComponent<FridgeDoorHandle>() != null) reachable = true;
        }
        Check(reachable, "Mouse can reach microwave grip at " + door.Angle + " degrees");
    }
    void CheckSteamCollision()
    {
        ParticleSystem steam = microwave.chamberSteam;
        steam.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        BoxCollider collider = door.GetComponent<BoxCollider>();
        Vector3 normal = door.transform.forward;
        Vector3 center = door.transform.TransformPoint(collider.center);
        Vector3 start = center - normal * .15f;
        RaycastHit hit;
        Check(Physics.Raycast(start, normal, out hit, .3f, ~0, QueryTriggerInteraction.Ignore) && hit.collider == collider, "Half open door physically blocks steam path");
        ParticleSystem.EmitParams puff = new ParticleSystem.EmitParams();
        puff.position = start;
        puff.velocity = normal * .8f;
        puff.startLifetime = 2;
        puff.startSize = .035f;
        steam.Emit(puff, 1);
        steam.Simulate(.4f, true, false, true);
        ParticleSystem.Particle[] particles = new ParticleSystem.Particle[10];
        int count = steam.GetParticles(particles);
        Check(count > 0, "Steam survives contact");
        Check(Vector3.Dot(particles[0].velocity, normal) < .15f, "Door deflects steam instead of passing through");
        Check(particles[0].velocity.y > 0, "Steam continues rising after deflection");
    }
}
