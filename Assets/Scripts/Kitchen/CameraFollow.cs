using UnityEngine;

namespace Leftovers
{
    [DefaultExecutionOrder(-100)]
    public class CameraFollow : MonoBehaviour
    {
        [SerializeField] Transform target;
        [SerializeField] Transform cameraPivot;
        [SerializeField] float edgeThreshold = 0.08f;
        [SerializeField] float edgeLookSpeed = 60f;
        [SerializeField] float defaultPitch = 12f;
        [SerializeField] float minPitch = -6f;
        [SerializeField] float maxPitch = 25f;

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

        float pitch;
        float previousTimeScale = 1f;
        MouseDoorInteractor doorInteractor;
        MouseCabinetInteractor cabinetInteractor;
        bool skipMouseLook;
        Camera viewCamera;

        void Awake()
        {
            doorInteractor = GetComponent<MouseDoorInteractor>();
            cabinetInteractor = GetComponent<MouseCabinetInteractor>();
            viewCamera = GetComponent<Camera>();

            if (cameraPivot == null)
            {
                cameraPivot = transform.parent;
            }
        }

        void Start()
        {
            if (cameraPivot != null)
            {
                pitch = Mathf.Clamp(defaultPitch, minPitch, maxPitch);
                ApplyPitch();
            }

            SetPaused(!Application.isFocused);
        }

        void Update()
        {
            KeepCursorAvailable();

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                SetPaused(!IsPaused);
            }

            if (IsPaused || Time.timeScale == 0f)
            {
                return;
            }

            UpdateEdgeLook();
        }

        void UpdateEdgeLook()
        {
            if (skipMouseLook)
            {
                skipMouseLook = false;
                return;
            }

            if (target == null || cameraPivot == null || viewCamera == null || IsDragging
                || Input.GetMouseButton(0) || Input.GetMouseButtonUp(0))
            {
                return;
            }

            Rect view = viewCamera.pixelRect;
            Vector2 pointer = Input.mousePosition;
            if (!view.Contains(pointer))
            {
                return;
            }

            float normalizedX = Mathf.InverseLerp(view.xMin, view.xMax, pointer.x);
            float normalizedY = Mathf.InverseLerp(view.yMin, view.yMax, pointer.y);
            float horizontalInput = GetEdgeInput(normalizedX);
            float verticalInput = GetEdgeInput(normalizedY);
            float lookAmount = edgeLookSpeed * Time.deltaTime;

            target.Rotate(0f, horizontalInput * lookAmount, 0f, Space.World);
            pitch = pitch - verticalInput * lookAmount;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);
            ApplyPitch();
        }

        float GetEdgeInput(float normalizedPosition)
        {
            float threshold = Mathf.Clamp(edgeThreshold, 0.01f, 0.45f);
            if (normalizedPosition < threshold)
            {
                float amount = Mathf.InverseLerp(threshold, 0f, normalizedPosition);
                return -Mathf.SmoothStep(0f, 1f, amount);
            }

            if (normalizedPosition > 1f - threshold)
            {
                float amount = Mathf.InverseLerp(1f - threshold, 1f, normalizedPosition);
                return Mathf.SmoothStep(0f, 1f, amount);
            }

            return 0f;
        }

        void ApplyPitch()
        {
            cameraPivot.localRotation = Quaternion.Euler(pitch, 0f, 0f);
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
            KeepCursorAvailable();
        }

        void KeepCursorAvailable()
        {
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
            else
            {
                KeepCursorAvailable();
            }
        }
    }
}
