using UnityEngine;

namespace Leftovers
{
    public class MouseFoodInteractor : MonoBehaviour
    {
        public Camera interactionCamera;
        public MouseDoorInteractor doorInteractor;
        public Microwave microwave;
        public Transform foodRest;
        public float reach = 40f;

        public MicrowaveFood Held { get; private set; }

        void Update()
        {
            if (Time.timeScale == 0f || Input.GetKeyDown(KeyCode.Escape))
            {
                Release(false);
                return;
            }

            if (Held != null)
            {
                MoveFood();
                if (!Input.GetMouseButton(0))
                {
                    Release(true);
                }
                return;
            }

            if (!Input.GetMouseButtonDown(0) || doorInteractor.Held != null)
            {
                return;
            }

            Physics.SyncTransforms();
            Ray ray = interactionCamera.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;
            if (!Physics.Raycast(ray, out hit, reach, ~0, QueryTriggerInteraction.Collide))
            {
                return;
            }

            MicrowaveFood food = hit.collider.GetComponentInParent<MicrowaveFood>();
            if (food == null)
            {
                return;
            }
            if (microwave.food == food && !microwave.TakeFood(food))
            {
                return;
            }

            food.transform.SetParent(null, true);
            Held = food;
            Held.Hold();
        }

        void MoveFood()
        {
            Ray ray = interactionCamera.ScreenPointToRay(Input.mousePosition);
            Plane plane = new Plane(Vector3.up, microwave.turntable.position);
            float distance;
            if (plane.Raycast(ray, out distance))
            {
                Held.MoveTo(ray.GetPoint(distance) + Vector3.up * 0.05f);
            }
        }

        void Release(bool place)
        {
            if (Held == null)
            {
                return;
            }

            bool overTray = false;
            if (place)
            {
                Ray ray = interactionCamera.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit, reach, ~0, QueryTriggerInteraction.Collide))
                {
                    overTray = hit.collider.transform.IsChildOf(microwave.turntable);
                }
                if (Vector3.Distance(Held.transform.position, microwave.turntable.position) < 0.5f)
                {
                    overTray = true;
                }
            }

            if (overTray && microwave.PutFood(Held))
            {
                Held = null;
                return;
            }
            if (place && foodRest != null && Vector3.Distance(Held.transform.position, foodRest.position) < 0.45f && Held.CanReach(foodRest.position))
            {
                Held.Place();
                Held.transform.SetParent(foodRest, false);
                Held.transform.localPosition = Vector3.zero;
                Held.transform.localRotation = Quaternion.identity;
                Held.transform.localScale = Vector3.one;
            }
            else
            {
                Held.Drop();
            }
            Held = null;
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
