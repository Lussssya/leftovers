using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using Leftovers;

public class BuildMicrowave
{
    static string modelFolder = "Assets/Models/Microwave";
    static Material enamel;
    static Material trim;
    static Material interior;
    static Material dark;
    static Material glass;
    static Material steamMaterial;
    static Material lit;
    static Material off;
    static int meshNumber;

    public static void Run()
    {
        Folder("Assets/Models", "Microwave");
        enamel = Material("Warm enamel", new Color(.76f, .75f, .66f), .12f, .3f);
        trim = Material("Sage casing", new Color(.24f, .36f, .34f), .35f, .28f);
        interior = Material("Interior enamel", new Color(.54f, .57f, .53f), .3f, .25f);
        dark = Material("Dark trim", new Color(.035f, .044f, .043f), .18f, .22f);
        glass = Material("Door glass", new Color(.12f, .17f, .16f, .24f), .1f, .4f);
        Transparent(glass);
        lit = Material("Lamp on", new Color(1f, .78f, .4f), 0, .1f);
        lit.EnableKeyword("_EMISSION");
        lit.SetColor("_EmissionColor", new Color(1.5f, .85f, .3f));
        off = Material("Lamp off", new Color(.26f, .25f, .18f), 0, .1f);
        MakeSteamMaterial();
        GameObject microwave = MakeMicrowave();
        MirrorMicrowave(microwave);
        GameObject microwaveAsset = PrefabUtility.SaveAsPrefabAsset(microwave, "Assets/Prefabs/Microwave_Interactive.prefab");
        UnityEngine.Object.DestroyImmediate(microwave);
        GameObject food = MakeFood();
        GameObject foodAsset = PrefabUtility.SaveAsPrefabAsset(food, "Assets/Prefabs/Sandwich_Test.prefab");
        UnityEngine.Object.DestroyImmediate(food);
        EditorSceneManager.OpenScene("Assets/Scenes/Kitchen.unity");
        GameObject counter = new GameObject("Microwave counter");
        Part(counter.transform, "Countertop", new Vector3(2.95f, 1.56f, .03f), new Vector3(3.35f, .15f, 1.65f), enamel, .035f);
        Part(counter.transform, "Cabinet", new Vector3(2.95f, .76f, -.08f), new Vector3(3.12f, 1.48f, 1.25f), trim, .04f);
        Part(counter.transform, "Cabinet seam", new Vector3(2.95f, .8f, .553f), new Vector3(.012f, 1.3f, .012f), dark, .002f, false);
        Part(counter.transform, "Left cabinet handle", new Vector3(2.83f, 1.22f, .59f), new Vector3(.045f, .3f, .055f), dark, .015f);
        Part(counter.transform, "Right cabinet handle", new Vector3(3.07f, 1.22f, .59f), new Vector3(.045f, .3f, .055f), dark, .015f);
        GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(microwaveAsset);
        instance.transform.position = new Vector3(2.5f, 1.64f, 0f);
        instance.transform.localScale = Vector3.one * 2.6f;
        Microwave controller = instance.GetComponent<Microwave>();
        GameObject rest = new GameObject("Sandwich resting spot");
        rest.transform.position = new Vector3(4.08f, 1.65f, .29f);
        rest.transform.localScale = Vector3.one * 2.6f;
        GameObject testFood = (GameObject)PrefabUtility.InstantiatePrefab(foodAsset);
        testFood.transform.SetParent(rest.transform, false);
        Camera camera = Camera.main;
        camera.fieldOfView = 43f;
        camera.transform.LookAt(new Vector3(1.15f, 1.9f, .1f));
        MouseFoodInteractor pickup = camera.gameObject.AddComponent<MouseFoodInteractor>();
        pickup.interactionCamera = camera;
        pickup.doorInteractor = camera.GetComponent<MouseDoorInteractor>();
        pickup.microwave = controller;
        pickup.foodRest = rest.transform;
        EditorSceneManager.SaveScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
        AssetDatabase.SaveAssets();
        File.WriteAllText("microwave-build-passed.txt", "Generated microwave, sandwich, plate, sound, steam, and Kitchen integration.");
        Debug.Log("MICROWAVE_BUILD_PASSED");
    }

