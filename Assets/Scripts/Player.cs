using UnityEngine;

/// <summary>
/// 玩家控制器。
/// 桌面端：WASD 移动 + 鼠标视角。
/// VR 端（XR 激活时）：本脚本所有输入逻辑自动禁用；
///   移动 / 视角 / 位姿全部由 XR Origin / OpenXR 驱动，
///   本脚本仅保留 InputEnabled 属性供游戏流程层（WasteGameFlowController 等）读写。
/// </summary>
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

    // VR 模式下是否跳过键鼠输入
    private bool _isVRMode;

    public bool InputEnabled => _inputEnabled;

    private void Awake()
    {
        _rigidbody = GetComponent<Rigidbody>();

        // 检测 XR 设备，决定是否进入 VR 模式
        _isVRMode = UnityEngine.XR.XRSettings.isDeviceActive
                 || UnityEngine.XR.XRSettings.loadedDeviceName.Length > 0;
    }

    public void SetInputEnabled(bool enabled)
    {
        _inputEnabled = enabled;
    }

    private void Update()
    {
        // VR 模式下不处理任何键鼠输入，头显位姿由 XR Origin 驱动
        if (_isVRMode) return;

        if (!_inputEnabled) return;

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

        bool isGrounded = checkGround != null
            && Physics.CheckSphere(checkGround.position, 0.5f, LayerMask.GetMask("Ground"));
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded && _rigidbody != null)
        {
            _rigidbody.AddForce(Vector3.up * jumpForce, ForceMode.Impulse);
        }
    }
}
