using System;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using Leftovers;

public class UseDownloadedMicrowave
{
    [Serializable]
    public class MeshPart
    {
        public string name;
        public Vector3[] vertices;
        public Vector3[] normals;
        public Vector2[] uv;
        public int[] triangles;
    }

    [Serializable]
    public class MeshPackage
    {
        public MeshPart[] meshes;
        public float[] hinge;
        public float[] turntable;
    }

    static string folder = "Assets/Models/Microwave/Downloaded";
    static MeshPackage package;
    static Material oven;
    static Material doorGlass;
    static Material steamMaterial;

    public static void Run()
    {
        package = JsonUtility.FromJson<MeshPackage>(File.ReadAllText(folder + "/PreparedMeshes.json"));
        foreach (MeshPart data in package.meshes)
        {
            Mesh mesh = MakeMesh(data);
            AssetDatabase.CreateAsset(mesh, folder + "/" + data.name + ".asset");
        }
        PrepareTextures();
        oven = new Material(Shader.Find("Standard"));
        oven.name = "Downloaded microwave enamel";
        oven.mainTexture = Texture("Microwave_Albedo.png");
        oven.SetTexture("_BumpMap", Texture("Microwave_Normal.png"));
        oven.SetFloat("_BumpScale", .45f);
        oven.EnableKeyword("_NORMALMAP");
        oven.SetTexture("_MetallicGlossMap", Texture("Microwave_MetalSmooth.png"));
        oven.EnableKeyword("_METALLICGLOSSMAP");
        oven.SetFloat("_GlossMapScale", .7f);
        AssetDatabase.CreateAsset(oven, folder + "/Microwave_Enamel.mat");
        doorGlass = new Material(Shader.Find("Standard"));
        doorGlass.name = "Microwave window";
        doorGlass.mainTexture = Texture("Microwave_Glass.png");
        doorGlass.color = new Color(1f, 1f, 1f, .55f);
        doorGlass.SetFloat("_Glossiness", .22f);
        Transparent(doorGlass);
        AssetDatabase.CreateAsset(doorGlass, folder + "/Microwave_Window.mat");
        MakeSteamMaterial();
        ReplaceMicrowave();
        ReplaceFood();
        AssetDatabase.SaveAssets();
        Debug.Log("DOWNLOADED_MICROWAVE_READY");
    }

    static void PrepareTextures()
    {
        TextureImporter normal = (TextureImporter)AssetImporter.GetAtPath(folder + "/Microwave_Normal.png");
        normal.textureType = TextureImporterType.NormalMap;
        normal.maxTextureSize = 2048;
        normal.SaveAndReimport();
        TextureImporter opacity = (TextureImporter)AssetImporter.GetAtPath(folder + "/Microwave_Opacity.png");
        opacity.isReadable = true;
        opacity.sRGBTexture = false;
        opacity.SaveAndReimport();
        TextureImporter metal = (TextureImporter)AssetImporter.GetAtPath(folder + "/Microwave_MetalSmooth.png");
        metal.sRGBTexture = false;
        metal.SaveAndReimport();
        TextureImporter steam = (TextureImporter)AssetImporter.GetAtPath(folder + "/SteamWisps.png");
        steam.alphaIsTransparency = true;
        steam.wrapMode = TextureWrapMode.Clamp;
        steam.textureCompression = TextureImporterCompression.Uncompressed;
        steam.SaveAndReimport();
    }

