
    //Old freemovement 2d cartesian
    
    // private void HandleMovement()
    // {
    //     Vector3 forward = mainCamera.forward;
    //     Vector3 right = mainCamera.right;
    //     forward.y = 0f; right.y = 0f;
    //     forward.Normalize(); right.Normalize();

    //     Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;

    //     float yawCamera = mainCamera.eulerAngles.y;
    //     float currentRotSpeed = isAiming ? (rotationSpeed * 2f) : rotationSpeed;
        
    //     transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, yawCamera, 0), Time.deltaTime * currentRotSpeed);

    //     if (characterController.isGrounded) verticalVelocity = -2f;
    //     else verticalVelocity += gravity * Time.deltaTime;

    //     float currentSpeedLimit = isAiming ? aimMoveSpeed : moveSpeed;
    //     Vector3 velocity = moveDirection * currentSpeedLimit;
    //     velocity.y = verticalVelocity;
        
    //     characterController.Move(velocity * Time.deltaTime);
    // }



using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Animations.Rigging; 

public class PlayerController : MonoBehaviour
{
    [Header("References")]
    public GameObject aimVirtualCamera; 
    public Transform mainCamera; 
    public GameObject cameraTarget; 
    public Transform worldAimTarget; 
    
    [Tooltip("Drag your AimRig GameObject here (the one with the Rig component)")]
    public Rig aimRig; // BACK TO MASTER RIG CONTROL

    [Header("Movement Settings")]
    public float moveSpeed = 4f;
    public float aimMoveSpeed = 2f;
    public float rotationSpeed = 10f;
    private float gravity = -9.81f;
    private float verticalVelocity;

    [Header("Camera Look Settings")]
    public float lookSensitivity = 0.2f;
    public float topClamp = 70.0f;
    public float bottomClamp = -30.0f;
    private float cinemachineTargetYaw;
    private float cinemachineTargetPitch;

    [Header("Raycast Targeting Settings")]
    public LayerMask aimColliderMask;
    public float maxAimDistance = 100f;

    private Animator animator;
    private PlayerControls inputActions;
    private CharacterController characterController;
    
    private Vector2 moveInput;
    private Vector2 lookInput;
    private bool isAiming;
    private float currentAnimX, currentAnimY, currentSpeed;
    private float aimLayerWeight;

    [Header("Combat Settings")]
        public WeaponComponent equippedWeapon;
    private bool isFiring;

    [Header("UI References")]
        public GameObject crosshairUI;


    private void Awake()
    {
        animator = GetComponent<Animator>();
        characterController = GetComponent<CharacterController>();
        inputActions = new PlayerControls();

        inputActions.Player.Aim.performed += ctx => isAiming = true;
        inputActions.Player.Aim.canceled += ctx => isAiming = false;

        inputActions.Player.Fire.performed += ctx => isFiring = true;
        inputActions.Player.Fire.canceled += ctx => isFiring = false;

        inputActions.Player.Move.performed += ctx => moveInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Move.canceled += ctx => moveInput = Vector2.zero;

        inputActions.Player.Look.performed += ctx => lookInput = ctx.ReadValue<Vector2>();
        inputActions.Player.Look.canceled += ctx => lookInput = Vector2.zero;
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void OnEnable() => inputActions.Enable();
    private void OnDisable() => inputActions.Disable();

    private void Update()
    {
        HandleRaycastTarget();
        HandleMovement();
        HandleAnimations();
        HandleCameraToggle();
        HandleShooting();
    }

    private void LateUpdate()
    {
        CameraRotation();
    }

    private void HandleRaycastTarget()
    {
        Vector2 screenCenterPoint = new Vector2(Screen.width / 2f, Screen.height / 2f);
        Ray ray = mainCamera.GetComponent<Camera>().ScreenPointToRay(screenCenterPoint);

        if (Physics.Raycast(ray, out RaycastHit raycastHit, maxAimDistance, aimColliderMask))
        {
            worldAimTarget.position = raycastHit.point;
        }
        else
        {
            worldAimTarget.position = ray.GetPoint(maxAimDistance);
        }

        // --- VISUAL DEBUGGING LASERS ---
        
        // 1. Blue Line: Camera to the Orange Sphere
        Debug.DrawLine(mainCamera.position, worldAimTarget.position, Color.blue);

        // 2. Red Line: Muzzle to the Orange Sphere
        if (equippedWeapon != null && equippedWeapon.muzzlePoint != null)
        {
            // If the red line perfectly overlaps the blue line, your IK math is flawless!
            Debug.DrawLine(equippedWeapon.muzzlePoint.position, worldAimTarget.position, Color.red);
            
            // 3. Green Line: The actual forward direction of the barrel
            // If this drifts away from the red line, your gun's WeaponSocket rotation is off.
            Debug.DrawRay(equippedWeapon.muzzlePoint.position, equippedWeapon.muzzlePoint.forward * maxAimDistance, Color.green);
        }
    }

    private void CameraRotation()
    {
        cinemachineTargetYaw += lookInput.x * lookSensitivity;
        cinemachineTargetPitch -= lookInput.y * lookSensitivity;
        cinemachineTargetPitch = Mathf.Clamp(cinemachineTargetPitch, bottomClamp, topClamp);

        cameraTarget.transform.rotation = Quaternion.Euler(cinemachineTargetPitch, cinemachineTargetYaw, 0.0f);
    }


    private void HandleMovement()
    {
        Vector3 forward = mainCamera.forward;
        Vector3 right = mainCamera.right;
        forward.y = 0f; right.y = 0f;
        forward.Normalize(); right.Normalize();

        Vector3 moveDirection = (forward * moveInput.y + right * moveInput.x).normalized;

        if (!isAiming) 
        {
            // FREE ROAM: Character physically turns their body to face where they are running
            if (moveDirection != Vector3.zero) 
            {
                Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
            }
        } 
        else 
        {
            // COMBAT STRAFING: Character locks their back to the camera to aim
            float yawCamera = mainCamera.eulerAngles.y;
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(0, yawCamera, 0), Time.deltaTime * rotationSpeed * 2f);
        }

        if (characterController.isGrounded) verticalVelocity = -2f;
        else verticalVelocity += gravity * Time.deltaTime;

        float currentSpeedLimit = isAiming ? aimMoveSpeed : moveSpeed;
        Vector3 velocity = moveDirection * currentSpeedLimit;
        velocity.y = verticalVelocity;
        
        characterController.Move(velocity * Time.deltaTime);
    }

