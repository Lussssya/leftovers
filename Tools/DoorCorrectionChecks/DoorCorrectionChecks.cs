using System;
using System.Collections;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Leftovers;
public class DoorCorrectionChecks : MonoBehaviour
{
    Microwave microwave;
    void Check(bool value, string message) { if (!value) throw new Exception(message); Debug.Log("PASS " + message); }
    public static void Run() { EditorSceneManager.OpenScene("Assets/Scenes/Kitchen.unity"); new GameObject("Checks").AddComponent<DoorCorrectionChecks>(); EditorApplication.isPlaying = true; }
    IEnumerator Start()
    {
        yield return null;
        try { Tests(); EditorApplication.Exit(0); } catch(Exception error) { Debug.LogException(error); EditorApplication.Exit(1); }
    }
    void MoveDoor(FridgeDoor door, float angle)
    {
        door.BeginHold();
        for(int i=0;i<500 && Mathf.Abs(door.Angle-angle)>.001f;i++) { door.DragBy(Mathf.Clamp(angle-door.Angle,-4,4)); door.Step(1f/60); }
        door.EndHold(true); Physics.SyncTransforms();
    }
    void Tests()
    {
        microwave=FindObjectOfType<Microwave>(); microwave.enabled=false;
        FridgeDoor[] doors=FindObjectsOfType<FridgeDoor>(); foreach(FridgeDoor door in doors) door.enabled=false;
        MicrowaveFood food=FindObjectOfType<MicrowaveFood>();
        InspectAppliances.Capture(Camera.main,"layout-corrected.png");
        foreach(FridgeDoor door in doors) { MoveDoor(door,105); Check(door.Angle>104,"Door opens freely: "+door.name); }
        InspectAppliances.Capture(Camera.main,"layout-open.png");
        Check(food.transform.position.x<0,"Sandwich starts inside fridge on screen right");
        RaycastHit pick; Vector3 origin=Camera.main.transform.position;
        Check(Physics.Raycast(origin,food.transform.position+Vector3.up*.15f-origin,out pick,30,~0,QueryTriggerInteraction.Collide) && pick.collider.GetComponentInParent<MicrowaveFood>()==food,"Food can be picked through open fridge");
        food.transform.SetParent(null,true); food.Hold();
        food.MoveTo(food.transform.position+Vector3.forward*2);
        Check(food.transform.position.z>1,"Food leaves fridge through open door");
        food.MoveTo(new Vector3(microwave.transform.position.x,food.transform.position.y,2));
        Vector3 tray=microwave.turntable.TransformPoint(new Vector3(0,.002f,0));
        food.MoveTo(new Vector3(tray.x,tray.y,3));
        microwave.Step(.2f);
        Check(microwave.PutFood(food),"Food can be placed through open microwave");
        MoveDoor(microwave.door,0); microwave.Step(.01f);
        Check(microwave.door.Angle==0,"Loaded door fully closes");
        Check(!microwave.IsCooking && !microwave.beepSource.isPlaying,"Close sound gets time before start beep");
        microwave.Step(.23f); Check(microwave.IsCooking && microwave.beepSource.isPlaying,"Delayed start beeps and cooks");
        microwave.Step(5); Check(food.IsCooked && microwave.IsFinished && microwave.insideLight.enabled,"Cooking completes with beep and light");
        MoveDoor(microwave.door,105); microwave.Step(.2f);
        Check(microwave.chamberSteam.GetComponent<ParticleSystemRenderer>().renderMode==ParticleSystemRenderMode.Mesh && !microwave.chamberSteam.textureSheetAnimation.enabled,"Steam uses low poly mesh puffs");
        Check(microwave.TakeFood(food),"Cooked food removable"); food.transform.SetParent(null,true); food.Hold(); food.MoveTo(new Vector3(tray.x,tray.y,3));
        Debug.Log("EXTRACT_POSITION " + food.transform.position + " TRAY " + tray); MoveDoor(microwave.door,0); Debug.Log("STOP_ANGLE " + microwave.door.Angle); Check(microwave.door.Angle==0,"Empty microwave closes");
        Vector3 before=food.transform.position; food.MoveTo(tray);
        Check(food.transform.position.z>tray.z+.15f,"Held plate cannot pass through closed microwave door");
        food.MoveTo(new Vector3(tray.x,tray.y,3)); MoveDoor(microwave.door,105); food.MoveTo(new Vector3(tray.x,tray.y,microwave.door.transform.position.z));
        MoveDoor(microwave.door,0); Check(microwave.door.Angle>0,"Door stops against plate and sandwich");
        food.MoveTo(new Vector3(tray.x,tray.y,3)); MoveDoor(microwave.door,0); Camera camera=Camera.main; camera.transform.position=microwave.transform.position+new Vector3(-.4f,.55f,2.6f); camera.transform.LookAt(microwave.transform.position+Vector3.up*.45f); InspectAppliances.Capture(camera,"microwave-handle-window.png");
        foreach(FridgeDoor door in doors)
        {
            MoveDoor(door,0); Check(door.Angle==0,"Door closes freely: "+door.name);
            MoveDoor(door,105);
            BoxCollider panel=door.collision.parts[0];
            Vector3 local=door.transform.InverseTransformPoint(panel.transform.TransformPoint(panel.center));
            GameObject obstacle=GameObject.CreatePrimitive(PrimitiveType.Cube); obstacle.transform.position=door.PointAtAngle(local,45); obstacle.transform.localScale=Vector3.one*.15f;
            MoveDoor(door,0); Check(door.Angle>1,"Door stops against scene objects: "+door.name);
            DestroyImmediate(obstacle); MoveDoor(door,0); Check(door.Angle==0,"Door resumes when obstacle removed: "+door.name);
        }
        food.transform.position=new Vector3(0,3,2); food.Drop();
        Physics.SyncTransforms(); Physics.autoSimulation=false;
        for(int i=0;i<160;i++) Physics.Simulate(.02f);
        Physics.autoSimulation=true;
        Debug.Log("DROP_POSITION " + food.transform.position); Check(food.transform.position.y>-.2f && food.transform.position.y<.2f,"Dropped plate lands on floor");
        Debug.Log("DOOR_CORRECTIONS_PASSED");
    }
}



