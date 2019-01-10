using Rayman2FunBox;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Toe;
using WindowsInput;

namespace Rayman2FunBox {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 

    public partial class MainWindow : Window {
        
        Thread fpsModeThread = null;
        public bool fpsModeEnabled = false;

        Thread zeroHealthModeThread = null;
        public bool zeroHealthModeEnabled = false;

        Thread randomizeRaymanModeThread = null;
        public bool randomizeRaymanModeEnabled = false;

        public float fpsModeSensitivity = 0.5f;

        public MainWindow()
        {
            InitializeComponent();
        }

        public int GetRayman2ProcessHandle()
        {
            Process process;
            if (Process.GetProcessesByName("Rayman2").Length > 0) {
                process = Process.GetProcessesByName("Rayman2")[0];
            } else if (Process.GetProcessesByName("Rayman2.exe").Length > 0) {
                process = Process.GetProcessesByName("Rayman2.exe")[0];
            } else if (Process.GetProcessesByName("Rayman2.exe.noshim").Length > 0) {
                process = Process.GetProcessesByName("Rayman2.exe.noshim")[0];
            } else {
                MessageBox.Show("Error opening process handle: Couldn't find process 'Rayman2'. Please make sure Rayman is running or try launching this program with Administrator rights.");
                return -1;
            }
            IntPtr processHandle = Memory.OpenProcess(Memory.PROCESS_WM_READ | Memory.PROCESS_VM_WRITE | Memory.PROCESS_VM_OPERATION, false, process.Id);
            return (int)processHandle;
        }

        public void btn_zerohp_Click(object sender, RoutedEventArgs e)
        {
            int processHandle = GetRayman2ProcessHandle();
            if (processHandle < 0) { return; }

            int bytesReadOrWritten = 0; // Required somehow

            byte[] buffer = new byte[] { 0 };
            byte[] healthPointerBuffer = new byte[4];
            Memory.ReadProcessMemory((int)processHandle, Constants.off_healthpointer_1, healthPointerBuffer, healthPointerBuffer.Length, ref bytesReadOrWritten);
            int off_healthPointer = BitConverter.ToInt32(healthPointerBuffer, 0) + 0x245;

            Memory.WriteProcessMemory((int)processHandle, off_healthPointer, buffer, buffer.Length, ref bytesReadOrWritten);
        }

        public void Dispose()
        {
        }

        private void StartFpsModeThread(MainWindow window)
        {
            FpsMode.FpsModeThread(window);
        }

        private void StartZeroHealthModeThread(MainWindow window)
        {
            ZeroHealthMode.ZeroHealthModeThread(window);
        }

        private void StartRandomizeRaymanModeThread(MainWindow window)
        {
            RandomizeRaymanMode.RandomizeRaymanModeThread(window);
        }

        private void chk_fpsmode_Checked(object sender, RoutedEventArgs e)
        {
            fpsModeEnabled = true;
            fpsModeThread = new Thread(() => StartFpsModeThread(this));
            fpsModeThread.Start();
        }

        private void chk_fpsmode_Unchecked(object sender, RoutedEventArgs e)
        {
            fpsModeEnabled = false;
        }

        private void chk_zeroHealthMode_Checked(object sender, RoutedEventArgs e)
        {
            zeroHealthModeEnabled = true;
            zeroHealthModeThread = new Thread(() => StartZeroHealthModeThread(this));
            zeroHealthModeThread.Start();
        }

        private void chk_zeroHealthMode_Unchecked(object sender, RoutedEventArgs e)
        {
            zeroHealthModeEnabled = false;
        }

        private void chk_randomizeRaymanMode_Checked(object sender, RoutedEventArgs e)
        {
            randomizeRaymanModeEnabled = true;
            randomizeRaymanModeThread = new Thread(() => StartRandomizeRaymanModeThread(this));
            randomizeRaymanModeThread.Start();
        }

        private void chk_randomizeRaymanMode_Unchecked(object sender, RoutedEventArgs e)
        {
            randomizeRaymanModeEnabled = false;
        }

        private void sensitivitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            fpsModeSensitivity = (float)sensitivitySlider.Value;
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            Environment.Exit(0);
        }

        private void aboutButton_Click(object sender, RoutedEventArgs e)
        {
            new AboutWindow().ShowDialog();
        }
    }
}
