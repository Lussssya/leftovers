using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace Leftovers
{
    public class KetchupUI : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private KetchupFlip ketchup;
        [SerializeField] private Camera playerCamera;

        [Header("UI Elements")]
        [SerializeField] private GameObject instructionPanel;
        [SerializeField] private GameObject outlineObject;
        [SerializeField] private Text instructionText;
        [SerializeField] private Text resultText;

        [Header("Settings")]
        [SerializeField] private float interactionDistance = 5f;
        [SerializeField] private float resultDuration = 3f;

        private Coroutine messageCoroutine;
        private bool completed;
        private bool failed;

        void Start()
        {
            if (playerCamera == null)
                playerCamera = Camera.main;

            if (instructionPanel != null)
                instructionPanel.SetActive(false);

            if (outlineObject != null)
                outlineObject.SetActive(false);

            if (resultText != null)
                resultText.gameObject.SetActive(false);
        }

        void Update()
        {
            if (ketchup == null || playerCamera == null)
                return;

            if (completed || failed)
            {
                HideInstructions();
                return;
            }

            if (ketchup.IsHeld)
            {
                HideInstructions();
                return;
            }

            if (ketchup.IsAirborne)
            {
                HideInstructions();
                return;
            }

            float distance = Vector3.Distance(
                playerCamera.transform.position,
                ketchup.transform.position
            );

            bool nearby = distance <= interactionDistance;

            if (instructionPanel != null)
                instructionPanel.SetActive(nearby);

            if (outlineObject != null)
                outlineObject.SetActive(nearby);

            if (nearby && instructionText != null)
            {
                instructionText.text =
                    "TOSS, FLIP,\nAND CATCH IT";
            }
        }

        void HideInstructions()
        {
            if (instructionPanel != null)
                instructionPanel.SetActive(false);

            if (outlineObject != null)
                outlineObject.SetActive(false);
        }

        public void ShowSuccess()
        {
            completed = true;
            HideInstructions();

            ShowMessage(
                "PERFECT FLIP!\nKETCHUP READY!",
                Color.green
            );
        }

        public void ShowFailure()
        {
            failed = true;
            HideInstructions();

            ShowMessage(
                "TOO LOUD!\nKETCHUP DROPPED!",
                Color.red
            );
        }

        private void ShowMessage(string message, Color color)
        {
            if (resultText == null)
                return;

            if (messageCoroutine != null)
                StopCoroutine(messageCoroutine);

            resultText.text = message;
            resultText.color = color;
            resultText.gameObject.SetActive(true);

            messageCoroutine =
                StartCoroutine(HideMessageAfterDelay());
        }

        private IEnumerator HideMessageAfterDelay()
        {
            yield return new WaitForSeconds(resultDuration);

            if (resultText != null)
                resultText.gameObject.SetActive(false);
        }
    }
}
