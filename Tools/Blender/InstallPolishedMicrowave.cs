using System;
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Leftovers;
public class InstallPolishedMicrowave
{
    [Serializable] public class MeshData { public string name; public string part; public string material; public float[] vertices; public float[] normals; public float[] uv; public int[] triangles; }
    [Serializable] public class MeshList { public MeshData[] meshes; }
    static string folder="Assets/Models/Microwave/Polished";
    static Dictionary<string,Material> materials=new Dictionary<string,Material>();
    public static void Run()
    {
        PrepareTextures();
        AddMaterial("Enamel",new Color(.78f,.85f,.88f),.28f,.08f);
        AddMaterial("DoorEnamel",new Color(.85f,.89f,.90f),.3f,.06f);
        AddMaterial("Interior",new Color(.86f,.84f,.75f),.27f,0);
        AddMaterial("Seal",new Color(.025f,.035f,.043f),.12f,0);
        AddMaterial("Bezel",new Color(.10f,.14f,.17f),.4f,.16f);
        AddMaterial("Steel",new Color(.31f,.38f,.40f),.55f,.45f);
        AddMaterial("Turntable",new Color(.60f,.68f,.68f),.65f,.2f);
        AddMaterial("Handle",new Color(.89f,.92f,.92f),.35f,.08f);
        AddMaterial("Lamp",new Color(.95f,.83f,.57f),.3f,0);
        AddMaterial("Keypad",Color.white,.25f,0,"Keypad.png");
        AddMaterial("Timer",Color.white,0,0,"RedTimerAtlas.png","Unlit/Texture");
        AddMaterial("FineScreen",Color.white,0,0,"FineScreen.png","LEFTOVERS/Microwave Window");
        Material timer=materials["Timer"];timer.mainTextureScale=new Vector2(.1f,.1f);timer.mainTextureOffset=new Vector2(.5f,.9f);
        GameObject root=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Microwave_Interactive.prefab");
        Microwave microwave=root.GetComponent<Microwave>();
        Transform body=root.transform.Find("Microwave model"); Transform hinge=microwave.door.transform;
        foreach(MeshRenderer renderer in root.GetComponentsInChildren<MeshRenderer>(true)) renderer.enabled=false;
        foreach(Collider collider in root.GetComponentsInChildren<Collider>(true)) collider.enabled=false;
        hinge.localPosition=new Vector3(.307f,.228f,.212f);
        microwave.turntable.localPosition=new Vector3(.055f,.086f,-.006f);
        MeshList data=JsonUtility.FromJson<MeshList>(File.ReadAllText(folder+"/MicrowaveMeshes.json"));
        foreach(MeshData item in data.meshes)
        {
            Transform parent=body; Vector3 pivot=Vector3.zero;
            if(item.part=="door") { parent=hinge; pivot=hinge.localPosition; }
            if(item.part=="tray") { parent=microwave.turntable; pivot=microwave.turntable.localPosition; }
            GameObject part=new GameObject(item.name);part.transform.SetParent(parent,false);
            Mesh mesh=new Mesh();mesh.name=item.name;int count=item.vertices.Length/3;
            Vector3[] vertices=new Vector3[count];Vector3[] normals=new Vector3[count];Vector2[] uv=new Vector2[count];
            for(int i=0;i<count;i++)
            {
                vertices[i]=new Vector3(item.vertices[i*3],item.vertices[i*3+1],item.vertices[i*3+2])-pivot;
                normals[i]=new Vector3(item.normals[i*3],item.normals[i*3+1],item.normals[i*3+2]);
                uv[i]=new Vector2(item.uv[i*2],item.uv[i*2+1]);
                if(item.part=="display")uv[i]=new Vector2((uv[i].x-.5f)*10,(uv[i].y-.9f)*10);
            }
            mesh.vertices=vertices;mesh.normals=normals;mesh.uv=uv;mesh.triangles=item.triangles;mesh.RecalculateBounds();
            AssetDatabase.CreateAsset(mesh,folder+"/"+item.name.Replace(" ","_")+".asset");
            part.AddComponent<MeshFilter>().sharedMesh=mesh;
            MeshRenderer renderer=part.AddComponent<MeshRenderer>();renderer.sharedMaterial=materials[item.material.Split('.')[0]];
            if(item.part=="display") { microwave.display.screen=renderer;renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off; }
            if(item.part=="lamp")microwave.lamp=renderer;
            if(item.material.StartsWith("FineScreen"))renderer.shadowCastingMode=UnityEngine.Rendering.ShadowCastingMode.Off;
            if(item.name.StartsWith("Door outer frame")){ MeshFilter filter=hinge.GetComponent<MeshFilter>(); if(filter==null)filter=hinge.gameObject.AddComponent<MeshFilter>(); filter.sharedMesh=mesh; }
        }
        microwave.display.Show(5);
        microwave.insideLight.transform.SetParent(body,false);microwave.insideLight.transform.localPosition=new Vector3(-.145f,.325f,.025f);
        microwave.insideLight.color=new Color(1,.83f,.58f);microwave.insideLight.intensity=1.8f;microwave.insideLight.range=1.2f;
        microwave.lampOff=materials["Lamp"];
        Material lit=new Material(materials["Lamp"]);lit.name="Warm lamp on";lit.EnableKeyword("_EMISSION");lit.SetColor("_EmissionColor",new Color(1,.66f,.27f)*1.7f);AssetDatabase.CreateAsset(lit,folder+"/LampOn.mat");microwave.lampOn=lit;
        microwave.chamberSteam.transform.localPosition=new Vector3(.055f,.20f,.19f);
        SetupColliders(root,microwave,body,hinge);
        foreach(MeshRenderer oldRenderer in root.GetComponentsInChildren<MeshRenderer>(true))
        {
            if(oldRenderer.enabled)continue;
            GameObject oldPart=oldRenderer.gameObject;
            UnityEngine.Object.DestroyImmediate(oldRenderer);
            MeshFilter oldMesh=oldPart.GetComponent<MeshFilter>();
            if(oldMesh!=null && oldPart!=hinge.gameObject)UnityEngine.Object.DestroyImmediate(oldMesh);
            foreach(Collider oldCollider in oldPart.GetComponents<Collider>())if(!oldCollider.enabled)UnityEngine.Object.DestroyImmediate(oldCollider);
            if(oldPart.transform.childCount==0 && oldPart.GetComponents<Component>().Length==1)UnityEngine.Object.DestroyImmediate(oldPart);
        }
        PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/Microwave_Interactive.prefab");PrefabUtility.UnloadPrefabContents(root);
        AssetDatabase.SaveAssets();
        EditorSceneManager.OpenScene("Assets/Scenes/Microwave Fridge Flow.unity");
        GameObject fillObject=GameObject.Find("Appliance soft fill");
        if(fillObject==null)fillObject=new GameObject("Appliance soft fill");
        Light fill=fillObject.GetComponent<Light>();if(fill==null)fill=fillObject.AddComponent<Light>();
        fill.type=LightType.Directional;fill.color=new Color(.88f,.94f,1);fill.intensity=.75f;fill.shadows=LightShadows.None;
        fillObject.transform.rotation=Quaternion.Euler(25,180,0);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("POLISHED_MICROWAVE_INSTALLED");
    }
    static void SetupColliders(GameObject root,Microwave microwave,Transform body,Transform hinge)
    {
        AddBox(body,"Top solid",new Vector3(0,.412f,0),new Vector3(.66f,.035f,.43f));
        AddBox(body,"Bottom solid",new Vector3(0,.041f,0),new Vector3(.65f,.034f,.42f));
        AddBox(body,"Left solid",new Vector3(.313f,.228f,0),new Vector3(.034f,.38f,.43f));
        AddBox(body,"Controls solid",new Vector3(-.257f,.228f,0),new Vector3(.14f,.38f,.43f));
        AddBox(body,"Back solid",new Vector3(0,.228f,-.202f),new Vector3(.63f,.37f,.027f));
        AddBox(body,"Interior floor solid",new Vector3(.055f,.067f,-.012f),new Vector3(.48f,.018f,.36f));
        AddBox(microwave.turntable,"Turntable solid",new Vector3(0,-.005f,0),new Vector3(.30f,.008f,.30f));
        BoxCollider panel=hinge.GetComponent<BoxCollider>();panel.enabled=true;panel.center=new Vector3(-.252f,0,.01f);panel.size=new Vector3(.524f,.381f,.038f);
        Transform handle=hinge.Find("Handle");handle.localPosition=new Vector3(-.486f,0,.07f);
        BoxCollider grip=null;
        foreach(BoxCollider box in handle.GetComponents<BoxCollider>()) if(box.isTrigger)grip=box;
        grip.enabled=true;grip.center=Vector3.zero;grip.size=new Vector3(.075f,.29f,.09f);
        BoxCollider solid=AddBox(hinge,"Handle solid",new Vector3(-.486f,0,.065f),new Vector3(.03f,.27f,.04f));
        Transform inside=hinge.Find("Inside handle");inside.localPosition=new Vector3(-.486f,0,-.02f);BoxCollider insideGrip=inside.GetComponent<BoxCollider>();insideGrip.enabled=true;insideGrip.size=new Vector3(.075f,.28f,.025f);
        microwave.door.collision.parts=new BoxCollider[]{panel,solid};microwave.door.collision.applianceRoot=root.transform;
    }
    static BoxCollider AddBox(Transform parent,string name,Vector3 center,Vector3 size)
    {
        GameObject part=new GameObject(name);part.transform.SetParent(parent,false);part.transform.localPosition=center;BoxCollider collider=part.AddComponent<BoxCollider>();collider.size=size;return collider;
    }
    static void AddMaterial(string name,Color color,float smooth,float metal,string texture=null,string shader="Standard")
    {
        Material material=new Material(Shader.Find(shader));material.name=name;if(material.HasProperty("_Color"))material.color=color;
        if(shader=="Standard") {material.SetFloat("_Glossiness",smooth);material.SetFloat("_Metallic",metal);}
        if(texture!=null)material.mainTexture=AssetDatabase.LoadAssetAtPath<Texture2D>(folder+"/"+texture);
        AssetDatabase.CreateAsset(material,folder+"/"+name+".mat");materials.Add(name,material);
    }
    static void PrepareTextures()
    {
        foreach(string file in new string[]{"Keypad.png","RedTimerAtlas.png","FineScreen.png"})
        {
            TextureImporter importer=(TextureImporter)AssetImporter.GetAtPath(folder+"/"+file);importer.maxTextureSize=4096;importer.textureCompression=TextureImporterCompression.Uncompressed;
            importer.wrapMode=TextureWrapMode.Clamp;importer.filterMode=FilterMode.Bilinear;
            if(file=="FineScreen.png") {importer.wrapMode=TextureWrapMode.Repeat;importer.alphaIsTransparency=true;}
            if(file=="RedTimerAtlas.png")importer.mipmapEnabled=false;
            importer.SaveAndReimport();
        }
    }
}
