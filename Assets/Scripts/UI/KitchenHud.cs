using UnityEngine;


namespace Leftovers
{
    public class KitchenHud : MonoBehaviour
    {
        [Header("Product Request Sequence")]
        [SerializeField] string[] requestedProducts =
        {
            "Toast",
            "Cheese",
            "Tomato",
            "Ketchup"
        };

        int currentRequestIndex = 0;
        [SerializeField] float fridgeWarningShakeAmount = 5f;
        [Header("Product Request Feedback")]
        [SerializeField] float feedbackDuration = 2.5f;
        [SerializeField] TodoListUI todoListUI;

        string feedbackMessage = "";
        float feedbackUntil;

        public bool RoundCompleted { get; private set; }

        public int RequestedProductCount
        {
            get
            {
                if (requestedProducts == null)
                {
                    return 0;
                }

                return requestedProducts.Length;
            }
        }

        CameraFollow viewController;
        FirstPersonController firstPersonController;
        MouseDoorInteractor doorInteractor;
        MouseCabinetInteractor cabinetInteractor;
        ProductInteractor productInteractor;
        ProductHidingSpots productHidingSpots;
        FridgeDoorAlarm[] fridgeAlarms;
        ProductInteractable highlightedProduct;
        Renderer highlightedHandle;
        Renderer[] highlightedRenderers;
        MaterialPropertyBlock[] originalColors;
        bool[] rendererWasVisible;
        GUIStyle labelStyle;
        GUIStyle titleStyle;
        GUIStyle productNameStyle;
        GUIStyle productControlStyle;
        GUIStyle requestStyle;
        GUIStyle fridgeWarningStyle;
        GUIStyle productRequestHeaderStyle;
        GUIStyle productRequestTitleStyle;
        GUIStyle productRequestHintStyle;
        GUIStyle productRequestShadowStyle;
        Texture2D productRequestFrame;
        Texture2D productRequestIcon;
        string productRequestIconName;
        bool requestActive = true;
        float fridgeWarningTime;

        Color highlightColor = new Color(1f, 0.8f, 0.35f, 1f);
        Color productHighlightColor = new Color(1f, 0.72f, 0.2f, 1f);

        void Awake()
        {
            viewController = GetComponent<CameraFollow>();
            firstPersonController = GetComponentInParent<FirstPersonController>();
            doorInteractor = GetComponent<MouseDoorInteractor>();
            cabinetInteractor = GetComponent<MouseCabinetInteractor>();
            productInteractor = GetComponent<ProductInteractor>();
            productHidingSpots = GetComponent<ProductHidingSpots>();
            fridgeAlarms = FindObjectsOfType<FridgeDoorAlarm>();
        }

        void Start()
        {
            PlaceRequestedProduct();
        }

        void PlaceRequestedProduct()
        {
            if (productHidingSpots != null && requestActive && requestedProducts != null
                && currentRequestIndex < requestedProducts.Length
                && productHidingSpots.HasProduct(requestedProducts[currentRequestIndex]))
            {
                productHidingSpots.PlaceRequestedProduct(requestedProducts[currentRequestIndex]);
            }
        }

        void LateUpdate()
        {
            UpdateFridgeWarningTime();

            ProductInteractable nextProduct = null;
            Renderer nextHandle = null;
            if (!IsGamePaused())
            {
                if (productInteractor != null)
                {
                    nextProduct = productInteractor.TargetedProduct;
                }

                if (nextProduct == null)
                {
                    nextHandle = FindHandleRenderer();
                }
            }

            if (nextProduct == highlightedProduct && nextHandle == highlightedHandle)
            {
                return;
            }

            ClearHighlight();
            if (nextProduct != null)
            {
                HighlightProduct(nextProduct);
            }
            else if (nextHandle != null)
            {
                HighlightHandle(nextHandle);
            }
        }

        void HighlightProduct(ProductInteractable product)
        {
            highlightedProduct = product;
            Renderer[] renderers = product.GetComponentsInChildren<Renderer>(true);
            Highlight(renderers, productHighlightColor, false);
        }

        void HighlightHandle(Renderer handle)
        {
            highlightedHandle = handle;
            Highlight(new Renderer[] { handle }, highlightColor, true);
        }

