using UnityEngine;
using UnityEditor;
using Leftovers;
public static class FeelChecks
{
    static void Check(bool value, string message)
    {
        if (!value) throw new System.Exception(message);
        Debug.Log("PASS " + message);
    }
    static void Move(FridgeDoor door,float angle)
    {
        door.BeginHold();
        for(int i=0;i<300 && Mathf.Abs(door.Angle-angle)>.01f;i++)
        {
            door.DragBy(Mathf.Clamp(angle-door.Angle,-4,4));
            door.Step(1f/60);
        }
        door.EndHold(true);
        Physics.SyncTransforms();
    }
    public static void Verify()
    {
        Microwave microwave = Object.FindObjectOfType<Microwave>();
        microwave.enabled = false;
        foreach (FridgeDoor door in Object.FindObjectsOfType<FridgeDoor>()) door.enabled = false;
        foreach (AudioSource source in Object.FindObjectsOfType<AudioSource>(true))
        {
            Check(source.spatialBlend==1 && source.maxDistance>source.minDistance,"Positional audio: " + source.name);
        }
        Move(microwave.door,90);
        microwave.Step(.1f);
        Check(!microwave.insideLight.enabled,"Microwave stays dark immediately after opening");
        microwave.Step(.4f);
        Check(microwave.insideLight.enabled && microwave.insideLight.intensity>4,"Microwave delayed bright light");
        foreach (DoorInteriorLight light in Object.FindObjectsOfType<DoorInteriorLight>())
        {
            light.enabled = false;
            Move(light.door,100);
            light.Step(.1f);
            Check(!light.insideLight.enabled,"Fridge opening delay: " + light.name);
            light.Step(.4f);
            Check(light.insideLight.enabled,"Fridge light comes on: " + light.name);
        }
        InspectAppliances.Capture(Camera.main,"interior-lights-preview.png");
        microwave.chamberSteam.Emit(20);
        microwave.chamberSteam.Simulate(.65f,true,false);
        Check(microwave.chamberSteam.GetComponent<ParticleSystemRenderer>().mesh == AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Effects/SteamPuff.asset"),"Steam uses clustered fridge puff mesh");
        InspectAppliances.Capture(Camera.main,"steam-puffs-preview.png");
        foreach (DoorInteriorLight light in Object.FindObjectsOfType<DoorInteriorLight>())
        {
            Move(light.door,0);
            light.Step(.01f);
            Check(!light.insideLight.enabled,"Fridge light goes off on close: " + light.name);
        }
        Move(microwave.door,0);
        microwave.Step(.01f);
        Check(!microwave.insideLight.enabled,"Microwave light goes off on close");
        microwave.display.Show(5);
        Move(microwave.door,90);
        microwave.door.BeginHold();
        microwave.door.DragBy(-90);
        for (int i=0;i<40;i++) microwave.door.Step(1f/60);
        microwave.door.EndHold(true);
        microwave.display.StepGlitch(.05f);
        MaterialPropertyBlock block = new MaterialPropertyBlock();
        microwave.display.screen.GetPropertyBlock(block);
        Check(block.GetFloat("_Glitch")>.1f,"Actual door slam triggers RGB display glitch");
        Camera camera = Camera.main;
        Vector3 oldPosition = camera.transform.position;
        Quaternion oldRotation = camera.transform.rotation;
        camera.transform.position = microwave.transform.position + new Vector3(-.35f,.55f,2.1f);
        camera.transform.LookAt(microwave.transform.position+Vector3.up*.55f);
        InspectAppliances.Capture(camera,"display-glitch-preview.png");
        camera.transform.SetPositionAndRotation(oldPosition,oldRotation);
        microwave.display.Show(3);
        microwave.display.StepGlitch(.5f);
        microwave.display.screen.GetPropertyBlock(block);
        Check(block.GetFloat("_Glitch")==0,"Glitch clears after half a second");
        Check(Vector4.Distance(block.GetVector("_MainTex_ST"),new Vector4(.1f,.1f,.3f,.9f))<.001f,"Display restores current countdown after glitch");
    }
}


