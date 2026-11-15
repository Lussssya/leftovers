#if UNITY_EDITOR
using System.IO;
using Leftovers;
using UnityEditor;
using UnityEngine;

public static class RequestIconBake
{
    static void AddOutline(Texture2D image)
    {
        Color32[] pixels = image.GetPixels32();
        Color32[] result = (Color32[])pixels.Clone();
        int width = image.width;
        int height = image.height;
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                if (pixels[index].a > 0)
                {
                    continue;
                }
                int closest = 1000;
                for (int offsetY = -9; offsetY <= 9; offsetY++)
                {
                    for (int offsetX = -9; offsetX <= 9; offsetX++)
                    {
                        int sampleX = x + offsetX;
                        int sampleY = y + offsetY;
                        if (sampleX < 0 || sampleX >= width || sampleY < 0 || sampleY >= height)
                        {
                            continue;
                        }
                        int distance = offsetX * offsetX + offsetY * offsetY;
                        if (distance < closest && pixels[sampleY * width + sampleX].a > 128)
                        {
                            closest = distance;
                        }
                    }
                }
                if (closest <= 36)
                {
                    result[index] = new Color32(248, 229, 205, 255);
                }
                else if (closest <= 81)
                {
                    result[index] = new Color32(40, 24, 16, 255);
                }
            }
        }
        image.SetPixels32(result);
        image.Apply();
    }

    public static void Bake()
    {
        ProductInteractable[] products = Object.FindObjectsOfType<ProductInteractable>(true);
        string folder = "Assets/Resources/RequestPanel";
        Directory.CreateDirectory(folder);
        foreach (ProductInteractable product in products)
        {
            string name = product.DisplayName.Trim();
            string path = folder + "/" + name + ".png";
            if (File.Exists(path))
            {
                continue;
            }
            GameObject model = Object.Instantiate(product.gameObject);
            model.SetActive(true);
            model.transform.SetParent(null);
            model.transform.position = Vector3.zero;
            model.transform.rotation = Quaternion.identity;
            MonoBehaviour[] scripts = model.GetComponentsInChildren<MonoBehaviour>(true);
            foreach (MonoBehaviour script in scripts)
            {
                Object.DestroyImmediate(script);
            }
            Transform[] children = model.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                child.gameObject.layer = 31;
            }
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers)
            {
                bounds.Encapsulate(renderer.bounds);
                Material[] materials = renderer.sharedMaterials;
                for (int i = 0; i < materials.Length; i++)
                {
                    Material original = materials[i];
                    if (original.mainTexture != null)
                    {
                        Material iconMaterial = new Material(Shader.Find("Unlit/Texture"));
                        iconMaterial.mainTexture = original.mainTexture;
                        materials[i] = iconMaterial;
                    }
                    else
                    {
                        Material iconMaterial = new Material(Shader.Find("Unlit/Color"));
                        iconMaterial.color = original.color;
                        materials[i] = iconMaterial;
                    }
                }
                renderer.sharedMaterials = materials;
                renderer.SetPropertyBlock(null);
            }
            GameObject cameraObject = new GameObject("Request icon camera");
            Camera camera = cameraObject.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = Color.clear;
            camera.cullingMask = 1 << 31;
            camera.orthographic = true;
            float size = Mathf.Max(bounds.size.x, bounds.size.y, bounds.size.z);
            Vector3 direction = new Vector3(0.6f, 0.8f, -1f);
            if (name == "Toast")
            {
                direction = new Vector3(0.15f, 1f, -0.25f);
                if (bounds.size.z < bounds.size.y)
                {
                    direction = new Vector3(0.15f, 0.25f, -1f);
                }
            }
            camera.transform.position = bounds.center + direction.normalized * size * 4f;
            camera.transform.LookAt(bounds.center);
            camera.orthographicSize = size * 0.68f;
            camera.nearClipPlane = size * 0.05f;
            camera.farClipPlane = size * 10f;
            GameObject lightObject = new GameObject("Request icon light");
            Light light = lightObject.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.1f;
            light.cullingMask = 1 << 31;
            light.transform.rotation = Quaternion.Euler(40f, -35f, 0f);
            RenderTexture target = new RenderTexture(256, 256, 24, RenderTextureFormat.ARGB32);
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            Texture2D image = new Texture2D(256, 256, TextureFormat.RGBA32, false);
            image.ReadPixels(new Rect(0f, 0f, 256f, 256f), 0, 0);
            image.Apply();
            AddOutline(image);
            File.WriteAllBytes(path, image.EncodeToPNG());
            RenderTexture.active = null;
            camera.targetTexture = null;
            target.Release();
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(cameraObject);
            Object.DestroyImmediate(lightObject);
            Object.DestroyImmediate(model);
        }
        AssetDatabase.Refresh();
        string[] files = Directory.GetFiles(folder, "*.png");
        foreach (string path in files)
        {
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Default;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.SaveAndReimport();
        }
    }
}

#endif