    static GameObject MakeMicrowave()
    {
        GameObject root = new GameObject("Microwave_Interactive");
        Microwave microwave = root.AddComponent<Microwave>();
        GameObject visuals = new GameObject("Microwave model");
        visuals.transform.SetParent(root.transform, false);
        Transform body = visuals.transform;
        Part(body, "Top shell", new Vector3(0, .365f, -.005f), new Vector3(.66f, .035f, .47f), trim, .014f);
        Part(body, "Left shell", new Vector3(-.315f, .19f, -.005f), new Vector3(.03f, .34f, .47f), trim, .012f);
        Part(body, "Rear shell", new Vector3(0, .19f, -.225f), new Vector3(.63f, .34f, .025f), trim, .009f);
        Part(body, "Base", new Vector3(0, .026f, -.005f), new Vector3(.65f, .04f, .46f), dark, .012f);
        Part(body, "Chamber floor", new Vector3(-.068f, .057f, -.005f), new Vector3(.475f, .02f, .42f), interior, .005f);
        Part(body, "Chamber ceiling", new Vector3(-.068f, .332f, -.005f), new Vector3(.475f, .02f, .42f), interior, .005f);
        Part(body, "Chamber back", new Vector3(-.068f, .195f, -.2f), new Vector3(.475f, .27f, .018f), interior, .005f);
        Part(body, "Chamber left", new Vector3(-.295f, .195f, 0), new Vector3(.015f, .27f, .41f), interior, .004f);
        Part(body, "Chamber right", new Vector3(.161f, .195f, 0), new Vector3(.015f, .27f, .41f), interior, .004f);
        Part(body, "Control housing", new Vector3(.252f, .19f, .005f), new Vector3(.154f, .335f, .455f), enamel, .018f);
        Part(body, "Control face", new Vector3(.252f, .19f, .242f), new Vector3(.146f, .328f, .018f), dark, .012f);
        for (int i = 0; i < 8; i++)
        {
            Part(body, "Side vent " + i, new Vector3(.331f, .15f + i * .018f, -.1f), new Vector3(.002f, .006f, .16f), dark, .001f, false);
        }
        for (int i = 0; i < 4; i++)
        {
            float x = -.27f;
            float z = -.16f;
            if (i % 2 == 1) x = .27f;
            if (i >= 2) z = .16f;
            Part(body, "Rubber foot " + i, new Vector3(x, .008f, z), new Vector3(.052f, .016f, .052f), dark, .005f);
        }
        Part(body, "Timer recess", new Vector3(.252f, .275f, .255f), new Vector3(.124f, .068f, .008f), dark, .007f, false);
        MakeDisplay(body, microwave);
        GameObject knob = Cylinder(body, "Power knob", new Vector3(.253f, .134f, .27f), new Vector3(.057f, .018f, .057f), enamel);
        knob.transform.localRotation = Quaternion.Euler(90, 0, 0);
        Part(body, "Knob indicator", new Vector3(.253f, .149f, .291f), new Vector3(.004f, .019f, .004f), dark, .001f, false);
        for (int i = 0; i < 7; i++)
        {
            float angle = (i - 3) * 32 * Mathf.Deg2Rad;
            Part(body, "Power tick " + i, new Vector3(.253f + Mathf.Sin(angle) * .043f, .134f + Mathf.Cos(angle) * .043f, .254f), new Vector3(.003f, .005f, .002f), enamel, .0005f, false);
        }
        GameObject hinge = new GameObject("Microwave door");
        hinge.transform.SetParent(body, false);
        hinge.transform.localPosition = new Vector3(-.307f, .195f, .232f);
        FridgeDoor door = hinge.AddComponent<FridgeDoor>();
        door.maximumAngle = 110f;
        door.slamSpeed = 100f;
        door.movementVolume = .08f;
        door.openingNoise = .6f;
        door.gentleCloseNoise = 1.1f;
        door.slamNoise = 20f;
        door.oneShotSource = Source(hinge, .8f);
        door.openingClips = new AudioClip[] { Clip("Open") };
        door.closingClips = new AudioClip[] { Clip("Close") };
        door.slamClips = new AudioClip[] { Clip("Slam") };
        GameObject movement = new GameObject("Door movement sound");
        movement.transform.SetParent(hinge.transform, false);
        door.movementSource = Source(movement, 0);
        door.movementLoop = Clip("Movement");
        Rigidbody rigidbody = hinge.AddComponent<Rigidbody>();
        rigidbody.isKinematic = true;
        rigidbody.useGravity = false;
        BoxCollider doorCollider = hinge.AddComponent<BoxCollider>();
        doorCollider.center = new Vector3(.237f, 0f, .007f);
        doorCollider.size = new Vector3(.474f, .328f, .033f);
        Part(hinge.transform, "Upper door frame", new Vector3(.237f, .145f, .01f), new Vector3(.478f, .04f, .041f), enamel, .012f, false);
        Part(hinge.transform, "Lower door frame", new Vector3(.237f, -.145f, .01f), new Vector3(.478f, .04f, .041f), enamel, .012f, false);
        Part(hinge.transform, "Hinge frame", new Vector3(.022f, 0, .01f), new Vector3(.047f, .27f, .041f), enamel, .01f, false);
        Part(hinge.transform, "Latch frame", new Vector3(.453f, 0, .01f), new Vector3(.047f, .27f, .041f), enamel, .01f, false);
        Part(hinge.transform, "Glass window", new Vector3(.237f, 0, .016f), new Vector3(.39f, .25f, .006f), glass, .008f, false);
        for (int i = 0; i < 11; i++)
        {
            Part(hinge.transform, "Window grille " + i, new Vector3(.06f + i * .035f, 0, .021f), new Vector3(.0012f, .246f, .001f), dark, .0001f, false);
        }
        GameObject handle = Part(hinge.transform, "Handle", new Vector3(.44f, 0, .062f), new Vector3(.025f, .207f, .034f), dark, .011f, false);
        FridgeDoorHandle grip = handle.AddComponent<FridgeDoorHandle>();
        grip.door = door;
        BoxCollider handleCollider = handle.AddComponent<BoxCollider>();
        handleCollider.isTrigger = true;
        handleCollider.size = new Vector3(.085f, .25f, .09f);
        GameObject innerGrip = new GameObject("Inside handle");
        innerGrip.transform.SetParent(hinge.transform, false);
        innerGrip.transform.localPosition = new Vector3(.44f, 0, -.023f);
        innerGrip.AddComponent<FridgeDoorHandle>().door = door;
        BoxCollider insideCollider = innerGrip.AddComponent<BoxCollider>();
        insideCollider.isTrigger = true;
        insideCollider.size = new Vector3(.08f, .25f, .04f);
        visuals.AddComponent<FridgeSlamEffect>().shakeAngle = .38f;
        GameObject turntable = new GameObject("Spinning turntable");
        turntable.transform.SetParent(body, false);
        turntable.transform.localPosition = new Vector3(-.069f, .076f, .01f);
        Material plateGlass = Material("Turntable glass", new Color(.42f, .55f, .51f), .08f, .55f);
        Cylinder(turntable.transform, "Glass turntable", Vector3.zero, new Vector3(.352f, .009f, .352f), plateGlass);
        Cylinder(body, "Turntable hub", new Vector3(-.069f, .068f, .01f), new Vector3(.04f, .013f, .04f), dark);
        GameObject lamp = Part(body, "Interior lamp", new Vector3(.15f, .265f, -.045f), new Vector3(.007f, .065f, .085f), off, .004f, false);
        GameObject light = new GameObject("Interior light");
        light.transform.SetParent(body, false);
        light.transform.localPosition = new Vector3(.085f, .26f, .03f);
        Light bulb = light.AddComponent<Light>();
        bulb.type = LightType.Point;
        bulb.color = new Color(1f, .78f, .46f);
        bulb.intensity = 1.7f;
        bulb.range = .65f;
        bulb.enabled = false;
        bulb.shadows = LightShadows.Soft;
        GameObject audio = new GameObject("Cooking sound");
        audio.transform.SetParent(root.transform, false);
        AudioSource cooking = Source(audio, .5f);
        cooking.clip = Clip("Cooking");
        cooking.loop = true;
        microwave.door = door;
        microwave.turntable = turntable.transform;
        microwave.insideLight = bulb;
        microwave.lamp = lamp.GetComponent<Renderer>();
        microwave.lampOn = lit;
        microwave.lampOff = off;
        microwave.cookingSound = cooking;
        microwave.beepSource = Source(root, .8f);
        microwave.beep = Clip("Beep");
        microwave.chamberSteam = Steam(body, "Chamber steam", new Vector3(-.069f, .21f, .175f), false);
        return root;
    }

