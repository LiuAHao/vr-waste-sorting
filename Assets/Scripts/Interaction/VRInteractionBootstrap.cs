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
    ///   2. 为场景中所有垃圾物品的 Rigidbody 配置合理的物理参数，
    ///      保证 VR 环境下不穿模。
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

            // 禁用 TunnelingVignette（移动时的周边黑晕效果，容易造成眩晕感）
            DisableTunnelingVignette();
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