        Renderer FindHandleRenderer()
        {
            if (doorInteractor != null && doorInteractor.isActiveAndEnabled)
            {
                FridgeDoorHandle handle = doorInteractor.Held;
                if (handle == null)
                {
                    handle = doorInteractor.Hovered;
                }

                if (handle != null)
                {
                    return handle.GetHighlightRenderer();
                }
            }

            if (cabinetInteractor != null && cabinetInteractor.isActiveAndEnabled)
            {
                CabinetPart part = cabinetInteractor.Held;
                if (part == null)
                {
                    part = cabinetInteractor.Hovered;
                }

                if (part != null)
                {
                    Renderer[] pieces = part.GetComponentsInChildren<Renderer>();
                    foreach (Renderer piece in pieces)
                    {
                        if (piece.name == "Door handle" || piece.name == "Drawer handle")
                        {
                            return piece;
                        }
                    }
                }
            }

            return null;
        }

        void Highlight(Renderer[] renderers, Color color, bool showWhileHighlighted)
        {
            highlightedRenderers = renderers;
            originalColors = new MaterialPropertyBlock[renderers.Length];
            rendererWasVisible = new bool[renderers.Length];

            for (int i = 0; i < renderers.Length; i++)
            {
                Renderer renderer = renderers[i];
                if (renderer == null)
                {
                    continue;
                }

                rendererWasVisible[i] = renderer.enabled;
                originalColors[i] = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(originalColors[i]);
                MaterialPropertyBlock colors = new MaterialPropertyBlock();
                renderer.GetPropertyBlock(colors);
                colors.SetColor("_Color", color);
                colors.SetColor("_BaseColor", color);
                renderer.SetPropertyBlock(colors);

                if (showWhileHighlighted)
                {
                    renderer.enabled = true;
                }
            }
        }

        void ClearHighlight()
        {
            if (highlightedRenderers != null)
            {
                for (int i = 0; i < highlightedRenderers.Length; i++)
                {
                    Renderer renderer = highlightedRenderers[i];
                    if (renderer != null)
                    {
                        renderer.SetPropertyBlock(originalColors[i]);
                        renderer.enabled = rendererWasVisible[i];
                    }
                }
            }

            highlightedProduct = null;
            highlightedHandle = null;
            highlightedRenderers = null;
            originalColors = null;
            rendererWasVisible = null;
        }

        void OnGUI()
        {
            CreateStyles();

            if (IsGamePaused())
            {
                DrawPauseMenu();
            }
            if (requestActive)
            {
                DrawProductRequest();
            }

            if (Time.time < feedbackUntil &&
                !string.IsNullOrEmpty(feedbackMessage))
            {
                DrawRequestFeedback();
            }

            if (HasFridgeWarning())
            {
                DrawFridgeWarning();
            }

            if (IsGamePaused())
            {
                return;
            }

            if (productInteractor == null)
            {
                return;
            }

            if (productInteractor.HeldProduct != null)
            {
                string heldControl = productInteractor.CanPlaceHeldProduct
                    ? "[E] Place"
                    : "[E] Drop";
                DrawProductPrompt(productInteractor.HeldProduct.DisplayName, heldControl);
            }
            else if (productInteractor.TargetedProduct != null)
            {
                DrawProductPrompt(productInteractor.TargetedProduct.DisplayName, "[E] Pick up");
            }
            else
            {
                DrawMouseInteractionPrompt();
            }
        }

        void DrawMouseInteractionPrompt()
        {
            if (doorInteractor != null && doorInteractor.isActiveAndEnabled)
            {
                FridgeDoorHandle handle = doorInteractor.Held;
                if (handle == null)
                {
                    handle = doorInteractor.Hovered;
                }

                if (handle != null && handle.Door != null)
                {
                    DrawProductPrompt(
                        GetDoorName(handle.Door),
                        "[Hold LMB] Drag");
                    return;
                }
            }

            if (cabinetInteractor != null && cabinetInteractor.isActiveAndEnabled)
            {
                CabinetPart part = cabinetInteractor.Held;
                if (part == null)
                {
                    part = cabinetInteractor.Hovered;
                }

                if (part != null)
                {
                    string partName = part.slidesOut ? "Drawer" : "Cabinet";
                    DrawProductPrompt(partName, "[Hold LMB] Drag");
                }
            }
        }

        string GetDoorName(FridgeDoor door)
        {
            if (door.GetComponentInParent<Microwave>() != null)
            {
                return "Microwave";
            }

            if (door.transform.root.name.Contains("Fridge"))
            {
                return "Fridge";
            }

            return "Door";
        }

