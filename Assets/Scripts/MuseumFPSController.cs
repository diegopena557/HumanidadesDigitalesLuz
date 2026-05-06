using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class MuseumFPSController : MonoBehaviour
{
    //  INSPECTOR

    [Header("Movimiento")]
    [SerializeField] private float walkSpeed = 3.0f;
    [SerializeField] private float runSpeed = 6.0f;
    [SerializeField] private float smoothTime = 0.08f;

    [Header("Cámara")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float mouseSensitivity = 0.15f;  // más bajo que en Old Input
    [SerializeField] private float verticalClamp = 80f;
    [SerializeField] private bool invertY = false;

    [Header("Gravedad")]
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float groundCheckDist = 0.15f;
    [SerializeField] private LayerMask groundMask;

    [Header("Interacción")]
    [SerializeField] private float interactRange = 3.0f;
    [SerializeField] private LayerMask interactMask;


    //  PRIVADOS


    private CharacterController _cc;

    // Input
    private Vector2 _moveInput;
    private Vector2 _lookInput;
    private bool _isRunning;

    // Movimiento
    private Vector3 _velocity;
    private Vector3 _currentMoveVel;
    private Vector3 _moveDampVel;

    // Cámara
    private float _xRotation = 0f;

    // Estado
    private bool _isGrounded;
    private bool _cursorLocked = true;
    private bool _canMove = true;


    //  UNITY LIFECYCLE


    private void Awake()
    {
        // Deshabilita todos los maps primero
        var playerInput = GetComponent<PlayerInput>();
        foreach (var map in playerInput.actions.actionMaps)
            map.Disable();

        // Habilita solo el que necesitas
        playerInput.actions.FindActionMap("Player").Enable();
        _cc = GetComponent<CharacterController>();

        if (cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null) cameraTransform = cam.transform;
            else Debug.LogError("[MuseumFPS] Asigna la cámara en el inspector.");
        }

        LockCursor(true);
    }

    private void Update()
    {
        HandleGroundCheck();
        HandleCamera();
        HandleMovement();
    }


    //  CALLBACKS DEL NEW INPUT SYSTEM
    //  (Unity los llama automáticamente si usas
    //   PlayerInput component + Send Messages)


    /// <summary>Action: Move (Value, Vector2)</summary>
    public void OnMove(InputValue value)
    {
        _moveInput = value.Get<Vector2>();
    }

    /// <summary>Action: Look (Value, Vector2 — Delta position)</summary>
    public void OnLook(InputValue value)
    {
        _lookInput = value.Get<Vector2>();
    }

    /// <summary>Action: Sprint (Button)</summary>
    public void OnSprint(InputValue value)
    {
        _isRunning = value.isPressed;
    }

    /// <summary>Action: Interact (Button) — tecla E</summary>
    public void OnInteract(InputValue value)
    {
        if (!value.isPressed || !_cursorLocked) return;
        HandleInteraction();
    }

    /// <summary>Action: ToggleCursor (Button) — tecla Escape</summary>
    public void OnToggleCursor(InputValue value)
    {
        if (value.isPressed)
            LockCursor(!_cursorLocked);
    }


    //  GROUND CHECK


    private void HandleGroundCheck()
    {
        Vector3 spherePos = transform.position +
                            Vector3.down * (_cc.height / 2f - _cc.radius);

        _isGrounded = Physics.CheckSphere(spherePos,
                                          _cc.radius + groundCheckDist,
                                          groundMask,
                                          QueryTriggerInteraction.Ignore);

        if (_isGrounded && _velocity.y < 0f)
            _velocity.y = -2f;

        _velocity.y += gravity * Time.deltaTime;
        _cc.Move(_velocity * Time.deltaTime);
    }


    //  CÁMARA


    private void HandleCamera()
    {
        if (!_cursorLocked) return;

        float mouseX = _lookInput.x * mouseSensitivity;
        float mouseY = (_lookInput.y * mouseSensitivity) * (invertY ? 1f : -1f);

        transform.Rotate(Vector3.up * mouseX);

        _xRotation += mouseY;
        _xRotation = Mathf.Clamp(_xRotation, -verticalClamp, verticalClamp);
        cameraTransform.localRotation = Quaternion.Euler(_xRotation, 0f, 0f);
    }


    //  MOVIMIENTO


    private void HandleMovement()
    {
        if (!_canMove) return;

        float speed = _isRunning ? runSpeed : walkSpeed;
        Vector3 targetVel = (transform.right * _moveInput.x +
                             transform.forward * _moveInput.y).normalized * speed;

        _currentMoveVel = Vector3.SmoothDamp(_currentMoveVel, targetVel,
                                              ref _moveDampVel, smoothTime);
        _cc.Move(_currentMoveVel * Time.deltaTime);
    }


    //  INTERACCIÓN


    private void HandleInteraction()
    {
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, interactRange, interactMask))
        {
            IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
            interactable?.OnInteract(hit);
        }
    }


    //  API PÚBLICA


    public void SetMovement(bool enabled) => _canMove = enabled;

    public void LockCursor(bool locked)
    {
        _cursorLocked = locked;
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }

    public void Teleport(Vector3 position, Quaternion rotation)
    {
        _cc.enabled = false;
        transform.SetPositionAndRotation(position, rotation);
        _cc.enabled = true;
    }


    //  GIZMOS


    private void OnDrawGizmosSelected()
    {
        CharacterController cc = GetComponent<CharacterController>();
        if (cc == null) return;

        Gizmos.color = Color.green;
        Vector3 spherePos = transform.position + Vector3.down * (cc.height / 2f - cc.radius);
        Gizmos.DrawWireSphere(spherePos, cc.radius + groundCheckDist);

        if (cameraTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(cameraTransform.position, cameraTransform.forward * interactRange);
        }
    }
}