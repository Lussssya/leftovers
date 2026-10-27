using UnityEngine;

namespace Leftovers
{
    public class KitchenHud : MonoBehaviour
    {
        CameraFollow viewController;
        MouseDoorInteractor doorInteractor;
        MouseCabinetInteractor cabinetInteractor;
        ProductInteractor productInteractor;
        ProductInteractable highlightedProduct;
        Renderer highlightedHandle;
        Renderer[] highlightedRenderers;
        MaterialPropertyBlock[] originalColors;
        bool[] rendererWasVisible;
        GUIStyle labelStyle;
        GUIStyle titleStyle;
        GUIStyle productNameStyle;
        GUIStyle productControlStyle;

        Color highlightColor = new Color(1f, 0.8f, 0.35f, 1f);
        Color productHighlightColor = new Color(1f, 0.72f, 0.2f, 1f);

        void Awake()
        {
            viewController = GetComponent<CameraFollow>();
            doorInteractor = GetComponent<MouseDoorInteractor>();
            cabinetInteractor = GetComponent<MouseCabinetInteractor>();
            productInteractor = GetComponent<ProductInteractor>();
        }

        void LateUpdate()
        {
            ProductInteractable nextProduct = null;
            Renderer nextHandle = null;
            if (viewController != null && !viewController.IsPaused)
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
            if (viewController != null && viewController.IsPaused)
            {
                CreateStyles();
                DrawPauseMenu();
                return;
            }

            if (productInteractor == null)
            {
                return;
            }

            CreateStyles();
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
                viewController.SetPaused(false);
            }

            GUI.Label(new Rect(left, top + 132f, 300f, 28f), "Press Escape to resume", labelStyle);
        }

        void OnDisable()
        {
            ClearHighlight();
        }
    }
}
