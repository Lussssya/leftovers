using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Leftovers;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public class CheckHidingSpots : MonoBehaviour
{
    ProductHidingSpots placement;
    ProductHidingSpot[] spots;
    ProductInteractable[] products;
    Transform[] originalParents;
    Vector3[] originalScales;
    KitchenHud hud;
    ProductInteractor interactor;
    Transform player;
    Camera cameraView;
    FirstPersonController firstPersonController;
    bool environmentScene;
    Vector3 spawnPosition;
    float cameraHeight;
    float minimumPitch = -6f;
    float maximumPitch = 25f;
    List<string> results = new List<string>();

    public static void Run()
    {
        RunScene("Assets/Scenes/Kitchen.unity");
    }

    public static void RunEnvironment()
    {
        RunScene("Assets/Scenes/Environment.unity");
    }

    static void RunScene(string scenePath)
    {
        EditorSceneManager.OpenScene(scenePath);
        GameObject todoCanvas = GameObject.Find("TodoCanvas");
        if (todoCanvas != null)
        {
            todoCanvas.SetActive(false);
        }
        new GameObject("Hiding spot checks").AddComponent<CheckHidingSpots>();
        EditorApplication.isPlaying = true;
    }

    void Awake()
    {
        Application.logMessageReceived += ReadLog;
    }

    void ReadLog(string message, string trace, LogType type)
    {
        if (type == LogType.Error || type == LogType.Exception)
        {
            File.AppendAllText("hiding-spots-results.txt", message + "\n" + trace + "\n");
            EditorApplication.Exit(1);
        }
    }

    IEnumerator Start()
    {
        yield return null;
        hud = FindObjectOfType<KitchenHud>();
        placement = FindObjectOfType<ProductHidingSpots>();
        interactor = FindObjectOfType<ProductInteractor>();
        spots = ReadField<ProductHidingSpot[]>(placement, "hidingSpots");
        products = ReadField<ProductInteractable[]>(placement, "products");
        originalParents = ReadField<Transform[]>(placement, "originalParents");
        originalScales = ReadField<Vector3[]>(placement, "originalScales");
        player = ReadField<Transform>(interactor, "interactionOrigin");
        cameraView = Camera.main;
        CameraFollow view = FindObjectOfType<CameraFollow>();
        environmentScene = hud.gameObject.scene.name == "Environment";
        spawnPosition = player.position;
        cameraHeight = cameraView.transform.position.y - player.position.y;
        if (view != null)
        {
            view.SetPaused(false);
            view.enabled = false;
            player.GetComponent<Mover>().enabled = false;
        }
        firstPersonController = player.GetComponent<FirstPersonController>();
        if (firstPersonController != null)
        {
            firstPersonController.SetPaused(false);
            minimumPitch = -firstPersonController.maxLookAngle;
            maximumPitch = firstPersonController.maxLookAngle;
            firstPersonController.enabled = false;
            player.GetComponent<Rigidbody>().isKinematic = true;
        }
        Time.captureDeltaTime = 1f / 60f;
        interactor.enabled = false;
        int expectedProducts = 7;
        if (environmentScene)
        {
            expectedProducts = 2;
        }
        Require(spots.Length == 3 && products.Length == expectedProducts, "Three spots and the expected products remain.");
        ProductInteractable initialProduct = ReadField<ProductInteractable>(placement, "currentProduct");
        Require(initialProduct != null && initialProduct.DisplayName == "Cheese", "The first request places Cheese automatically.");

        yield return CheckAllPlacements();
        CheckRandomPlacement();
        if (!environmentScene)
        {
            CheckDuplicateProducts();
            results.Add("The checks disable the existing TodoCanvas in memory because its font material throws an exception. The saved scene is unchanged by the checks.");
        }
        CheckRequestSequence();
        if (environmentScene)
        {
            firstPersonController.SetPaused(true);
            Require((bool)Call(hud, "IsGamePaused"), "The request HUD follows first-person pause.");
            firstPersonController.SetPaused(false);
            Require(!(bool)Call(hud, "IsGamePaused"), "The request HUD follows first-person resume.");
            results.Add("PASS: Environment uses the existing first-person controller and pause state.");
        }
        File.WriteAllLines("hiding-spots-results.txt", results);
        Debug.Log(string.Join("\n", results));
        EditorApplication.Exit(0);
    }

    IEnumerator CheckAllPlacements()
    {
        int combinations = 0;
        for (int s = 0; s < spots.Length; s++)
        {
            yield return SetContainerOpen(s, false);
            for (int p = 0; p < products.Length; p++)
            {
                RestoreProducts();
                ProductInteractable product = products[p];
                spots[s].PlaceProduct(product);
                Physics.SyncTransforms();
                BoxCollider collider = product.GetComponent<BoxCollider>();
                Vector3 space = ReadField<Vector3>(spots[s], "availableSpace");
                Require(collider.bounds.size.x <= space.x + 0.002f
                    && collider.bounds.size.y <= space.y + 0.002f
                    && collider.bounds.size.z <= space.z + 0.002f, product.name + " fits " + spots[s].name);
                Require(Mathf.Abs(collider.bounds.min.y - spots[s].transform.position.y) < 0.002f, product.name + " rests on its support.");
                PlacePlayer(s);
                Require(!IsVisible(product), "Closed container blocks " + product.name + " at " + spots[s].name);
                yield return SetContainerOpen(s, true);
                Require(FindPickupPosition(s, product), product.name + " needs a reachable pickup position in " + spots[s].name);
                Vector3 screenPoint = cameraView.WorldToScreenPoint(product.GetTargetPoint());
                ProductInteractable hovered = (ProductInteractable)Call(interactor, "FindMouseHoverTargetAt", new Vector2(screenPoint.x, screenPoint.y));
                Require(hovered == product, "Mouse targeting reaches " + product.name + " in " + spots[s].name);
                if (environmentScene)
                {
                    Require((ProductInteractable)Call(interactor, "FindExactCenterTarget") == product,
                        "The first-person crosshair targets " + product.name + " in " + spots[s].name);
                    Call(interactor, "SetTarget", product);
                    Call(hud, "LateUpdate");
                    Require(ReadField<ProductInteractable>(hud, "highlightedProduct") == product,
                        "The first-person request HUD highlights the targeted product.");
                    Call(hud, "ClearHighlight");
                }
                RequireClearProduct(collider);
                if (product.DisplayName == "Cheese")
                {
                    CaptureSpot(s);
                }
                Vector3 oldPosition = product.transform.position;
                yield return SetContainerOpen(s, false);
                if (s == 2)
                {
                    Require(Vector3.Distance(oldPosition, product.transform.position) > 0.5f, "Drawer carries " + product.name);
                }
                combinations++;
            }
        }
        results.Add("PASS: All " + combinations + " product/spot combinations fit without solid overlaps and rest on their support.");
        results.Add("PASS: Closed containers block pickup. Open containers allow mouse targeting from a clear standing position with a walking route from the player spawn.");
        results.Add("PASS: All tested products move with the drawer.");

    }

    void CheckRandomPlacement()
    {
        RestoreProducts();
        HashSet<ProductHidingSpot> selectedSpots = new HashSet<ProductHidingSpot>();
        ProductInteractable cheese = FindProduct("Cheese");
        for (int i = 0; i < 90; i++)
        {
            placement.PlaceRequestedProduct("Cheese");
            selectedSpots.Add(cheese.GetComponentInParent<ProductHidingSpot>());
        }
        Require(selectedSpots.Count == 3, "Random placement reaches all three spots.");
        results.Add("PASS: Random selection uses all three spots over 90 placements.");
    }

    void CheckDuplicateProducts()
    {
        placement.PlaceRequestedProduct(" Tomato ");
        int tomatoCount = 0;
        for (int i = 0; i < products.Length; i++)
        {
            if (products[i].DisplayName == "Tomato" && products[i].gameObject.activeSelf)
            {
                tomatoCount++;
            }
        }
        Require(tomatoCount == 1, "The requested Tomato has exactly one active copy.");
        results.Add("PASS: Duplicate Tomato objects produce one active requested product.");
    }

    void CheckRequestSequence()
    {
        RestoreProducts();
        ProductInteractable cheese = FindProduct("Cheese");
        placement.PlaceRequestedProduct("Cheese");
        ProductHidingSpot currentSpot = cheese.GetComponentInParent<ProductHidingSpot>();
        Require(!hud.TryAcceptProduct(FindProduct("Ketchup")), "Wrong product is rejected.");
        Require(cheese.GetComponentInParent<ProductHidingSpot>() == currentSpot, "Wrong product does not reroll the spot.");
        CollectProduct(cheese);
        Require(interactor.BasketCount == 1 && !cheese.gameObject.activeSelf, "Cheese enters the basket.");
        ProductInteractable ketchup = ReadField<ProductInteractable>(placement, "currentProduct");
        Require(ketchup != null && ketchup.DisplayName == "Ketchup", "The next request places Ketchup.");
        CollectProduct(ketchup);
        Require(interactor.BasketCount == 2 && hud.RoundCompleted, "The sequence completes with both ingredients in the basket.");
        if (environmentScene)
        {
            Require(ReadField<string>(hud, "feedbackMessage") == "All ingredients collected!",
                "Environment completion does not direct the player to a missing assembly table.");
        }
        Require(ReadField<ProductInteractable>(placement, "currentProduct") == null, "The finished sequence has no hidden target.");
        for (int i = 0; i < products.Length; i++)
        {
            if (products[i] == cheese || products[i] == ketchup)
            {
                Require(products[i].transform.parent == originalParents[i]
                    && products[i].transform.localScale == originalScales[i], "Collection restores the original parent and size.");
            }
        }
        results.Add("PASS: Wrong pickups keep the spot. Correct pickups enter the basket, restore original size, advance the request, and complete the sequence.");
    }

    IEnumerator SetContainerOpen(int spotIndex, bool open)
    {
        player.position = spawnPosition;
        if (!environmentScene)
        {
            player.position = new Vector3(5f, 0.5f, 3f);
        }
        Physics.SyncTransforms();
        Transform container = spots[spotIndex].transform.parent;
        CabinetPart drawer = container.GetComponent<CabinetPart>();
        FridgeDoor[] doors = container.GetComponentsInChildren<FridgeDoor>();
        if (drawer != null)
        {
            drawer.BeginHold();
            float amount = 0f;
            if (open)
            {
                amount = drawer.maximumOpen;
            }
            drawer.HoldAtOpen(amount);
        }
        for (int i = 0; i < doors.Length; i++)
        {
            doors[i].BeginHold();
            float angle = 0f;
            if (open)
            {
                angle = 100f;
            }
            doors[i].HoldAtAngle(angle);
        }
        for (int i = 0; i < 55; i++)
        {
            yield return null;
        }
        if (drawer != null)
        {
            drawer.EndHold(true);
        }
        for (int i = 0; i < doors.Length; i++)
        {
            doors[i].EndHold(true);
        }
        Physics.SyncTransforms();
    }

    void PlacePlayer(int spotIndex)
    {
        Vector3 position = spots[spotIndex].transform.position;
        position.y = spawnPosition.y;
        if (environmentScene)
        {
            Vector3 front = spots[spotIndex].transform.parent.forward;
            position += new Vector3(front.x, 0f, front.z).normalized * 2.2f;
        }
        else
        {
            position.z = position.z - 2.2f;
            if (spotIndex == 2)
            {
                position.z = 6.6f;
            }
        }
        player.position = position;
        cameraView.transform.position = position + Vector3.up * cameraHeight;
        cameraView.transform.rotation = Quaternion.Euler(12f, 0f, 0f);
    }

    bool FindPickupPosition(int spotIndex, ProductInteractable product)
    {
        int clearStanding = 0;
        int clearPaths = 0;
        int visibleTargets = 0;
        int visibleModels = 0;
        for (int x = -8; x <= 8; x++)
        {
            for (int z = -6; z <= 8; z++)
            {
                PlacePlayer(spotIndex);
                player.position += new Vector3(x * 0.25f, 0f, -z * 0.2f);
                Physics.SyncTransforms();
                Collider[] standing = FindStandingCollisions();
                bool blocked = false;
                for (int i = 0; i < standing.Length; i++)
                {
                    if (!standing[i].transform.IsChildOf(player))
                    {
                        blocked = true;
                    }
                }
                if (blocked)
                {
                    continue;
                }
                clearStanding++;
                if (!CanWalkTo(player.position))
                {
                    continue;
                }
                clearPaths++;
                if (IsVisible(product))
                {
                    visibleTargets++;
                }
                if (IsModelVisible(product))
                {
                    visibleModels++;
                }
                Vector3 direction = product.GetTargetPoint() - cameraView.transform.position;
                float pitch = Mathf.Clamp(-Mathf.Atan2(direction.y, new Vector2(direction.x, direction.z).magnitude) * Mathf.Rad2Deg, minimumPitch, maximumPitch);
                float yaw = Mathf.Atan2(direction.x, direction.z) * Mathf.Rad2Deg;
                cameraView.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
                Vector3 screen = cameraView.WorldToViewportPoint(product.GetTargetPoint());
                if (screen.z > 0f && screen.x > 0.02f && screen.x < 0.98f && screen.y > 0.02f && screen.y < 0.98f
                    && IsVisible(product) && IsModelVisible(product))
                {
                    return true;
                }
            }
        }
        Debug.Log("PICKUP_DIAGNOSTIC " + product.name + " " + spots[spotIndex].name + " standing=" + clearStanding + " paths=" + clearPaths + " targets=" + visibleTargets + " models=" + visibleModels);
        return false;
    }

    Collider[] FindStandingCollisions()
    {
        if (environmentScene)
        {
            CapsuleCollider capsule = player.GetComponent<CapsuleCollider>();
            Vector3 center = player.TransformPoint(capsule.center);
            float half = capsule.height * 0.5f - capsule.radius;
            return Physics.OverlapCapsule(center + Vector3.up * half, center - Vector3.up * half,
                capsule.radius - 0.02f, ~0, QueryTriggerInteraction.Ignore);
        }
        BoxCollider playerCollider = player.GetComponent<BoxCollider>();
        return Physics.OverlapBox(playerCollider.bounds.center, playerCollider.bounds.extents - Vector3.one * 0.025f,
            player.rotation, ~0, QueryTriggerInteraction.Ignore);
    }

    bool CanWalkTo(Vector3 destination)
    {
        Vector3 start = spawnPosition;
        Vector3 corner = new Vector3(destination.x, start.y, start.z);
        if (ClearWalkSegment(start, corner) && ClearWalkSegment(corner, destination))
        {
            return true;
        }
        corner = new Vector3(start.x, start.y, destination.z);
        return ClearWalkSegment(start, corner) && ClearWalkSegment(corner, destination);
    }

    bool ClearWalkSegment(Vector3 start, Vector3 end)
    {
        Vector3 direction = end - start;
        RaycastHit[] hits;
        if (environmentScene)
        {
            CapsuleCollider capsule = player.GetComponent<CapsuleCollider>();
            Vector3 center = start + capsule.center;
            float half = capsule.height * 0.5f - capsule.radius;
            hits = Physics.CapsuleCastAll(center + Vector3.up * half, center - Vector3.up * half,
                capsule.radius - 0.02f, direction.normalized, direction.magnitude, ~0, QueryTriggerInteraction.Ignore);
        }
        else
        {
            hits = Physics.BoxCastAll(start + Vector3.up * 0.45f, new Vector3(0.35f, 0.925f, 0.35f),
                direction.normalized, Quaternion.identity, direction.magnitude, ~0, QueryTriggerInteraction.Ignore);
        }
        for (int i = 0; i < hits.Length; i++)
        {
            if (!hits[i].collider.transform.IsChildOf(player))
            {
                return false;
            }
        }
        return true;
    }

    void RequireClearProduct(BoxCollider productCollider)
    {
        Collider[] overlaps = Physics.OverlapBox(productCollider.bounds.center, productCollider.bounds.extents * 0.98f,
            Quaternion.identity, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < overlaps.Length; i++)
        {
            Collider other = overlaps[i];
            if (other.GetComponentInParent<ProductInteractable>() != null || other.transform.IsChildOf(player))
            {
                continue;
            }
            Vector3 direction;
            float depth;
            bool intersects = Physics.ComputePenetration(productCollider, productCollider.transform.position, productCollider.transform.rotation,
                other, other.transform.position, other.transform.rotation, out direction, out depth);
            Require(!intersects || depth < 0.003f, productCollider.name + " overlaps " + other.name + " by " + depth
                + " in direction " + direction + ". Product bounds: " + productCollider.bounds);
        }
    }

    bool IsVisible(ProductInteractable product)
    {
        return (bool)Call(interactor, "IsImmediatelyValid", product);
    }

    bool IsModelVisible(ProductInteractable product)
    {
        Bounds bounds = product.GetComponent<Renderer>().bounds;
        Vector3 direction = bounds.center - cameraView.transform.position;
        RaycastHit hit;
        if (Physics.Raycast(cameraView.transform.position, direction.normalized, out hit, direction.magnitude + 0.01f,
            ~0, QueryTriggerInteraction.Ignore))
        {
            return hit.collider.GetComponentInParent<ProductInteractable>() == product;
        }
        return false;
    }

    void CollectProduct(ProductInteractable product)
    {
        Call(interactor, "SetTarget", product);
        Call(interactor, "PickUpTargetedProduct");
    }

    void CaptureSpot(int spotIndex)
    {
        if (SystemInfo.graphicsDeviceType == UnityEngine.Rendering.GraphicsDeviceType.Null)
        {
            return;
        }
        RenderTexture target = new RenderTexture(1000, 700, 24);
        cameraView.targetTexture = target;
        cameraView.Render();
        RenderTexture.active = target;
        Texture2D picture = new Texture2D(1000, 700, TextureFormat.RGB24, false);
        picture.ReadPixels(new Rect(0f, 0f, 1000f, 700f), 0, 0);
        picture.Apply();
        File.WriteAllBytes("hiding-spot-" + spotIndex + ".png", picture.EncodeToPNG());
        cameraView.targetTexture = null;
        RenderTexture.active = null;
        Destroy(picture);
        target.Release();
        Destroy(target);
    }

    void RestoreProducts()
    {
        for (int i = 0; i < products.Length; i++)
        {
            products[i].gameObject.SetActive(false);
            products[i].transform.SetParent(originalParents[i], false);
            products[i].transform.localScale = originalScales[i];
            products[i].transform.position = new Vector3(-20f - i, 1f, -20f);
            products[i].gameObject.SetActive(true);
        }
    }

    ProductInteractable FindProduct(string name)
    {
        for (int i = 0; i < products.Length; i++)
        {
            if (products[i].DisplayName == name)
            {
                return products[i];
            }
        }
        return null;
    }

    static T ReadField<T>(object target, string name)
    {
        return (T)target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(target);
    }

    static object Call(object target, string name, params object[] arguments)
    {
        return target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, arguments);
    }

    static void Require(bool condition, string message)
    {
        if (!condition)
        {
            throw new Exception(message);
        }
    }
}
