using PInvoke;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace WindowArranger
{
    public class WindowInfo
    {
        public IntPtr Handle;
        public string Title;
        public Process Process;
        public string ProcessName => this.Process?.ProcessName;

        public static WindowInfo Get(IntPtr handle)
        {
            var output = new WindowInfo();
            output.Handle = handle;
            output.Title = User32.GetWindowText(handle);
            int pid = Helpers.GetWindowsProcessId(handle);
            output.Process = Process.GetProcessById(pid);

            return output;
        }
    }
}
