using DokanNet;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Shaman.Dokan
{
    public abstract class FileSystemBasedFs : FileSystemBase
    {

        public override NtStatus MoveFile(string oldName, string newName, bool replace, DokanFileInfo info)
        {
            OnFileRead(oldName);
            OnFileChanged(oldName);
            OnFileChanged(newName);
            var oldpath = GetPath(oldName);
            var newpath = GetPathAware(newName);
            if (newpath == null) return DokanResult.PathNotFound;

            (info.Context as Stream)?.Dispose();
            info.Context = null;

            var exist = info.IsDirectory ? Directory_Exists(newpath) : File_Exists(newpath);

            try
            {

                if (!exist)
                {
                    info.Context = null;
                    if (info.IsDirectory)
                        Directory_Move(oldpath, newpath);
                    else
                        File_Move(oldpath, newpath);
                    return Trace(nameof(MoveFile), oldName, info, DokanResult.Success, newName,
                        replace.ToString(CultureInfo.InvariantCulture));
                }
                else if (replace)
                {
                    info.Context = null;

                    if (info.IsDirectory) //Cannot replace directory destination - See MOVEFILE_REPLACE_EXISTING
                        return Trace(nameof(MoveFile), oldName, info, DokanResult.AccessDenied, newName,
                            replace.ToString(CultureInfo.InvariantCulture));

                    File_Delete(newpath);
                    File_Move(oldpath, newpath);
                    return Trace(nameof(MoveFile), oldName, info, DokanResult.Success, newName,
                        replace.ToString(CultureInfo.InvariantCulture));
                }
            }
            catch (UnauthorizedAccessException)
            {
                return Trace(nameof(MoveFile), oldName, info, DokanResult.AccessDenied, newName,
                    replace.ToString(CultureInfo.InvariantCulture));
            }
            return Trace(nameof(MoveFile), oldName, info, DokanResult.FileExists, newName,
                replace.ToString(CultureInfo.InvariantCulture));
        }


        public override NtStatus DeleteFile(string fileName, DokanFileInfo info)
        {
            OnFileChanged(fileName);
            var filePath = GetPath(fileName);

            if (Directory_Exists(filePath))
                return Trace(nameof(DeleteFile), fileName, info, DokanResult.AccessDenied);

            if (!File_Exists(filePath))
                return Trace(nameof(DeleteFile), fileName, info, DokanResult.FileNotFound);

            if (File_GetAttributes(filePath).HasFlag(FileAttributes.Directory))
                return Trace(nameof(DeleteFile), fileName, info, DokanResult.AccessDenied);

            return Trace(nameof(DeleteFile), fileName, info, DokanResult.Success);
            // we just check here if we could delete the file - the true deletion is in Cleanup
        }

        public override NtStatus SetFileAttributes(string fileName, FileAttributes attributes, DokanFileInfo info)
        {
            OnFileChanged(fileName);
            try
            {
                File_SetAttributes(GetPath(fileName), attributes);
                return Trace(nameof(SetFileAttributes), fileName, info, DokanResult.Success, attributes.ToString());
            }
            catch (UnauthorizedAccessException)
            {
                return Trace(nameof(SetFileAttributes), fileName, info, DokanResult.AccessDenied, attributes.ToString());
            }
            catch (FileNotFoundException)
            {
                return Trace(nameof(SetFileAttributes), fileName, info, DokanResult.FileNotFound, attributes.ToString());
            }
            catch (DirectoryNotFoundException)
            {
                return Trace(nameof(SetFileAttributes), fileName, info, DokanResult.PathNotFound, attributes.ToString());
            }
        }

        protected string GetPath(string fileName)
        {
            var p = GetPathAware(fileName);
            if (p == null)
            {
                return "C:\\NON_EXISTING_PATH";
            }
            return p;
        }





        protected abstract string GetPathAware(string fileName);
    }
}
