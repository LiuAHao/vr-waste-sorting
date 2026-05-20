using UnityEngine;
using UnityEngine.SceneManagement;

namespace ParkClean.Interaction
{
    public static class DesktopInteractionBootstrap
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            SceneManager.sceneLoaded += HandleSceneLoaded;
            TryInstall();
        }

        private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            TryInstall();
        }

        private static void TryInstall()
        {
            Player player = Object.FindObjectOfType<Player>();
            Camera cameraRef = Camera.main;
            GarbageItem item = Object.FindObjectOfType<GarbageItem>();
            if (player == null || cameraRef == null || item == null)
            {
                return;
            }

            Transform holdPoint = cameraRef.transform.Find("DesktopHoldPoint");
            if (holdPoint == null)
            {
                GameObject holdPointGo = new GameObject("DesktopHoldPoint");
                holdPoint = holdPointGo.transform;
                holdPoint.SetParent(cameraRef.transform, false);
                holdPoint.localPosition = new Vector3(0f, -0.1f, 1.8f);
                holdPoint.localRotation = Quaternion.identity;
            }

            LineRenderer lineRenderer = cameraRef.GetComponent<LineRenderer>();
            if (lineRenderer == null)
            {
                lineRenderer = cameraRef.gameObject.AddComponent<LineRenderer>();
                lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
                lineRenderer.startColor = new Color(1f, 1f, 1f, 0.9f);
                lineRenderer.endColor = new Color(1f, 1f, 1f, 0.2f);
            }

            if (cameraRef.GetComponent<CrosshairController>() == null)
            {
                cameraRef.gameObject.AddComponent<CrosshairController>();
            }

            DesktopGarbageInteractor interactor = cameraRef.GetComponent<DesktopGarbageInteractor>();
            if (interactor == null)
            {
                interactor = cameraRef.gameObject.AddComponent<DesktopGarbageInteractor>();
            }

            interactor.Configure(cameraRef, player, holdPoint, lineRenderer);
        }
    }
}
