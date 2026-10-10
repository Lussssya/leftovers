using UnityEngine;

namespace Leftovers
{
    public class MouseDoorInteractor : MonoBehaviour
    {
        public Camera interactionCamera;
        public float reach = 4.5f;
        public float degreesPerScreenHeight = 210f;
        public LayerMask interactionLayers = ~0;
        public FridgeDoorHandle Hovered { get; private set; }
        public FridgeDoorHandle Held { get; private set; }
        public Vector2 DragAxis { get; private set; } = Vector2.right;
        public Camera InteractionCamera { get { return interactionCamera; } }

        Vector2 lastPointer;
        CursorLockMode previousCursorMode;

        void Awake()
        {
            if (interactionCamera == null)
            {
                interactionCamera = GetComponent<Camera>();
            }
        }

        void Update()
        {
            if (interactionCamera == null)
            {
                return;
            }

            if (Time.timeScale == 0f || Input.GetKeyDown(KeyCode.Escape))
            {
                Release(true);
                return;
            }

            Vector2 pointer = Input.mousePosition;
            if (Held != null)
            {
                DragHandle(pointer);
                return;
            }

            FindHandle(pointer);
            if (Hovered != null && Input.GetMouseButtonDown(0))
            {
                GrabHandle(pointer);
            }
        }

        void FindHandle(Vector2 pointer)
        {
            Hovered = null;
            Physics.SyncTransforms();
            Ray ray = interactionCamera.ScreenPointToRay(pointer);
            RaycastHit hit;
            bool found = Physics.Raycast(ray, out hit, reach, interactionLayers, QueryTriggerInteraction.Collide);

            if (!found)
            {
                return;
            }

            FridgeDoorHandle handle = hit.collider.GetComponent<FridgeDoorHandle>();
            if (handle != null && handle.Door != null && handle.Door.isActiveAndEnabled)
            {
                Hovered = handle;
            }
        }

        void GrabHandle(Vector2 pointer)
        {
            Held = Hovered;
            lastPointer = pointer;
            FridgeDoor door = Held.Door;
            Vector3 localHandle = door.transform.InverseTransformPoint(Held.transform.position);
            Vector2 start = interactionCamera.WorldToScreenPoint(door.PointAtAngle(localHandle, 10f));
            Vector2 end = interactionCamera.WorldToScreenPoint(door.PointAtAngle(localHandle, 90f));
            Vector2 direction = end - start;
            DragAxis = Vector2.right;

            if (direction.sqrMagnitude > 16f)
            {
                DragAxis = direction.normalized;
            }

            previousCursorMode = Cursor.lockState;
            Cursor.lockState = CursorLockMode.Confined;
            door.BeginHold();
        }

        void DragHandle(Vector2 pointer)
        {
            FridgeDoor door = Held.Door;
            float distance = Vector3.Distance(interactionCamera.transform.position, Held.transform.position);
            if (door == null || !door.isActiveAndEnabled || !door.IsHeld || distance > reach + 0.5f)
            {
                Release(true);
                return;
            }

            if (!Input.GetMouseButton(0))
            {
                Release(false);
                return;
            }

            Vector2 movement = pointer - lastPointer;
            lastPointer = pointer;
            float drag = Vector2.Dot(movement, DragAxis);
            float degrees = drag * degreesPerScreenHeight / Mathf.Max(1, Screen.height);
            door.DragBy(degrees);
        }

        void Release(bool cancelMomentum)
        {
            if (Held != null)
            {
                if (Held.Door != null)
                {
                    Held.Door.EndHold(cancelMomentum);
                }

                Cursor.lockState = previousCursorMode;
            }

            Held = null;
            Hovered = null;
        }

        void OnDisable()
        {
            Release(true);
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused)
            {
                Release(true);
            }
        }
    }
}
