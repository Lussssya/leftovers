using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
public class BuildFlowRoom
{
    static Transform room;
    public static void Run()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Microwave Fridge Flow.unity");
        GameObject previous = GameObject.Find("Flow test room");
        if (previous != null) Object.DestroyImmediate(previous);
        room = new GameObject("Flow test room").transform;
        System.IO.Directory.CreateDirectory("Assets/Materials/FlowRoom");
        Material wall = MakeMaterial("Warm plaster", new Color(.33f,.30f,.26f), .12f);
        Material floor = MakeMaterial("Slate floor", new Color(.16f,.18f,.17f), .22f);
        Material trim = MakeMaterial("Dark trim", new Color(.10f,.115f,.11f), .25f);
        AddBox("Back wall", new Vector3(0,2.6f,-1.25f),new Vector3(13,5.4f,.18f),wall);
        AddBox("Left wall",new Vector3(6.4f,2.6f,3),new Vector3(.18f,5.4f,8.5f),wall);
        AddBox("Right wall",new Vector3(-6.4f,2.6f,3),new Vector3(.18f,5.4f,8.5f),wall);
        AddBox("Back skirting",new Vector3(0,.12f,-1.13f),new Vector3(12.8f,.24f,.08f),trim);
        GameObject ground = GameObject.Find("Floor (1)");
        if (ground != null) ground.GetComponent<Renderer>().sharedMaterial = floor;
        Light key = GameObject.Find("Directional Light").GetComponent<Light>();
        key.transform.rotation = Quaternion.Euler(42,150,0);
        key.color = new Color(1,.83f,.65f);
        key.intensity = .65f;
        key.shadows = LightShadows.Soft;
        key.shadowStrength = .65f;
        Light fill = GameObject.Find("Appliance soft fill").GetComponent<Light>();
        fill.color = new Color(.75f,.82f,.91f);
        fill.intensity = .16f;
        RenderSettings.skybox = null;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.19f,.19f,.18f);
        RenderSettings.ambientEquatorColor = new Color(.12f,.13f,.14f);
        RenderSettings.ambientGroundColor = new Color(.07f,.065f,.06f);
        RenderSettings.reflectionIntensity = .25f;
        Camera.main.clearFlags = CameraClearFlags.SolidColor;
        Camera.main.backgroundColor = new Color(.12f,.11f,.095f);
        GameObject lamp = new GameObject("Warm wall wash");
        lamp.transform.SetParent(room);
        lamp.transform.position = new Vector3(.7f,3.9f,.1f);
        Light bulb = lamp.AddComponent<Light>();
        bulb.type = LightType.Point;
        bulb.color = new Color(1,.72f,.43f);
        bulb.intensity = 1.8f;
        bulb.range = 5;
        bulb.shadows = LightShadows.Soft;
        AddBox("Wall lamp housing",new Vector3(.7f,4,-1.02f),new Vector3(.7f,.16f,.28f),trim);
        Material glow = MakeMaterial("Lamp glow",new Color(1,.8f,.5f),.1f);
        glow.EnableKeyword("_EMISSION");
        glow.SetColor("_EmissionColor",new Color(1,.65f,.3f)*2);
        AddBox("Wall lamp diffuser",new Vector3(.7f,3.93f,-.99f),new Vector3(.6f,.07f,.2f),glow);
        AssetDatabase.SaveAssets();
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        InspectAppliances.Capture(Camera.main,"dim-room-preview.png");
        Debug.Log("FLOW_ROOM_SAVED");
    }
    static Material MakeMaterial(string name, Color color, float smoothness)
    {
        string path = "Assets/Materials/FlowRoom/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material == null)
        {
            material = new Material(Shader.Find("Standard"));
            AssetDatabase.CreateAsset(material,path);
        }
        material.color = color;
        material.SetFloat("_Glossiness",smoothness);
        return material;
    }
    static void AddBox(string name, Vector3 position, Vector3 size, Material material)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cube);
        part.name = name;
        part.transform.SetParent(room);
        part.transform.position = position;
        part.transform.localScale = size;
        part.GetComponent<Renderer>().sharedMaterial = material;
    }
}
