using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace Rayman2FunBox {
    class EmulateKeyboardPress {

        public const UInt32 WM_KEYDOWN = 0x0100;
        public const UInt32 WM_KEYUP = 0x0101;
        public const int VK_LControl = 0x74;

        [DllImport("user32.dll")]
        public static extern bool PostMessage(IntPtr hWnd, UInt32 Msg, int wParam, int lParam);

        public static void SendKeyDown(int windowHandle, int vk)
        {
            PostMessage((IntPtr)windowHandle, EmulateKeyboardPress.WM_KEYDOWN, vk, 0);
        }

        public static void SendKeyUp(int windowHandle, int vk)
        {
            PostMessage((IntPtr)windowHandle, EmulateKeyboardPress.WM_KEYUP, vk, 0);
        }
    }
}