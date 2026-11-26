# File identity (volume serial number and file index) read from an open handle with GetFileInformationByHandle, using
# the native BY_HANDLE_FILE_INFORMATION layout (FILETIME is two DWORDs, so no padding). WriteChecked opens the
# destination once, compares the identity of that handle with the protected identities and only then truncates and
# writes through the same handle, so the file written is the file checked. ReadAll returns a file's bytes together with
# the identity of the handle they were read through, so an input's recorded identity is the file actually read.
# DirectoryOf returns the same identity for a directory (opened with backup semantics), so two spellings of one physical
# directory (a short 8.3 alias, a junction, another case) compare equal. LongPathOf expands short (8.3) components of an
# existing path, so names derived from it (a receipt or lock beside a ZIP) use the real long filename. LinkCountOf is the
# file's hard-link count.
if (-not ('KwrFileIdentityV4' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
public static class KwrFileIdentityV4 {
    [StructLayout(LayoutKind.Sequential)]
    struct ByHandleFileInformation {
        public uint FileAttributes;
        public uint CreationTimeLow, CreationTimeHigh, LastAccessTimeLow, LastAccessTimeHigh, LastWriteTimeLow, LastWriteTimeHigh;
        public uint VolumeSerialNumber, FileSizeHigh, FileSizeLow, NumberOfLinks, FileIndexHigh, FileIndexLow;
    }
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool GetFileInformationByHandle(SafeFileHandle h, out ByHandleFileInformation i);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] static extern uint GetLongPathNameW(string shortPath, System.Text.StringBuilder longPath, uint size);
    [DllImport("kernel32.dll", CharSet = CharSet.Unicode, SetLastError = true)] static extern SafeFileHandle CreateFileW(string name, uint access, uint share, IntPtr security, uint disposition, uint flags, IntPtr template);
    static string IdOf(SafeFileHandle handle) {
        ByHandleFileInformation i;
        if (!GetFileInformationByHandle(handle, out i)) throw new IOException("GetFileInformationByHandle failed: " + Marshal.GetLastWin32Error());
        return i.VolumeSerialNumber.ToString("x8") + ":" + i.FileIndexHigh.ToString("x8") + i.FileIndexLow.ToString("x8");
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
    public static string LongPathOf(string path) {
        var buffer = new System.Text.StringBuilder(32768);
        uint length = GetLongPathNameW(path, buffer, (uint)buffer.Capacity);
        return (length == 0 || length >= buffer.Capacity) ? path : buffer.ToString();
    }
    public static uint LinkCountOf(string path) {
        using (var f = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) {
            ByHandleFileInformation i;
            if (!GetFileInformationByHandle(f.SafeFileHandle, out i)) throw new IOException("GetFileInformationByHandle failed: " + Marshal.GetLastWin32Error());
            return i.NumberOfLinks;
        }
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
