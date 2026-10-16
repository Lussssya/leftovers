using UnityEngine;

namespace Leftovers
{
    public class MouseDoorInteractor : MonoBehaviour
    {
        public Camera interactionCamera;
        public float reach = 4.5f;
        public float degreesPerScreenHeight = 210f;
        public float drawerDistancePerScreenHeight = 1.2f;
        public LayerMask interactionLayers = ~0;
        public FridgeDoorHandle Hovered { get; private set; }
        public FridgeDoorHandle Held { get; private set; }
        public DrawerHandle HoveredDrawer { get; private set; }
        public DrawerHandle HeldDrawer { get; private set; }
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
            if (Held != null || HeldDrawer != null)
            {
                DragHandle(pointer);
                return;
            }

            FindHandle(pointer);
            if (Input.GetMouseButtonDown(0))
            {
                if (Hovered != null)
                {
                    GrabHandle(pointer);
                }
                else if (HoveredDrawer != null)
                {
                    GrabDrawerHandle(pointer);
                }
            }
        }

        void FindHandle(Vector2 pointer)
        {
            Hovered = null;
            HoveredDrawer = null;
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
                return;
            }

            DrawerHandle drawerHandle = hit.collider.GetComponent<DrawerHandle>();
            if (drawerHandle != null && drawerHandle.Drawer != null && drawerHandle.Drawer.isActiveAndEnabled)
            {
                HoveredDrawer = drawerHandle;
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

        void GrabDrawerHandle(Vector2 pointer)
        {
            HeldDrawer = HoveredDrawer;
            lastPointer = pointer;
            Drawer drawer = HeldDrawer.Drawer;
            Vector3 localHandle = drawer.transform.InverseTransformPoint(HeldDrawer.transform.position);
            Vector2 start = interactionCamera.WorldToScreenPoint(drawer.PointAtDistance(localHandle, 0f));
            Vector2 end = interactionCamera.WorldToScreenPoint(
                drawer.PointAtDistance(localHandle, drawer.MaximumOpeningDistance));
            Vector2 direction = end - start;
            DragAxis = Vector2.right;

            if (direction.sqrMagnitude > 16f)
            {
                DragAxis = direction.normalized;
            }

            previousCursorMode = Cursor.lockState;
            Cursor.lockState = CursorLockMode.Confined;
            drawer.BeginHold();
        }

        void DragHandle(Vector2 pointer)
        {
            if (HeldDrawer != null)
            {
                DragDrawerHandle(pointer);
                return;
            }

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

        void DragDrawerHandle(Vector2 pointer)
        {
            Drawer drawer = HeldDrawer.Drawer;
            float distance = Vector3.Distance(interactionCamera.transform.position, HeldDrawer.transform.position);
            if (drawer == null || !drawer.isActiveAndEnabled || !drawer.IsHeld || distance > reach + 0.5f)
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
            float drawerDistance = drag * drawerDistancePerScreenHeight / Mathf.Max(1, Screen.height);
            drawer.DragBy(drawerDistance);
        }

        void Release(bool cancelMomentum)
        {
            bool wasHolding = Held != null || HeldDrawer != null;

            if (Held != null)
            {
                if (Held.Door != null)
                {
                    Held.Door.EndHold(cancelMomentum);
                }
            }

            if (HeldDrawer != null)
            {
                if (HeldDrawer.Drawer != null)
                {
                    HeldDrawer.Drawer.EndHold(cancelMomentum);
                }
            }

            if (wasHolding)
            {
                Cursor.lockState = previousCursorMode;
            }

            Held = null;
            Hovered = null;
            HeldDrawer = null;
            HoveredDrawer = null;
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
