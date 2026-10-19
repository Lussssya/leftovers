using UnityEngine;

namespace Leftovers
{
    [DefaultExecutionOrder(-100)]
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Vector3 offset = new Vector3(0f, 7f, -8f);
        [SerializeField] float followSpeed = 6f;
        [SerializeField] float lookHeight = 0.5f;
        [SerializeField] float mouseSensitivity = 2f;
        [SerializeField] float minimumLookAngle = -25f;
        [SerializeField] float maximumLookAngle = 80f;
        [SerializeField] float shoulderOffset = 1.1f;
        [SerializeField] float edgeWidth = 120f;
        [SerializeField] float edgeWidthFraction = 0.35f;
        [SerializeField] float edgeTurnSpeed = 60f;

        public bool IsPaused { get; private set; }

        public bool IsDragging
        {
            get
            {
                if (doorInteractor != null && doorInteractor.Held != null)
                {
                    return true;
                }

                if (cabinetInteractor != null && cabinetInteractor.Held != null)
                {
                    return true;
                }

                return false;
            }
        }

        float horizontalAngle;
        float verticalAngle;
        float previousTimeScale = 1f;
        Vector3 followPosition;
        MouseDoorInteractor doorInteractor;
        MouseCabinetInteractor cabinetInteractor;
        bool skipMouseLook;
        Camera viewCamera;

        void Awake()
        {
            doorInteractor = GetComponent<MouseDoorInteractor>();
            cabinetInteractor = GetComponent<MouseCabinetInteractor>();
            viewCamera = GetComponent<Camera>();
        }

        void Start()
        {
            if (target == null)
            {
                return;
            }

            followPosition = target.position;
            Vector3 lookDirection = Vector3.up * lookHeight - offset;
            Quaternion startingRotation = Quaternion.LookRotation(lookDirection);
            horizontalAngle = startingRotation.eulerAngles.y;
            verticalAngle = Mathf.DeltaAngle(0f, startingRotation.eulerAngles.x);
            SetPaused(!Application.isFocused);
            UpdateCameraPosition();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SetPaused(!IsPaused);
            }
        }

        void UpdateMouseLook()
        {
            if (skipMouseLook)
            {
                skipMouseLook = false;
                return;
            }

            if (IsDragging || Input.GetMouseButton(0) || Input.GetMouseButtonUp(0))
            {
                return;
            }

            Rect view = viewCamera.pixelRect;
            Vector2 pointer = Input.mousePosition;
            if (!view.Contains(pointer))
            {
                return;
            }

            float edgeTurnAmount = edgeTurnSpeed * mouseSensitivity * 0.5f * Time.deltaTime;
            float horizontalEdge = GetEdgeInput(pointer.x, view.xMin, view.xMax);
            float verticalEdge = GetEdgeInput(pointer.y, view.yMin, view.yMax);
            horizontalAngle = horizontalAngle + horizontalEdge * edgeTurnAmount;
            verticalAngle = verticalAngle - verticalEdge * edgeTurnAmount;
            verticalAngle = Mathf.Clamp(verticalAngle, minimumLookAngle, maximumLookAngle);
        }

        float GetEdgeInput(float position, float minimum, float maximum)
        {
            float viewSize = maximum - minimum;
            float width = Mathf.Max(edgeWidth, viewSize * edgeWidthFraction);
            width = Mathf.Min(width, viewSize * 0.45f);
            if (width <= 0f)
            {
                return 0f;
            }

            if (position < minimum + width)
            {
                float amount = Mathf.InverseLerp(minimum + width, minimum, position);
                return -Mathf.SmoothStep(0f, 1f, amount);
            }

            if (position > maximum - width)
            {
                float amount = Mathf.InverseLerp(maximum - width, maximum, position);
                return Mathf.SmoothStep(0f, 1f, amount);
            }

            return 0f;
        }

        void LateUpdate()
        {
            if (target == null || IsPaused || Time.timeScale == 0f || IsDragging)
            {
                return;
            }

            UpdateMouseLook();
            UpdateCameraPosition();
        }

        void UpdateCameraPosition()
        {
            float followAmount = Mathf.Clamp01(followSpeed * Time.deltaTime);
            followPosition = Vector3.Lerp(followPosition, target.position, followAmount);
            Vector3 lookPosition = followPosition + Vector3.up * lookHeight;
            float followDistance = (offset - Vector3.up * lookHeight).magnitude;
            Quaternion viewRotation = Quaternion.Euler(verticalAngle, horizontalAngle, 0f);
            Vector3 cameraOffset = new Vector3(shoulderOffset, 0f, -followDistance);
            Vector3 cameraPosition = lookPosition + viewRotation * cameraOffset;
            float minimumHeight = target.position.y + 0.2f;
            if (cameraPosition.y < minimumHeight)
            {
                cameraPosition.y = minimumHeight;
            }

            transform.position = cameraPosition;
            transform.rotation = viewRotation;
        }

        public void SetPaused(bool paused)
        {
            if (paused && !IsPaused)
            {
                previousTimeScale = Time.timeScale;
                Time.timeScale = 0f;
            }
            else if (!paused && IsPaused)
            {
                Time.timeScale = previousTimeScale;
            }

            IsPaused = paused;
            skipMouseLook = true;

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void OnDisable()
        {
            if (IsPaused)
            {
                Time.timeScale = previousTimeScale;
                IsPaused = false;
            }

            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        void OnApplicationFocus(bool focused)
        {
            if (!focused)
            {
                SetPaused(true);
            }
        }
    }
}
