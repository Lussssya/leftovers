
using System;
using System.Collections.Generic;
using UnityEngine;

namespace Leftovers
{
    public class SandwichAssembly : MonoBehaviour
    {
        [Header("Recipe")]
        [SerializeField] string[] recipeOrder =
        {
            "Toast",
            "Cheese",
            "Tomato",
            "Ketchup"
        };

        [Header("Placement")]
        [SerializeField] Transform[] layerSlots;
        [SerializeField] float assemblyReach = 3f;

        [Header("References")]
        [SerializeField] KitchenHud kitchenHud;
        [SerializeField] TodoListUI todoList;

        private int nextIngredientIndex = 0;

        private readonly HashSet<ProductInteractable> placed =
            new HashSet<ProductInteractable>();

        public bool IsFinished =>
            recipeOrder != null &&
            recipeOrder.Length > 0 &&
            nextIngredientIndex >= recipeOrder.Length;

        public bool IsInRange(Vector3 playerPosition)
        {
            Vector3 stationPosition = transform.position;
            playerPosition.y = stationPosition.y;

            return Vector3.Distance(
                playerPosition, stationPosition
            ) <= assemblyReach;
        }

        public bool TryPlaceNext(
            IReadOnlyList<ProductInteractable> basket)
        {
            if (basket == null || IsFinished ||
                kitchenHud == null ||
                !kitchenHud.RoundCompleted)
            {
                return false;
            }

            if (recipeOrder == null ||
                nextIngredientIndex >= recipeOrder.Length ||
                layerSlots == null ||
                nextIngredientIndex >= layerSlots.Length ||
                layerSlots[nextIngredientIndex] == null)
            {
                Debug.LogWarning(
                    "Sandwich assembly slots are not configured."
                );
                return false;
            }

            string neededName =
                recipeOrder[nextIngredientIndex].Trim();

            ProductInteractable ingredient = null;

            foreach (ProductInteractable item in basket)
            {
                if (item != null &&
                    !placed.Contains(item) &&
                    string.Equals(
                        item.DisplayName.Trim(),
                        neededName,
                        StringComparison.OrdinalIgnoreCase))
                {
                    ingredient = item;
                    break;
                }
            }

            if (ingredient == null)
            {
                Debug.LogWarning(
                    "Missing ingredient: " + neededName
                );
                return false;
            }

            PlaceIngredient(
                ingredient,
                layerSlots[nextIngredientIndex]
            );

            placed.Add(ingredient);

            if (nextIngredientIndex == 0 && todoList != null)
            {
                todoList.CompleteTask(3);
            }

            nextIngredientIndex++;

            Debug.Log("Added to sandwich: " + neededName);

            if (IsFinished)
            {
                Debug.Log("Sandwich assembled successfully!");

                if (todoList != null)
                    todoList.CompleteTask(4);
            }

            return true;
        }

        void PlaceIngredient(
            ProductInteractable ingredient,
            Transform slot)
        {
            Rigidbody rb = ingredient.GetComponent<Rigidbody>();

            if (rb != null)
            {
                if (!rb.isKinematic)
                {
                    rb.velocity = Vector3.zero;
                    rb.angularVelocity = Vector3.zero;
                }

                rb.isKinematic = true;
                rb.useGravity = false;
                rb.detectCollisions = false;
            }

            ingredient.transform.SetParent(slot, false);
            ingredient.transform.localPosition = Vector3.zero;
            ingredient.transform.localRotation = Quaternion.identity;

            foreach (Collider col in
                ingredient.GetComponentsInChildren<Collider>(true))
            {
                col.enabled = false;
            }

            ingredient.gameObject.SetActive(true);
        }

        void OnGUI()
        {
            if (kitchenHud == null ||
                !kitchenHud.RoundCompleted ||
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
                message = "[E] Add " +
                    recipeOrder[nextIngredientIndex] +
                    " to sandwich (" +
                    nextIngredientIndex + "/" +
                    recipeOrder.Length + ")";
            }

            GUI.Box(
                new Rect(
                    Screen.width / 2f - 200f,
                    Screen.height - 105f,
                    400f,
                    48f
                ),
                message
            );
        }
    }
}

