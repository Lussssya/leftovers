using UnityEngine;

namespace Leftovers
{
    public class SandwichAssembly : MonoBehaviour
    {
        [SerializeField] Transform[] layerSlots;
        [SerializeField] float assemblyReach = 3f;
        [SerializeField] AssemblyTable assemblyTable;
        [SerializeField] KitchenHud kitchenHud;
        [SerializeField] TodoListUI todoList;

        [SerializeField] Transform finishedSandwich;
        [SerializeField] Collider assemblySurface;

        int nextIngredientIndex;

        void ShowFinishedSandwich()
        {
            if (finishedSandwich == null) return;

            for (int i = 0; i < nextIngredientIndex; i++)
            {
                ProductInteractable ingredient = layerSlots[i].GetComponentInChildren<ProductInteractable>();
                if (ingredient != null) ingredient.gameObject.SetActive(false);
            }

            finishedSandwich.SetParent(null, true);
            Vector3 position = layerSlots[0].position;
            if (assemblySurface != null) position.y = assemblySurface.bounds.max.y;
            finishedSandwich.SetPositionAndRotation(position, layerSlots[0].rotation);
            finishedSandwich.gameObject.SetActive(true);
            MicrowaveFood food = finishedSandwich.GetComponent<MicrowaveFood>();
            food.Place();
            if (todoList != null) todoList.TrackSandwich(food);
        }

        public bool IsFinished
        {
            get
            {
                return kitchenHud != null &&
                    kitchenHud.RequestedProductCount > 0 &&
                    nextIngredientIndex >= kitchenHud.RequestedProductCount;
            }
        }

        public bool IsAimedAt(Camera camera)
        {
            if (assemblySurface == null) return true;

            Vector2 pointer = Input.mousePosition;
            if (Cursor.lockState == CursorLockMode.Locked) pointer = camera.pixelRect.center;
            Ray ray = camera.ScreenPointToRay(pointer);
            RaycastHit hit;
            return Physics.Raycast(ray, out hit, 10f, ~0, QueryTriggerInteraction.Ignore) &&
                (hit.collider == assemblySurface || hit.transform.IsChildOf(transform));
        }

        public bool IsInRange(Vector3 playerPosition)
        {
            Vector3 stationPosition = transform.position;
            playerPosition.y = stationPosition.y;
            return Vector3.Distance(playerPosition, stationPosition) <= assemblyReach;
        }

        public bool TryPlaceNext()
        {
            if (assemblyTable == null || kitchenHud == null ||
                !kitchenHud.RoundCompleted || IsFinished)
            {
                return false;
            }

            if (layerSlots == null || nextIngredientIndex >= layerSlots.Length ||
                layerSlots[nextIngredientIndex] == null)
            {
                Debug.LogWarning("Sandwich assembly slots are not configured.");
                return false;
            }

            string neededName =
                kitchenHud.GetRequestedProductName(nextIngredientIndex);
            ProductInteractable ingredient =
                assemblyTable.FindPlacedProductByName(neededName);

            if (ingredient == null)
            {
                Debug.LogWarning("Missing ingredient on Assembly Table: " + neededName);
                return false;
            }

            if (!assemblyTable.TryMoveToSandwich(
                ingredient,
                layerSlots[nextIngredientIndex]))
            {
                return false;
            }

            nextIngredientIndex++;
            Debug.Log("Added to sandwich: " + neededName);

            if (IsFinished)
            {
                ShowFinishedSandwich();
                Debug.Log("Sandwich assembled successfully!");
                if (todoList != null)
                {
                    todoList.CompleteTask(4);
                }
            }

            return true;
        }

        void OnGUI()
        {
            if (kitchenHud == null || !kitchenHud.RoundCompleted ||
                Camera.main == null ||
                !IsInRange(Camera.main.transform.position))
            {
                return;
            }

            string message;
            if (IsFinished)
            {
                message = "Sandwich assembled! Next: microwave.";
            }
            else
            {
                string ingredientName =
                    kitchenHud.GetRequestedProductName(nextIngredientIndex);
                message = "[E] Add " + ingredientName +
                    " to sandwich (" + nextIngredientIndex + "/" +
                    kitchenHud.RequestedProductCount + ")";
            }

            GUI.Box(
                new Rect(
                    Screen.width / 2f - 200f,
                    Screen.height - 105f,
                    400f,
                    48f),
                message);
        }
    }
}
