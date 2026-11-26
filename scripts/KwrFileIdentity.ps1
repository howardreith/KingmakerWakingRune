# File identity (volume serial number and file index) read from an open handle with GetFileInformationByHandle, using
# the native BY_HANDLE_FILE_INFORMATION layout (FILETIME is two DWORDs, so no padding). WriteChecked opens the
# destination once, compares the identity of that handle with the protected identities and only then truncates and
# writes through the same handle, so the file written is the file checked. ReadAll returns a file's bytes together with
# the identity of the handle they were read through, so an input's recorded identity is the file actually read.
if (-not ('KwrFileIdentityV3' -as [type])) {
    Add-Type -TypeDefinition @'
using System;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;
public static class KwrFileIdentityV3 {
    [StructLayout(LayoutKind.Sequential)]
    struct ByHandleFileInformation {
        public uint FileAttributes;
        public uint CreationTimeLow, CreationTimeHigh, LastAccessTimeLow, LastAccessTimeHigh, LastWriteTimeLow, LastWriteTimeHigh;
        public uint VolumeSerialNumber, FileSizeHigh, FileSizeLow, NumberOfLinks, FileIndexHigh, FileIndexLow;
    }
    [DllImport("kernel32.dll", SetLastError = true)] static extern bool GetFileInformationByHandle(SafeFileHandle h, out ByHandleFileInformation i);
    static string IdOf(SafeFileHandle handle) {
        ByHandleFileInformation i;
        if (!GetFileInformationByHandle(handle, out i)) throw new IOException("GetFileInformationByHandle failed: " + Marshal.GetLastWin32Error());
        return i.VolumeSerialNumber.ToString("x8") + ":" + i.FileIndexHigh.ToString("x8") + i.FileIndexLow.ToString("x8");
    }
    public static string Of(string path) {
        using (var f = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite | FileShare.Delete)) return IdOf(f.SafeFileHandle);
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
