using UnityEngine;

public class Player : MonoBehaviour
{
    private const float MinLookPitch = -75f;
    private const float MaxLookPitch = 55f;

    public float moveSpeed = 5f;
    public float jumpForce = 5f;
    public float rotationSpeed = 5f;
    public Transform checkGround;

    private Rigidbody _rigidbody;
    private float _verticalRotation;
    private bool _inputEnabled = true;

    public bool InputEnabled => _inputEnabled;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();
    }

    public void SetInputEnabled(bool enabled)
    {
        _inputEnabled = enabled;
    }

    private void Update()
    {
        if (!_inputEnabled)
        {
            return;
        }

        float moveInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        Vector3 movement = new Vector3(moveInput, 0f, verticalInput);
        transform.Translate(movement * moveSpeed * Time.deltaTime);

        float mouseX = Input.GetAxis("Mouse X");
        transform.Rotate(Vector3.up * mouseX * rotationSpeed);

        float mouseY = Input.GetAxis("Mouse Y");
        _verticalRotation -= mouseY * 2f;
        _verticalRotation = Mathf.Clamp(_verticalRotation, MinLookPitch, MaxLookPitch);

        Camera mainCamera = Camera.main;
        if (mainCamera != null)
        {
            mainCamera.transform.localEulerAngles = new Vector3(_verticalRotation, 0f, 0f);
        }

        bool isGrounded = checkGround != null && Physics.CheckSphere(checkGround.position, 0.5f, LayerMask.GetMask("Ground"));
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && _rigidbody != null)
        {
            _rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }
}