    static void ReplaceMicrowave()
    {
        string path = "Assets/Prefabs/Microwave_Interactive.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        Microwave microwave = root.GetComponent<Microwave>();
        Transform body = root.transform.Find("Microwave model");
        Transform hinge = microwave.door.transform;
        Transform display = microwave.display.transform;
        Transform recess = body.Find("Timer recess");
        for (int i = body.childCount - 1; i >= 0; i--)
        {
            Transform part = body.GetChild(i);
            if (part == hinge || part == display || part == recess || part == microwave.turntable || part == microwave.insideLight.transform || part == microwave.lamp.transform || part == microwave.chamberSteam.transform)
            {
                continue;
            }
            UnityEngine.Object.DestroyImmediate(part.gameObject);
        }
        GameObject shell = Model(body, "Downloaded microwave body", "Oven", oven);
        MeshCollider shellCollider = shell.AddComponent<MeshCollider>();
        shellCollider.sharedMesh = shell.GetComponent<MeshFilter>().sharedMesh;
        hinge.localPosition = new Vector3(package.hinge[0], package.hinge[1], package.hinge[2]);
        FridgeDoorHandle[] handles = hinge.GetComponentsInChildren<FridgeDoorHandle>();
        for (int i = hinge.childCount - 1; i >= 0; i--)
        {
            Transform part = hinge.GetChild(i);
            if (part.GetComponent<FridgeDoorHandle>() != null || part.GetComponent<AudioSource>() != null)
            {
                continue;
            }
            UnityEngine.Object.DestroyImmediate(part.gameObject);
        }
        Mesh doorMesh = AssetDatabase.LoadAssetAtPath<Mesh>(folder + "/Door.asset");
        MeshPart doorData = null;
        foreach (MeshPart part in package.meshes)
        {
            if (part.name == "Door") doorData = part;
        }
        SplitDoorMaterials(doorMesh, doorData);
        GameObject door = Model(hinge, "Downloaded door", "Door", oven);
        door.GetComponent<Renderer>().sharedMaterials = new Material[] { oven, doorGlass };
        BoxCollider doorCollider = hinge.GetComponent<BoxCollider>();
        doorCollider.center = doorMesh.bounds.center;
        doorCollider.size = doorMesh.bounds.size;
        foreach (FridgeDoorHandle handle in handles)
        {
            MeshRenderer renderer = handle.GetComponent<MeshRenderer>();
            MeshFilter filter = handle.GetComponent<MeshFilter>();
            if (renderer != null) UnityEngine.Object.DestroyImmediate(renderer);
            if (filter != null) UnityEngine.Object.DestroyImmediate(filter);
            float z = .015f;
            if (handle.name == "Inside handle") z = -.075f;
            handle.transform.localPosition = new Vector3(-.472f, .01f, z);
            BoxCollider grip = handle.GetComponent<BoxCollider>();
            grip.size = new Vector3(.075f, .32f, .055f);
        }
        display.localPosition = new Vector3(-.2703f, .3455f, .237f);
        recess.localPosition = new Vector3(-.2703f, .3455f, .229f);
        for (int i = microwave.turntable.childCount - 1; i >= 0; i--)
        {
            UnityEngine.Object.DestroyImmediate(microwave.turntable.GetChild(i).gameObject);
        }
        microwave.turntable.localPosition = new Vector3(package.turntable[0], package.turntable[1], package.turntable[2]);
        GameObject turntable = Model(microwave.turntable, "Downloaded glass turntable", "Turntable", oven);
        BoxCollider trayCollider = turntable.AddComponent<BoxCollider>();
        trayCollider.center = turntable.GetComponent<MeshFilter>().sharedMesh.bounds.center;
        trayCollider.size = turntable.GetComponent<MeshFilter>().sharedMesh.bounds.size;
        microwave.door.oneShotSource.clip = null;
        microwave.door.openingClips = new AudioClip[] { Clip("Open") };
        microwave.door.closingClips = new AudioClip[] { Clip("Close") };
        microwave.door.slamClips = new AudioClip[] { Clip("Slam") };
        microwave.door.movementLoop = Clip("Movement");
        microwave.cookingSound.clip = Clip("Cooking");
        microwave.cookingSound.volume = .7f;
        microwave.insideLight.intensity = .95f;
        microwave.startBeep = Clip("Start");
        microwave.beep = Clip("Beep");
        microwave.steamSeconds = 2.4f;
        microwave.steamRate = 110;
        microwave.chamberSteam.transform.localPosition = new Vector3(.06f, .245f, .145f);
        Steam(microwave.chamberSteam, false);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    static void SplitDoorMaterials(Mesh mesh, MeshPart data)
    {
        Texture2D opacity = Texture("Microwave_Opacity.png");
        System.Collections.Generic.List<int> frame = new System.Collections.Generic.List<int>();
        System.Collections.Generic.List<int> glass = new System.Collections.Generic.List<int>();
        for (int i = 0; i < data.triangles.Length; i += 3)
        {
            Vector2 uv = (data.uv[data.triangles[i]] + data.uv[data.triangles[i + 1]] + data.uv[data.triangles[i + 2]]) / 3;
            float alpha = opacity.GetPixelBilinear(uv.x, uv.y).r;
            System.Collections.Generic.List<int> target = frame;
            if (alpha < .9f) target = glass;
            for (int j = 0; j < 3; j++) target.Add(data.triangles[i + j]);
        }
        mesh.subMeshCount = 2;
        mesh.SetTriangles(frame, 0);
        mesh.SetTriangles(glass, 1);
        EditorUtility.SetDirty(mesh);
        Debug.Log("Door frame triangles " + frame.Count / 3 + ", glass " + glass.Count / 3);
    }

    static void ReplaceFood()
    {
        string path = "Assets/Prefabs/Sandwich_Test.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        MicrowaveFood food = root.GetComponent<MicrowaveFood>();
        for (int i = root.transform.childCount - 1; i >= 0; i--)
        {
            Transform part = root.transform.GetChild(i);
            if (part == food.steam.transform) continue;
            UnityEngine.Object.DestroyImmediate(part.gameObject);
        }
        Material plate = new Material(Shader.Find("Standard"));
        plate.name = "Downloaded plate ceramic";
        plate.color = new Color(.86f, .9f, .88f);
        plate.SetFloat("_Glossiness", .5f);
        AssetDatabase.CreateAsset(plate, folder + "/Plate_Ceramic.mat");
        Model(root.transform, "Downloaded ceramic plate", "CeramicPlate", plate);
        Material sandwich = new Material(Shader.Find("Standard"));
        sandwich.name = "Downloaded sandwich";
        sandwich.mainTexture = Texture("Sandwich_Albedo.png");
        sandwich.SetFloat("_Glossiness", .12f);
        AssetDatabase.CreateAsset(sandwich, folder + "/Sandwich.mat");
        Material cooked = new Material(sandwich);
        cooked.name = "Cooked sandwich";
        cooked.color = new Color(1f, .95f, .87f);
        AssetDatabase.CreateAsset(cooked, folder + "/Sandwich_Cooked.mat");
        GameObject filling = Model(root.transform, "Downloaded sandwich", "Sandwich", sandwich);
        filling.transform.localPosition = new Vector3(0, .018f, 0);
        food.bread = filling.GetComponent<Renderer>();
        food.cookedMaterial = cooked;
        food.steam.transform.localPosition = new Vector3(0, .104f, 0);
        Steam(food.steam, true);
        BoxCollider pickup = root.GetComponent<BoxCollider>();
        pickup.center = new Vector3(0, .055f, 0);
        pickup.size = new Vector3(.25f, .115f, .25f);
        PrefabUtility.SaveAsPrefabAsset(root, path);
        PrefabUtility.UnloadPrefabContents(root);
    }

    static void Steam(ParticleSystem steam, bool food)
    {
        steam.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = steam.main;
        main.startSize3D = true;
        main.startSizeX = new ParticleSystem.MinMaxCurve(.055f, .085f);
        main.startSizeY = new ParticleSystem.MinMaxCurve(.12f, .2f);
        main.startSizeZ = 1;
        main.startRotation = new ParticleSystem.MinMaxCurve(-.16f, .16f);
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.8f, 2.8f);
        main.startColor = new Color(.94f, .96f, .98f, .34f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(.08f, .15f);
        main.maxParticles = 440;
        ParticleSystem.EmissionModule emission = steam.emission;
        emission.rateOverTime = 32;
        ParticleSystem.ShapeModule shape = steam.shape;
        shape.scale = new Vector3(.34f, .09f, .018f);
        if (food) shape.scale = new Vector3(.12f, .08f, .01f);
        ParticleSystem.ForceOverLifetimeModule buoyancy = steam.forceOverLifetime;
        buoyancy.y = .14f;
        ParticleSystem.NoiseModule noise = steam.noise;
        noise.separateAxes = true;
        noise.strengthX = .035f;
        noise.strengthY = .018f;
        noise.strengthZ = .035f;
        noise.frequency = .8f;
        noise.scrollSpeed = .12f;
        noise.quality = ParticleSystemNoiseQuality.High;
        ParticleSystem.TextureSheetAnimationModule animation = steam.textureSheetAnimation;
        animation.enabled = true;
        animation.numTilesX = 4;
        animation.numTilesY = 4;
        animation.animation = ParticleSystemAnimationType.WholeSheet;
        animation.frameOverTime = new ParticleSystem.MinMaxCurve(1, AnimationCurve.Linear(0, 0, 1, 1));
        animation.startFrame = new ParticleSystem.MinMaxCurve(0, 1);
        ParticleSystemRenderer renderer = steam.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.VerticalBillboard;
        renderer.sharedMaterial = steamMaterial;
        renderer.sortMode = ParticleSystemSortMode.Distance;
    }

    static void MakeSteamMaterial()
    {
        steamMaterial = new Material(Shader.Find("Particles/Standard Unlit"));
        steamMaterial.name = "Animated steam wisps";
        steamMaterial.mainTexture = Texture("SteamWisps.png");
        Transparent(steamMaterial);
        AssetDatabase.CreateAsset(steamMaterial, folder + "/SteamWisps.mat");
    }

    static GameObject Model(Transform parent, string name, string meshName, Material material)
    {
        GameObject model = new GameObject(name);
        model.transform.SetParent(parent, false);
        model.AddComponent<MeshFilter>().sharedMesh = AssetDatabase.LoadAssetAtPath<Mesh>(folder + "/" + meshName + ".asset");
        model.AddComponent<MeshRenderer>().sharedMaterial = material;
        return model;
    }

    static Mesh MakeMesh(MeshPart data)
    {
        Mesh mesh = new Mesh();
        mesh.name = data.name;
        mesh.vertices = data.vertices;
        mesh.normals = data.normals;
        mesh.uv = data.uv;
        mesh.triangles = data.triangles;
        mesh.RecalculateBounds();
        mesh.RecalculateTangents();
        return mesh;
    }

    static Texture2D Texture(string name)
    {
        return AssetDatabase.LoadAssetAtPath<Texture2D>(folder + "/" + name);
    }

    static AudioClip Clip(string name)
    {
        return AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Microwave/Microwave_" + name + ".wav");
    }

    static void Transparent(Material material)
    {
        material.SetFloat("_Mode", 2);
        material.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
        material.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
        material.SetInt("_ZWrite", 0);
        material.EnableKeyword("_ALPHABLEND_ON");
        material.SetOverrideTag("RenderType", "Transparent");
        material.renderQueue = 3000;
    }
}
