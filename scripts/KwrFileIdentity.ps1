# File identity (volume serial number and file index) read from an open handle with GetFileInformationByHandle, using
# the native BY_HANDLE_FILE_INFORMATION layout (FILETIME is two DWORDs, so no padding). WriteChecked opens the
# destination once, compares the identity of that handle with the protected identities and only then truncates and
# writes through the same handle, so the file written is the file checked. ReadAll returns a file's bytes together with
# the identity of the handle they were read through, so an input's recorded identity is the file actually read.
# DirectoryOf returns the same identity for a directory (opened with backup semantics), so two spellings of one physical
# directory (a short 8.3 alias, a junction, another case) compare equal. LongPathOf expands short (8.3) components of an
# existing path, so names derived from it (a receipt or lock beside a ZIP) use the real long filename. LinkCountOf is the
# file's hard-link count.
# FinalPathOf is the physical location of an existing file or directory: GetFinalPathNameByHandle on a handle opened
# through the given path, so directory junctions, symbolic links, short (8.3) names and letter case are resolved to the
# path the file system stores. OpenRead/InfoOf bind a read to one handle (opened without write sharing, so the file's
# bytes cannot change while it is open; another hard-link name can still be removed) and report that handle's identity,
# hard-link count and physical path. LinkNamesOf lists every hard-link name of a file (FindFirstFileNameW).
# The type carries a new version name whenever its members change, so a session that loaded an earlier draft still gets
# these members.
if (-not ('KwrFileIdentityV5' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using Microsoft.Win32.SafeHandles;
public sealed class KwrHandleInfoV5 {
    public string Id;
    public uint LinkCount;
    public string FinalPath;
}
public static class KwrFileIdentityV5 {
    [StructLayout(LayoutKind.Sequential)]
    struct ByHandleFileInformation {
        public uint FileAttributes;
        public uint CreationTimeLow, CreationTimeHigh, LastAccessTimeLow, LastAccessTimeHigh, LastWriteTimeLow, LastWriteTimeHigh;
        public uint VolumeSerialNumber, FileSizeHigh, FileSizeLow, NumberOfLinks, FileIndexHigh, FileIndexLow;
    }
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool GetFileInformationByHandle(SafeFileHandle h, out ByHandleFileInformation i);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] static extern uint GetLongPathNameW(string shortPath, StringBuilder longPath, uint size);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] static extern SafeFileHandle CreateFileW(string name, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] static extern uint GetFinalPathNameByHandleW(SafeFileHandle h, StringBuilder path, uint size, uint flags);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] static extern IntPtr FindFirstFileNameW(string fileName, uint flags, ref uint length, StringBuilder linkName);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] static extern bool FindNextFileNameW(IntPtr find, ref uint length, StringBuilder linkName);
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool FindClose(IntPtr find);
    static ByHandleFileInformation InformationOf(SafeFileHandle handle) {
        ByHandleFileInformation i;
        if (!GetFileInformationByHandle(handle, out i)) throw new IOException("GetFileInformationByHandle failed: " + Marshal.GetLastWin32Error());
        return i;
    }
    static string IdOf(SafeFileHandle handle) {
        var i = InformationOf(handle);
        return i.VolumeSerialNumber.ToString("x8") + ":" + i.FileIndexHigh.ToString("x8") + i.FileIndexLow.ToString("x8");
    }
    static string FinalOf(SafeFileHandle handle) {
        var buffer = new StringBuilder(32768);
        // FILE_NAME_NORMALIZED | VOLUME_NAME_DOS.
        uint length = GetFinalPathNameByHandleW(handle, buffer, (uint)buffer.Capacity, 0);
        if (length == 0 || length >= buffer.Capacity) throw new IOException("GetFinalPathNameByHandle failed: " + Marshal.GetLastWin32Error());
        string path = buffer.ToString();
        if (path.StartsWith(@"\\?\UNC\", StringComparison.Ordinal)) return @"\\" + path.Substring(8);
        if (path.StartsWith(@"\\?\", StringComparison.Ordinal)) return path.Substring(4);
        return path;
    }
    public static string Of(string path) {
        using (var f = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) return IdOf(f.SafeFileHandle);
    }
    public static string DirectoryOf(string path) {
        // FILE_READ_ATTRIBUTES, share read/write/delete, OPEN_EXISTING, FILE_FLAG_BACKUP_SEMANTICS (required for a directory).
        using (var h = CreateFileW(path, 0x80, 7, IntPtr.Zero, 3, 0x02000000, IntPtr.Zero)) {
            if (h.IsInvalid) throw new IOException("Cannot open directory " + path + ": " + Marshal.GetLastWin32Error());
            return IdOf(h);
        }
    }
    public static string FinalPathOf(string path) {
        using (var h = CreateFileW(path, 0x80, 7, IntPtr.Zero, 3, 0x02000000, IntPtr.Zero)) {
            if (h.IsInvalid) throw new IOException("Cannot open " + path + ": " + Marshal.GetLastWin32Error());
            return FinalOf(h);
        }
    }
    public static string LongPathOf(string path) {
        var buffer = new StringBuilder(32768);
        uint length = GetLongPathNameW(path, buffer, (uint)buffer.Capacity);
        return (length == 0 || length >= buffer.Capacity) ? path : buffer.ToString();
    }
    public static uint LinkCountOf(string path) {
        using (var f = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) return InformationOf(f.SafeFileHandle).NumberOfLinks;
    }
    public static FileStream OpenRead(string path) {
        return new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
    }
    public static KwrHandleInfoV5 InfoOf(FileStream f) {
        var i = InformationOf(f.SafeFileHandle);
        return new KwrHandleInfoV5 {
            Id = i.VolumeSerialNumber.ToString("x8") + ":" + i.FileIndexHigh.ToString("x8") + i.FileIndexLow.ToString("x8"),
            LinkCount = i.NumberOfLinks,
            FinalPath = FinalOf(f.SafeFileHandle)
        };
    }
    public static string[] LinkNamesOf(string path) {
        string root = Path.GetPathRoot(path).TrimEnd('\\');
        var names = new List<string>();
        uint length = 32768;
        var buffer = new StringBuilder((int)length);
        IntPtr find = FindFirstFileNameW(path, 0, ref length, buffer);
        if (find == new IntPtr(-1)) throw new IOException("FindFirstFileNameW failed for " + path + ": " + Marshal.GetLastWin32Error());
        try {
            while (true) {
                names.Add(root + buffer.ToString());
                length = 32768;
                buffer = new StringBuilder((int)length);
                if (!FindNextFileNameW(find, ref length, buffer)) {
                    int error = Marshal.GetLastWin32Error();
                    if (error != 38) throw new IOException("FindNextFileNameW failed for " + path + ": " + error);
                    break;
                }
            }
        }
        finally { FindClose(find); }
        return names.ToArray();
    }
    public static byte[] ReadAll(string path, out string id) {
        using (var f = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read)) {
            id = IdOf(f.SafeFileHandle);
            var copy = new MemoryStream();
            f.CopyTo(copy);
            return copy.ToArray();
        }
    }
    public static string WriteChecked(string path, string[] protectedIds, byte[] content) {
        using (var f = new FileStream(path, FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read)) {
            string id = IdOf(f.SafeFileHandle);
            foreach (var p in protectedIds) if (p != null && string.Equals(p, id, StringComparison.Ordinal)) throw new InvalidOperationException("The report path must not be an input of the validation (file identity " + id + ").");
            f.SetLength(0);
            f.Write(content, 0, content.Length);
            return id;
        }
    }
}
'@
}
