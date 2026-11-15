using System;
using System.IO;
using Leftovers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

public class CheckProductModels
{
    static string outputFolder = "Tools/ProductModels/Checks";

    public static void Run()
    {
        Directory.CreateDirectory(outputFolder);
        try
        {
            EditorSceneManager.OpenScene("Assets/Scenes/Kitchen.unity");
            ProductInteractable[] products = UnityEngine.Object.FindObjectsOfType<ProductInteractable>(true);
            Require(products.Length == 7, "Kitchen must contain seven product instances.");
            AssemblyTable table = UnityEngine.Object.FindObjectOfType<AssemblyTable>();
            Require(table != null, "Assembly table is missing.");
            Material sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/Models/Products/ProductColors.mat");
            Require(sharedMaterial != null && sharedMaterial.mainTexture != null, "Product material or color texture is missing.");
            Require(sharedMaterial.shader.isSupported, "Product shader is unsupported.");
            for (int i = 0; i < products.Length; i++)
            {
                CheckModel(products[i], sharedMaterial);
                CheckInteraction(products[i], table);
            }
            CaptureProducts(products, sharedMaterial);
            string result = "PASS: Unity loaded all seven product instances and the shared color texture.\n"
                + "PASS: Six mesh assets contain valid geometry and stay inside the original solid colliders.\n"
                + "PASS: All seven products support pickup, table placement, pickup again, and drop.\n"
                + "PASS: Drop restores the original parent and collider states.\n"
                + "PASS: Native Unity product render saved to unity-products.png.\n"
                + "These checks call the interaction methods in Edit mode. Manual first-person input remains untested.\n";
            File.WriteAllText(outputFolder + "/results.txt", result);
            Debug.Log(result);
            EditorApplication.Exit(0);
        }
        catch (Exception error)
        {
            File.WriteAllText(outputFolder + "/results.txt", error.ToString());
            Debug.LogException(error);
            EditorApplication.Exit(1);
        }
    }

    static void CheckModel(ProductInteractable product, Material material)
    {
        MeshFilter filter = product.GetComponent<MeshFilter>();
        MeshRenderer renderer = product.GetComponent<MeshRenderer>();
        BoxCollider collider = product.GetComponent<BoxCollider>();
        Require(filter != null && filter.sharedMesh != null, product.DisplayName + " mesh is missing.");
        Require(renderer != null && renderer.sharedMaterial == material, product.DisplayName + " material is incorrect.");
        Require(collider != null && !collider.isTrigger, product.DisplayName + " solid collider is missing.");
        Mesh mesh = filter.sharedMesh;
        Require(AssetDatabase.GetAssetPath(mesh).StartsWith("Assets/Models/Products/"), product.DisplayName + " still uses a placeholder.");
        Require(mesh.vertexCount > 0 && mesh.uv.Length == mesh.vertexCount, product.DisplayName + " has invalid vertex data.");
        Require(mesh.normals.Length == mesh.vertexCount, product.DisplayName + " normals are missing.");
        int[] indices = mesh.triangles;
        Require(indices.Length > 0 && indices.Length % 3 == 0, product.DisplayName + " has invalid triangle data.");
        for (int i = 0; i < indices.Length; i++)
        {
            Require(indices[i] >= 0 && indices[i] < mesh.vertexCount, product.DisplayName + " has an invalid vertex index.");
        }
        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        for (int i = 0; i < indices.Length; i += 3)
        {
            int a = indices[i];
            int b = indices[i + 1];
            int c = indices[i + 2];
            Vector3 faceNormal = Vector3.Cross(vertices[b] - vertices[a], vertices[c] - vertices[a]);
            Vector3 averageNormal = normals[a] + normals[b] + normals[c];
            Require(Vector3.Dot(faceNormal, averageNormal) >= -0.000001f, product.DisplayName + " has reversed faces.");
        }
        Bounds allowedBounds = new Bounds(collider.center, collider.size + Vector3.one * 0.001f);
        Require(allowedBounds.Contains(mesh.bounds.min) && allowedBounds.Contains(mesh.bounds.max), product.DisplayName + " extends beyond its collider.");
        Require(Mathf.Abs(mesh.bounds.min.y - (collider.center.y - collider.size.y * 0.5f)) < 0.001f, product.DisplayName + " no longer rests at the original bottom height.");
        Debug.Log("PASS model: " + product.DisplayName + ", triangles: " + indices.Length / 3);
    }

