using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using Leftovers;
public class MergeSceneSetup
{
    public static void FixEnvironment()
    {
        Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/Environment.unity");
        GameObject placeholder = GameObject.Find("Temporary Microwave Visibility Test");
        if (placeholder != null) placeholder.SetActive(false);
        GameObject toast = GameObject.Find("Toast");
        AssemblyTable assembly = Object.FindObjectOfType<AssemblyTable>();
        foreach (Renderer top in assembly.GetComponentsInChildren<Renderer>())
        {
            if (top.name != "Table top") continue;
            toast.transform.position = new Vector3(top.bounds.center.x + .5f, top.bounds.max.y + .08f, top.bounds.center.z + .2f);
        }
        GameObject rest = GameObject.Find("Sandwich rest");
        rest.transform.localScale = Vector3.one * 1.3f;
        CheckScene(scene);
        EditorSceneManager.SaveScene(scene);
    }
    public static void Run()
    {
        string destination = "Assets/Scenes/Environment.unity";
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(destination) != null) throw new System.Exception("Environment already exists");
        if (!AssetDatabase.CopyAsset("Assets/Scenes/Kitchen.unity", destination)) throw new System.Exception("Scene copy failed");
        Scene environment = EditorSceneManager.OpenScene(destination);
        GameObject cabinet = GameObject.Find("MicrowaveBaseCabinet");
        GameObject fridge = GameObject.Find("Fridge_Interactive");
        Camera camera = Camera.main;
        Scene flow = EditorSceneManager.OpenScene("Assets/Scenes/Microwave Fridge Flow.unity", OpenSceneMode.Additive);
        SceneManager.SetActiveScene(environment);
        Microwave source = null;
        MicrowaveFood sourceFood = null;
        MouseFoodInteractor sourceInteraction = null;
        GameObject sourceFridge = null;
        foreach (GameObject root in flow.GetRootGameObjects())
        {
            if (root.GetComponentInChildren<Microwave>() != null) source = root.GetComponentInChildren<Microwave>();
            if (root.GetComponentInChildren<MicrowaveFood>() != null) sourceFood = root.GetComponentInChildren<MicrowaveFood>();
            if (root.GetComponentInChildren<MouseFoodInteractor>() != null) sourceInteraction = root.GetComponentInChildren<MouseFoodInteractor>();
            if (root.name == "Fridge_Interactive") sourceFridge = root;
        }
        GameObject microwaveObject = CopyPrefab(source.transform.root.gameObject);
        Microwave microwave = microwaveObject.GetComponentInChildren<Microwave>();
        Renderer countertop = null;
        foreach (Renderer part in cabinet.GetComponentsInChildren<Renderer>()) if (part.name == "Countertop") countertop = part;
        if (countertop == null || fridge == null || camera == null) throw new System.Exception("Kitchen setup missing");
        microwaveObject.transform.rotation = cabinet.transform.rotation;
        microwaveObject.transform.position = new Vector3(countertop.bounds.center.x, countertop.bounds.max.y, countertop.bounds.center.z);
        GameObject upper = GameObject.Find("UpperCabinet");
        Bounds microwaveBounds = BoundsOf(microwaveObject);
        Bounds upperBounds = BoundsOf(upper);
        if (upperBounds.min.y < microwaveBounds.max.y + .12f) upper.transform.position += Vector3.up * (microwaveBounds.max.y + .12f - upperBounds.min.y);
        GameObject rest = new GameObject("Sandwich rest");
        rest.transform.SetParent(fridge.transform, false);
        rest.transform.localPosition = sourceFridge.transform.InverseTransformPoint(sourceInteraction.foodRest.position);
        rest.transform.localRotation = Quaternion.Inverse(fridge.transform.rotation) * cabinet.transform.rotation;
        GameObject food = CopyPrefab(sourceFood.gameObject);
        food.transform.SetParent(rest.transform, false);
        food.transform.localPosition = Vector3.zero;
        food.transform.localRotation = Quaternion.identity;
        food.transform.localScale = sourceFood.transform.localScale;
        MouseFoodInteractor interaction = camera.gameObject.AddComponent<MouseFoodInteractor>();
        interaction.interactionCamera = camera;
        interaction.doorInteractor = camera.GetComponent<MouseDoorInteractor>();
        interaction.microwave = microwave;
        interaction.foodRest = rest.transform;
        interaction.reach = interaction.doorInteractor.reach;
        EditorSceneManager.CloseScene(flow, true);
        CheckScene(environment);
        EditorSceneManager.SaveScene(environment);
        AssetDatabase.SaveAssets();
        Debug.Log("ENVIRONMENT_CREATED microwave=" + microwave.transform.position + " counter=" + countertop.bounds.max.y);
        foreach (string name in new string[] { "Kitchen", "Kitchen Microwave", "Microwave Fridge Flow", "FridgeInteraction", "CabinetInteraction" })
        {
            Scene scene = EditorSceneManager.OpenScene("Assets/Scenes/" + name + ".unity");
            CheckScene(scene);
        }
        EditorSceneManager.OpenScene(destination);
        Debug.Log("PASS All six scenes open with intact scripts");
    }

    static GameObject CopyPrefab(GameObject source)
    {
        string path = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(source);
        GameObject copy = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
        PrefabUtility.SetPropertyModifications(copy, PrefabUtility.GetPropertyModifications(source));
        copy.transform.localScale = source.transform.localScale;
        return copy;
    }

    static Bounds BoundsOf(GameObject root)
    {
        Renderer[] renderers = root.GetComponentsInChildren<Renderer>();
        Bounds bounds = renderers[0].bounds;
        foreach (Renderer renderer in renderers) if (!(renderer is ParticleSystemRenderer)) bounds.Encapsulate(renderer.bounds);
        return bounds;
    }

    public static void CheckScene(Scene scene)
    {
        foreach (GameObject root in scene.GetRootGameObjects())
        {
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
            {
                if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) > 0) throw new System.Exception("Missing script in " + scene.name + ": " + child.name);
                foreach (Component component in child.GetComponents<Component>())
                {
                    if (component == null) continue;
                    SerializedObject serialized = new SerializedObject(component);
                    SerializedProperty property = serialized.GetIterator();
                    while (property.NextVisible(true))
                    {
                        if (property.propertyType == SerializedPropertyType.ObjectReference && property.objectReferenceValue == null && property.objectReferenceInstanceIDValue != 0) throw new System.Exception("Broken reference in " + scene.name + ": " + child.name + "." + property.propertyPath);
                    }
                }
            }
        }
    }
}
