using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ModdingAssistant
{
    internal class MemoryHelper
    {
        [DllImport("kernel32.dll")]
        extern static ulong OpenProcess(uint dwDesiredAccess, bool bInheritHandle, uint dwProcessId);

        [DllImport("kernel32.dll")]
        extern static ulong VirtualAllocEx(ulong hProcess, ulong lpAddress, int size, uint flAllocationType, uint flProtect);

        [DllImport("kernel32.dll")]
        extern static bool WriteProcessMemory(ulong hProcess, ulong lpBaseAddress, [MarshalAs(UnmanagedType.AsAny)] object lpBuffer, int nSize, ulong lpNumberOfBytesWritten);

        [DllImport("kernel32.dll", SetLastError = true)]
        extern static bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, int nSize, out int lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        extern static ulong GetProcAddress(ulong hModule, string procName);

        [DllImport("kernel32.dll")]
        extern static ulong GetModuleHandle(string lpModuleName);

        [DllImport("kernel32.dll")]
        extern static ulong CreateRemoteThread(ulong handle, ulong lpThreadAttributes, ulong dwStackSize, ulong lpStartAddress, ulong lpParameter, ulong dwCreationFlags, ulong lpThreadId);

        [DllImport("kernel32.dll")]
        extern static bool CloseHandle(ulong handle);

        [DllImport("kernel32.dll")]
        extern static uint WaitForSingleObject(ulong hHandle, uint dwMilliseconds);

        private static uint PROCESS_ALL_ACCESS = (uint)(0x000F0000L | 0x00100000L | 0xFFFF);

        private static uint MEM_COMMIT = 0x00001000;
        private static uint MEM_RESERVE = 0x00002000;
        private static uint PAGE_READWRITE = 4;

        public static bool CheckMinecraft()
        {
            return Process.GetProcessesByName("Minecraft.Windows").Length > 0;
        }

        public static Process GetMinecraftProcess()
        {
            return Process.GetProcessesByName("Minecraft.Windows")[0];
        }

        public static bool AlreadyInjected(string dllpath)
        {
            var process = GetMinecraftProcess();
            return GetModule(process, dllpath) != null;
        }

        public static void Unload(string dllpath)
        {
            var process = GetMinecraftProcess();
            var module = GetModule(process, dllpath);

            var hProc = OpenProcess(PROCESS_ALL_ACCESS, false, (uint)process.Id);
            if (hProc == 0 || hProc == 0xffffffffffffffff)
            {
                return;
            }
            ulong freeLibrary = GetProcAddress(GetModuleHandle("kernel32.dll"), "FreeLibrary");
            var hThread = CreateRemoteThread(hProc, 0, 0, freeLibrary, (ulong)module.BaseAddress, 0, 0);
            WaitForSingleObject(hThread, 0xFFFFFFFF);
            if (hThread > 0)
                CloseHandle(hThread);
            if (hProc > 0)
                CloseHandle(hProc);
        }

        private static ProcessModule GetModule(Process process, string dllpath)
        {
            var dll = Path.GetFileName(dllpath).ToLower();
            foreach (ProcessModule module in process.Modules)
            {
                var f = module.FileName.ToLower();
                if (Path.GetFileName(f) == dll && !f.Contains("system32"))
                {
                    return module;
                }
            }

            return null;
        }

        public static void Inject(string dllpath)
        {
            var process = GetMinecraftProcess();

            var hProc = OpenProcess(PROCESS_ALL_ACCESS, false, (uint)process.Id);
            if (hProc == 0 || hProc == 0xffffffffffffffff)
            {
                return;
            }

            byte[] bytes = Encoding.Unicode.GetBytes(dllpath);

            ulong loadLibraryW = GetProcAddress(GetModuleHandle("kernel32.dll"), "LoadLibraryW");
            ulong loc = VirtualAllocEx(hProc, 0, bytes.Length + 2, MEM_COMMIT | MEM_RESERVE, PAGE_READWRITE);
            WriteProcessMemory(hProc, loc, bytes, bytes.Length, 0);
            var hThread = CreateRemoteThread(hProc, 0, 0, loadLibraryW, loc, 0, 0);
            if (hThread > 0)
                CloseHandle(hThread);
            if (hProc > 0)
                CloseHandle(hProc);
        }

        public static void DumpToFile(string dllpath, string to)
        {
            var process = GetMinecraftProcess();
            var module = GetModule(process, dllpath);

            var hProc = OpenProcess(PROCESS_ALL_ACCESS, false, (uint)process.Id);
            if (hProc == 0 || hProc == 0xffffffffffffffff)
            {
                return;
            }

            int size = module.ModuleMemorySize;
            byte[] buffer = new byte[size];
            ReadProcessMemory(process.Handle, module.BaseAddress, buffer, size, out int _);
            File.WriteAllBytes(to, buffer);

            CloseHandle(hProc);
        }

        public static void AddPermission(string dllpath)
        {
            var psi = new ProcessStartInfo();
            psi.FileName = "cmd.exe";
            psi.Arguments = $"/c icacls {dllpath} /grant \"ALL APPLICATION PACKAGES\":(F)";
            psi.CreateNoWindow = true;
            psi.UseShellExecute = false;

            var process = Process.Start(psi);
            process.WaitForExit();
        }
    }
}