    static void CheckInteraction(ProductInteractable product, AssemblyTable table)
    {
        Transform originalParent = product.transform.parent;
        Vector3 originalPosition = product.transform.position;
        Quaternion originalRotation = product.transform.rotation;
        Collider[] colliders = product.GetComponentsInChildren<Collider>(true);
        bool[] enabledStates = new bool[colliders.Length];
        for (int i = 0; i < colliders.Length; i++)
        {
            enabledStates[i] = colliders[i].enabled;
        }
        GameObject holdObject = new GameObject("Product check hold point");
        holdObject.transform.position = originalPosition + Vector3.up;
        product.PickUp(holdObject.transform);
        Require(product.IsHeld, product.DisplayName + " pickup failed.");
        for (int i = 0; i < colliders.Length; i++)
        {
            Require(!colliders[i].enabled, product.DisplayName + " held collider remains enabled.");
        }
        Require(table.TryPlace(product) && !product.IsHeld, product.DisplayName + " table placement failed.");
        product.PickUp(holdObject.transform);
        Require(product.IsHeld, product.DisplayName + " second pickup failed.");
        product.Drop(originalPosition, originalRotation);
        Require(!product.IsHeld && product.transform.parent == originalParent, product.DisplayName + " drop did not restore the parent.");
        for (int i = 0; i < colliders.Length; i++)
        {
            Require(colliders[i].enabled == enabledStates[i], product.DisplayName + " drop did not restore collider state.");
        }
        UnityEngine.Object.DestroyImmediate(holdObject);
        Debug.Log("PASS interaction: " + product.DisplayName);
    }

    static void CaptureProducts(ProductInteractable[] products, Material material)
    {
        Light[] originalLights = UnityEngine.Object.FindObjectsOfType<Light>();
        for (int i = 0; i < originalLights.Length; i++)
        {
            originalLights[i].enabled = false;
        }
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.65f, 0.65f, 0.65f);
        string[] names = { "Ketchup", "Tomato", "Cheese", "Egg", "Toast", "Lettuce" };
        Vector3 origin = new Vector3(1000f, 0f, 1000f);
        for (int i = 0; i < names.Length; i++)
        {
            ProductInteractable source = null;
            for (int j = 0; j < products.Length; j++)
            {
                if (products[j].DisplayName == names[i])
                {
                    source = products[j];
                    break;
                }
            }
            Require(source != null, "Preview source is missing: " + names[i]);
            GameObject display = new GameObject(names[i] + " model preview");
            display.layer = 31;
            display.transform.position = origin + new Vector3((i % 3) * 0.8f, source.transform.localScale.y * 0.5f, (i / 3) * -0.9f);
            display.transform.localScale = source.transform.localScale;
            display.AddComponent<MeshFilter>().sharedMesh = source.GetComponent<MeshFilter>().sharedMesh;
            display.AddComponent<MeshRenderer>().sharedMaterial = material;
        }
        GameObject floor = GameObject.CreatePrimitive(PrimitiveType.Cube);
        floor.layer = 31;
        floor.transform.position = origin + new Vector3(0.8f, -0.031f, -0.45f);
        floor.transform.localScale = new Vector3(3f, 0.06f, 2.5f);
        Material floorMaterial = new Material(Shader.Find("Standard"));
        floorMaterial.color = new Color(0.78f, 0.78f, 0.75f);
        floorMaterial.SetFloat("_Glossiness", 0.05f);
        floor.GetComponent<MeshRenderer>().sharedMaterial = floorMaterial;
        Light light = new GameObject("Product preview light").AddComponent<Light>();
        light.type = LightType.Directional;
        light.intensity = 1.1f;
        light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
        light.cullingMask = 1 << 31;
        light.shadows = LightShadows.Soft;
        Camera camera = new GameObject("Product preview camera").AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.9f, 0.9f, 0.87f);
        camera.cullingMask = 1 << 31;
        camera.orthographic = true;
        camera.orthographicSize = 1.05f;
        camera.transform.position = origin + new Vector3(1.45f, 2.1f, -3.3f);
        camera.transform.LookAt(origin + new Vector3(0.8f, 0.08f, -0.4f));
        RenderTexture target = new RenderTexture(1500, 1000, 24);
        target.antiAliasing = 4;
        camera.targetTexture = target;
        camera.Render();
        RenderTexture.active = target;
        Texture2D image = new Texture2D(1500, 1000, TextureFormat.RGB24, false);
        image.ReadPixels(new Rect(0f, 0f, 1500f, 1000f), 0, 0);
        image.Apply();
        File.WriteAllBytes(outputFolder + "/unity-products.png", image.EncodeToPNG());
        RenderTexture.active = null;
        camera.targetTexture = null;
        target.Release();
        UnityEngine.Object.DestroyImmediate(target);
        UnityEngine.Object.DestroyImmediate(image);
    }

    static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(message);
        }
    }
}
