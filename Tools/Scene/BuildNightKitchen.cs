using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEditor;
using UnityEditor.SceneManagement;
using Leftovers;
public class BuildNightKitchen
{
    [Serializable] public class MaterialData { public string name; public float[] color; public float smoothness; public float metallic; public bool unlit; }
    [Serializable] public class PartData { public string name; public string group; public string material; public bool solid; public float[] vertices; public float[] normals; public int[] triangles; }
    [Serializable] public class KitchenData { public MaterialData[] materials; public PartData[] meshes; }
    static string assets = "Assets/Models/NightKitchen";
    static string source = "C:/Users/Mompas/Documents/GitHub/leftovers/leftovers/ArtSource/NightKitchen";
    static Dictionary<string, Material> materials = new Dictionary<string, Material>();
    static Dictionary<string, Transform> groups = new Dictionary<string, Transform>();
    public static void Run()
    {
        Directory.CreateDirectory(assets + "/Meshes");
        Directory.CreateDirectory(assets + "/Materials");
        AssetDatabase.Refresh();
        Scene original = EditorSceneManager.OpenScene("Assets/Scenes/Environment.unity");
        GameObject player = UnityEngine.Object.Instantiate(UnityEngine.Object.FindObjectOfType<Mover>().gameObject);
        player.name = "Player";
        Scene night = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
        SceneManager.MoveGameObjectToScene(player, night);
        SceneManager.SetActiveScene(night);
        EditorSceneManager.CloseScene(original, true);
        Scene flow = EditorSceneManager.OpenScene("Assets/Scenes/Microwave Fridge Flow.unity", OpenSceneMode.Additive);
        SceneManager.SetActiveScene(night);
        GameObject sourceFridge = null;
        Microwave sourceMicrowave = null;
        MicrowaveFood sourceFood = null;
        MouseFoodInteractor sourceFoodInput = null;
        GameObject sourceFloor = null;
        foreach (GameObject root in flow.GetRootGameObjects())
        {
            if (root.name == "Fridge_Interactive") sourceFridge = root;
            if (root.GetComponentInChildren<Microwave>() != null) sourceMicrowave = root.GetComponentInChildren<Microwave>();
            if (root.GetComponentInChildren<MicrowaveFood>() != null) sourceFood = root.GetComponentInChildren<MicrowaveFood>();
            if (root.GetComponentInChildren<MouseFoodInteractor>() != null) sourceFoodInput = root.GetComponentInChildren<MouseFoodInteractor>();
            foreach (Transform child in root.GetComponentsInChildren<Transform>()) if (child.name == "Beveled wood floor") sourceFloor = child.gameObject;
        }
        GameObject floor = UnityEngine.Object.Instantiate(sourceFloor);
        floor.name = "Reclaimed plank floor";
        SceneManager.MoveGameObjectToScene(floor, night);
        GameObject fridge = CopyPrefab(sourceFridge);
        fridge.name = "Fridge";
        fridge.transform.rotation = Quaternion.identity;
        Fit(fridge, 0f, 3.78f, 0f, new Vector3(-4.9f,.025f,.06f));
        GameObject microwaveObject = CopyPrefab(sourceMicrowave.gameObject);
        microwaveObject.name = "Microwave";
        microwaveObject.transform.rotation = Quaternion.identity;
        Fit(microwaveObject, 1.72f, 0f, 0f, new Vector3(4.55f,1.73f,.07f));
        Microwave microwave = microwaveObject.GetComponent<Microwave>();
        Vector3 restingPoint = sourceFridge.transform.InverseTransformPoint(sourceFoodInput.foodRest.position);
        GameObject foodRest = new GameObject("Sandwich resting spot");
        foodRest.transform.SetParent(fridge.transform, false);
        foodRest.transform.localPosition = restingPoint;
        foodRest.transform.localScale = Vector3.one * (microwave.transform.localScale.x / fridge.transform.localScale.x);
        GameObject food = CopyPrefab(sourceFood.gameObject);
        food.name = "Sandwich";
        food.transform.SetParent(foodRest.transform,false);
        food.transform.localPosition = Vector3.zero;
        food.transform.localRotation = Quaternion.identity;
        food.transform.localScale = Vector3.one;
        EditorSceneManager.CloseScene(flow,true);
        BuildMeshes();
        groups["Dining furniture"].position = new Vector3(-.8f,0f,-1.1f);
        BaseCabinet("Microwave cabinet",4.55f,1.90f,false);
        BaseCabinet("Sink cabinet left",2.70f,1.80f,true);
        BaseCabinet("Sink cabinet right",.90f,1.80f,true);
        BaseCabinet("Right counter cabinet",-2.65f,1.95f,false);
        UpperCabinet("Left wall cabinet",4.55f,1.90f,3.05f,1.22f);
        UpperCabinet("Over oven cabinet",-.88f,1.83f,3.07f,1.20f);
        UpperCabinet("Right wall cabinet",-2.65f,1.78f,2.82f,1.45f);
        foreach (ProductInteractor old in player.GetComponentsInChildren<ProductInteractor>(true)) UnityEngine.Object.DestroyImmediate(old);
        foreach (KitchenHud old in player.GetComponentsInChildren<KitchenHud>(true)) UnityEngine.Object.DestroyImmediate(old);
        player.transform.position = new Vector3(0,.5f,7.0f);
        player.transform.rotation = Quaternion.Euler(0,180,0);
        Camera camera = player.GetComponentInChildren<Camera>();
        camera.transform.parent.localPosition = new Vector3(0,2.15f,0);
        camera.transform.parent.localRotation = Quaternion.Euler(7f,0,0);
        camera.transform.localPosition = Vector3.zero;
        camera.transform.localRotation = Quaternion.identity;
        camera.fieldOfView = 66f;
        camera.nearClipPlane = .04f;
        camera.farClipPlane = 80f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(.018f,.027f,.042f);
        camera.allowHDR = true;
        camera.depthTextureMode = DepthTextureMode.Depth;
        CameraFollow look = camera.GetComponent<CameraFollow>();
        SerializedObject lookSettings = new SerializedObject(look);
        lookSettings.FindProperty("defaultPitch").floatValue = 7;
        lookSettings.FindProperty("minPitch").floatValue = -55;
        lookSettings.FindProperty("maxPitch").floatValue = 70;
        lookSettings.ApplyModifiedPropertiesWithoutUndo();
        MouseFoodInteractor foodInput = camera.GetComponent<MouseFoodInteractor>();
        foodInput.interactionCamera = camera;
        foodInput.doorInteractor = camera.GetComponent<MouseDoorInteractor>();
        foodInput.microwave = microwave;
        foodInput.foodRest = foodRest.transform;
        Light moon = AddLight("Moonlight through window",LightType.Directional, new Vector3(1,4,-2),new Color(.44f,.63f,1f),.75f,20f);
        moon.transform.rotation = Quaternion.LookRotation(new Vector3(-.18f,-.65f,1f));
        moon.shadowStrength = .85f;
        AddLight("Window blue bounce",LightType.Point,new Vector3(1.85f,3.30f,-.16f),new Color(.37f,.58f,1f),.85f,7.5f);
        AddLight("Fourth wall lamp",LightType.Point,new Vector3(-1.2f,3.13f,6.78f),new Color(1f,.68f,.37f),1.5f,10.5f);
        AddLight("Soft moon bounce",LightType.Point,new Vector3(0,3.8f,4.0f),new Color(.48f,.61f,.85f),.8f,12f);
        RenderSettings.skybox = null;
        RenderSettings.sun = moon;
        RenderSettings.ambientMode = AmbientMode.Trilight;
        RenderSettings.ambientSkyColor = new Color(.25f,.31f,.43f);
        RenderSettings.ambientEquatorColor = new Color(.16f,.20f,.28f);
        RenderSettings.ambientGroundColor = new Color(.08f,.065f,.05f);
        RenderSettings.reflectionIntensity = .18f;
        RenderSettings.fog = false;
        QualitySettings.shadows = ShadowQuality.All;
        QualitySettings.shadowResolution = ShadowResolution.High;
        QualitySettings.shadowDistance = 25f;
        QualitySettings.pixelLightCount = 6;
        Validate(night);
        EditorSceneManager.SaveScene(night,"Assets/Scenes/Environment.unity");
        AssetDatabase.SaveAssets();
        Capture(camera,"night-kitchen-preview.png");
        camera.transform.position = new Vector3(0,2.65f,2.5f);
        camera.transform.rotation = Quaternion.Euler(0,0,0);
        Capture(camera,"night-kitchen-fourth-wall.png");
        camera.transform.localRotation = Quaternion.identity;
        Debug.Log("NIGHT_KITCHEN_CREATED objects="+UnityEngine.Object.FindObjectsOfType<Transform>().Length);
    }
    static GameObject CopyPrefab(GameObject sourceObject)
    {
        string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(sourceObject);
        GameObject copy = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        PrefabUtility.SetPropertyModifications(copy,PrefabUtility.GetPropertyModifications(sourceObject));
        copy.transform.localScale = sourceObject.transform.localScale;
        return copy;
    }
    static Bounds BoundsOf(GameObject root)
    {
        Bounds bounds = new Bounds();
        bool first = true;
        foreach (MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>())
        {
            if (!renderer.enabled) continue;
            if (first) { bounds = renderer.bounds; first = false; }
            else bounds.Encapsulate(renderer.bounds);
        }
        return bounds;
    }
    static void Fit(GameObject root,float width,float height,float depth,Vector3 bottomCenter)
    {
        Bounds bounds = BoundsOf(root);
        Vector3 factor = Vector3.one;
        if (width > 0) factor = Vector3.one * width / bounds.size.x;
        if (height > 0 && width == 0) factor = Vector3.one * height / bounds.size.y;
        if (height > 0 && width > 0) factor.y = height / bounds.size.y;
        if (depth > 0) factor.z = depth / bounds.size.z;
        root.transform.localScale = Vector3.Scale(root.transform.localScale,factor);
        bounds = BoundsOf(root);
        root.transform.position += bottomCenter - new Vector3(bounds.center.x,bounds.min.y,bounds.center.z);
    }
    static void BaseCabinet(string name,float x,float width,bool sink)
    {
        GameObject cabinet = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MicrowaveBaseCabinet.prefab"));
        cabinet.name = name;
        Fit(cabinet,width,1.67f,1.25f,new Vector3(x,.035f,.03f));
        if (sink)
        {
            foreach (Transform child in cabinet.GetComponentsInChildren<Transform>())
                if (child.name == "Countertop") UnityEngine.Object.DestroyImmediate(child.gameObject);
        }
    }
    static void UpperCabinet(string name,float x,float width,float bottom,float height)
    {
        GameObject cabinet = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/UpperCabinet.prefab"));
        cabinet.name = name;
        Fit(cabinet,width,height,.62f,new Vector3(x,bottom,-.22f));
    }
    static void BuildMeshes()
    {
        KitchenData data = JsonUtility.FromJson<KitchenData>(File.ReadAllText(source+"/NightKitchenMeshes.json"));
        foreach (MaterialData item in data.materials)
        {
            string path = assets+"/Materials/"+item.name+".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find(item.unlit ? "Unlit/Color" : "Standard")); AssetDatabase.CreateAsset(material,path); }
            material.color = new Color(item.color[0],item.color[1],item.color[2]);
            if (!item.unlit) { material.SetFloat("_Glossiness",item.smoothness); material.SetFloat("_Metallic",item.metallic); }
            EditorUtility.SetDirty(material);
            materials[item.name] = material;
        }
        int index = 0;
        foreach (PartData item in data.meshes)
        {
            if (!groups.ContainsKey(item.group)) groups[item.group] = new GameObject(item.group).transform;
            GameObject part = new GameObject(item.name);
            part.transform.SetParent(groups[item.group],false);
            Mesh mesh = new Mesh(); mesh.name = item.name;
            int count = item.vertices.Length/3;
            Vector3[] vertices = new Vector3[count]; Vector3[] normals = new Vector3[count];
            for (int i=0;i<count;i++)
            {
                vertices[i] = new Vector3(item.vertices[i*3],item.vertices[i*3+1],item.vertices[i*3+2]);
                normals[i] = new Vector3(item.normals[i*3],item.normals[i*3+1],item.normals[i*3+2]);
            }
            mesh.vertices = vertices; mesh.normals = normals; mesh.triangles = item.triangles; mesh.RecalculateBounds();
            string path = assets+"/Meshes/"+index.ToString("D3")+".asset";
            Mesh existing = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (existing == null) AssetDatabase.CreateAsset(mesh,path);
            else { EditorUtility.CopySerialized(mesh,existing); UnityEngine.Object.DestroyImmediate(mesh); mesh=existing; }
            part.AddComponent<MeshFilter>().sharedMesh = mesh;
            MeshRenderer renderer = part.AddComponent<MeshRenderer>(); renderer.sharedMaterial=materials[item.material];
            if (item.group == "Night beyond window") renderer.shadowCastingMode=ShadowCastingMode.Off;
            if (item.solid) part.AddComponent<MeshCollider>().sharedMesh=mesh;
            part.isStatic=true;
            index++;
        }
    }
    static Light AddLight(string name,LightType type,Vector3 position,Color color,float intensity,float range)
    {
        GameObject root = new GameObject(name); root.transform.position=position;
        Light light = root.AddComponent<Light>(); light.type=type; light.color=color; light.intensity=intensity; light.range=range;
        light.shadows=LightShadows.Soft; light.shadowBias=.025f; light.shadowNormalBias=.15f; light.renderMode=LightRenderMode.ForcePixel;
        return light;
    }
    public static void Validate(Scene scene)
    {
        foreach(GameObject root in scene.GetRootGameObjects()) foreach(Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            if(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject)>0) throw new Exception("Missing script: "+child.name);
        }
        if(UnityEngine.Object.FindObjectsOfType<Camera>().Length!=1) throw new Exception("Expected one player camera");
        if(UnityEngine.Object.FindObjectOfType<Microwave>()==null || UnityEngine.Object.FindObjectOfType<Mover>()==null) throw new Exception("Appliance or player missing");
    }
    public static void Capture(Camera camera,string name)
    {
        RenderTexture target=new RenderTexture(1536,1024,24,RenderTextureFormat.ARGB32);
        camera.targetTexture=target; camera.Render(); RenderTexture.active=target;
        Texture2D image=new Texture2D(1536,1024,TextureFormat.RGB24,false);
        image.ReadPixels(new Rect(0,0,1536,1024),0,0); image.Apply();
        File.WriteAllBytes(source+"/"+name,image.EncodeToPNG());
        camera.targetTexture=null; RenderTexture.active=null;
        UnityEngine.Object.DestroyImmediate(image); UnityEngine.Object.DestroyImmediate(target);
    }
}

