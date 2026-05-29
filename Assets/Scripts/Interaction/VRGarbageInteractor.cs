using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

namespace ParkClean.Interaction
{
    /// <summary>
    /// VR 手柄抓取交互器（PICO4 / OpenXR）。
    /// 挂在左手或右手 Controller GameObject 上。
    /// 依赖 XR Interaction Toolkit 2.x：
    ///   - XRRayInteractor（远距离射线抓取）
    ///   - XRDirectInteractor（近距离直接抓取）
    /// 本脚本负责把 XRI 的 Select 事件桥接到现有的
    ///   GarbageItem.SetHeld() / DropZone.TryClassifyReleasedItem() 接口，
    /// 保证游戏逻辑层（GarbageItem、DropZone、ClassificationEvents 等）
    /// 完全不需要改动。
    /// </summary>
    [RequireComponent(typeof(XRBaseInteractor))]
    public class VRGarbageInteractor : MonoBehaviour
    {
        // ── 可在 Inspector 调整 ──────────────────────────────────
        [Tooltip("抓取时物品跟随手柄的速度（Lerp）。0 = 瞬移，越大越快")]
        [SerializeField] private float followSpeed = 20f;

        [Tooltip("抓取后把物品对齐到手柄前方多少距离（仅射线模式生效）")]
        [SerializeField] private float holdDistance = 0.4f;
        // ─────────────────────────────────────────────────────────

        private XRBaseInteractor _xrInteractor;
        private GarbageItem _heldItem;
        private Rigidbody _heldRigidbody;

        // ── Unity 生命周期 ────────────────────────────────────────

        private void Awake()
        {
            _xrInteractor = GetComponent<XRBaseInteractor>();
        }

        private void OnEnable()
        {
            _xrInteractor.selectEntered.AddListener(OnSelectEntered);
            _xrInteractor.selectExited.AddListener(OnSelectExited);
        }

        private void OnDisable()
        {
            _xrInteractor.selectEntered.RemoveListener(OnSelectEntered);
            _xrInteractor.selectExited.RemoveListener(OnSelectExited);
        }

        private void Update()
        {
            if (_heldItem == null) return;

            // 如果垃圾在飞行途中已被判定完成（例如上一帧刚入桶），清空持有状态
            if (_heldItem.IsCompleted)
            {
                ClearHeldState();
                return;
            }

            // 让物品平滑跟随手柄（射线/直接抓取都用同一套逻辑）
            Vector3 targetPos = GetHoldPosition();
            _heldItem.transform.position = Vector3.Lerp(
                _heldItem.transform.position,
                targetPos,
                Time.deltaTime * followSpeed);
        }

        // ── XRI 事件回调 ──────────────────────────────────────────

        private void OnSelectEntered(SelectEnterEventArgs args)
        {
            // 从被选中对象的 GameObject 上找 GarbageItem
            GarbageItem item = ResolveGarbageItem(args.interactableObject.transform);
            if (item == null || !item.CanInteract()) return;

            _heldItem = item;
            _heldRigidbody = item.GetComponent<Rigidbody>();

            if (_heldRigidbody != null)
            {
                _heldRigidbody.velocity = Vector3.zero;
                _heldRigidbody.angularVelocity = Vector3.zero;
                _heldRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                _heldRigidbody.isKinematic = true;
                _heldRigidbody.useGravity = false;
            }

            _heldItem.SetHeld(true);
        }

        private void OnSelectExited(SelectExitEventArgs args)
        {
            if (_heldItem == null) return;

            GarbageItem releasedItem = _heldItem;

            // 还原物理
            if (_heldRigidbody != null)
            {
                _heldRigidbody.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                _heldRigidbody.isKinematic = false;
                _heldRigidbody.useGravity = true;
                _heldRigidbody.velocity = Vector3.zero;
                _heldRigidbody.angularVelocity = Vector3.zero;
            }

            releasedItem.SetHeld(false);
            ClearHeldState();

            // 松手时触发分类判定 —— 与桌面端完全一致
            DropZone.TryClassifyReleasedItem(releasedItem);
        }

        // ── 辅助方法 ──────────────────────────────────────────────

        /// <summary>计算持有点世界坐标。射线模式取手柄前方，直接抓取模式取手柄本身。</summary>
        private Vector3 GetHoldPosition()
        {
            // 如果是 XRRayInteractor，从射线原点沿方向偏移
            if (_xrInteractor is XRRayInteractor rayInteractor)
            {
                return transform.position + transform.forward * holdDistance;
            }

            // XRDirectInteractor：直接跟随手柄位置
            return transform.position;
        }

        private static GarbageItem ResolveGarbageItem(Transform t)
        {
            if (t == null) return null;

            GarbageItem item = t.GetComponent<GarbageItem>();
            if (item != null) return item;

            item = t.GetComponentInParent<GarbageItem>();
            if (item != null) return item;

            return t.GetComponentInChildren<GarbageItem>();
        }

        private void ClearHeldState()
        {
            _heldItem = null;
            _heldRigidbody = null;
        }
    }
}
