using System;
using System.Runtime.InteropServices;
using TaskbarTactics.Core.Services;
using UnityEngine;

namespace TaskbarTactics.Platform.Windows
{
    public sealed class WindowsWindowController : IWindowController
    {
        private const int StripWidth = 960;
        private const int StripHeight = 176;
        private const int ManagementWidth = 960;
        private const int ManagementHeight = 640;
        private const int GwlStyle = -16;
        private const int GwlExStyle = -20;
        private const long WsCaption = 0x00C00000L;
        private const long WsSysMenu = 0x00080000L;
        private const long WsMinimizeBox = 0x00020000L;
        private const long WsThickFrame = 0x00040000L;
        private const long WsExLayered = 0x00080000L;
        private const long WsExToolWindow = 0x00000080L;
        private const uint LwaColorKey = 0x00000001;
        private const uint SwpNoActivate = 0x0010;
        private const uint SwpShowWindow = 0x0040;
        private static readonly IntPtr HwndTopMost = new IntPtr(-1);
        private static readonly IntPtr HwndNotTopMost = new IntPtr(-2);
        private static readonly Color32 TransparentColor = new Color32(255, 0, 255, 255);

        private IntPtr windowHandle;
        private long originalStyle;
        private long originalExtendedStyle;

        public WindowMode CurrentMode { get; private set; } = WindowMode.Management;
        public static Color32 ColorKey => TransparentColor;

        public WindowsWindowController()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            windowHandle = GetActiveWindow();
            if (windowHandle == IntPtr.Zero)
            {
                windowHandle = GetForegroundWindow();
            }

            if (windowHandle != IntPtr.Zero)
            {
                originalStyle = GetWindowLongPtr(windowHandle, GwlStyle).ToInt64();
                originalExtendedStyle = GetWindowLongPtr(windowHandle, GwlExStyle).ToInt64();
            }
#endif
        }

        public void SetMode(WindowMode mode)
        {
            CurrentMode = mode;
            int width = mode == WindowMode.Strip ? StripWidth : ManagementWidth;
            int height = mode == WindowMode.Strip ? StripHeight : ManagementHeight;
            Screen.SetResolution(width, height, FullScreenMode.Windowed);

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            RefreshHandle();
            if (windowHandle == IntPtr.Zero)
            {
                return;
            }

            if (mode == WindowMode.Strip)
            {
                long style = originalStyle & ~(WsCaption | WsThickFrame | WsMinimizeBox);
                long extended = originalExtendedStyle | WsExLayered | WsExToolWindow;
                SetWindowLongPtr(windowHandle, GwlStyle, new IntPtr(style));
                SetWindowLongPtr(windowHandle, GwlExStyle, new IntPtr(extended));
                uint colorRef = (uint)(TransparentColor.r |
                                       (TransparentColor.g << 8) |
                                       (TransparentColor.b << 16));
                SetLayeredWindowAttributes(windowHandle, colorRef, 255, LwaColorKey);
            }
            else
            {
                long style = originalStyle | WsCaption | WsSysMenu | WsMinimizeBox;
                SetWindowLongPtr(windowHandle, GwlStyle, new IntPtr(style));
                SetWindowLongPtr(windowHandle, GwlExStyle, new IntPtr(
                    originalExtendedStyle & ~(WsExLayered | WsExToolWindow)));
            }
#endif
            Reposition();
        }

        public void Reposition()
        {
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
            RefreshHandle();
            if (windowHandle == IntPtr.Zero)
            {
                return;
            }

            int logicalWidth = CurrentMode == WindowMode.Strip ? StripWidth : ManagementWidth;
            int logicalHeight = CurrentMode == WindowMode.Strip ? StripHeight : ManagementHeight;
            uint dpi = GetDpiForWindow(windowHandle);
            float scale = dpi > 0 ? dpi / 96f : 1f;
            int width = Mathf.RoundToInt(logicalWidth * scale);
            int height = Mathf.RoundToInt(logicalHeight * scale);
            int screenWidth = GetSystemMetrics(0);
            int screenHeight = GetSystemMetrics(1);
            int x = (screenWidth - width) / 2;
            int y = (screenHeight - height) / 2;

            if (CurrentMode == WindowMode.Strip)
            {
                IntPtr taskbar = FindWindow("Shell_TrayWnd", null);
                if (taskbar != IntPtr.Zero && GetWindowRect(taskbar, out Rect taskbarRect))
                {
                    bool horizontal = taskbarRect.Width >= taskbarRect.Height;
                    if (horizontal && taskbarRect.Top > screenHeight / 2)
                    {
                        y = taskbarRect.Top - height;
                    }
                    else if (horizontal)
                    {
                        y = taskbarRect.Bottom;
                    }
                    else
                    {
                        y = screenHeight - height;
                        int usableLeft = taskbarRect.Left <= 0 ? taskbarRect.Right : 0;
                        int usableRight = taskbarRect.Right >= screenWidth ? taskbarRect.Left : screenWidth;
                        x = usableLeft + (usableRight - usableLeft - width) / 2;
                    }
                }
                else
                {
                    y = screenHeight - height;
                }
            }

            SetWindowPos(
                windowHandle,
                CurrentMode == WindowMode.Strip ? HwndTopMost : HwndNotTopMost,
                x,
                y,
                width,
                height,
                SwpNoActivate | SwpShowWindow);
#endif
        }

#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        private void RefreshHandle()
        {
            if (windowHandle == IntPtr.Zero || !IsWindow(windowHandle))
            {
                windowHandle = GetActiveWindow();
            }
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct Rect
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public int Width => Right - Left;
            public int Height => Bottom - Top;
        }

        [DllImport("user32.dll")]
        private static extern IntPtr GetActiveWindow();

        [DllImport("user32.dll")]
        private static extern IntPtr GetForegroundWindow();

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr handle);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr FindWindow(string className, string windowName);

        [DllImport("user32.dll")]
        private static extern bool GetWindowRect(IntPtr handle, out Rect rect);

        [DllImport("user32.dll")]
        private static extern int GetSystemMetrics(int index);

        [DllImport("user32.dll")]
        private static extern uint GetDpiForWindow(IntPtr handle);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtrW")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr handle, int index);

        [DllImport("user32.dll", EntryPoint = "GetWindowLongW")]
        private static extern IntPtr GetWindowLongPtr32(IntPtr handle, int index);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtrW")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr handle, int index, IntPtr value);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongW")]
        private static extern IntPtr SetWindowLongPtr32(IntPtr handle, int index, IntPtr value);

        [DllImport("user32.dll")]
        private static extern bool SetLayeredWindowAttributes(
            IntPtr handle,
            uint colorKey,
            byte alpha,
            uint flags);

        [DllImport("user32.dll")]
        private static extern bool SetWindowPos(
            IntPtr handle,
            IntPtr insertAfter,
            int x,
            int y,
            int width,
            int height,
            uint flags);

        private static IntPtr GetWindowLongPtr(IntPtr handle, int index)
        {
            return IntPtr.Size == 8
                ? GetWindowLongPtr64(handle, index)
                : GetWindowLongPtr32(handle, index);
        }

        private static IntPtr SetWindowLongPtr(IntPtr handle, int index, IntPtr value)
        {
            return IntPtr.Size == 8
                ? SetWindowLongPtr64(handle, index, value)
                : SetWindowLongPtr32(handle, index, value);
        }
#endif
    }
}
