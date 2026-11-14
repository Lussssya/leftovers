using UnityEngine;
using UnityEngine.UIElements;
using Leftovers;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider))]
[DefaultExecutionOrder(100)]
public class FirstPersonController : MonoBehaviour
{
    public Camera playerCamera;
    public Transform joint;
    public float fov = 66f;
    public bool invertCamera;
    public bool cameraCanMove = true;
    public float mouseSensitivity = 2f;
    public float maxLookAngle = 75f;
    public bool playerCanMove = true;
    public float walkSpeed = 3f;
    public float maxVelocityChange = 20f;
    public bool enableSprint = true;
    public KeyCode sprintKey = KeyCode.LeftShift;
    public float sprintSpeed = 4.5f;
    public bool enableJump;
    public KeyCode jumpKey = KeyCode.Space;
    public float jumpPower = 4f;
    public bool enableCrouch = true;
    public bool holdToCrouch = true;
    public KeyCode crouchKey = KeyCode.LeftControl;
    public float crouchHeight = .6f;
    public float speedReduction = .5f;
    public bool crosshair = true;
    public bool enableHeadBob = true;
    public float bobSpeed = 8f;
    public Vector3 bobAmount = new Vector3(0f,.018f,0f);
    [SerializeField] AudioSource footstepSource;
    [SerializeField] AudioClip footstepClip;
    [SerializeField] float stepDistance = 1.4f;
    [SerializeField] [Range(0f, 1f)] float footstepVolume = 0.15f;
    public bool IsPaused { get; private set; }

    VisualElement aimDot;
    PanelSettings aimPanel;
    MouseFoodInteractor food;
    Rigidbody rb;
    CapsuleCollider capsule;
    MouseDoorInteractor doors;
    MouseCabinetInteractor cabinets;
    Vector3 jointOriginalPos;
    Vector3 standingCenter;
    Vector2 movement;
    float standingHeight;
    float pitch;
    float timer;
    float previousTimeScale = 1f;
    bool isCrouched;
    bool interacting;
    bool sprinting;
    bool jumpRequested;
    bool skipLook = true;
    Vector3 previousStepPosition;
    float distanceSinceStep;

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
        capsule = GetComponent<CapsuleCollider>();
        if (playerCamera == null) playerCamera = GetComponentInChildren<Camera>();
        if (playerCamera == null) { enabled = false; return; }
        if (joint == null) joint = playerCamera.transform;
        jointOriginalPos = joint.localPosition;
        standingHeight = capsule.height;
        standingCenter = capsule.center;
        doors = playerCamera.GetComponent<MouseDoorInteractor>();
        cabinets = playerCamera.GetComponent<MouseCabinetInteractor>();
        food = playerCamera.GetComponent<MouseFoodInteractor>();
        if (crosshair)
        {
            if (doors != null) doors.useCenterAim = true;
            if (cabinets != null) cabinets.useCenterAim = true;
            if (food != null) food.useCenterAim = true;
            CreateCrosshair();
        }

