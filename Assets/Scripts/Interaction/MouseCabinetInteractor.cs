using UnityEngine;

namespace Leftovers
{
    public class MouseCabinetInteractor : MonoBehaviour
    {
        public Camera interactionCamera;
        public float reach = 5.5f;
        public Transform interactionOrigin;
        public CabinetPart Hovered { get; private set; }
        public CabinetPart Held { get { return heldPart; } }

        CabinetPart heldPart;
        Vector2 dragAxis;
        Vector2 lastPointer;
        Vector3 heldLocalPoint;

        void Awake()
        {
            if (interactionCamera == null)
            {
                interactionCamera = GetComponent<Camera>();
            }
        }

        void Update()
        {
            Hovered = null;

            if (interactionCamera == null)
            {
                return;
            }

            if (Time.timeScale == 0f || Input.GetKeyDown(KeyCode.Escape))
            {
                Release(true);
                return;
            }

            if (heldPart != null)
            {
                DragPart();
                return;
            }

            Vector2 pointer = Input.mousePosition;
            FindPart(pointer);
        }

        void FindPart(Vector2 pointer)
        {
            Hovered = null;
            Ray ray = interactionCamera.ScreenPointToRay(pointer);
            RaycastHit hit;
            Physics.SyncTransforms();
            float rayDistance = reach;
            if (interactionOrigin != null)
            {
                rayDistance = rayDistance + Vector3.Distance(interactionCamera.transform.position, interactionOrigin.position);
            }

            if (!Physics.Raycast(ray, out hit, rayDistance, ~0, QueryTriggerInteraction.Ignore))
            {
                return;
            }

            CabinetPart part = hit.collider.GetComponentInParent<CabinetPart>();
            if (part == null || !part.isActiveAndEnabled || DistanceToObject(hit.point) > reach)
            {
                return;
            }

            Hovered = part;
            if (Input.GetMouseButtonDown(0))
            {
                GrabPart(part, hit.point, pointer);
            }
        }

        void GrabPart(CabinetPart part, Vector3 hitPoint, Vector2 pointer)
        {
            heldPart = part;
            lastPointer = pointer;

            Vector3 localPoint = part.transform.InverseTransformPoint(hitPoint);
            heldLocalPoint = localPoint;
            Vector3 closedPoint = part.PointAtOpen(localPoint, 0f);
            Vector3 openPoint = part.PointAtOpen(localPoint, part.maximumOpen);
            if (part.hingedDoor != null)
            {
                closedPoint = part.PointAtOpen(localPoint, 10f);
                openPoint = part.PointAtOpen(localPoint, 90f);
            }
            Vector2 start = interactionCamera.WorldToScreenPoint(closedPoint);
            Vector2 end = interactionCamera.WorldToScreenPoint(openPoint);
            Vector2 direction = end - start;

            if (direction.sqrMagnitude > 16f)
            {
                dragAxis = direction.normalized;
            }
            else if (part.slidesOut)
            {
                dragAxis = Vector2.down;
            }
            else
            {
                dragAxis = Vector2.right;
            }

            part.BeginHold();
        }

        void DragPart()
        {
            Vector3 heldPoint = heldPart.transform.TransformPoint(heldLocalPoint);
            float distance = DistanceToObject(heldPoint);
            if (!heldPart.isActiveAndEnabled || !heldPart.IsHeld || distance > reach + 0.5f)
            {
                Release(true);
                return;
            }

            if (!Input.GetMouseButton(0))
            {
                Release();
                return;
            }

            Vector2 pointer = Input.mousePosition;
            Vector2 movement = pointer - lastPointer;
            lastPointer = pointer;

            float drag = Vector2.Dot(movement, dragAxis);
            float amount = drag * heldPart.maximumOpen * 1.5f / Mathf.Max(1, Screen.height);
            if (heldPart.hingedDoor != null)
            {
                amount = drag * 210f / Mathf.Max(1, Screen.height);
            }
            heldPart.DragBy(amount);
        }

        float DistanceToObject(Vector3 point)
        {
            Vector3 origin = interactionCamera.transform.position;
            if (interactionOrigin != null)
            {
                origin = interactionOrigin.position;
            }

            return Vector3.Distance(origin, point);
        }

        void Release(bool cancelMomentum = false)
        {
            if (heldPart == null)
            {
                return;
            }

            heldPart.EndHold(cancelMomentum);
            heldPart = null;
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