        void CreateStyles()
        {
            if (labelStyle != null)
            {
                return;
            }

            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.normal.textColor = Color.white;
            titleStyle = new GUIStyle(labelStyle);
            titleStyle.fontSize = 24;
            productNameStyle = new GUIStyle(labelStyle);
            productNameStyle.fontSize = 32;
            productNameStyle.fontStyle = FontStyle.Bold;
            productNameStyle.normal.textColor = Color.white;
            productControlStyle = new GUIStyle(labelStyle);
            productControlStyle.fontSize = 20;
            requestStyle = new GUIStyle(labelStyle);
            requestStyle.alignment = TextAnchor.MiddleLeft;
            requestStyle.fontSize = 20;
            requestStyle.wordWrap = true;
            fridgeWarningStyle = new GUIStyle(requestStyle);
            fridgeWarningStyle.alignment = TextAnchor.MiddleCenter;
            fridgeWarningStyle.normal.textColor = new Color(1f, 0.35f, 0.35f, 1f);
        }

        void UpdateFridgeWarningTime()
        {
            if (!HasFridgeWarning())
            {
                fridgeWarningTime = 0f;
                return;
            }

            fridgeWarningTime = fridgeWarningTime + Time.deltaTime;
        }

        float GetFridgeWarningShake()
        {
            float shakeTime = fridgeWarningTime % 3f;
            if (Time.timeScale == 0f || shakeTime >= 0.45f)
            {
                return 0f;
            }

            float strength = 1f - shakeTime / 0.45f;
            float movement = Mathf.Sin(shakeTime * Mathf.PI * 18f);
            return movement * Mathf.Max(0f, fridgeWarningShakeAmount) * strength;
        }

        bool HasFridgeWarning()
        {
            foreach (FridgeDoorAlarm alarm in fridgeAlarms)
            {
                if (alarm != null && alarm.IsWarningActive)
                {
                    return true;
                }
            }

            return false;
        }

        void DrawFridgeWarning()
        {
            string warningText = "Fridge left open. Close the door!";
            float width = Mathf.Min(420f, Screen.width - 32f);
            float textHeight = fridgeWarningStyle.CalcHeight(new GUIContent(warningText), width - 32f);
            float height = Mathf.Max(52f, textHeight + 24f);
            float left = (Screen.width - width) * 0.5f;
            float top = Screen.height - height - 16f;

            Rect panel = new Rect(left, top, width, height);
            Color backgroundColor = new Color(0f, 0f, 0f, 0.7f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture, ScaleMode.StretchToFill,
                true, 0f, backgroundColor, 0f, 20f);
            float textLeft = left + 16f + GetFridgeWarningShake();
            GUI.Label(new Rect(textLeft, top + 12f, width - 32f, height - 24f), warningText, fridgeWarningStyle);
        }

        void DrawProductPrompt(string productName, string controlText)
        {
            float width = 360f;
            float height = 105f;
            float left = Screen.width * 0.5f - width * 0.5f;
            float top = Screen.height * 0.5f + 55f;
            Rect panel = new Rect(left, top, width, height);

            Color previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.32f);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = previousColor;

            GUI.Label(new Rect(left, top + 8f, width, 44f), productName, productNameStyle);
            GUI.Label(new Rect(left, top + 57f, width, 32f), controlText, productControlStyle);
        }
        

        [ContextMenu("Complete Request")]
        public void CompleteRequest()
        {
            requestActive = false;
            RoundCompleted = true;

            if (todoListUI != null)
            {
                todoListUI.CompleteTask(3);
            }
        }

        public bool CanPickRequestedProduct(ProductInteractable product)
        {
            if (product == null)
            {
                return false;
            }

            if (!requestActive)
            {
                return true;
            }

            if (IsCurrentRequestedProduct(product) &&
                (productHidingSpots == null || productHidingSpots.CanCollectProduct(product)))
            {
                return true;
            }

            string requestedName = GetRequestedProductName(currentRequestIndex);
            ShowFeedback("Wrong product! Find the " + requestedName + ".");
            Debug.Log("Wrong product: " + product.DisplayName);
            return false;
        }

        public void NotifyRequestedProductPickedUp(ProductInteractable product)
        {
            if (productHidingSpots != null)
            {
                productHidingSpots.MarkProductCollected(product);
            }

            if (todoListUI != null && requestActive &&
                currentRequestIndex == RequestedProductCount - 1 && IsCurrentRequestedProduct(product))
                todoListUI.CompleteTask(2);
        }

