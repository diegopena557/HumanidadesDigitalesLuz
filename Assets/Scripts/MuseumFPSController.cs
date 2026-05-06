using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController))]
public class MuseumFPSController : MonoBehaviour
{
    [Header("Movimiento")]
    [SerializeField] private float walkSpeed = 3.0f;
    [SerializeField] private float runSpeed = 6.0f;
    [SerializeField] private float smoothTime = 0.08f;

    [Header("Camara")]
    [SerializeField] private Transform cameraTransform;
    [SerializeField] private float mouseSensitivity = 0.15f;
    [SerializeField] private float verticalClamp = 80f;
    [SerializeField] private bool invertY = false;

    [Header("Gravedad")]
    [SerializeField] private float gravity = -9.81f;
    [SerializeField] private float groundCheckDist = 0.15f;
    [SerializeField] private LayerMask groundMask;

    [Header("Interaccion")]
    [SerializeField] private float interactRange = 3.0f;

    private CharacterController _cc;

    private Vector2 _moveInput;
    private Vector2 _lookInput;
    private bool _isRunning;

    private Vector3 _velocity;
    private Vector3 _currentMoveVel;
    private Vector3 _moveDampVel;

    private float _xRotation = 0f;
    private bool _cursorLocked = true;
    private bool _canMove = true;

    private void Awake()
    {
        _cc = GetComponent<CharacterController>();

        if (cameraTransform == null)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam != null) cameraTransform = cam.transform;
            else Debug.LogError("[MuseumFPS] Asigna la camara en el inspector.");
        }

        var playerInput = GetComponent<PlayerInput>();
        foreach (var map in playerInput.actions.actionMaps)
            map.Disable();
        playerInput.actions.FindActionMap("Player").Enable();

        LockCursor(true);
    }

    private void Update()
    {
        HandleGroundCheck();
        HandleCamera();
        HandleMovement();
    }

    public void OnMove(InputValue value) => _moveInput = value.Get<Vector2>();
    public void OnLook(InputValue value) => _lookInput = value.Get<Vector2>();
    public void OnSprint(InputValue value) => _isRunning = value.isPressed;

    public void OnToggleCursor(InputValue value)
    {
        if (value.isPressed) LockCursor(!_cursorLocked);
    }

    public void OnInteract(InputValue value)
    {
        if (!value.isPressed) return;
        HandleInteraction();
    }

    private void HandleGroundCheck()
    {
        Vector3 spherePos = transform.position +
                            Vector3.down * (_cc.height / 2f - _cc.radius);

        bool grounded = Physics.CheckSphere(spherePos, _cc.radius + groundCheckDist,
                                            groundMask, QueryTriggerInteraction.Ignore);

        if (grounded && _velocity.y < 0f) _velocity.y = -2f;

        _velocity.y += gravity * Time.deltaTime;
        _cc.Move(_velocity * Time.deltaTime);
    }

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

    private void HandleInteraction()
    {
        Ray ray = new Ray(cameraTransform.position, cameraTransform.forward);

        Debug.DrawRay(ray.origin, ray.direction * interactRange, Color.red, 2f);

        if (!Physics.Raycast(ray, out RaycastHit hit, interactRange))
        {
            Debug.Log("El rayo no golpeo nada.");
            return;
        }

        Debug.Log("Golpeo: " + hit.collider.name + " | Layer: " + LayerMask.LayerToName(hit.collider.gameObject.layer));

        IInteractable interactable = hit.collider.GetComponentInParent<IInteractable>();
        if (interactable != null)
            interactable.OnInteract(hit);
        else
            Debug.Log("El objeto no tiene IInteractable.");
    }

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

    private void OnDrawGizmosSelected()
    {
        CharacterController cc = GetComponent<CharacterController>();
        if (cc == null) return;

        Gizmos.color = Color.green;
        Vector3 s = transform.position + Vector3.down * (cc.height / 2f - cc.radius);
        Gizmos.DrawWireSphere(s, cc.radius + groundCheckDist);

        if (cameraTransform != null)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawRay(cameraTransform.position, cameraTransform.forward * interactRange);
        }
    }
}