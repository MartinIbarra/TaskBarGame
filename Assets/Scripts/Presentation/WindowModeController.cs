using System.Collections;
using TaskbarTactics.Core.Services;
using TaskbarTactics.Platform.Windows;
using UnityEngine;

namespace TaskbarTactics.Presentation
{
    public sealed class WindowModeController : MonoBehaviour
    {
        [Header("Editable UI roots")]
        [SerializeField] private GameObject stripRoot;
        [SerializeField] private GameObject managementRoot;
        [SerializeField] private Camera gameCamera;
        [SerializeField] private bool startInManagementInEditor = true;

        private IWindowController windowController;

        public WindowMode CurrentMode => windowController?.CurrentMode ?? WindowMode.Management;

        public void Configure(GameObject strip, GameObject management, Camera camera)
        {
            stripRoot = strip;
            managementRoot = management;
            gameCamera = camera;
        }

        private IEnumerator Start()
        {
            windowController = new WindowsWindowController();
            if (gameCamera != null)
            {
                gameCamera.backgroundColor = WindowsWindowController.ColorKey;
            }

            yield return null;
#if UNITY_EDITOR
            SetMode(startInManagementInEditor ? WindowMode.Management : WindowMode.Strip);
#else
            SetMode(WindowMode.Strip);
#endif
        }

        public void ShowManagement()
        {
            SetMode(WindowMode.Management);
        }

        public void ShowStrip()
        {
            SetMode(WindowMode.Strip);
        }

        public void Toggle()
        {
            SetMode(CurrentMode == WindowMode.Strip ? WindowMode.Management : WindowMode.Strip);
        }

        private void SetMode(WindowMode mode)
        {
            if (stripRoot != null)
            {
                stripRoot.SetActive(mode == WindowMode.Strip);
            }

            if (managementRoot != null)
            {
                managementRoot.SetActive(mode == WindowMode.Management);
            }

            Application.targetFrameRate = mode == WindowMode.Strip ? 30 : 60;
            windowController.SetMode(mode);
            StartCoroutine(RepositionAfterResize());
        }

        private IEnumerator RepositionAfterResize()
        {
            yield return null;
            yield return new WaitForEndOfFrame();
            windowController.Reposition();
        }
    }
}
