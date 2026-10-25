using System.Collections;
using TaskbarTactics.Core.Services;
using TaskbarTactics.Platform.Windows;
using UnityEngine;

namespace TaskbarTactics.Presentation
{
    public sealed class WindowModeController : MonoBehaviour
    {
        private const string HudAmbientResourcePath = "Audio/UI/antorchAmbient";

        [Header("Editable UI roots")]
        [SerializeField] private GameObject stripRoot;
        [SerializeField] private GameObject managementRoot;
        [SerializeField] private Camera gameCamera;
        [SerializeField] private bool startInManagementInEditor = true;

        [Header("HUD ambient audio")]
        [SerializeField, Range(0f, 1f)] private float hudAmbientVolume = 0.18f;

        private IWindowController windowController;
        private AudioSource hudAmbientSource;
        private AudioClip hudAmbientClip;
        private bool hudAmbientStarted;
        private bool hudAmbientWarningLogged;
        private Coroutine resizeRoutine;

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
            ConfigureHudAmbientAudio();
            if (gameCamera != null)
            {
#if UNITY_EDITOR
                gameCamera.backgroundColor = new Color(0.18f, 0.19f, 0.22f, 1f);
#else
                gameCamera.backgroundColor = WindowsWindowController.ColorKey;
#endif
            }

            yield return null;
#if UNITY_EDITOR
            SetMode(startInManagementInEditor ? WindowMode.Management : WindowMode.Strip);
#else
            // Allows standalone layout checks without depending on a taskbar/tool window capture.
            bool openManagement = System.Array.IndexOf(
                System.Environment.GetCommandLineArgs(), "-management-window") >= 0;
            SetMode(openManagement ? WindowMode.Management : WindowMode.Strip);
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
            windowController?.SetMode(mode);
            UpdateHudAmbientAudio(mode);
            if (resizeRoutine != null)
            {
                StopCoroutine(resizeRoutine);
            }
            resizeRoutine = StartCoroutine(RepositionAfterResize());
        }

        private void ConfigureHudAmbientAudio()
        {
            hudAmbientClip = Resources.Load<AudioClip>(HudAmbientResourcePath);
            if (hudAmbientClip == null)
            {
                if (!hudAmbientWarningLogged)
                {
                    Debug.LogWarning($"HUD ambient audio was not found at Resources/{HudAmbientResourcePath}.");
                    hudAmbientWarningLogged = true;
                }

                return;
            }

            hudAmbientSource = GetComponent<AudioSource>();
            if (hudAmbientSource == null)
            {
                hudAmbientSource = gameObject.AddComponent<AudioSource>();
            }

            hudAmbientSource.clip = hudAmbientClip;
            hudAmbientSource.playOnAwake = false;
            hudAmbientSource.loop = true;
            hudAmbientSource.volume = hudAmbientVolume;
            hudAmbientSource.spatialBlend = 0f;
        }

        private void UpdateHudAmbientAudio(WindowMode mode)
        {
            if (hudAmbientSource == null || hudAmbientClip == null)
            {
                return;
            }

            if (mode == WindowMode.Management)
            {
                if (hudAmbientStarted)
                {
                    hudAmbientSource.UnPause();
                }
                else
                {
                    hudAmbientSource.Play();
                    hudAmbientStarted = true;
                }
            }
            else if (hudAmbientSource.isPlaying)
            {
                hudAmbientSource.Pause();
            }
        }

        private IEnumerator RepositionAfterResize()
        {
            yield return null;
            yield return new WaitForEndOfFrame();
            windowController?.Reposition();
            yield return null;
            Canvas.ForceUpdateCanvases();
            resizeRoutine = null;
        }
    }
}