    public static void MirrorMicrowave(GameObject root)
    {
        foreach (Transform part in root.GetComponentsInChildren<Transform>(true))
        {
            Vector3 position = part.localPosition;
            position.x = -position.x;
            part.localPosition = position;
            Quaternion rotation = part.localRotation;
            part.localRotation = new Quaternion(rotation.x, -rotation.y, -rotation.z, rotation.w);
            BoxCollider box = part.GetComponent<BoxCollider>();
            if (box != null)
            {
                Vector3 center = box.center;
                center.x = -center.x;
                box.center = center;
            }
        }
        root.GetComponent<Microwave>().door.openingDirection = 1f;
        root.GetComponent<Microwave>().insideLight.range = 1.6f;
    }

    static GameObject MakeFood()
    {
        GameObject root = new GameObject("Sandwich_Test");
        MicrowaveFood food = root.AddComponent<MicrowaveFood>();
        Material plate = Material("Ceramic plate", new Color(.84f, .89f, .83f), 0, .6f);
        Material crust = Material("Bread crust", new Color(.55f, .27f, .09f), 0, .06f);
        Material bread = Material("Bread", new Color(.91f, .72f, .4f), 0, .04f);
        Material toasted = Material("Toasted bread", new Color(.75f, .45f, .18f), 0, .06f);
        Material cheese = Material("Cheese", new Color(.97f, .67f, .08f), 0, .1f);
        Material lettuce = Material("Lettuce", new Color(.25f, .49f, .11f), 0, .05f);
        Material tomato = Material("Tomato", new Color(.7f, .09f, .045f), 0, .1f);
        GameObject dish = new GameObject("Ceramic plate");
        dish.transform.SetParent(root.transform, false);
        MeshFilter filter = dish.AddComponent<MeshFilter>();
        filter.sharedMesh = PlateMesh();
        dish.AddComponent<MeshRenderer>().sharedMaterial = plate;
        BoxCollider pick = root.AddComponent<BoxCollider>();
        pick.center = new Vector3(0, .045f, 0);
        pick.size = new Vector3(.24f, .1f, .24f);
        Part(root.transform, "Lower crust", new Vector3(0, .025f, 0), new Vector3(.15f, .025f, .14f), crust, .011f, false);
        Part(root.transform, "Lower bread", new Vector3(0, .03f, 0), new Vector3(.143f, .018f, .133f), bread, .008f, false);
        for (int i = 0; i < 5; i++)
        {
            GameObject leaf = Part(root.transform, "Lettuce leaf " + i, new Vector3((i - 2) * .025f, .045f, 0), new Vector3(.042f, .009f, .15f), lettuce, .004f, false);
            leaf.transform.localRotation = Quaternion.Euler(0, (i - 2) * 7, (i - 2) * 4);
        }
        Part(root.transform, "Cheese", new Vector3(0, .054f, 0), new Vector3(.16f, .007f, .14f), cheese, .003f, false).transform.localRotation = Quaternion.Euler(0, 9, 0);
        Cylinder(root.transform, "Tomato slice", new Vector3(0, .06f, -.01f), new Vector3(.11f, .008f, .11f), tomato, false);
        Part(root.transform, "Upper crust", new Vector3(0, .077f, 0), new Vector3(.15f, .026f, .14f), crust, .011f, false);
        GameObject top = Part(root.transform, "Upper bread", new Vector3(0, .084f, 0), new Vector3(.14f, .016f, .131f), bread, .007f, false);
        for (int i = 0; i < 6; i++)
        {
            Part(root.transform, "Toast mark " + i, new Vector3(-.043f + i * .017f, .093f, 0), new Vector3(.003f, .001f, .08f), crust, .0004f, false).transform.localRotation = Quaternion.Euler(0, 25, 0);
        }
        food.bread = top.GetComponent<Renderer>();
        food.cookedMaterial = toasted;
        food.steam = Steam(root.transform, "Food steam", new Vector3(0, .095f, 0), true);
        return root;
    }

