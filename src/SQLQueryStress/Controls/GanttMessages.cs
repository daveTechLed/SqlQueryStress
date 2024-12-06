using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace SQLQueryStress.Controls;

public static class GanttMessages
{
    public const int WM_USER = 0x0400;
    public const int WM_ONXEVENT = WM_USER + 1;
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);
    public static void SendFitToData(FormMain formMain)
    {
        if (!formMain.IsDisposed && formMain.IsHandleCreated)
        {
            formMain.BeginInvoke(new Action(() =>
            {
                Debug.WriteLine("Sending ONXEVENT message");

                SendMessage(formMain.Handle, WM_ONXEVENT, IntPtr.Zero, IntPtr.Zero);
            }));
        }
    }
}