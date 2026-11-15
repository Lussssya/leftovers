using UnityEngine;

namespace Leftovers
{
    public class MouseFoodInteractor : MonoBehaviour
    {
        public Camera interactionCamera;
        public bool useCenterAim;
        Vector3 Pointer
        {
            get
            {
                if (useCenterAim) return interactionCamera.pixelRect.center;
                return Input.mousePosition;
            }
        }
        public MouseDoorInteractor doorInteractor;
        public Microwave microwave;
        public Transform foodRest;
        public float reach = 40f;

        public MicrowaveFood Held { get; private set; }

        public int InteractionFrame { get; private set; } = -1;
        Collider[] heldColliders;
        bool[] colliderStates;
        ProductInteractor ingredients;
        KitchenNoise kitchenNoise;
        Collider servingTable;

        void Start()
        {
            ingredients = GetComponent<ProductInteractor>();
            kitchenNoise = FindObjectOfType<KitchenNoise>();
            GameObject table = GameObject.Find("Dining tabletop");
            if (table != null) servingTable = table.GetComponent<Collider>();
        }

        void Update()
        {
            if (Time.timeScale == 0f || Input.GetKeyDown(KeyCode.Escape))
            {
                Release(false);
                return;
            }

            if (Held != null)
            {
                if (Input.GetKeyDown(KeyCode.E))
                {
                    InteractionFrame = Time.frameCount;
                    Release(true);
                }
                return;
            }

            if (!Input.GetKeyDown(KeyCode.E) || doorInteractor.Held != null ||
                (ingredients != null && ingredients.IsCarryingIngredient))
            {
                return;
            }

            Physics.SyncTransforms();
            Ray ray = interactionCamera.ScreenPointToRay(Pointer);
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit, reach, ~0, QueryTriggerInteraction.Collide))
            {
                return;
            }

            MicrowaveFood food = hit.collider.GetComponentInParent<MicrowaveFood>();
            if (food == null && hit.collider.transform.IsChildOf(microwave.transform))
                food = microwave.food;
            if (food == null) return;
            if (microwave.food == food && !microwave.TakeFood(food))
            {
                return;
            }

            food.transform.SetParent(null, true);
            Held = food;
            Held.Hold();
            InteractionFrame = Time.frameCount;
            heldColliders = Held.GetComponentsInChildren<Collider>();
            colliderStates = new bool[heldColliders.Length];
            for (int i = 0; i < heldColliders.Length; i++)
            {
                colliderStates[i] = heldColliders[i].enabled;
                heldColliders[i].enabled = false;
            }
            Transform hand = interactionCamera.transform;
            if (ingredients != null && ingredients.HoldPoint != null) hand = ingredients.HoldPoint;
            Held.transform.SetParent(hand, true);
            Held.transform.localPosition = Vector3.zero;
            Held.transform.localRotation = Quaternion.identity;
        }

        void Release(bool place)
        {
            if (Held == null)
            {
                return;
            }

            Ray ray = interactionCamera.ScreenPointToRay(Pointer);
            RaycastHit hit = new RaycastHit();
            bool hitSurface = place && Physics.Raycast(ray, out hit, reach, ~0, QueryTriggerInteraction.Ignore);
            bool overMicrowave = hitSurface && hit.collider.transform.IsChildOf(microwave.transform);

            if (overMicrowave)
            {
                if (!microwave.PutFood(Held)) return;
                RestoreHeldColliders();
                Held = null;
                return;
            }

            Held.transform.SetParent(null, true);
            RestoreHeldColliders();
            if (hitSurface && hit.collider == servingTable)
            {
                Held.Place();
                Held.transform.rotation = Quaternion.identity;
                Bounds surface = servingTable.bounds;
                Held.transform.position = new Vector3(surface.center.x, surface.max.y, surface.center.z);
                Physics.SyncTransforms();
                float bottom = float.MaxValue;
                foreach (Collider part in heldColliders)
                {
                    if (part.enabled && !part.isTrigger)
                        bottom = Mathf.Min(bottom, part.bounds.min.y);
                }
                if (bottom < float.MaxValue)
                    Held.transform.position += Vector3.up * (surface.max.y - bottom + .003f);
                MicrowaveFood served = Held;
                Held = null;
                Physics.SyncTransforms();
                KitchenGameResult result = FindObjectOfType<KitchenGameResult>();
                if (result != null) result.ServeFood(served);
                return;
            }
            Held.Drop();
            if (place && kitchenNoise != null)
            {
                kitchenNoise.AddItemDropNoise();
            }
            Held = null;
        }

        void RestoreHeldColliders()
        {
            for (int i = 0; i < heldColliders.Length; i++)
            {
                if (heldColliders[i] != null) heldColliders[i].enabled = colliderStates[i];
            }
        }

        void OnDisable()
        {
            Release(false);
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused)
            {
                Release(false);
            }
        }
    }
}
