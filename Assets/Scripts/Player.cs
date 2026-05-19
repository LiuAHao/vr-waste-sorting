using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Player : MonoBehaviour 
{
    public float moveSpeed = 5f; // 玩家移动速度
    public float jumpForce = 5f; // 玩家跳跃力量
    public float rotationSpeed = 5f; // 玩家旋转速度
    public Transform checkGround; // 用于检测地面的Transform
    public int clearCount = 0; // 已清理垃圾数量
    public Text clearText; // 显示已清理数量的文本
    public float countdownTime = 120f; // 倒计时时间
    public Text countdownText; // 倒计时文本
    public GameObject failPanel; // 失败面板
    public GameObject winPanel; // 胜利面板

    private float verticalRotation = 0f; // 当前俯仰角
    private bool isGrounded = false; // 玩家是否在地面上
    private float currentTime; // 当前倒计时时间

    private void Start() 
    {
        currentTime = countdownTime; // 初始化倒计时时间
    }

    void Update() 
    {
        // 获取水平和垂直输入
        float moveInput = Input.GetAxis("Horizontal");
        float verticalInput = Input.GetAxis("Vertical");
        Vector3 movement = new Vector3(moveInput, 0, verticalInput); // 生成移动向量
        transform.Translate(movement * moveSpeed * Time.deltaTime); // 移动玩家

        // 获取鼠标X轴输入并旋转玩家
        float mouseX = Input.GetAxis("Mouse X");
        transform.Rotate(Vector3.up * mouseX * rotationSpeed);

        // 获取鼠标Y轴输入并处理俯仰角
        float mouseY = Input.GetAxis("Mouse Y");
        verticalRotation -= mouseY * 2; // 反向处理
        verticalRotation = Mathf.Clamp(verticalRotation, -30f, 30f); // 限制俯仰角

        // 更新主相机的俯仰角
        Camera mainCamera = Camera.main;
        mainCamera.transform.localEulerAngles = new Vector3(verticalRotation, 0, 0);

        // 检测玩家是否在地面上
        isGrounded = Physics.CheckSphere(checkGround.position, 0.5f, LayerMask.GetMask("Ground"));

        // 检查是否可以跳跃
        if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
        {
            GetComponent<Rigidbody>().AddForce(new Vector3(0, jumpForce, 0), ForceMode.Impulse); // 执行跳跃
        }

        // 更新倒计时
        if (currentTime > 0)
        {
            currentTime -= Time.deltaTime; // 减去流逝的时间
            countdownText.text = "倒计时：" + Mathf.FloorToInt(currentTime).ToString(); // 更新倒计时文本
        }
        else // 倒计时结束
        {
            countdownText.text = "倒计时：0"; // 显示倒计时为0
            failPanel.SetActive(true); // 显示结束面板
            moveSpeed = 0; // 停止移动
        }

        // 更新已清理垃圾数量的文本
        clearText.text = "清理目标：" + clearCount + "/12";
        if (clearCount == 12) // 检查是否清理12个垃圾
        {
            winPanel.SetActive(true); // 显示结束面板
            moveSpeed = 0; // 停止移动
        }
    }
}