    static void MakeDisplay(Transform parent, Microwave microwave)
    {
        Material digit = Material("Timer green", new Color(.21f, 1f, .61f), 0, 0);
        digit.EnableKeyword("_EMISSION");
        digit.SetColor("_EmissionColor", new Color(.15f, 1.7f, .65f));
        GameObject display = new GameObject("Digital timer 00 05");
        display.transform.SetParent(parent, false);
        display.transform.localPosition = new Vector3(.252f, .276f, .261f);
        MicrowaveDisplay timer = display.AddComponent<MicrowaveDisplay>();
        Digit(display.transform, -.044f, digit, "1111110");
        Digit(display.transform, -.022f, digit, "1111110");
        Part(display.transform, "Colon upper", new Vector3(-.005f, .007f, 0), new Vector3(.003f, .003f, .002f), digit, .0004f, false);
        Part(display.transform, "Colon lower", new Vector3(-.005f, -.007f, 0), new Vector3(.003f, .003f, .002f), digit, .0004f, false);
        timer.tens = Digit(display.transform, .012f, digit, "1111110");
        timer.ones = Digit(display.transform, .035f, digit, "1011011");
        microwave.display = timer;
    }

    static GameObject[] Digit(Transform parent, float x, Material material, string pattern)
    {
        GameObject[] segments = new GameObject[7];
        Vector2[] positions = { new Vector2(0, .019f), new Vector2(.008f, .01f), new Vector2(.008f, -.01f), new Vector2(0, -.019f), new Vector2(-.008f, -.01f), new Vector2(-.008f, .01f), Vector2.zero };
        for (int i = 0; i < 7; i++)
        {
            Vector3 size = new Vector3(.003f, .015f, .002f);
            if (i == 0 || i == 3 || i == 6) size = new Vector3(.013f, .003f, .002f);
            segments[i] = Part(parent, "Digit segment " + i, new Vector3(x + positions[i].x, positions[i].y, 0), size, material, .0006f, false);
            segments[i].SetActive(pattern[i] == '1');
        }
        return segments;
    }

