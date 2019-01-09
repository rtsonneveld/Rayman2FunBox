using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using Toe;
using WindowsInput;

namespace Rayman2FunBox
{
    static class ZeroHealthMode
    {
        public static void ZeroHealthModeThread(MainWindow w)
        {
            int processHandle = w.GetRayman2ProcessHandle();

            while (w.zeroHealthModeEnabled)
            {
                int healthPointer = Memory.ReadProcessMemoryInt32(processHandle, Constants.off_healthpointer_1) + 0x245;

                Memory.WriteProcessMemoryByte(processHandle, healthPointer, 0);

                Thread.Sleep(20);
            }
        }
    }
}