    private void HandleAnimations()
    {
        // Fix Moonwalking: Clamp diagonal input values so magnitude never exceeds 1.0f
        Vector2 structuralInput = moveInput;
        if (structuralInput.sqrMagnitude > 1f) 
        {
            structuralInput.Normalize();
        }

        // Smooth inputs cleanly within a strict -1 to 1 boundary range
        currentAnimX = Mathf.Lerp(currentAnimX, structuralInput.x, Time.deltaTime * 10f);
        currentAnimY = Mathf.Lerp(currentAnimY, structuralInput.y, Time.deltaTime * 10f);
        currentSpeed = Mathf.Lerp(currentSpeed, structuralInput.magnitude, Time.deltaTime * 10f);

        animator.SetBool("IsAiming", isAiming);
        animator.SetFloat("Speed", currentSpeed);
        animator.SetFloat("MoveX", currentAnimX);
        animator.SetFloat("MoveY", currentAnimY);

        float targetWeight = isAiming ? 1f : 0f;
        aimLayerWeight = Mathf.Lerp(aimLayerWeight, targetWeight, Time.deltaTime * 10f);
        
        animator.SetLayerWeight(1, aimLayerWeight);

        // Turn the ENTIRE Rig on and off smoothly
        if (aimRig != null)
        {
            if (isAiming)
            {
                // Normal combat tracking behavior
                aimRig.weight = aimLayerWeight;
            }
            else
            {
                // POSTURE FIX: Reads the curve value from your specific Rifle Run animation.
                // If any other animation plays (like Idle), it reads 0, keeping the rig completely off.
                float correctionValue = animator.GetFloat("PostureCorrection");
                aimRig.weight = correctionValue;
            }
        }
    }

    private void HandleCameraToggle()
    {
        aimVirtualCamera.SetActive(isAiming);
        
        if (crosshairUI != null) 
        {
            crosshairUI.SetActive(true); 

            UnityEngine.UI.Image crosshairImg = crosshairUI.GetComponent<UnityEngine.UI.Image>();
            if (crosshairImg != null)
            {
                Color c = crosshairImg.color;
                // 1f is solid (Aiming), 0.7f is highly visible but slightly dimmed (Free Roam)
                c.a = isAiming ? 1f : 0.7f; 
                crosshairImg.color = c;
            }
        }
    }

    private void HandleShooting()
        {
            if (isFiring && isAiming && equippedWeapon != null)
            {
                // CRITICAL: We pass the orange sphere's position!
                equippedWeapon.PullTrigger(worldAimTarget.position);
            }
            else if (!isFiring && equippedWeapon != null)
            {
                equippedWeapon.StopFiring();
            }
        }
}