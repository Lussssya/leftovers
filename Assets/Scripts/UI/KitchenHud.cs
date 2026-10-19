using UnityEngine;

namespace Leftovers
{
    public class KitchenHud : MonoBehaviour
    {
        CameraFollow viewController;
        MouseDoorInteractor doorInteractor;
        MouseCabinetInteractor cabinetInteractor;
        Renderer highlightedHandle;
        MaterialPropertyBlock originalColors;
        bool handleWasVisible;
        GUIStyle labelStyle;
        GUIStyle titleStyle;

        Color highlightColor = new Color(1f, 0.8f, 0.35f, 1f);

        void Awake()
        {
            viewController = GetComponent<CameraFollow>();
            doorInteractor = GetComponent<MouseDoorInteractor>();
            cabinetInteractor = GetComponent<MouseCabinetInteractor>();
        }

        void LateUpdate()
        {
            Renderer nextHandle = null;
            if (viewController != null && !viewController.IsPaused)
            {
                nextHandle = FindHighlightedHandle();
            }

            if (nextHandle == highlightedHandle)
            {
                return;
            }

            ClearHighlight();
            if (nextHandle != null)
            {
                Highlight(nextHandle);
            }
        }

        Renderer FindHighlightedHandle()
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

        void Highlight(Renderer handle)
        {
            highlightedHandle = handle;
            handleWasVisible = handle.enabled;
            originalColors = new MaterialPropertyBlock();
            handle.GetPropertyBlock(originalColors);
            MaterialPropertyBlock colors = new MaterialPropertyBlock();
            handle.GetPropertyBlock(colors);
            colors.SetColor("_Color", highlightColor);
            colors.SetColor("_BaseColor", highlightColor);
            handle.SetPropertyBlock(colors);
            handle.enabled = true;
        }

        void ClearHighlight()
        {
            if (highlightedHandle != null)
            {
                highlightedHandle.SetPropertyBlock(originalColors);
                highlightedHandle.enabled = handleWasVisible;
            }

            highlightedHandle = null;
            originalColors = null;
        }

        void OnGUI()
        {
            if (viewController == null || !viewController.IsPaused)
            {
                return;
            }

            if (labelStyle == null)
            {
                labelStyle = new GUIStyle(GUI.skin.label);
                labelStyle.alignment = TextAnchor.MiddleCenter;
                labelStyle.normal.textColor = Color.white;
                titleStyle = new GUIStyle(labelStyle);
                titleStyle.fontSize = 24;
            }

            DrawPauseMenu();
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
