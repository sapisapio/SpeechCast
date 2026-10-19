// Reproduce RocketDock 1.3.5's first-group lookup, without changing its settings.
// csc /r:System.Drawing.dll RocketDockCompatibilityTests.cs
// RocketDockCompatibilityTests.exe old-exe fixed-exe
// GPLv2.
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
class RocketDockCompatibilityTests
{
    delegate bool EnumName(IntPtr module, IntPtr type, IntPtr name, IntPtr parameter);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern IntPtr LoadLibraryEx(string path, IntPtr file, uint flags);
    [DllImport("kernel32.dll")] static extern bool FreeLibrary(IntPtr module);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern bool EnumResourceNames(IntPtr module, IntPtr type, EnumName callback, IntPtr parameter);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode)] static extern IntPtr FindResource(IntPtr module, string name, IntPtr type);
    [DllImport("kernel32.dll")] static extern uint SizeofResource(IntPtr module, IntPtr resource);
    [DllImport("kernel32.dll")] static extern IntPtr LoadResource(IntPtr module, IntPtr resource);
    [DllImport("kernel32.dll")] static extern IntPtr LockResource(IntPtr resource);
    [DllImport("user32.dll")] static extern int LookupIconIdFromDirectoryEx(byte[] data, bool icon, int width, int height, uint flags);
    [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Unicode)] struct SHFILEINFO
    {
        public IntPtr icon; public int index; public uint attributes;
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=260)] public string displayName;
        [MarshalAs(UnmanagedType.ByValTStr,SizeConst=80)] public string typeName;
    }
    [DllImport("shell32.dll",CharSet=CharSet.Unicode)] static extern IntPtr SHGetFileInfo(string path,uint attributes,out SHFILEINFO info,uint size,uint flags);
    [DllImport("user32.dll")] static extern bool DestroyIcon(IntPtr icon);
    static void Check(bool condition, string message) { if(!condition)throw new Exception(message); Console.WriteLine("PASS "+message); }
    static void Test(string path, bool fixedFile)
    {
        path=Path.GetFullPath(path);
        IntPtr module=LoadLibraryEx(path,IntPtr.Zero,2);
        Check(module!=IntPtr.Zero,"load EXE as data");
        try
        {
            string original=null,lookedUp=null;
            EnumName callback=delegate(IntPtr m,IntPtr t,IntPtr n,IntPtr p)
            {
                long id=n.ToInt64();
                if((id&~0xffffL)==0)
                {
                    original="#"+id;
                    // Installed RocketDock.exe RVA 0x27d52: and eax,0xfff.
                    lookedUp="#"+(id&0x0fff);
                }
                else original=lookedUp=Marshal.PtrToStringUni(n);
                return false;
            };
            EnumResourceNames(module,new IntPtr(14),callback,IntPtr.Zero);
            GC.KeepAlive(callback);
            Console.WriteLine(Path.GetFileName(path)+": "+original+" -> RocketDock looks up "+lookedUp);
            IntPtr resource=FindResource(module,lookedUp,new IntPtr(14));
            if(!fixedFile)
            {
                Check(original=="#32512" && lookedUp=="#3840" && resource==IntPtr.Zero,"old numeric group is truncated and cannot be found");
                SHFILEINFO info;
                SHGetFileInfo(path,0,out info,(uint)Marshal.SizeOf(typeof(SHFILEINFO)),0x100);
                Check(info.icon!=IntPtr.Zero,"legacy Shell fallback supplies an icon");
                try {using(var icon=Icon.FromHandle(info.icon)) Check(icon.Width==32,"RocketDock fallback returns 32px");}
                finally {DestroyIcon(info.icon);}
                return;
            }
            Check(original=="#1" && lookedUp=="#1" && resource!=IntPtr.Zero,"fixed group survives RocketDock lookup");
            byte[] group=new byte[SizeofResource(module,resource)];
            Marshal.Copy(LockResource(LoadResource(module,resource)),group,0,group.Length);
            int selected=LookupIconIdFromDirectoryEx(group,true,512,512,0); // RocketDock's actual request
            int width=0;
            for(int i=0;i<BitConverter.ToUInt16(group,4);i++)
            {
                int at=6+i*14;
                if(BitConverter.ToUInt16(group,at+12)==selected) width=group[at]==0?256:group[at];
            }
            Check(width==256,"RocketDock's 512px request selects the original 256px frame");
        }
        finally {FreeLibrary(module);}
    }
    static void Main(string[] args)
    {
        try{Test(args[0],false);Test(args[1],true);Console.WriteLine("RocketDock compatibility regression passed.");}
        catch(Exception error){Console.Error.WriteLine(error.Message);Environment.ExitCode=1;}
    }
}
