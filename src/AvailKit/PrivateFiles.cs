
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
namespace AvailKit;
/// <summary>Linux x64 owned0700/0600 storage with openat/no-follow, fstat-equivalent statx and durable exclusive creation.
/// Other platforms should use an application vault and RequestIntent.ExportPrivate/ImportPrivate.</summary>
public static class PrivateFiles
{
    private const int CloseExec=0x80000,NoFollow=0x20000,NonBlock=0x800,DirectoryFlag=0x10000;
    [StructLayout(LayoutKind.Explicit,Size=256)] private struct Stat { [FieldOffset(0)] public uint Mask;[FieldOffset(20)] public uint Uid;[FieldOffset(28)]public ushort Mode;[FieldOffset(40)]public ulong Size; }
    [DllImport("libc",SetLastError=true)]private static extern int open(string path,int flags);
    [DllImport("libc",SetLastError=true)]private static extern int openat(int dir,string path,int flags,uint mode);
    [DllImport("libc",SetLastError=true)]private static extern int statx(int fd,string path,int flags,uint mask,out Stat stat);
    [DllImport("libc",SetLastError=true)]private static extern uint geteuid();
    [DllImport("libc",SetLastError=true)]private static extern int fsync(int fd);
    private static Stat Check(SafeFileHandle handle,bool directory)
    {
        if(handle.IsInvalid||statx(handle.DangerousGetHandle().ToInt32(),"",0x1000,0x7ff,out var s)!=0||(s.Mask&0x20b)!=0x20b||s.Uid!=geteuid()||(s.Mode&0xf000)!=(directory?0x4000:0x8000)||(s.Mode&0xfff)!=(directory?0x1c0:0x180))throw new IOException();return s;
    }
    private static SafeFileHandle Parent(string path)
    {
        if(!OperatingSystem.IsLinux()||RuntimeInformation.OSArchitecture!=Architecture.X64)throw new IOException();var parent=Path.GetDirectoryName(Path.GetFullPath(path))??throw new IOException();var handle=new SafeFileHandle(open(parent,CloseExec|NoFollow|NonBlock|DirectoryFlag),true);
        try{Check(handle,true);return handle;}catch{handle.Dispose();throw;}
    }
    public static byte[] Read(string path,int limit=131072)
    {
        try {if(limit<1||limit>131072)throw new IOException();using var parent=Parent(path);using var handle=new SafeFileHandle(openat(parent.DangerousGetHandle().ToInt32(),Path.GetFileName(path),CloseExec|NoFollow|NonBlock,0),true);var s=Check(handle,false);if(s.Size<1||s.Size>(ulong)limit)throw new IOException();using var stream=new FileStream(handle,FileAccess.Read);var bytes=new byte[(int)s.Size];stream.ReadExactly(bytes);if(stream.ReadByte()!=-1||Check(handle,false).Size!=s.Size)throw new IOException();return bytes;}
        catch{throw AvailKitException.Input("invalid_private_file");}
    }
    public static void WriteNew(string path,byte[] bytes)
    {
        try {if(bytes.Length<1||bytes.Length>131072)throw new IOException();using var parent=Parent(path);using var handle=new SafeFileHandle(openat(parent.DangerousGetHandle().ToInt32(),Path.GetFileName(path),1|0x40|0x80|CloseExec|NoFollow|NonBlock,0x180),true);Check(handle,false);using(var stream=new FileStream(handle,FileAccess.Write)){stream.Write(bytes);stream.Flush(true);}if(fsync(parent.DangerousGetHandle().ToInt32())!=0)throw new IOException();}
        catch{throw AvailKitException.Input("intent_persistence_failed");}
    }
    public static string Credential(string path) {try {var value=new System.Text.UTF8Encoding(false,true).GetString(Read(path,4096));if(value.EndsWith("\n",StringComparison.Ordinal))value=value[..^1];if(value.EndsWith("\r",StringComparison.Ordinal))value=value[..^1];return Protocol.Token(value);}catch {throw AvailKitException.Input("invalid_private_file");}}
}