    static ParticleSystem Steam(Transform parent, string name, Vector3 position, bool food)
    {
        GameObject cloud = new GameObject(name);
        cloud.transform.SetParent(parent, false);
        cloud.transform.localPosition = position;
        cloud.transform.localRotation = Quaternion.Euler(-30, 0, 0);
        if (food) cloud.transform.localRotation = Quaternion.Euler(-90, 0, 0);
        ParticleSystem particles = cloud.AddComponent<ParticleSystem>();
        particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        ParticleSystem.MainModule main = particles.main;
        main.loop = true;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.scalingMode = ParticleSystemScalingMode.Hierarchy;
        main.startLifetime = new ParticleSystem.MinMaxCurve(1.1f, 1.9f);
        main.startSize = new ParticleSystem.MinMaxCurve(.035f, .07f);
        main.startColor = new Color(.92f, .95f, .93f, .15f);
        main.startSpeed = new ParticleSystem.MinMaxCurve(.09f, .17f);
        main.maxParticles = 240;
        ParticleSystem.EmissionModule emission = particles.emission;
        emission.enabled = food;
        emission.rateOverTime = 9;
        ParticleSystem.ShapeModule shape = particles.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = new Vector3(.19f, .025f, .025f);
        if (food) shape.scale = new Vector3(.11f, .11f, .015f);
        ParticleSystem.ForceOverLifetimeModule buoyancy = particles.forceOverLifetime;
        buoyancy.enabled = true;
        buoyancy.space = ParticleSystemSimulationSpace.World;
        buoyancy.y = .18f;
        ParticleSystem.NoiseModule noise = particles.noise;
        noise.enabled = true;
        noise.strength = .055f;
        noise.frequency = .65f;
        noise.scrollSpeed = .18f;
        noise.damping = true;
        ParticleSystem.SizeOverLifetimeModule size = particles.sizeOverLifetime;
        size.enabled = true;
        size.size = new ParticleSystem.MinMaxCurve(1, new AnimationCurve(new Keyframe(0, .35f), new Keyframe(.4f, 1), new Keyframe(1, 2.1f)));
        Gradient fade = new Gradient();
        fade.SetKeys(new GradientColorKey[] { new GradientColorKey(Color.white, 0), new GradientColorKey(Color.white, 1) }, new GradientAlphaKey[] { new GradientAlphaKey(0, 0), new GradientAlphaKey(1, .15f), new GradientAlphaKey(.6f, .5f), new GradientAlphaKey(0, 1) });
        ParticleSystem.ColorOverLifetimeModule color = particles.colorOverLifetime;
        color.enabled = true;
        color.color = fade;
        ParticleSystem.CollisionModule collision = particles.collision;
        collision.enabled = true;
        collision.type = ParticleSystemCollisionType.World;
        collision.mode = ParticleSystemCollisionMode.Collision3D;
        collision.quality = ParticleSystemCollisionQuality.High;
        collision.enableDynamicColliders = true;
        collision.collidesWith = ~0;
        collision.bounce = .12f;
        collision.dampen = .12f;
        collision.lifetimeLoss = .025f;
        collision.radiusScale = .15f;
        collision.sendCollisionMessages = true;
        collision.maxCollisionShapes = 128;
        ParticleSystemRenderer renderer = particles.GetComponent<ParticleSystemRenderer>();
        renderer.sharedMaterial = steamMaterial;
        renderer.shadowCastingMode = ShadowCastingMode.Off;
        renderer.receiveShadows = false;
        return particles;
    }

