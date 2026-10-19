using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Leftovers;
public class InspectAppliances
{
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Kitchen.unity");
        foreach (FridgeDoor door in Object.FindObjectsOfType<FridgeDoor>())
        {
            MeshFilter filter = door.GetComponent<MeshFilter>();
            if (filter != null) Debug.Log("DOOR_BOUNDS " + door.name + " " + filter.sharedMesh.bounds);
            foreach (Collider collider in door.GetComponentsInChildren<Collider>()) Debug.Log("DOOR_COLLIDER " + door.name + " " + collider.GetType().Name + " " + collider.bounds);
            if (door.name != "Microwave door")
            {
                door.BeginHold();door.DragBy(110);
                for (int i=0;i<40;i++) door.Step(1f/60);
                door.EndHold(true);
            }
        }
        Camera camera = Camera.main;
        Capture(camera, "fridge-interior.png");
    }
    public static void Capture(Camera camera, string file)
    {
        RenderTexture target = new RenderTexture(1200,900,24);camera.targetTexture=target;camera.Render();RenderTexture.active=target;
        Texture2D image = new Texture2D(1200,900,TextureFormat.RGB24,false);image.ReadPixels(new Rect(0,0,1200,900),0,0);image.Apply();
        System.IO.File.WriteAllBytes(file,image.EncodeToPNG());camera.targetTexture=null;RenderTexture.active=null;
        Object.DestroyImmediate(image);Object.DestroyImmediate(target);
    }
}
