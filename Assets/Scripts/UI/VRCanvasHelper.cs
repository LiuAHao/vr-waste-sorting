using UnityEngine;
using UnityEngine.SceneManagement;

namespace ParkClean.UI
{
    /// <summary>
    /// VR Canvas 自动转换工具。
    /// 桌面端使用 Screen Space - Overlay Canvas，VR 中这种 Canvas 会直接贴在
    /// 头显镜片上无法正常使用。本工具在 XR 激活时，把场景里所有
    /// Screen Space Canvas 自动转为 World Space，并放置到玩家正前方合适位置，
    /// 同时为每个 Canvas 添加 TrackedDeviceGraphicRaycaster 以支持手柄射线点击。
    ///
    /// 注意：本脚本只做运行时转换，不修改场景文件本身。
    /// </summary>
    public static class VRCanvasHelper
    {
        // ── 世界空间 Canvas 默认参数（可按场景需要调整）────────────
        /// <summary>Canvas 放在 XR Origin（或 Camera）前方多远</summary>
        private const float CanvasDistance = 2.5f;

        /// <summary>Canvas 在世界空间的缩放（原 1080p UI 缩放到合理大小）</summary>
        private const float CanvasWorldScale = 0.002f;

        /// <summary>HUD Canvas 在头部视野的垂直偏移（向上）</summary>
        private const float HudVerticalOffset = 0.3f;
        // ─────────────────────────────────────────────────────────

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            // WasteUiFactory.CreateCanvasRoot 已在创建时直接处理 VR/桌面分支，
            // 本工具只负责转换场景里预先放置的、非代码生成的 Canvas（如果有的话）。
            if (!IsXRActive()) return;

            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            ConvertAllCanvases();
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (IsXRActive())
            {
                ConvertAllCanvases();
            }
        }

        /// <summary>把当前场景中所有 Screen Space Canvas 转为 World Space。</summary>
        public static void ConvertAllCanvases()
        {
            Canvas[] allCanvases = Object.FindObjectsOfType<Canvas>(true);
            Camera xrCamera = Camera.main;

            foreach (Canvas canvas in allCanvases)
            {
                if (canvas == null) continue;

                // 只处理 Screen Space（Overlay 和 Camera），跳过已经是 World Space 的
                if (canvas.renderMode == RenderMode.WorldSpace) continue;

                ConvertToWorldSpace(canvas, xrCamera);
            }
        }

        private static void ConvertToWorldSpace(Canvas canvas, Camera xrCamera)
        {
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.worldCamera = xrCamera;

            // 缩放
            canvas.transform.localScale = Vector3.one * CanvasWorldScale;

            // 位置：放在摄像机前方，或绑定到摄像机跟随
            if (xrCamera != null)
            {
                Transform camT = xrCamera.transform;
                Vector3 forward = camT.forward;
                forward.y = 0f;
                if (forward.sqrMagnitude < 0.001f) forward = Vector3.forward;
                forward.Normalize();

                canvas.transform.position = camT.position
                    + forward * CanvasDistance
                    + Vector3.up * HudVerticalOffset;
                canvas.transform.rotation = Quaternion.LookRotation(forward);
            }

            // 添加 TrackedDeviceGraphicRaycaster 以支持手柄射线点 UI
            // （需要 XR Interaction Toolkit 2.x，类全名见 XRI 文档）
            // 用反射检查，避免在 XRI 包未导入时编译报错
            EnsureTrackedRaycaster(canvas.gameObject);

            // 移除原来的 GraphicRaycaster（避免两个 Raycaster 冲突）
            UnityEngine.UI.GraphicRaycaster legacyRaycaster =
                canvas.GetComponent<UnityEngine.UI.GraphicRaycaster>();
            if (legacyRaycaster != null)
            {
                Object.Destroy(legacyRaycaster);
            }
        }

        private static void EnsureTrackedRaycaster(GameObject go)
        {
            // 用反射动态添加，XRI 包导入后自动生效，未导入时静默跳过
            const string raycasterTypeName =
                "UnityEngine.XR.Interaction.Toolkit.UI.TrackedDeviceGraphicRaycaster," +
                "Unity.XR.Interaction.Toolkit";

            System.Type raycasterType = System.Type.GetType(raycasterTypeName);
            if (raycasterType == null) return;

            if (go.GetComponent(raycasterType) == null)
            {
                go.AddComponent(raycasterType);
            }
        }

        private static bool IsXRActive()
        {
            return UnityEngine.XR.XRSettings.isDeviceActive
                || UnityEngine.XR.XRSettings.loadedDeviceName.Length > 0;
        }
    }
}
