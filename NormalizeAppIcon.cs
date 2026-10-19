// Keep the Win32 app icon reachable by legacy launchers. GPLv2.
// csc emits RT_GROUP_ICON #32512. RocketDock 1.3.5 masks numeric first-group
// names with 0x0fff, so #32512 becomes missing #3840 and it falls back to 32px.
// Rename the group to #1; keep all image bytes, IDs and other resources intact.
using System;
using System.ComponentModel;
using System.IO;
using System.Runtime.InteropServices;

class NormalizeAppIcon
{
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr LoadLibraryEx(string path, IntPtr file, uint flags);
    [DllImport("kernel32.dll")] static extern bool FreeLibrary(IntPtr module);
    [DllImport("kernel32.dll", SetLastError = true)]
    static extern IntPtr FindResourceEx(IntPtr module, IntPtr type, IntPtr name, ushort language);
    [DllImport("kernel32.dll")] static extern uint SizeofResource(IntPtr module, IntPtr resource);
    [DllImport("kernel32.dll")] static extern IntPtr LoadResource(IntPtr module, IntPtr resource);
    [DllImport("kernel32.dll")] static extern IntPtr LockResource(IntPtr data);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern IntPtr BeginUpdateResource(string path, bool deleteExisting);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    static extern bool UpdateResource(IntPtr update, IntPtr type, IntPtr name, ushort language, byte[] data, uint size);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool EndUpdateResource(IntPtr update, bool discard);

    static void Main(string[] args)
    {
        if (args.Length != 1) throw new ArgumentException("Compiled EXE path is required.");
        string path = Path.GetFullPath(args[0]);
        var type = new IntPtr(14); var oldName = new IntPtr(32512); var newName = new IntPtr(1);
        byte[] group;
        IntPtr module = LoadLibraryEx(path, IntPtr.Zero, 2); // data only; never execute app
        if (module == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
        try
        {
            IntPtr old = FindResourceEx(module, type, oldName, 0);
            IntPtr current = FindResourceEx(module, type, newName, 0);
            if (old == IntPtr.Zero && current != IntPtr.Zero)
            {
                Console.WriteLine("App icon group already #1.");
                return;
            }
            if (old == IntPtr.Zero || current != IntPtr.Zero)
                throw new InvalidDataException("Expected one csc app icon group #32512 and unused #1.");
            group = new byte[SizeofResource(module, old)];
            IntPtr data = LockResource(LoadResource(module, old));
            if (data == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
            Marshal.Copy(data, group, 0, group.Length);
        }
        finally { FreeLibrary(module); }
        IntPtr update = BeginUpdateResource(path, false);
        if (update == IntPtr.Zero) throw new Win32Exception(Marshal.GetLastWin32Error());
        bool success = false;
        try
        {
            if (!UpdateResource(update, type, newName, 0, group, (uint)group.Length) ||
                !UpdateResource(update, type, oldName, 0, null, 0))
                throw new Win32Exception(Marshal.GetLastWin32Error());
            success = true;
        }
        finally
        {
            if (!EndUpdateResource(update, !success)) throw new Win32Exception(Marshal.GetLastWin32Error());
        }
        Console.WriteLine("Normalized app icon group #32512 -> #1 (image bytes unchanged).");
    }
}
