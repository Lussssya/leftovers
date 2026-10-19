using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using Leftovers;
public class InstallCabinetAndVibration
{
    public static void Run()
    {
        GameObject root=PrefabUtility.LoadPrefabContents("Assets/Prefabs/Microwave_Interactive.prefab");
        Microwave microwave=root.GetComponentInChildren<Microwave>();
        FridgeSlamEffect old=root.GetComponentInChildren<FridgeSlamEffect>();
        Transform body=old.transform;
        Object.DestroyImmediate(old);
        MicrowaveVibration vibration=body.gameObject.AddComponent<MicrowaveVibration>();
        vibration.door=microwave.door;
        microwave.vibration=vibration;
        PrefabUtility.SaveAsPrefabAsset(root,"Assets/Prefabs/Microwave_Interactive.prefab");
        PrefabUtility.UnloadPrefabContents(root);
        EditorSceneManager.OpenScene("Assets/Scenes/Microwave Fridge Flow.unity");
        microwave=Object.FindObjectOfType<Microwave>();
        GameObject oldCounter=GameObject.Find("Microwave counter");
        GameObject cabinet=(GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/MicrowaveBaseCabinet.prefab"));
        cabinet.transform.position=new Vector3(microwave.transform.position.x,0,microwave.transform.position.z);
        Renderer countertop=null;
        foreach(MeshRenderer part in cabinet.GetComponentsInChildren<MeshRenderer>())
        {
            if(part.name=="Countertop")countertop=part;
        }
        if(countertop==null)throw new System.Exception("Cabinet countertop missing");
        Bounds cabinetBounds=countertop.bounds;
        foreach(MeshRenderer part in cabinet.GetComponentsInChildren<MeshRenderer>())cabinetBounds.Encapsulate(part.bounds);
        cabinet.transform.position-=Vector3.up*cabinetBounds.min.y;
        float currentHeight=countertop.bounds.max.y;
        cabinet.transform.localScale*=1.64f/currentHeight;
        foreach(AudioSource sound in cabinet.GetComponentsInChildren<AudioSource>())
        {
            sound.spatialBlend=1;
            sound.rolloffMode=AudioRolloffMode.Linear;
            sound.minDistance=1.5f;
            sound.maxDistance=22;
            sound.dopplerLevel=0;
        }
        if(oldCounter!=null)Object.DestroyImmediate(oldCounter);
        MouseCabinetInteractor interaction=Camera.main.gameObject.AddComponent<MouseCabinetInteractor>();
        interaction.interactionCamera=Camera.main;
        interaction.reach=40;
        Debug.Log("CABINET_TOP "+countertop.bounds.max.y+" MICROWAVE_BASE "+microwave.transform.position.y);
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        InspectAppliances.Capture(Camera.main,"cabinet-flow-preview.png");
    }
}
