using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Leftovers;

public class FixApplianceDoors
{
    static string folder = "Assets/Models/Microwave/Downloaded";

    public static void Run()
    {
        FixMicrowave();
        FixFridge();
        FixFood();
        LayoutKitchen();
        AssetDatabase.SaveAssets();
        Debug.Log("APPLIANCE_CORRECTIONS_READY");
    }

    static void FixMicrowave()
    {
        string path = "Assets/Prefabs/Microwave_Interactive.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        Microwave microwave = root.GetComponent<Microwave>();
        Transform body = root.transform.Find("Microwave model");
        Transform hinge = microwave.door.transform;
        Mesh frame = hinge.Find("Downloaded door").GetComponent<MeshFilter>().sharedMesh;
        Mesh shell = body.Find("Downloaded microwave body").GetComponent<MeshFilter>().sharedMesh;
        Vector3 position = hinge.localPosition;
        float overhang = position.x + frame.bounds.max.x - shell.bounds.max.x;
        position.x -= overhang;
        hinge.localPosition = position;
        Debug.Log("Door alignment corrected by " + overhang + " metres");
        Material handleMaterial = new Material(Shader.Find("Standard"));
        handleMaterial.name = "Microwave handle enamel";
        handleMaterial.color = new Color(.7f, .72f, .68f);
        handleMaterial.SetFloat("_Metallic", .08f);
        handleMaterial.SetFloat("_Glossiness", .26f);
        AssetDatabase.CreateAsset(handleMaterial, folder + "/HandleEnamel.mat");
        Mesh source = HandleMesh();
        Transform handle = hinge.Find("Handle");
        handle.localPosition = new Vector3(-.472f, .012f, .06f);
        Mesh visual = ResizeMesh(source, new Vector3(.027f, .25f, .034f), "Minimal handle");
        handle.gameObject.AddComponent<MeshFilter>().sharedMesh = visual;
        handle.gameObject.AddComponent<MeshRenderer>().sharedMaterial = handleMaterial;
        BoxCollider grip = handle.GetComponent<BoxCollider>();
        grip.size = new Vector3(.07f, .29f, .07f);
        BoxCollider solid = handle.gameObject.AddComponent<BoxCollider>();
        solid.size = visual.bounds.size;
        for (int i = 0; i < 2; i++)
        {
            GameObject mount = new GameObject("Handle mount " + i);
            mount.transform.SetParent(hinge, false);
            float y = -.092f;
            if (i == 1) y = .116f;
            mount.transform.localPosition = new Vector3(-.472f, y, .025f);
            Mesh mesh = ResizeMesh(source, new Vector3(.03f, .026f, .055f), "Handle mount " + i);
            mount.AddComponent<MeshFilter>().sharedMesh = mesh;
            mount.AddComponent<MeshRenderer>().sharedMaterial = handleMaterial;
            mount.AddComponent<BoxCollider>().size = mesh.bounds.size;
        }
        Material glass = AssetDatabase.LoadAssetAtPath<Material>(folder + "/Microwave_Window.mat");
        glass.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/Microwave_Albedo.png");
        glass.color = new Color(.55f, .6f, .58f, .36f);
        glass.SetFloat("_Glossiness", .3f);
        EditorUtility.SetDirty(glass);
        AddHoneycomb(hinge, frame);
        microwave.startDelay = .22f;
        microwave.door.closingVolume = 1.8f;
        microwave.steamRate = 32;
        LowPolySteam(microwave.chamberSteam, false);
        SetupDoor(microwave.door, root.transform);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    static void AddHoneycomb(Transform hinge, Mesh source)
    {
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(folder + "/Honeycomb.png");
        importer.alphaIsTransparency = true;
        importer.wrapMode = TextureWrapMode.Repeat;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();
        Material screen = new Material(Shader.Find("Unlit/Transparent"));
        screen.name = "Microwave honeycomb screen";
        screen.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/Honeycomb.png");
        screen.renderQueue = 3001;
        AssetDatabase.CreateAsset(screen, folder + "/Honeycomb.mat");
        Mesh mesh = new Mesh();
        mesh.name = "Microwave honeycomb screen";
        Vector3[] vertices = source.vertices;
        Vector3[] normals = source.normals;
        Vector2[] uv = new Vector2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] += Vector3.forward * .001f;
            uv[i] = new Vector2(vertices[i].x / .0554f, vertices[i].y / .048f);
        }
        List<int> indices = new List<int>();
        int[] glass = source.GetTriangles(1);
        for (int i = 0; i < glass.Length; i += 3)
        {
            if (normals[glass[i]].z < .5f) continue;
            indices.Add(glass[i]);
            indices.Add(glass[i + 1]);
            indices.Add(glass[i + 2]);
        }
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.uv = uv;
        mesh.triangles = indices.ToArray();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, folder + "/HoneycombScreen.asset");
        GameObject window = new GameObject("Honeycomb window screen");
        window.transform.SetParent(hinge, false);
        window.AddComponent<MeshFilter>().sharedMesh = mesh;
        window.AddComponent<MeshRenderer>().sharedMaterial = screen;
        Debug.Log("Honeycomb screen triangles " + indices.Count / 3);
    }

    static void FixFridge()
    {
        string path = "Assets/Prefabs/Fridge_Interactive.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        foreach (FridgeDoor door in root.GetComponentsInChildren<FridgeDoor>())
        {
            Bounds bounds = door.GetComponent<MeshFilter>().sharedMesh.bounds;
            BoxCollider panel = door.gameObject.AddComponent<BoxCollider>();
            panel.center = bounds.center;
            panel.size = bounds.size - Vector3.one * .006f;
            door.GetComponent<MeshCollider>().enabled = false;
            SetupDoor(door, root.transform);
        }
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    static void SetupDoor(FridgeDoor door, Transform root)
    {
        DoorCollision collision = door.gameObject.AddComponent<DoorCollision>();
        collision.door = door;
        collision.applianceRoot = root;
        List<BoxCollider> parts = new List<BoxCollider>();
        foreach (BoxCollider part in door.GetComponentsInChildren<BoxCollider>())
        {
            if (!part.isTrigger) parts.Add(part);
        }
        collision.parts = parts.ToArray();
        door.collision = collision;
        Rigidbody body = door.GetComponent<Rigidbody>();
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
    }

    static void FixFood()
    {
        string path = "Assets/Prefabs/Sandwich_Test.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        BoxCollider plate = root.GetComponent<BoxCollider>();
        plate.center = new Vector3(0, .0125f, 0);
        plate.size = new Vector3(.248f, .025f, .248f);
        GameObject foodBody = new GameObject("Sandwich solid collider");
        foodBody.transform.SetParent(root.transform, false);
        foodBody.transform.localPosition = new Vector3(0, .062f, 0);
        foodBody.AddComponent<BoxCollider>().size = new Vector3(.19f, .09f, .14f);
        Rigidbody body = root.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.mass = .7f;
        body.drag = .25f;
        body.angularDrag = 3f;
        body.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
        LowPolySteam(root.GetComponent<MicrowaveFood>().steam, true);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    static void LowPolySteam(ParticleSystem steam, bool food)
    {
        steam.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = steam.main;
        main.startSize3D = true;
        main.startSizeX = new ParticleSystem.MinMaxCurve(.035f, .06f);
        main.startSizeY = new ParticleSystem.MinMaxCurve(.05f, .085f);
        main.startSizeZ = new ParticleSystem.MinMaxCurve(.035f, .06f);
        main.startRotation3D = true;
        main.startRotationX = new ParticleSystem.MinMaxCurve(0, 6.28f);
        main.startRotationY = new ParticleSystem.MinMaxCurve(0, 6.28f);
        main.startRotationZ = new ParticleSystem.MinMaxCurve(0, 6.28f);
        main.startColor = new Color(.95f, .98f, 1, .38f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.3f, 2.2f);
        main.maxParticles = 150;
        ParticleSystem.EmissionModule emission = steam.emission;
        emission.rateOverTime = 12;
        ParticleSystem.TextureSheetAnimationModule animation = steam.textureSheetAnimation;
        animation.enabled = false;
        ParticleSystemRenderer renderer = steam.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.mesh = AssetDatabase.LoadAssetAtPath<Mesh>("Assets/Effects/FridgePuff.asset");
        renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Effects/FridgePuff.mat");
    }

    static void LayoutKitchen()
    {
        EditorSceneManager.OpenScene("Assets/Scenes/Kitchen.unity");
        Microwave microwave = UnityEngine.Object.FindObjectOfType<Microwave>();
        microwave.transform.position = new Vector3(2.2f, 1.64f, 0);
        GameObject cabinet = GameObject.Find("Microwave counter");
        foreach (Transform part in cabinet.transform)
        {
            Vector3 position = part.localPosition;
            position.x -= 2.95f;
            part.localPosition = position;
            MeshFilter filter = part.GetComponent<MeshFilter>();
            if (part.name == "Countertop" || part.name == "Cabinet")
            {
                float width = 2.3f;
                if (part.name == "Cabinet") width = 2.1f;
                Vector3 size = filter.sharedMesh.bounds.size;
                size.x = width;
                filter.sharedMesh = ResizeMesh(filter.sharedMesh, size, part.name + " narrow");
                part.GetComponent<BoxCollider>().size = size;
            }
        }
        cabinet.transform.position = new Vector3(2.2f, 0, 0);
        Transform fridge = null;
        foreach (FridgeDoor door in UnityEngine.Object.FindObjectsOfType<FridgeDoor>())
        {
            if (door != microwave.door) fridge = door.transform.root;
        }
        fridge.position = new Vector3(-2.2f, 0, 0);
        Physics.SyncTransforms();
        Vector3 shelf = fridge.TransformPoint(new Vector3(0, 1.12f, 0));
        RaycastHit[] hits = Physics.RaycastAll(shelf, Vector3.down, 2.5f, ~0, QueryTriggerInteraction.Ignore);
        float distance = float.MaxValue;
        Vector3 point = fridge.TransformPoint(new Vector3(0, .85f, 0));
        foreach (RaycastHit hit in hits)
        {
            if (!hit.collider.transform.IsChildOf(fridge) || hit.collider.GetComponentInParent<FridgeDoor>() != null) continue;
            if (hit.distance >= distance) continue;
            distance = hit.distance;
            point = hit.point + Vector3.up * .003f;
        }
        Transform rest = GameObject.Find("Sandwich resting spot").transform;
        rest.SetParent(fridge, true);
        rest.position = point;
        rest.localRotation = Quaternion.identity;
        rest.localScale = Vector3.one * 1.3f;
        Camera camera = Camera.main;
        camera.transform.position = new Vector3(0, 2.85f, 8.5f);
        camera.transform.LookAt(new Vector3(0, 1.9f, 0));
        camera.fieldOfView = 42;
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        Debug.Log("Food rests inside fridge at " + point);
    }

    static Mesh HandleMesh()
    {
        foreach (string guid in AssetDatabase.FindAssets("t:Mesh", new string[] { "Assets/Models/Microwave" }))
        {
            Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(AssetDatabase.GUIDToAssetPath(guid));
            if (mesh == null) continue;
            if (Vector3.Distance(mesh.bounds.size, new Vector3(.025f, .207f, .034f)) < .001f) return mesh;
        }
        throw new Exception("Rounded handle source mesh not found");
    }

    static Mesh ResizeMesh(Mesh source, Vector3 size, string name)
    {
        Mesh mesh = UnityEngine.Object.Instantiate(source);
        mesh.name = name;
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Vector3 scale = new Vector3(size.x / source.bounds.size.x, size.y / source.bounds.size.y, size.z / source.bounds.size.z);
        for (int i = 0; i < vertices.Length; i++)
        {
            vertices[i] = Vector3.Scale(vertices[i], scale);
            normals[i] = new Vector3(normals[i].x / scale.x, normals[i].y / scale.y, normals[i].z / scale.z).normalized;
        }
        mesh.vertices = vertices;
        mesh.normals = normals;
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, folder + "/" + name.Replace(" ", "_") + ".asset");
        return mesh;
    }
}
