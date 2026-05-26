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
            if (player == null || cameraRef == null)
            {
                return;
            }

            Transform holdPoint = cameraRef.transform.Find("DesktopHoldPoint");
            if (holdPoint == null)
            {
                GameObject holdPointGo = new GameObject("DesktopHoldPoint");
                holdPoint = holdPointGo.transform;
                holdPoint.SetParent(cameraRef.transform, false);
                holdPoint.localPosition = new Vector3(0f, -0.15f, 2.4f);
                holdPoint.localRotation = Quaternion.identity;
            }

            DesktopGarbageInteractor interactor = cameraRef.GetComponent<DesktopGarbageInteractor>();
            if (interactor == null)
            {
                interactor = cameraRef.gameObject.AddComponent<DesktopGarbageInteractor>();
            }

            LineRenderer lineRenderer = cameraRef.GetComponent<LineRenderer>();
            if (lineRenderer != null)
            {
                Object.Destroy(lineRenderer);
            }

            CrosshairController crosshair = cameraRef.GetComponent<CrosshairController>();
            if (crosshair != null)
            {
                Object.Destroy(crosshair);
            }

            interactor.Configure(cameraRef, player, holdPoint);
        }
    }
}
