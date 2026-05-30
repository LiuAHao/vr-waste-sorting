using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR.Interaction.Toolkit;

namespace ParkClean.Interaction
{
    /// <summary>
    /// VR 交互引导器（对应桌面端的 DesktopInteractionBootstrap）。
    /// 在 VR 模式下，XR Origin / Rig 以及手柄 Interactor 应当在场景中
    /// 通过 Prefab 预先放置，本脚本只做以下两件事：
    ///   1. 在场景加载后，找到场景中的 XRBaseInteractor（左右手），
    ///      为每个 Interactor 挂载 VRGarbageInteractor 组件（若不存在）。
    ///   2. 为场景中所有垃圾物品添加 XRGrabInteractable 组件，并配置为
    ///      Kinematic 模式 + 禁用 Attach Transform，防止物品被 parent 到手柄下。
    ///
    /// 注意：本脚本使用 [RuntimeInitializeOnLoadMethod] 在不需要手动
    /// 挂载到任何 GameObject 的情况下自动运行，与 DesktopInteractionBootstrap
    /// 的工作方式相同，但二者通过条件判断（XR 是否处于激活状态）互斥。
    /// </summary>
    public static class VRInteractionBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            // 只在真正运行 XR 时激活（编辑器下也可用 XR Device Simulator 触发）
            if (!IsXRActive())
            {
                return;
            }

            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            TryInstall();
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (IsXRActive())
            {
                TryInstall();
            }
        }

        private static void TryInstall()
        {
            // 找场景中所有 XR Interactor（左右手各有一个）
            XRBaseInteractor[] interactors = Object.FindObjectsOfType<XRBaseInteractor>(true);
            if (interactors == null || interactors.Length == 0)
            {
                // XR Rig 可能还没加载，这是正常情况（DontDestroyOnLoad 场景）
                return;
            }

            foreach (XRBaseInteractor interactor in interactors)
            {
                if (interactor == null) continue;

                // 只给手柄节点挂，避免 XRSocketInteractor 等特殊 Interactor 被误挂
                if (interactor is XRRayInteractor || interactor is XRDirectInteractor)
                {
                    if (interactor.GetComponent<VRGarbageInteractor>() == null)
                    {
                        interactor.gameObject.AddComponent<VRGarbageInteractor>();
                    }
                }

                // XRDirectInteractor 需要一个 isTrigger=true 的 Collider，否则报警告
                if (interactor is XRDirectInteractor)
                {
                    EnsureDirectInteractorCollider(interactor.gameObject);
                }
            }

            // 给所有垃圾物品添加 XRGrabInteractable 组件（如果没有的话）
            // 并配置为 Kinematic 模式，禁用 Attach Transform（防止自动 parent）
            SetupGarbageItemsForVR();

            // 禁用 TunnelingVignette（移动时的周边黑晕效果，容易造成眩晕感）
            DisableTunnelingVignette();
        }

        private static void SetupGarbageItemsForVR()
        {
            // 找场景中所有 GarbageItem
            GarbageItem[] items = Object.FindObjectsOfType<GarbageItem>(true);
            foreach (GarbageItem item in items)
            {
                if (item == null) continue;

                // 如果已经有 XRGrabInteractable，跳过
                if (item.GetComponent<XRGrabInteractable>() != null) continue;

                // 添加 XRGrabInteractable 组件
                XRGrabInteractable grabInteractable = item.gameObject.AddComponent<XRGrabInteractable>();

                // 关键配置：使用 Kinematic 模式，物品位置完全由我们的代码控制
                grabInteractable.movementType = XRBaseInteractable.MovementType.Kinematic;

                // 禁用 Attach Transform（防止 XRI 自动把物品 parent 到手柄下）
                grabInteractable.attachTransform = null;

                // 禁用平滑移动（我们在 VRGarbageInteractor 里自己做 Lerp）
                grabInteractable.smoothPosition = false;
                grabInteractable.smoothRotation = false;

                // 禁用投掷（我们在 VRGarbageInteractor 里自己计算投掷速度）
                grabInteractable.throwOnDetach = false;
            }
        }

        private static void EnsureDirectInteractorCollider(GameObject go)
        {
            // 检查是否已有 isTrigger=true 的 Collider
            Collider[] cols = go.GetComponents<Collider>();
            foreach (Collider c in cols)
            {
                if (c.isTrigger) return;
            }

            // 添加一个小型球形 Trigger Collider（代表"手"的交互范围）
            SphereCollider sc = go.AddComponent<SphereCollider>();
            sc.isTrigger = true;
            sc.radius = 0.1f;
        }

        private static void DisableTunnelingVignette()
        {
            // TunnelingVignette 是 XR Origin 下的子 GameObject
            // 用名字查找并直接禁用
            GameObject vignetteGo = GameObject.Find("TunnelingVignette");
            if (vignetteGo != null)
            {
                vignetteGo.SetActive(false);
                return;
            }

            // 备用：通过类型查找 TunnelingVignette 组件并禁用
            const string typeName =
                "Unity.XR.CoreUtils.TunnelingVignette," +
                "Unity.XR.CoreUtils";
            System.Type t = System.Type.GetType(typeName);
            if (t != null)
            {
                Object comp = Object.FindObjectOfType(t);
                if (comp is MonoBehaviour mb)
                {
                    mb.gameObject.SetActive(false);
                }
            }
        }

        /// <summary>
        /// 检测当前是否处于 XR（VR）运行模式。
        /// 在编辑器中安装了 XR Device Simulator 时同样返回 true。
        /// </summary>
        private static bool IsXRActive()
        {
            return UnityEngine.XR.XRSettings.isDeviceActive
                || UnityEngine.XR.XRSettings.loadedDeviceName.Length > 0;
        }
    }
}
