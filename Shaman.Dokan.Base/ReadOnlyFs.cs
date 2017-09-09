using System;
using System.IO;
using System.Security.AccessControl;
using DokanNet;

namespace Shaman.Dokan
{
    public abstract class ReadOnlyFs : FileSystemBase
    {
        public override NtStatus WriteFile(string fileName, byte[] buffer, out int bytesWritten, long offset, DokanFileInfo info)
        {
            bytesWritten = 0;
            return NtStatus.DiskFull;
        }

        public override NtStatus SetAllocationSize(string fileName, long length, DokanFileInfo info)
        {
            return NtStatus.DiskFull;
        }

        public override NtStatus SetEndOfFile(string fileName, long length, DokanFileInfo info)
        {
            return NtStatus.DiskFull;
        }

        public override NtStatus SetFileAttributes(string fileName, FileAttributes attributes, DokanFileInfo info)
        {
            return NtStatus.DiskFull;
        }

        public override NtStatus SetFileSecurity(string fileName, FileSystemSecurity security, AccessControlSections sections, DokanFileInfo info)
        {
            return NtStatus.DiskFull;
        }

        public override NtStatus SetFileTime(string fileName, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime, DokanFileInfo info)
        {
            return NtStatus.DiskFull;
        }

        public override NtStatus MoveFile(string oldName, string newName, bool replace, DokanFileInfo info)
        {
            return NtStatus.DiskFull;
        }

        public override NtStatus DeleteDirectory(string fileName, DokanFileInfo info)
        {
            return NtStatus.DiskFull;
        }

        public override NtStatus DeleteFile(string fileName, DokanFileInfo info)
        {
            return NtStatus.DiskFull;
        }

        public override NtStatus LockFile(string fileName, long offset, long length, DokanFileInfo info)
        {
            return NtStatus.DiskFull;
        }

        public override NtStatus UnlockFile(string fileName, long offset, long length, DokanFileInfo info)
        {
            return NtStatus.DiskFull;
        }




        public override void Cleanup(string fileName, DokanFileInfo info)
        {
#if TRACE
            if (info.Context != null)
                Console.WriteLine(DokanFormat($"{nameof(Cleanup)}('{fileName}', {info} - entering"));
#endif

            (info.Context as Stream)?.Dispose();
            info.Context = null;
            Trace(nameof(Cleanup), fileName, info, DokanResult.Success);
        }

    }
}