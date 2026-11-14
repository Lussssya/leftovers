using UnityEngine.UI;
using UnityEngine;
using TMPro;

namespace Leftovers
{
    public class TodoListUI : MonoBehaviour
    {
        [Header("Task Texts")]
        [SerializeField] private TMP_Text[] taskTexts;
        [Header("Progress")]
        [SerializeField] private TMP_Text progressText;
        [SerializeField] private Image progressFill;

        private readonly string[] tasks =
        {
            "Open the fridge",
            "Collect the ingredients",
            "Bring ingredients to the table",
            "Assemble the sandwich",
            "Put sandwich in microwave",
            "Heat the sandwich",
            "Take out the sandwich"
        };

        private bool[] completed;

        private readonly Color completedColor =
            new Color(0.35f, 0.55f, 0.35f);

        private readonly Color activeColor =
            new Color(0.65f, 0.43f, 0.17f);

        private readonly Color pendingColor =
            new Color(0.40f, 0.35f, 0.31f);

        void Start()
        {
            completed = new bool[tasks.Length];
            RefreshUI();
        }

        public void CompleteTask(int taskNumber)
        {
            int index = taskNumber - 1;

            if (index < 0 || index >= tasks.Length)
                return;

            completed[index] = true;
            RefreshUI();
        }

        public void CompleteNextTask()
        {
            for (int i = 0; i < completed.Length; i++)
            {
                if (!completed[i])
                {
                    CompleteTask(i + 1);
                    return;
                }
            }
        }

        [ContextMenu("Test - Complete Next Task")]
        private void TestNextTask()
        {
            if (Application.isPlaying)
                CompleteNextTask();
        }

        private void RefreshUI()
        {
            int currentTask = -1;

            for (int i = 0; i < completed.Length; i++)
            {
                if (!completed[i])
                {
                    currentTask = i;
                    break;
                }
            }

            for (int i = 0; i < tasks.Length; i++)
            {
                if (taskTexts == null ||
                    i >= taskTexts.Length ||
                    taskTexts[i] == null)
                    continue;

                if (completed[i])
                {
                    taskTexts[i].text = "[x]  " + tasks[i];
                    taskTexts[i].color = completedColor;
                }
                else if (i == currentTask)
                {
                    taskTexts[i].text = "●  " + tasks[i];
                    taskTexts[i].color = activeColor;
                }
                else
                {
                    taskTexts[i].text = "○  " + tasks[i];
                    taskTexts[i].color = pendingColor;
                }
            }
            int completedCount = 0;

            foreach (bool done in completed)
            {
                if (done)
                    completedCount++;
            }

            if (progressText != null)
            {
                progressText.text =
                    $"{completedCount} / {tasks.Length} completed";
            }

            if (progressFill != null)
            {
                progressFill.fillAmount =
                    (float)completedCount / tasks.Length;
            }
        }
    }
}
