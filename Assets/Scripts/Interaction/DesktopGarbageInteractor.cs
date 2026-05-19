using UnityEngine;

namespace ParkClean.Interaction
{
    public class DesktopGarbageInteractor : MonoBehaviour
    {
        [Header("核心配置")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private float interactDistance = 4f;
        [SerializeField] private LayerMask interactMask;
        [SerializeField] private Transform holdPoint;

        [Header("视觉射线")]
        [SerializeField] private LineRenderer rayLine;

        private Garbage currentHover; // 已改为 Garbage
        private GameObject heldItem;
        private Rigidbody heldRb;

        void Start()
        {
            if (playerCamera == null) playerCamera = Camera.main;

            if (rayLine != null)
            {
                rayLine.positionCount = 2;
                rayLine.startWidth = 0.01f;
                rayLine.endWidth = 0.005f;
                rayLine.useWorldSpace = true;
            }
        }

        void Update()
        {
            DrawVisualRay();

            if (heldItem == null)
            {
                HandleSelection();
                if (Input.GetMouseButtonDown(0)) TryGrab();
            }
            else
            {
                HandleHolding();
                if (Input.GetMouseButtonUp(0)) Release();
            }
        }

        void DrawVisualRay()
        {
            if (rayLine == null || playerCamera == null) return;

            // 修正：将 Z 轴深度从 0.4f 增加到 0.8f，防止被相机裁剪
            Vector3 startPos = playerCamera.ViewportToWorldPoint(new Vector3(0.85f, 0.15f, 0.8f));
            Vector3 endPos = playerCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.5f, interactDistance));

            rayLine.SetPosition(0, startPos);
            rayLine.SetPosition(1, endPos);
        }

        void HandleSelection()
        {
            Ray ray = playerCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
            if (Physics.Raycast(ray, out RaycastHit hit, interactDistance, interactMask))
            {
                // 已改为获取 Garbage 组件
                Garbage item = hit.collider.GetComponent<Garbage>();

                // 注意：请确保你的 Garbage 脚本里有 CanInteract() 函数
                if (item != null && item.CanInteract())
                {
                    if (currentHover != item)
                    {
                        if (currentHover != null) currentHover.GetComponent<SelectableHighlighter>()?.SetHighlight(false);
                        currentHover = item;
                        currentHover.GetComponent<SelectableHighlighter>()?.SetHighlight(true);
                    }
                    return;
                }
            }
            if (currentHover != null)
            {
                currentHover.GetComponent<SelectableHighlighter>()?.SetHighlight(false);
                currentHover = null;
            }
        }

        void TryGrab()
        {
            if (currentHover == null) return;
            heldItem = currentHover.gameObject;
            heldRb = heldItem.GetComponent<Rigidbody>();
            if (heldRb != null) { heldRb.isKinematic = true; heldRb.useGravity = false; }

            // 注意：请确保你的 Garbage 脚本里有 SetHeld() 函数
            currentHover.SetHeld(true);
        }

        void HandleHolding()
        {
            heldItem.transform.position = Vector3.Lerp(heldItem.transform.position, holdPoint.position, Time.deltaTime * 15f);
        }

        void Release()
        {
            if (heldRb != null) { heldRb.isKinematic = false; heldRb.useGravity = true; }

            // 已改为获取 Garbage 组件
            heldItem.GetComponent<Garbage>()?.SetHeld(false);
            heldItem = null;
            heldRb = null;
        }
    }
}