        playerCamera.fieldOfView = fov;
        pitch = playerCamera.transform.localEulerAngles.x;
        if (pitch > 180f) pitch -= 360f;
        rb.constraints = RigidbodyConstraints.FreezeRotation;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        previousStepPosition = transform.position;
    }

    void CreateCrosshair()
    {
        GameObject overlay = new GameObject("Center crosshair");
        overlay.transform.SetParent(transform,false);
        UIDocument document = overlay.AddComponent<UIDocument>();
        aimPanel = Instantiate(Resources.Load<PanelSettings>("CrosshairPanel"));
        document.panelSettings = aimPanel;
        document.sortingOrder = 100;
        VisualElement root = document.rootVisualElement;
        root.pickingMode = PickingMode.Ignore;
        root.style.position = Position.Absolute;
        root.style.left = 0;
        root.style.right = 0;
        root.style.top = 0;
        root.style.bottom = 0;
        aimDot = new VisualElement();
        aimDot.pickingMode = PickingMode.Ignore;
        aimDot.style.position = Position.Absolute;
        aimDot.style.left = Length.Percent(50);
        aimDot.style.top = Length.Percent(50);
        aimDot.style.width = 6;
        aimDot.style.height = 6;
        aimDot.style.marginLeft = -3;
        aimDot.style.marginTop = -3;
        aimDot.style.backgroundColor = new Color(1f,1f,1f,.85f);
        aimDot.style.borderTopLeftRadius = 3;
        aimDot.style.borderTopRightRadius = 3;
        aimDot.style.borderBottomLeftRadius = 3;
        aimDot.style.borderBottomRightRadius = 3;
        root.Add(aimDot);
    }

    void Start()
    {
        SetPaused(!Application.isFocused);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape)) SetPaused(!IsPaused);
        if (IsPaused || Time.timeScale == 0f) return;


        interacting = (doors != null && doors.Held != null) || (cabinets != null && cabinets.Held != null);
        UnityEngine.Cursor.lockState = CursorLockMode.Locked;
        UnityEngine.Cursor.visible = false;

        if (cameraCanMove && !skipLook && !interacting)
        {
            float vertical = Input.GetAxisRaw("Mouse Y");
            if (invertCamera) vertical = -vertical;
            pitch = Mathf.Clamp(pitch - vertical * mouseSensitivity, -maxLookAngle, maxLookAngle);
            float horizontal = Input.GetAxisRaw("Mouse X");
            transform.Rotate(0f, horizontal * mouseSensitivity, 0f);
            playerCamera.transform.localRotation = Quaternion.Euler(pitch,0f,0f);
        }

        skipLook = false;
        movement = Vector2.zero;
        if (playerCanMove && !interacting)
            movement = Vector2.ClampMagnitude(new Vector2(Input.GetAxisRaw("Horizontal"),Input.GetAxisRaw("Vertical")),1f);
        sprinting = enableSprint && !interacting && Input.GetKey(sprintKey);

        bool crouch = false;
        if (enableCrouch)
        {
            if (holdToCrouch) crouch = Input.GetKey(crouchKey);
            else
            {
                crouch = isCrouched;
                if (Input.GetKeyDown(crouchKey)) crouch = !crouch;
            }
        }
        if (!interacting) SetCrouched(crouch);
        if (enableJump && !interacting && Input.GetKeyDown(jumpKey)) jumpRequested = true;
    }

    void FixedUpdate()
    {
        if (IsPaused || Time.timeScale == 0f) return;
        if ((doors != null && doors.Held != null) || (cabinets != null && cabinets.Held != null))
        {
            rb.velocity = new Vector3(0f, rb.velocity.y, 0f);
            jumpRequested = false;
            return;
        }
        float speed = walkSpeed;
        if (sprinting) speed = sprintSpeed;
        if (isCrouched) speed = walkSpeed * speedReduction;
        Vector3 target = (transform.right * movement.x + transform.forward * movement.y) * speed;
        Vector3 current = new Vector3(rb.velocity.x,0f,rb.velocity.z);
        Vector3 change = Vector3.ClampMagnitude(target-current,maxVelocityChange * Time.fixedDeltaTime);
        rb.AddForce(change,ForceMode.VelocityChange);
        if (jumpRequested)
        {
            RaycastHit hit;
            if (rb.SweepTest(Vector3.down,out hit,.12f,QueryTriggerInteraction.Ignore))
                rb.AddForce(Vector3.up * jumpPower,ForceMode.VelocityChange);
            jumpRequested = false;
        }
    }

    public void SetCrouched(bool crouch)
    {
        if (crouch == isCrouched) return;
        if (!crouch)
        {
            Vector3 center = transform.TransformPoint(standingCenter);
            float radius = capsule.radius * .95f;
            float half = standingHeight * .5f - capsule.radius;
            Collider[] nearby = Physics.OverlapCapsule(center+Vector3.up*half,center-Vector3.up*half,radius,~0,QueryTriggerInteraction.Ignore);
            foreach (Collider other in nearby)
                if (!other.transform.IsChildOf(transform)) return;
        }
        isCrouched = crouch;
        float height = standingHeight;
        if (crouch) height *= Mathf.Clamp(crouchHeight,.4f,1f);
        capsule.height = height;
        capsule.center = standingCenter - Vector3.up * (standingHeight-height)*.5f;
    }

    void LateUpdate()
    {
        UpdateFootsteps();
        if (aimDot != null) aimDot.visible = crosshair && !IsPaused;
        if (IsPaused || Time.timeScale == 0f) return;
        Vector3 position = jointOriginalPos;
        if (isCrouched) position.y -= standingHeight-capsule.height;
        Vector3 walkingVelocity = new Vector3(rb.velocity.x,0f,rb.velocity.z);
        if (enableHeadBob && movement.sqrMagnitude > .01f && walkingVelocity.sqrMagnitude > .04f && !interacting && (food == null || food.Held == null))
        {
            timer += Time.deltaTime * bobSpeed;
            position += new Vector3(Mathf.Cos(timer)*bobAmount.x,Mathf.Sin(timer*2f)*bobAmount.y,0f);
        }
        else timer = 0f;
        joint.localPosition = Vector3.Lerp(joint.localPosition,position,1f-Mathf.Exp(-14f*Time.deltaTime));
    }

    void UpdateFootsteps()
    {
        Vector3 displacement = transform.position - previousStepPosition;
        previousStepPosition = transform.position;
        displacement.y = 0f;
        RaycastHit ground;
        if (IsPaused || Time.timeScale == 0f || interacting ||
            movement.sqrMagnitude < 0.01f || displacement.magnitude > 1f ||
            !rb.SweepTest(Vector3.down, out ground, 0.15f, QueryTriggerInteraction.Ignore))
        {
            distanceSinceStep = 0f;
            return;
        }

        distanceSinceStep += displacement.magnitude;
        if (distanceSinceStep >= Mathf.Max(0.2f, stepDistance))
        {
            distanceSinceStep = 0f;
            if (footstepSource != null && footstepClip != null)
            {
                footstepSource.PlayOneShot(footstepClip, footstepVolume);
            }
        }
    }

    public void SetPaused(bool paused)
    {
        if (paused && !IsPaused) { previousTimeScale = Time.timeScale; Time.timeScale = 0f; }
        if (!paused && IsPaused) Time.timeScale = previousTimeScale;
        IsPaused = paused;
        skipLook = true;
        movement = Vector2.zero;
        jumpRequested = false;
        interacting = false;
        UnityEngine.Cursor.lockState = paused ? CursorLockMode.None : CursorLockMode.Locked;
        UnityEngine.Cursor.visible = paused;
    }

    void OnApplicationFocus(bool focused)
    {
        if (!focused) SetPaused(true);
    }

    void OnDestroy()
    {
        if (aimPanel != null) Destroy(aimPanel);
    }

    void OnDisable()
    {
        if (aimDot != null) aimDot.visible = false;
        if (IsPaused) Time.timeScale = previousTimeScale;
        UnityEngine.Cursor.lockState = CursorLockMode.None;
        UnityEngine.Cursor.visible = true;
    }
}
