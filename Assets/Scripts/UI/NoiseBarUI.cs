using UnityEngine;

namespace Leftovers
{
    public class NoiseBarUI : MonoBehaviour
    {
        [SerializeField] KitchenNoise kitchenNoise;

        GUIStyle labelStyle;
        GUIStyle levelStyle;
        GUIStyle shadowStyle;
        Texture2D panelTexture;
        Texture2D thumbTexture;
        float displayedNoise;

        public bool IsFull
        {
            get
            {
                return displayedNoise >= 1f;
            }
        }

        void Update()
        {
            if (kitchenNoise != null)
            {
                displayedNoise = Mathf.MoveTowards(displayedNoise,
                    kitchenNoise.NormalizedNoise, Time.deltaTime * 1.5f);
            }
        }

        void OnGUI()
        {
            if (kitchenNoise == null || !kitchenNoise.isActiveAndEnabled || Time.timeScale == 0f)
            {
                return;
            }

            CreateStyles();
            float width = Mathf.Min(400f, Screen.width - 32f);
            float scale = width / 800f;
            float top = Screen.height - 184f * scale - 16f;
            Matrix4x4 originalMatrix = GUI.matrix;
            Color originalColor = GUI.color;
            GUI.matrix = originalMatrix * Matrix4x4.TRS(new Vector3(16f, top, 0f),
                Quaternion.identity, new Vector3(scale, scale, 1f));
            GUI.color = Color.white;

            GUI.DrawTexture(new Rect(0f, 0f, 800f, 184f), panelTexture);
            DrawNoiseLevel();

            GUI.color = originalColor;
            GUI.matrix = originalMatrix;
        }

        void DrawNoiseLevel()
        {
            Color fillColor = new Color32(165, 188, 123, 255);
            string level = "Quiet";
            if (displayedNoise >= 0.7f)
            {
                fillColor = new Color32(222, 139, 119, 255);
                level = "Loud";
            }
            else if (displayedNoise >= 0.3f)
            {
                fillColor = new Color32(225, 185, 117, 255);
                level = "Noticeable";
            }

            Rect title = new Rect(136f, 25f, 260f, 55f);
            Rect status = new Rect(398f, 25f, 342f, 55f);
            DrawLabel(title, "Noise", labelStyle);
            levelStyle.normal.textColor = fillColor;
            DrawLabel(status, level, levelStyle);

            Rect track = new Rect(134f, 91f, 610f, 40f);
            DrawRectangle(track, new Color32(37, 24, 20, 255), 20f);
            DrawRectangle(new Rect(139f, 96f, 600f, 30f),
                new Color32(66, 62, 64, 255), 15f);
            DrawRectangle(new Rect(141f, 100f, 596f, 26f),
                new Color32(57, 55, 59, 255), 13f);

            float value = Mathf.Clamp01(displayedNoise);
            float markerPosition = Mathf.Lerp(154f, 724f, value);
            if (value > 0f)
            {
                Rect fill = new Rect(140f, 97f, markerPosition - 140f, 28f);
                DrawRectangle(fill, fillColor, 14f);
                Color highlight = Color.Lerp(fillColor, new Color32(241, 229, 189, 255), 0.25f);
                DrawRectangle(new Rect(fill.x + 3f, fill.y + 2f,
                    Mathf.Max(0f, fill.width - 6f), 7f), highlight, 3f);
            }

            GUI.DrawTexture(new Rect(markerPosition - 32f, 79f, 64f, 68f), thumbTexture);
        }

        void DrawLabel(Rect rectangle, string text, GUIStyle style)
        {
            Rect shadow = rectangle;
            shadow.y = shadow.y + 2f;
            shadowStyle.alignment = style.alignment;
            shadowStyle.fontSize = style.fontSize;
            GUI.Label(shadow, text, shadowStyle);
            GUI.Label(rectangle, text, style);
        }

        void CreateStyles()
        {
            if (labelStyle != null)
            {
                return;
            }

            panelTexture = Resources.Load<Texture2D>("NoiseBar/Panel");
            thumbTexture = Resources.Load<Texture2D>("NoiseBar/Thumb");
            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 46;
            labelStyle.fontStyle = FontStyle.Bold;
            labelStyle.alignment = TextAnchor.MiddleLeft;
            labelStyle.padding = new RectOffset(0, 0, 0, 0);
            labelStyle.normal.textColor = new Color32(246, 228, 202, 255);
            levelStyle = new GUIStyle(labelStyle);
            levelStyle.fontSize = 42;
            levelStyle.alignment = TextAnchor.MiddleRight;
            shadowStyle = new GUIStyle(labelStyle);
            shadowStyle.normal.textColor = new Color32(28, 20, 17, 190);
        }

        void DrawRectangle(Rect rectangle, Color color, float radius)
        {
            float cornerRadius = Mathf.Min(radius, rectangle.width * 0.5f);
            GUI.DrawTexture(rectangle, Texture2D.whiteTexture, ScaleMode.StretchToFill,
                true, 0f, color, 0f, cornerRadius);
        }
    }
}
