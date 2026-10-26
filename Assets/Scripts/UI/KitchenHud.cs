using UnityEngine;

namespace Leftovers
{
    public class KitchenHud : MonoBehaviour
    {
        CameraFollow viewController;
        MouseDoorInteractor doorInteractor;
        MouseCabinetInteractor cabinetInteractor;
        ProductInteractor productInteractor;
        Renderer highlightedRenderer;
        MaterialPropertyBlock originalColors;
        bool rendererWasVisible;
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
            Renderer nextRenderer = null;
            Color nextHighlightColor = highlightColor;
            if (viewController != null && !viewController.IsPaused)
            {
                nextRenderer = FindProductRenderer();
                if (nextRenderer != null)
                {
                    nextHighlightColor = productHighlightColor;
                }
                else
                {
                    nextRenderer = FindHandleRenderer();
                }
            }

            if (nextRenderer == highlightedRenderer)
            {
                return;
            }

            ClearHighlight();
            if (nextRenderer != null)
            {
                Highlight(nextRenderer, nextHighlightColor);
            }
        }

        Renderer FindProductRenderer()
        {
            if (productInteractor != null && productInteractor.TargetedProduct != null)
            {
                Renderer productRenderer = productInteractor.TargetedProduct.GetComponentInChildren<Renderer>();
                if (productRenderer != null)
                {
                    return productRenderer;
                }
            }

            return null;
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

        void Highlight(Renderer renderer, Color color)
        {
            highlightedRenderer = renderer;
            rendererWasVisible = renderer.enabled;
            originalColors = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(originalColors);
            MaterialPropertyBlock colors = new MaterialPropertyBlock();
            renderer.GetPropertyBlock(colors);
            colors.SetColor("_Color", color);
            colors.SetColor("_BaseColor", color);
            renderer.SetPropertyBlock(colors);
            renderer.enabled = true;
        }

        void ClearHighlight()
        {
            if (highlightedRenderer != null)
            {
                highlightedRenderer.SetPropertyBlock(originalColors);
                highlightedRenderer.enabled = rendererWasVisible;
            }

            highlightedRenderer = null;
            originalColors = null;
        }

        void OnGUI()
        {
            if (viewController != null && viewController.IsPaused)
            {
                CreateStyles();
                DrawPauseMenu();
                return;
            }

            if (productInteractor == null || productInteractor.TargetedProduct == null)
            {
                return;
            }

            CreateStyles();
            DrawProductPrompt(productInteractor.TargetedProduct.DisplayName);
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

        void DrawProductPrompt(string productName)
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
            GUI.Label(new Rect(left, top + 57f, width, 32f), "[E] Pick up", productControlStyle);
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