        public bool NotifyRequestedProductPlaced(ProductInteractable product)
        {
            if (!requestActive || !IsCurrentRequestedProduct(product))
            {
                return false;
            }

            string deliveredName = GetRequestedProductName(currentRequestIndex);
            currentRequestIndex++;

            if (currentRequestIndex < RequestedProductCount)
            {
                string nextProduct = GetRequestedProductName(currentRequestIndex);

                ShowFeedback(
                    deliveredName + " delivered! Next: " + nextProduct + "."
                );

                Debug.Log("Next requested product: " + nextProduct);
                PlaceRequestedProduct();
            }
            else
            {
                CompleteRequest();

                ShowFeedback(
                    "All ingredients delivered! Assemble the sandwich."
                );

                Debug.Log(
                    "Ingredient delivery complete! Ready for sandwich assembly."
                );
            }

            return true;
        }

        public string GetRequestedProductName(int index)
        {
            if (requestedProducts == null || index < 0 ||
                index >= requestedProducts.Length)
            {
                return "";
            }

            return (requestedProducts[index] ?? "").Trim();
        }

        bool IsCurrentRequestedProduct(ProductInteractable product)
        {
            if (product == null || currentRequestIndex < 0 ||
                currentRequestIndex >= RequestedProductCount)
            {
                return false;
            }

            string requestedName = GetRequestedProductName(currentRequestIndex);
            string selectedName = product.DisplayName.Trim();
            return string.Equals(
                selectedName,
                requestedName,
                System.StringComparison.OrdinalIgnoreCase);
        }


        void ShowFeedback(string message)
        {
            feedbackMessage = message;
            feedbackUntil = Time.time + feedbackDuration;
        }
        
        [ContextMenu("Fail Request")]
        public void FailRequest()
        {
            requestActive = false;
        }
        
        
        
        void DrawProductRequest()
        {
            if (requestedProducts == null ||
                requestedProducts.Length == 0 ||
                currentRequestIndex >= requestedProducts.Length)
            {
                return;
            }

            CreateProductRequestStyles();
            string productName = GetRequestedProductName(currentRequestIndex);
            if (productRequestIconName != productName)
            {
                productRequestIconName = productName;
                productRequestIcon = Resources.Load<Texture2D>("RequestPanel/" + productName);
            }

            float width = Mathf.Min(650f, Screen.width - 40f) * 0.6f;
            float scale = width / 1320f;
            float left = Screen.width - width - 24f;
            Matrix4x4 originalMatrix = GUI.matrix;
            Color originalColor = GUI.color;
            GUI.matrix = originalMatrix * Matrix4x4.TRS(new Vector3(left, 24f, 0f),
                Quaternion.identity, new Vector3(scale, scale, 1f));
            GUI.color = Color.white;

            GUI.DrawTexture(new Rect(0f, 0f, 1320f, 400f), productRequestFrame);
            GUI.Label(new Rect(99f, 27f, 390f, 72f),
                "REQUEST " + (currentRequestIndex + 1) + " / " + requestedProducts.Length,
                productRequestHeaderStyle);
            DrawProductRequestLabel(new Rect(90f, 127f, 940f, 122f),
                "Find the " + productName, productRequestTitleStyle);

            string requestHint = "Pick up the correct ingredient";
            if (productInteractor != null &&
                productInteractor.HeldProduct != null &&
                IsCurrentRequestedProduct(productInteractor.HeldProduct))
            {
                requestHint = "Bring the " + productName + " to the Assembly Table";
            }

            DrawProductRequestLabel(new Rect(94f, 260f, 930f, 84f),
                requestHint, productRequestHintStyle);
            DrawProductRequestIcon();

            GUI.color = originalColor;
            GUI.matrix = originalMatrix;
        }

        void CreateProductRequestStyles()
        {
            if (productRequestHeaderStyle != null)
            {
                return;
            }

            productRequestFrame = Resources.Load<Texture2D>("RequestPanel/Frame");
            productRequestHeaderStyle = new GUIStyle(GUI.skin.label);
            productRequestHeaderStyle.fontSize = 49;
            productRequestHeaderStyle.fontStyle = FontStyle.Bold;
            productRequestHeaderStyle.padding = new RectOffset(0, 0, 0, 0);
            productRequestHeaderStyle.alignment = TextAnchor.MiddleLeft;
            productRequestHeaderStyle.normal.textColor = new Color32(45, 25, 14, 255);
            productRequestTitleStyle = new GUIStyle(productRequestHeaderStyle);
            productRequestTitleStyle.fontSize = 104;
            productRequestTitleStyle.normal.textColor = new Color32(248, 228, 208, 255);
            productRequestTitleStyle.wordWrap = true;
            productRequestHintStyle = new GUIStyle(productRequestTitleStyle);
            productRequestHintStyle.fontSize = 44;
            productRequestHintStyle.fontStyle = FontStyle.Normal;
            productRequestHintStyle.normal.textColor = new Color32(233, 211, 187, 255);
            productRequestShadowStyle = new GUIStyle(productRequestTitleStyle);
            productRequestShadowStyle.normal.textColor = new Color32(28, 17, 12, 200);
        }