    static void MakeSteamMaterial()
    {
        Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float distance = Vector2.Distance(new Vector2(x, y), new Vector2(31.5f, 31.5f)) / 31.5f;
                float alpha = Mathf.Pow(Mathf.Clamp01(1 - distance), 2);
                texture.SetPixel(x, y, new Color(1, 1, 1, alpha));
            }
        }
        texture.Apply();
        File.WriteAllBytes(modelFolder + "/Steam.png", texture.EncodeToPNG());
        AssetDatabase.ImportAsset(modelFolder + "/Steam.png");
        steamMaterial = new Material(Shader.Find("Particles/Standard Unlit"));
        steamMaterial.name = "Steam";
        steamMaterial.mainTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(modelFolder + "/Steam.png");
        Transparent(steamMaterial);
        AssetDatabase.CreateAsset(steamMaterial, modelFolder + "/Steam.mat");
        UnityEngine.Object.DestroyImmediate(texture);
    }

    static Mesh PlateMesh()
    {
        Vector2[] profile = { new Vector2(0, .011f), new Vector2(.075f, .011f), new Vector2(.093f, .016f), new Vector2(.11f, .026f), new Vector2(.12f, .029f), new Vector2(.124f, .025f), new Vector2(.12f, .019f), new Vector2(.09f, .003f), new Vector2(.07f, .002f), new Vector2(0, .005f) };
        Vector3[] vertices = new Vector3[profile.Length * 33];
        int[] triangles = new int[(profile.Length - 1) * 32 * 6];
        int triangle = 0;
        for (int ring = 0; ring < profile.Length; ring++)
        {
            for (int side = 0; side <= 32; side++)
            {
                float angle = side * Mathf.PI * 2 / 32;
                int index = ring * 33 + side;
                vertices[index] = new Vector3(Mathf.Cos(angle) * profile[ring].x, profile[ring].y, Mathf.Sin(angle) * profile[ring].x);
                if (ring < profile.Length - 1 && side < 32)
                {
                    triangles[triangle++] = index;
                    triangles[triangle++] = index + 1;
                    triangles[triangle++] = index + 33;
                    triangles[triangle++] = index + 1;
                    triangles[triangle++] = index + 34;
                    triangles[triangle++] = index + 33;
                }
            }
        }
        Mesh mesh = new Mesh();
        mesh.name = "Ceramic plate";
        mesh.vertices = vertices;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, modelFolder + "/CeramicPlate.asset");
        return mesh;
    }

    static GameObject Part(Transform parent, string name, Vector3 position, Vector3 size, Material material, float bevel, bool collider = true)
    {
        GameObject part = new GameObject(name);
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.AddComponent<MeshFilter>().sharedMesh = RoundedMesh(size, bevel);
        part.AddComponent<MeshRenderer>().sharedMaterial = material;
        if (collider) part.AddComponent<BoxCollider>().size = size;
        return part;
    }

    static Mesh RoundedMesh(Vector3 size, float bevel)
    {
        Vector3[] normals = { Vector3.right, Vector3.left, Vector3.up, Vector3.down, Vector3.forward, Vector3.back };
        Vector3[] vertices = new Vector3[6 * 36];
        Vector3[] smooth = new Vector3[vertices.Length];
        int[] triangles = new int[6 * 25 * 6];
        Vector3 half = size * .5f;
        Vector3 inner = half - Vector3.one * Mathf.Min(bevel, Mathf.Min(half.x, Mathf.Min(half.y, half.z)) * .9f);
        int triangle = 0;
        for (int face = 0; face < 6; face++)
        {
            Vector3 normal = normals[face];
            Vector3 helper = Vector3.up;
            if (Mathf.Abs(normal.y) > .9f) helper = Vector3.right;
            Vector3 u = Vector3.Cross(helper, normal).normalized;
            Vector3 v = Vector3.Cross(normal, u);
            for (int y = 0; y < 6; y++)
            {
                for (int x = 0; x < 6; x++)
                {
                    int index = face * 36 + y * 6 + x;
                    Vector3 point = Vector3.Scale(normal + u * (x / 2.5f - 1) + v * (y / 2.5f - 1), half);
                    Vector3 center = new Vector3(Mathf.Clamp(point.x, -inner.x, inner.x), Mathf.Clamp(point.y, -inner.y, inner.y), Mathf.Clamp(point.z, -inner.z, inner.z));
                    smooth[index] = (point - center).normalized;
                    vertices[index] = center + smooth[index] * (half.x - inner.x);
                    if (x < 5 && y < 5)
                    {
                        triangles[triangle++] = index;
                        triangles[triangle++] = index + 1;
                        triangles[triangle++] = index + 6;
                        triangles[triangle++] = index + 1;
                        triangles[triangle++] = index + 7;
                        triangles[triangle++] = index + 6;
                    }
                }
            }
        }
        Mesh mesh = new Mesh();
        mesh.name = "Rounded part " + meshNumber;
        mesh.vertices = vertices;
        mesh.normals = smooth;
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        AssetDatabase.CreateAsset(mesh, modelFolder + "/Part" + meshNumber++ + ".asset");
        return mesh;
    }

    static GameObject Cylinder(Transform parent, string name, Vector3 position, Vector3 size, Material material, bool collider = true)
    {
        GameObject part = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        part.name = name;
        part.transform.SetParent(parent, false);
        part.transform.localPosition = position;
        part.transform.localScale = new Vector3(size.x, size.y * .5f, size.z);
        part.GetComponent<Renderer>().sharedMaterial = material;
        UnityEngine.Object.DestroyImmediate(part.GetComponent<Collider>());
        if (collider) part.AddComponent<BoxCollider>().size = new Vector3(1, 2, 1);
        return part;
    }

    static Material Material(string name, Color color, float metal, float smooth)
    {
        Material material = new Material(Shader.Find("Standard"));
        material.name = name;
        material.color = color;
        material.SetFloat("_Metallic", metal);
        material.SetFloat("_Glossiness", smooth);
        AssetDatabase.CreateAsset(material, modelFolder + "/" + name.Replace(" ", "_") + ".mat");
        return material;
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

    static AudioSource Source(GameObject parent, float volume)
    {
        AudioSource source = parent.AddComponent<AudioSource>();
        source.playOnAwake = false;
        source.spatialBlend = 1;
        source.minDistance = 3;
        source.maxDistance = 18;
        source.dopplerLevel = 0;
        source.volume = volume;
        return source;
    }

    static AudioClip Clip(string name)
    {
        return AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Microwave/Microwave_" + name + ".wav");
    }

    static void Folder(string parent, string name)
    {
        if (!AssetDatabase.IsValidFolder(parent + "/" + name)) AssetDatabase.CreateFolder(parent, name);
    }
}
