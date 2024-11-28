using UnityEngine;

public class CharacterController : MonoBehaviour
{
    private static float _lookSensitivity = 1.3f;
    private static float _baseMoveSpeed = 0.4f;



    [SerializeField] private Camera _characterCamera;



    private Vector2 _movementInput;
    private Vector3 _cameraRotation;



    public void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }



    public void FixedUpdate()
    {
        HandleMovement();
    }
    public void Update()
    {
        HandleMovementInput();
        HandleMouseLook();
    }



    private void HandleMovement()
    {
        this.transform.position += ((this.transform.TransformDirection(Vector3.forward) * _movementInput.x)
                                + (this.transform.TransformDirection(Vector3.right) * _movementInput.y)) * _baseMoveSpeed;
    }
    private void HandleMovementInput()
    {
        _movementInput = new Vector2(Input.GetAxisRaw("Vertical"), Input.GetAxisRaw("Horizontal")).normalized;
    }
    private void HandleMouseLook()
    {
        _cameraRotation.x = Mathf.Clamp(_cameraRotation.x - Input.GetAxis("Mouse Y"), -90f, 90f);
        _characterCamera.transform.localRotation = Quaternion.Euler(_cameraRotation.x, 0f, 0f);
        this.transform.rotation *= Quaternion.Euler(0f, Input.GetAxis("Mouse X") * _lookSensitivity, 0f);
    }
}