using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Rayman2FunBox {

    public class Memory {
        public const int PROCESS_WM_READ = 0x0010;
        public const int PROCESS_VM_WRITE = 0x0020;
        public const int PROCESS_VM_OPERATION = 0x0008;

        [DllImport("kernel32.dll")]
        public static extern IntPtr OpenProcess(int dwDesiredAccess,
               bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll")]
        public static extern bool ReadProcessMemory(int hProcess,
         int lpBaseAddress, byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesRead);

        [DllImport("kernel32.dll", SetLastError = true)]
        public static extern bool WriteProcessMemory(int hProcess, int lpBaseAddress,
          byte[] lpBuffer, int dwSize, ref int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")] //GetLastError function
        public static extern UInt32 GetLastError();

        public static int ReadProcessMemoryInt32(int processHandle, int address)
        {
            int bytesReadOrWritten = 0;

            byte[] buffer = new byte[4];
            Memory.ReadProcessMemory((int)processHandle, address, buffer, buffer.Length, ref bytesReadOrWritten);
            return BitConverter.ToInt32(buffer, 0);
        }

        public static short ReadProcessMemoryInt16(int processHandle, int address)
        {
            int bytesReadOrWritten = 0;

            byte[] buffer = new byte[2];
            Memory.ReadProcessMemory((int)processHandle, address, buffer, buffer.Length, ref bytesReadOrWritten);
            return BitConverter.ToInt16(buffer, 0);
        }

        public static float ReadProcessMemoryFloat(int processHandle, int address)
        {
            int bytesReadOrWritten = 0;

            byte[] buffer = new byte[4];
            Memory.ReadProcessMemory((int)processHandle, address, buffer, buffer.Length, ref bytesReadOrWritten);
            return BitConverter.ToSingle(buffer, 0);
        }

        public static byte[] ReadProcessMemoryBytes(int processHandle, int address, int bytes)
        {
            int bytesReadOrWritten = 0;

            byte[] buffer = new byte[bytes];
            Memory.ReadProcessMemory((int)processHandle, address, buffer, buffer.Length, ref bytesReadOrWritten);
            return buffer;
        }

        public static int WriteProcessMemoryBytes(int processHandle, int address, byte[] buffer)
        {
            int bytesReadOrWritten = 0;

            Memory.WriteProcessMemory((int)processHandle, address, buffer, buffer.Length, ref bytesReadOrWritten);
            return bytesReadOrWritten;
        }

        public static int WriteProcessMemoryInt32(int processHandle, int address, int value)
        {
            int bytesReadOrWritten = 0;

            byte[] buffer = BitConverter.GetBytes(value);
            Memory.WriteProcessMemory((int)processHandle, address, buffer, buffer.Length, ref bytesReadOrWritten);
            return bytesReadOrWritten;
        }

        public static int WriteProcessMemoryInt16(int processHandle, int address, short value)
        {
            int bytesReadOrWritten = 0;

            byte[] buffer = BitConverter.GetBytes(value);
            Memory.WriteProcessMemory((int)processHandle, address, buffer, buffer.Length, ref bytesReadOrWritten);
            return bytesReadOrWritten;
        }

        public enum Protection {
            PAGE_NOACCESS = 0x01,
            PAGE_READONLY = 0x02,
            PAGE_READWRITE = 0x04,
            PAGE_WRITECOPY = 0x08,
            PAGE_EXECUTE = 0x10,
            PAGE_EXECUTE_READ = 0x20,
            PAGE_EXECUTE_READWRITE = 0x40,
            PAGE_EXECUTE_WRITECOPY = 0x80,
            PAGE_GUARD = 0x100,
            PAGE_NOCACHE = 0x200,
            PAGE_WRITECOMBINE = 0x400
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MEMORY_BASIC_INFORMATION {
            public IntPtr BaseAddress;
            public IntPtr AllocationBase;
            public uint AllocationProtect;
            public IntPtr RegionSize;
            public uint State;
            public uint Protect;
            public uint Type;
        }

        [DllImport("kernel32.dll")]
        static extern int VirtualQueryEx(IntPtr hProcess, IntPtr lpAddress, out MEMORY_BASIC_INFORMATION lpBuffer, uint dwLength);

        [DllImport("kernel32.dll")]
        public static extern bool VirtualProtectEx(IntPtr hProcess, IntPtr lpAddress, UIntPtr dwSize, uint flNewProtect, out uint lpflOldProtect);

        public static int WriteProcessMemoryFloat(int processHandle, int address, float value, bool forceProtection = false)
        {

            if (forceProtection) {
                MEMORY_BASIC_INFORMATION info = new MEMORY_BASIC_INFORMATION();
                int query = VirtualQueryEx((IntPtr)processHandle, (IntPtr)address, out info, (uint)Marshal.SizeOf(typeof(MEMORY_BASIC_INFORMATION)));

                if (info.Protect == 2) {
                    uint oldProtect = 0;
                    bool protectExecuted = VirtualProtectEx((IntPtr)processHandle, (IntPtr)info.BaseAddress, (UIntPtr)(int)info.RegionSize, (uint)Protection.PAGE_READWRITE, out oldProtect);
                }
            }
            int bytesReadOrWritten = 0;

            byte[] buffer = BitConverter.GetBytes(value);
            bool success = Memory.WriteProcessMemory((int)processHandle, address, buffer, buffer.Length, ref bytesReadOrWritten);

            return bytesReadOrWritten;
        }

        public static byte ReadProcessMemoryByte(int processHandle, int address)
        {
            int bytesReadOrWritten = 0;

            byte[] buffer = new byte[1];
            Memory.ReadProcessMemory((int)processHandle, address, buffer, buffer.Length, ref bytesReadOrWritten);
            return buffer[0];
        }

        public static uint ReadProcessMemoryUInt32(int processHandle, int address)
        {
            int bytesReadOrWritten = 0;

            byte[] buffer = new byte[4];
            Memory.ReadProcessMemory((int)processHandle, address, buffer, buffer.Length, ref bytesReadOrWritten);
            return BitConverter.ToUInt32(buffer, 0);
        }

        public static string ReadProcessMemoryString(int processHandle, int offset, int maxLength)
        {
            int bytesReadOrWritten = 0;
            byte[] buffer = new byte[maxLength];
            Memory.ReadProcessMemory((int)processHandle, offset, buffer, buffer.Length, ref bytesReadOrWritten);
            string str = Encoding.ASCII.GetString(buffer);
            if (str.IndexOf((char)0)>0)
                str = str.Substring(0, str.IndexOf((char)0)); // remove after null terminator
            else
                str = "";
            return str;
        }

        public static int GetPointerPath(int processHandle, int baseAddress, params int[] offsets)
        {
            int currentAddress = baseAddress;
            int bytesReadOrWritten = 0;

            byte[] buffer = new byte[4];
            Memory.ReadProcessMemory((int)processHandle, currentAddress, buffer, buffer.Length, ref bytesReadOrWritten);
            currentAddress = BitConverter.ToInt32(buffer, 0);

            foreach (int offset in offsets) {
                Memory.ReadProcessMemory((int)processHandle, currentAddress + offset, buffer, buffer.Length, ref bytesReadOrWritten);
                currentAddress = BitConverter.ToInt32(buffer, 0);
            }

            return currentAddress;
        }

        public static int WriteProcessMemoryByte(int processHandle, int address, byte value)
        {
            int bytesReadOrWritten = 0;

            byte[] buffer = new byte[] { value };
            Memory.WriteProcessMemory((int)processHandle, address, buffer, buffer.Length, ref bytesReadOrWritten);
            return bytesReadOrWritten;
        }
    }
}
