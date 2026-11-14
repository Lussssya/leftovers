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

        int nextIngredientIndex;

        public bool IsFinished
        {
            get
            {
                return kitchenHud != null &&
                    kitchenHud.RequestedProductCount > 0 &&
                    nextIngredientIndex >= kitchenHud.RequestedProductCount;
            }
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
