using PInvoke;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace WindowArranger
{
    class Program
    {
        static void Main(string[] args)
        {
            var p = new Program();
            p.Run(args);
        }

        public enum LayoutEnum { Auto, Laptop, LaptopAnd4K, Only4K }

        public LayoutEnum LayoutMode;

        public static (int,int) Dimensions(ref RECT rect)
        {
            return (rect.right - rect.left, rect.bottom - rect.top);
        }

        public void Run(string[] args)
        {
            var parse_result = ParseArguments(args);
            if (!parse_result.Success)
                ExitWithError(parse_result.Message);

            if (this.LayoutMode == LayoutEnum.Auto)
            {
                var displays = Helpers.GetDisplayMonitors().ToList();
                if (displays.Count == 1)
                    this.LayoutMode = LayoutEnum.Laptop;
                else if (displays.Count == 2)
                    this.LayoutMode = LayoutEnum.LaptopAnd4K;
                else
                    ExitWithError("Auto mode only works with one or two screens.");
            }

            //this.Layout(this.LayoutTest);

            switch (this.LayoutMode)
            {
                case LayoutEnum.Laptop:
                    this.Layout(this.LayoutLaptop);
                    break;
                case LayoutEnum.LaptopAnd4K:
                    this.Layout(this.LayoutLaptopAnd4K);
                    break;
                case LayoutEnum.Only4K:
                    this.Layout(this.Layout4K);
                    break;
                default:
                    ExitWithError($"Unimplemented layout {this.LayoutMode}");
                    break;
            }
        }

        private ParseResult ParseArguments(string[] args)
        {
            if (args.Length == 0)
            {
                this.LayoutMode = LayoutEnum.Auto;
                return new ParseResult(true, null);
            }
            else if (args.Length == 1)
            {
                if (!Enum.TryParse<LayoutEnum>(args[0], out this.LayoutMode))
                    return new ParseResult(false, $"Unrecognized layout: {args[0]}");
                else
                    return new ParseResult(true, null);
            }
            else //if (args.Length > 1)
            {
                return new ParseResult(false, "Requires exactly one argument.");
            }
        }

        private void ExitWithError(string message)
        {
            Console.WriteLine(message);
            Console.WriteLine("Press enter to exit.");
            Console.ReadLine();
            Environment.Exit(-1);
        }       

        private unsafe void LayoutFromFractionalRelative(Helpers.DisplayMonitorInfo monitor, IntPtr window_handle, double origin_x, double origin_y, double width, double height)
        {
            var rel_left = (int)Math.Round(origin_x*monitor.WorkAreaRectangle.Width());
            var rel_right = (int)Math.Round((origin_x + width) * monitor.WorkAreaRectangle.Width());
            var rel_top = (int)Math.Round(origin_y * monitor.WorkAreaRectangle.Height());
            var rel_bottom = (int)Math.Round((origin_y + height) * monitor.WorkAreaRectangle.Height());

            var abs_left = monitor.WorkAreaRectangle.left + rel_left;
            var abs_top = monitor.WorkAreaRectangle.top + rel_top;
            var abs_width = rel_right - rel_left;
            var abs_height = rel_bottom - rel_top;

            bool current_pos_result = User32.GetWindowRect(window_handle, out RECT current_pos);
            var monitor_handle = User32.MonitorFromWindow(window_handle, User32.MonitorOptions.MONITOR_DEFAULTTONULL);
            bool current_placement_result = Helpers.GetWindowPlacement(window_handle, out User32.WINDOWPLACEMENT current_placement);

            if (current_placement.showCmd == User32.WindowShowStyle.SW_SHOWMINIMIZED)
            {
                var tmp_placement = current_placement;
                tmp_placement.showCmd = User32.WindowShowStyle.SW_SHOWNORMAL;
                Helpers.SetWindowPlacement(window_handle, ref tmp_placement);
                
                var flags = User32.SetWindowPosFlags.SWP_NOZORDER | User32.SetWindowPosFlags.SWP_SHOWWINDOW;
                var result = User32.SetWindowPos(window_handle, IntPtr.Zero, abs_left, abs_top, abs_width, abs_height, flags);

                if (width == 1.0 && height == 1.0)
                {
                    Helpers.GetWindowPlacement(window_handle, out tmp_placement);
                    tmp_placement.showCmd = User32.WindowShowStyle.SW_SHOWMAXIMIZED;
                    Helpers.SetWindowPlacement(window_handle, ref tmp_placement);
                }

                Helpers.GetWindowPlacement(window_handle, out tmp_placement);
                tmp_placement.showCmd = User32.WindowShowStyle.SW_SHOWMINIMIZED;
                Helpers.SetWindowPlacement(window_handle, ref tmp_placement);
            }
            else if (current_placement.showCmd == User32.WindowShowStyle.SW_SHOWMAXIMIZED)
            {
                var tmp_placement = current_placement;
                tmp_placement.showCmd = User32.WindowShowStyle.SW_SHOWNORMAL;
                Helpers.SetWindowPlacement(window_handle, ref tmp_placement);

                var flags = User32.SetWindowPosFlags.SWP_NOZORDER | User32.SetWindowPosFlags.SWP_SHOWWINDOW;
                var result = User32.SetWindowPos(window_handle, IntPtr.Zero, abs_left, abs_top, abs_width, abs_height, flags);

                if (width == 1.0 && height == 1.0)
                {
                    Helpers.GetWindowPlacement(window_handle, out tmp_placement);
                    tmp_placement.showCmd = User32.WindowShowStyle.SW_SHOWMAXIMIZED;
                    Helpers.SetWindowPlacement(window_handle, ref tmp_placement);
                }
            }
            else
            {
                var flags = User32.SetWindowPosFlags.SWP_NOZORDER | User32.SetWindowPosFlags.SWP_SHOWWINDOW;
                var result = User32.SetWindowPos(window_handle, IntPtr.Zero, abs_left, abs_top, abs_width, abs_height, flags);

                if (width == 1.0 && height == 1.0)
                {
                    var tmp_placement = current_placement;
                    Helpers.GetWindowPlacement(window_handle, out tmp_placement);
                    tmp_placement.showCmd = User32.WindowShowStyle.SW_SHOWMAXIMIZED;
                    Helpers.SetWindowPlacement(window_handle, ref tmp_placement);
                }
            }
            //if (!User32.MoveWindow(window_handle, abs_left, abs_top, abs_width, abs_height, true))
            
        }

        private void LayoutRightHalf(Helpers.DisplayMonitorInfo monitor, IntPtr window_handle)
        {
            LayoutFromFractionalRelative(monitor, window_handle, 0.5, 0.0, 0.5, 1.0);
        }

        private void LayoutLeftTop(Helpers.DisplayMonitorInfo monitor, IntPtr window_handle)
        {
            LayoutFromFractionalRelative(monitor, window_handle, 0.0, 0.0, 0.5, 0.5);
        }

        private void LayoutLeftBottom(Helpers.DisplayMonitorInfo monitor, IntPtr window_handle)
        {
            LayoutFromFractionalRelative(monitor, window_handle, 0.0, 0.5, 0.5, 0.5);
        }

        private void LayoutMaximized(Helpers.DisplayMonitorInfo monitor, IntPtr window_handle)
        {
            LayoutFromFractionalRelative(monitor, window_handle, 0.0, 0.0, 1.0, 1.0);
        }

        

        private static HashSet<string> WindowsToIgnore = new HashSet<string>()
        {
            "WindowArranger",
            "ApplicationFrameHost",            
        };

        private void Layout(LayoutLogicDel logic)
        {
            var displays = Helpers.GetDisplayMonitors().ToList();
            var windows = Helpers.EnumWindows().Where(x => Helpers.IsAltTabWindow(x)).ToList();

            var process_names = new List<string>();
            foreach (var main_window in windows)
            {
                var pid = Helpers.GetWindowsProcessId(main_window);
                var process = Process.GetProcessById(pid);

                if (WindowsToIgnore.Contains(process.ProcessName))
                    continue;

                var pname = process.ProcessName;

                logic(displays, main_window, pname);
                
            }
        }

        private delegate void LayoutLogicDel(List<Helpers.DisplayMonitorInfo> displays, IntPtr main_window, string pname);

        private void LayoutLaptop(List<Helpers.DisplayMonitorInfo> displays, IntPtr main_window, string pname)
        {
            LayoutMaximized(displays[0], main_window);
        }

        private void LayoutLaptopAnd4K(List<Helpers.DisplayMonitorInfo> displays, IntPtr main_window, string pname)
        {
            if (pname == "devenv")
                LayoutRightHalf(displays[1], main_window);
            else if (pname == "Teams")
                LayoutMaximized(displays[0], main_window);
            else if (pname == "chrome")
                LayoutMaximized(displays[0], main_window);
            else if (pname == "OUTLOOK")
                LayoutMaximized(displays[0], main_window);
            else if (pname == "explorer")
                LayoutLeftBottom(displays[1], main_window);
            else if (pname == "firefox")
                LayoutLeftTop(displays[1], main_window);
            else if (pname == "notepad++")
                LayoutLeftBottom(displays[1], main_window);
            else
                LayoutLeftBottom(displays[1], main_window);
        }

        private void Layout4K(List<Helpers.DisplayMonitorInfo> displays, IntPtr main_window, string pname)
        {
            if (pname == "devenv")
                LayoutRightHalf(displays[1], main_window);
            else if (pname == "firefox")
                LayoutLeftTop(displays[1], main_window);
            else
                LayoutLeftBottom(displays[1], main_window);
        }

        private void LayoutTest(List<Helpers.DisplayMonitorInfo> displays, IntPtr main_window, string pname)
        {
            if (pname == "notepad++")
                LayoutLeftBottom(displays[1], main_window);
        }

        private class ParseResult
        {
            public bool Success;
            public string Message;

            public ParseResult(bool success, string message)
            {
                this.Success = success;
                this.Message = message;
            }
        }
    }
}
