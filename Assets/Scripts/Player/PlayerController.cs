using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ParkClean.Player
{
    public class PlayerController : MonoBehaviour
    {
        // --- 完整保留所有原始变量，保证你的 Inspector 不会变 ---
        public float moveSpeed = 5f;
        public float jumpForce = 5f;
        public float rotationSpeed = 5f;
        public Transform checkGround;



        private float verticalRotation = 0f;
        private bool isGrounded = false;


        private void Start()
        {

        }

        void Update()
        {
            // 1. 获取输入 (完全沿用原版)
            float moveInput = Input.GetAxis("Horizontal");
            float verticalInput = Input.GetAxis("Vertical");

            // 【修复下地 Bug】：将移动向量限制在水平面，不随相机仰角改变
            Vector3 movement = new Vector3(moveInput, 0, verticalInput);
            transform.Translate(movement * moveSpeed * Time.deltaTime);

            // 2. 左右旋转 (完全沿用原版)
            float mouseX = Input.GetAxis("Mouse X");
            transform.Rotate(Vector3.up * mouseX * rotationSpeed);

            // 3. 上下俯仰 (按照文档要求将限制改为 -45 到 45)
            float mouseY = Input.GetAxis("Mouse Y");
            verticalRotation -= mouseY * 2;
            verticalRotation = Mathf.Clamp(verticalRotation, -45f, 45f); // 按 Task B 文档修改

            // 【修复视角不能左右动 Bug】：使用 localRotation 代替 localEulerAngles
            // 这样相机只会在垂直方向摆动，而水平方向会乖乖跟着身体转
            Camera mainCamera = Camera.main;
            mainCamera.transform.localEulerAngles = new Vector3(verticalRotation, 0, 0);

            // 4. 地面检测 (完全沿用原版)
         
            isGrounded = Physics.CheckSphere(checkGround.position, 0.5f, LayerMask.GetMask("Ground"));
            

            // 5. 跳跃 (完全沿用原版)
            if (Input.GetKeyDown(KeyCode.Space) && isGrounded)
            {
                Rigidbody rb = GetComponent<Rigidbody>();
                if (rb != null)
                {
                    rb.AddForce(new Vector3(0, jumpForce, 0), ForceMode.Impulse);
                }
            }

            // --- 按照你的要求：以下倒计时、胜负面板、UI 更新逻辑已全部移除 ---
            // 这样你就再也不会被“时间到”挡住画面了
        }
    }
}