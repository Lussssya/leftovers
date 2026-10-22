using UnityEngine;

namespace Leftovers
{
    public class KetchupUIFollow : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Transform ketchupTarget;
        [SerializeField] private RectTransform instructionPanel;
        [SerializeField] private Canvas canvas;

        [Header("Position")]
        [SerializeField] private Vector3 worldOffset =
            new Vector3(0f, 0.7f, 0f);

        [SerializeField] private Vector2 screenOffset =
            new Vector2(0f, 0f);

        void LateUpdate()
        {
            if (playerCamera == null ||
                ketchupTarget == null ||
                instructionPanel == null ||
                canvas == null)
            {
                return;
            }

            Vector3 target =
                ketchupTarget.position + worldOffset;

            Vector3 screenPosition =
                playerCamera.WorldToScreenPoint(target);

            if (screenPosition.z <= 0f)
            {
                return;
            }

            RectTransform canvasRect =
                canvas.GetComponent<RectTransform>();

            Camera uiCamera =
                canvas.renderMode == RenderMode.ScreenSpaceOverlay
                    ? null
                    : canvas.worldCamera;

            Vector2 localPosition;

            if (RectTransformUtility
                .ScreenPointToLocalPointInRectangle(
                    canvasRect,
                    screenPosition,
                    uiCamera,
                    out localPosition))
            {
                Vector2 desiredPosition = localPosition + screenOffset;

                float halfWidth = instructionPanel.rect.width * 0.5f;
                float halfHeight = instructionPanel.rect.height * 0.5f;

                float minX = canvasRect.rect.xMin + halfWidth;
                float maxX = canvasRect.rect.xMax - halfWidth;

                float minY = canvasRect.rect.yMin + halfHeight;
                float maxY = canvasRect.rect.yMax - halfHeight;

                desiredPosition.x = Mathf.Clamp(
                    desiredPosition.x,
                    minX,
                    maxX
                );

                desiredPosition.y = Mathf.Clamp(
                    desiredPosition.y,
                    minY,
                    maxY
                );

                instructionPanel.anchoredPosition = desiredPosition;
            }
        }
    }
}