        void DrawProductRequestLabel(Rect rectangle, string text, GUIStyle style)
        {
            Rect shadow = rectangle;
            shadow.y = shadow.y + 3f;
            productRequestShadowStyle.fontSize = style.fontSize;
            productRequestShadowStyle.fontStyle = style.fontStyle;
            GUI.Label(shadow, text, productRequestShadowStyle);
            GUI.Label(rectangle, text, style);
        }

        void DrawProductRequestIcon()
        {
            if (productRequestIcon == null)
            {
                return;
            }

            Rect icon = new Rect(1050f, 133f, 216f, 216f);
            GUI.DrawTexture(icon, productRequestIcon, ScaleMode.ScaleToFit);
        }

        void DrawRequestFeedback()
        {
            if (string.IsNullOrEmpty(feedbackMessage))
                return;

            bool isWrong = feedbackMessage.IndexOf(
                "wrong",
                System.StringComparison.OrdinalIgnoreCase
            ) >= 0;

            bool isComplete = feedbackMessage.IndexOf(
                "all ingredients",
                System.StringComparison.OrdinalIgnoreCase
            ) >= 0;

            string title;

            if (isWrong)
                title = "WRONG INGREDIENT!";
            else if (isComplete)
                title = "COLLECTION COMPLETE!";
            else
                title = "CORRECT INGREDIENT!";

            Color background = isWrong
                ? new Color32(98, 40, 45, 245)
                : new Color32(39, 91, 64, 245);

            Color accent = isWrong
                ? new Color32(255, 185, 185, 255)
                : new Color32(170, 240, 190, 255);

            float width = Mathf.Min(850f, Screen.width - 40f);
            float height = 145f;

            float x = (Screen.width - width) / 2f;
            float y = Screen.height - height - 75f;

            Rect panel = new Rect(x, y, width, height);

            GUI.DrawTexture(
                panel,
                Texture2D.whiteTexture,
                ScaleMode.StretchToFill,
                true,
                0f,
                background,
                0f,
                18f
            );

            GUIStyle titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.fontSize = 32;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.normal.textColor = accent;

            GUIStyle messageStyle = new GUIStyle(GUI.skin.label);
            messageStyle.alignment = TextAnchor.MiddleCenter;
            messageStyle.fontSize = 34;
            messageStyle.fontStyle = FontStyle.Bold;
            messageStyle.wordWrap = true;
            messageStyle.normal.textColor = Color.white;

            GUI.Label(
                new Rect(x + 15f, y + 12f, width - 30f, 45f),
                title,
                titleStyle
            );

            GUI.Label(
                new Rect(x + 25f, y + 60f, width - 50f, 75f),
                feedbackMessage,
                messageStyle
            );
        }


        void DrawPauseMenu()
        {
            Color previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.65f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previousColor;

            float left = Screen.width * 0.5f - 150f;
            float top = Screen.height * 0.5f - 90f;
            GUI.Box(new Rect(left, top, 300f, 180f), "");
            GUI.Label(new Rect(left, top + 20f, 300f, 40f), "Paused", titleStyle);
            if (GUI.Button(new Rect(left + 40f, top + 80f, 220f, 42f), "Resume"))
            {
                if (viewController != null && viewController.isActiveAndEnabled)
                {
                    viewController.SetPaused(false);
                }
                else if (firstPersonController != null)
                {
                    firstPersonController.SetPaused(false);
                }
            }

            GUI.Label(new Rect(left, top + 132f, 300f, 28f), "Press Escape to resume", labelStyle);
        }

        void OnDisable()
        {
            ClearHighlight();
        }

        bool IsGamePaused()
        {
            if (viewController != null && viewController.isActiveAndEnabled)
            {
                return viewController.IsPaused || Time.timeScale == 0f;
            }

            if (firstPersonController != null)
            {
                return firstPersonController.IsPaused || Time.timeScale == 0f;
            }

            return Time.timeScale == 0f;
        }
    }
}
