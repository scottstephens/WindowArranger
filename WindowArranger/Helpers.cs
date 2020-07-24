using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using PInvoke;

namespace WindowArranger
{
    public class Helpers
    {
        private class HandleEnumeratorState
        {
            public List<IntPtr> Items = new List<IntPtr>();

            public bool Update(IntPtr handle, IntPtr arg)
            {
                this.Items.Add(handle);
                return true;
            }

            public void Get()
            {
                User32.EnumWindows(this.Update, IntPtr.Zero);
            }
        }

        public static List<IntPtr> EnumWindows()
        {
            var state = new HandleEnumeratorState();
            state.Get();
            return state.Items;
        }

        public static int GetWindowsProcessId(IntPtr window_handle)
        {
            int thread_id = User32.GetWindowThreadProcessId(window_handle, out int process_id);
            return process_id;
        }
        // GetWindowThreadProcessId - Get window thread and process ID

        // GetWindowInfo

        [Flags()]
        public enum DisplayDeviceStateFlags : int
        {
            /// <summary>The device is part of the desktop.</summary>
            AttachedToDesktop = 0x1,
            MultiDriver = 0x2,
            /// <summary>The device is part of the desktop.</summary>
            PrimaryDevice = 0x4,
            /// <summary>Represents a pseudo device used to mirror application drawing for remoting or other purposes.</summary>
            MirroringDriver = 0x8,
            /// <summary>The device is VGA compatible.</summary>
            VGACompatible = 0x10,
            /// <summary>The device is removable; it cannot be the primary display.</summary>
            Removable = 0x20,
            /// <summary>The device has more display modes than its output devices support.</summary>
            ModesPruned = 0x8000000,
            Remote = 0x4000000,
            Disconnect = 0x2000000
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        public struct DISPLAY_DEVICE
        {
            [MarshalAs(UnmanagedType.U4)]
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            [MarshalAs(UnmanagedType.U4)]
            public DisplayDeviceStateFlags StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        private unsafe class EnumDisplayMonitorsState
        {
            public List<DisplayMonitorInfo1> Items = new List<DisplayMonitorInfo1>();

            private bool Update(IntPtr hMonitor, IntPtr hdcMonitor, RECT* lprcMonitor, void* dwData)
            {
                this.Items.Add(new DisplayMonitorInfo1(hMonitor, hdcMonitor, lprcMonitor));
                return true;
            }

            public void Get()
            {
                var result = User32.EnumDisplayMonitors(IntPtr.Zero, null, this.Update, null);
            }
        }

        private unsafe class DisplayMonitorInfo1
        {
            public IntPtr hMonitor;
            public IntPtr hdcMonitor;
            public RECT rcMonitor;

            public DisplayMonitorInfo1(IntPtr hMonitor, IntPtr hdcMonitor, RECT* lprcMonitor)
            {
                this.hMonitor = hMonitor;
                this.hdcMonitor = hdcMonitor;
                this.rcMonitor = *lprcMonitor;
            }
        }

        public enum MonitorInfoFlags : uint { None = 0, Primary = 1 }

        public unsafe class DisplayMonitorInfo
        {
            public IntPtr Handle;
            public RECT MonitorRectangle;
            public RECT WorkAreaRectangle;
            public MonitorInfoFlags Flags;
            public string Name;

            internal DisplayMonitorInfo(IntPtr handle, ref User32.MONITORINFOEX input)
            {
                this.Handle = handle;
                this.MonitorRectangle = input.Monitor;
                this.WorkAreaRectangle = input.WorkArea;
                this.Flags = (MonitorInfoFlags)input.Flags;
                fixed (char* ptr = input.DeviceName)
                    this.Name = new string(ptr);
            }

        }

        public static unsafe List<DisplayMonitorInfo> GetDisplayMonitors()
        {
            var state = new EnumDisplayMonitorsState();
            state.Get();
            var output = new List<DisplayMonitorInfo>(state.Items.Count);
            foreach (var monitor in state.Items)
            {
                User32.MONITORINFOEX monitor_info = new User32.MONITORINFOEX();
                monitor_info.cbSize = sizeof(User32.MONITORINFOEX);
                var got_info = User32.GetMonitorInfoEx(monitor.hMonitor, &monitor_info);

                if (got_info)
                {

                    output.Add(new DisplayMonitorInfo(monitor.hMonitor, ref monitor_info));
                }
            }
            return output;
        }

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetTitleBarInfo(IntPtr hwnd, ref TITLEBARINFO pti);

        [StructLayout(LayoutKind.Sequential)]
        unsafe struct TITLEBARINFO
        {
            public const int CCHILDREN_TITLEBAR = 5;
            public uint cbSize;
            public RECT rcTitleBar;
            public fixed uint rgstate[CCHILDREN_TITLEBAR + 1];
        }

        [Flags]
        public enum StateSystemEnum : uint { FOCUSABLE = 0x00100000, INVISIBLE = 0x00008000, OFFSCREEN = 0x00010000, UNAVAILABLE = 0x00000001, PRESSED = 0x00000008 };

        [DllImport("user32.dll")]
        static extern IntPtr GetLastActivePopup(IntPtr hWnd);

        public static unsafe bool IsAltTabWindow(IntPtr hwnd)
        {
            IntPtr hwndTry, hwndWalk = IntPtr.Zero;

            if (!User32.IsWindowVisible(hwnd))
                return false;

            hwndTry = User32.GetAncestor(hwnd, User32.GetAncestorFlags.GA_ROOTOWNER);
            while (hwndTry != hwndWalk)
            {
                hwndWalk = hwndTry;
                hwndTry = GetLastActivePopup(hwndWalk);
                if (User32.IsWindowVisible(hwndTry))
                    break;
            }
            if (hwndWalk != hwnd)
                return false;

            // the following removes some task tray programs and "Program Manager"
            var ti = new TITLEBARINFO();
            ti.cbSize = (uint)sizeof(TITLEBARINFO);
            GetTitleBarInfo(hwnd, ref ti);
            if ((ti.rgstate[0] & (uint)StateSystemEnum.INVISIBLE) > 0)
                return false;

            // Tool windows should not be displayed either, these do not appear in the
            // task bar.
            if ( (User32.GetWindowLong(hwnd, User32.WindowLongIndexFlags.GWL_EXSTYLE) & (uint)User32.WindowStylesEx.WS_EX_TOOLWINDOW) > 0)
                return false;

            return true;
        }

        public static class PlacementConstants
        {
            public static IntPtr BOTTOM = new IntPtr(1);
            public static IntPtr NOTOPMOST = new IntPtr(-2);
            public static IntPtr TOP = new IntPtr(0);
            public static IntPtr TOPMOST = new IntPtr(-1);
        }

        public static unsafe bool GetWindowPlacement(IntPtr window_handle, out User32.WINDOWPLACEMENT output)
        {
            output = new User32.WINDOWPLACEMENT();
            output.length = sizeof(User32.WINDOWPLACEMENT);
            fixed (User32.WINDOWPLACEMENT* ptr = &output)
            {
                return User32.GetWindowPlacement(window_handle, ptr);
            }
        }

        public static unsafe bool SetWindowPlacement(IntPtr window_handle, ref User32.WINDOWPLACEMENT output)
        {
            fixed (User32.WINDOWPLACEMENT* ptr = &output)
                return User32.SetWindowPlacement(window_handle, ptr);
        }
    }
}
