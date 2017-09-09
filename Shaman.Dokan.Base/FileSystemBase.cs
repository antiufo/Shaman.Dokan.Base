using DokanNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Security.AccessControl;
using System.Text.RegularExpressions;
using System.Globalization;
using FileAccess = DokanNet.FileAccess;
using System.Diagnostics;
using System.Threading;
using Shaman.Runtime;
using Microsoft.Win32.SafeHandles;
using Monitor.Core.Utilities;

namespace Shaman.Dokan
{
    public abstract class FileSystemBase : IDokanOperations
    {
        protected const FileAccess DataAccess = FileAccess.ReadData | FileAccess.WriteData | FileAccess.AppendData |
                                         FileAccess.Execute |
                                         FileAccess.GenericExecute | FileAccess.GenericWrite |
                                         FileAccess.GenericRead;

        protected const FileAccess DataWriteAccess = FileAccess.WriteData | FileAccess.AppendData |
                                                   FileAccess.Delete |
                                                   FileAccess.GenericWrite;


        public abstract void Cleanup(string fileName, DokanFileInfo info);
        public abstract NtStatus CreateFile(string fileName, DokanNet.FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes, DokanFileInfo info);
        public abstract NtStatus DeleteDirectory(string fileName, DokanFileInfo info);
        public abstract NtStatus DeleteFile(string fileName, DokanFileInfo info);
        public abstract NtStatus GetFileInformation(string fileName, out FileInformation fileInfo, DokanFileInfo info);
        public abstract NtStatus GetVolumeInformation(out string volumeLabel, out FileSystemFeatures features, out string fileSystemName, DokanFileInfo info);
        public abstract NtStatus MoveFile(string oldName, string newName, bool replace, DokanFileInfo info);
        public abstract NtStatus SetEndOfFile(string fileName, long length, DokanFileInfo info);
        public abstract NtStatus SetFileAttributes(string fileName, FileAttributes attributes, DokanFileInfo info);
        public abstract NtStatus SetFileSecurity(string fileName, FileSystemSecurity security, AccessControlSections sections, DokanFileInfo info);
        public abstract NtStatus SetFileTime(string fileName, DateTime? creationTime, DateTime? lastAccessTime, DateTime? lastWriteTime, DokanFileInfo info);
        public abstract NtStatus WriteFile(string fileName, byte[] buffer, out int bytesWritten, long offset, DokanFileInfo info);



        public virtual NtStatus Mounted(DokanFileInfo info)
        {
            return Trace(nameof(Mounted), null, info, DokanResult.Success);
        }

        public virtual NtStatus Unmounted(DokanFileInfo info)
        {
            return Trace(nameof(Unmounted), null, info, DokanResult.Success);
        }

        public virtual NtStatus FlushFileBuffers(string fileName, DokanFileInfo info)
        {
            try
            {
                ((Stream)(info.Context)).Flush();
                return Trace(nameof(FlushFileBuffers), fileName, info, DokanResult.Success);
            }
            catch (IOException)
            {
                return Trace(nameof(FlushFileBuffers), fileName, info, DokanResult.DiskFull);
            }
        }


        public virtual void CloseFile(string fileName, DokanFileInfo info)
        {
#if TRACE
            if (info.Context != null)
                Console.WriteLine(DokanFormat($"{nameof(CloseFile)}('{fileName}', {info} - entering"));
#endif

            (info.Context as Stream)?.Dispose();
            info.Context = null;
            Trace(nameof(CloseFile), fileName, info, DokanResult.Success);
            // could recreate cleanup code here but this is not called sometimes
        }


        public virtual NtStatus LockFile(string fileName, long offset, long length, DokanFileInfo info)
        {
            try
            {
                (info.Context as FileStream)?.Lock(offset, length);
                return Trace(nameof(LockFile), fileName, info, DokanResult.Success,
                    offset.ToString(CultureInfo.InvariantCulture), length.ToString(CultureInfo.InvariantCulture));
            }
            catch (IOException)
            {
                return Trace(nameof(LockFile), fileName, info, DokanResult.AccessDenied,
                    offset.ToString(CultureInfo.InvariantCulture), length.ToString(CultureInfo.InvariantCulture));
            }
        }

        public virtual NtStatus UnlockFile(string fileName, long offset, long length, DokanFileInfo info)
        {
            try
            {
                (info.Context as FileStream)?.Unlock(offset, length);
                return Trace(nameof(UnlockFile), fileName, info, DokanResult.Success,
                    offset.ToString(CultureInfo.InvariantCulture), length.ToString(CultureInfo.InvariantCulture));
            }
            catch (IOException)
            {
                return Trace(nameof(UnlockFile), fileName, info, DokanResult.AccessDenied,
                    offset.ToString(CultureInfo.InvariantCulture), length.ToString(CultureInfo.InvariantCulture));
            }
        }


        public NtStatus FindStreams(string fileName, IntPtr enumContext, out string streamName, out long streamSize,
            DokanFileInfo info)
        {
            streamName = string.Empty;
            streamSize = 0;
            return Trace(nameof(FindStreams), fileName, info, DokanResult.NotImplemented, enumContext.ToString(),
                "out " + streamName, "out " + streamSize.ToString());
        }

        public virtual NtStatus FindStreams(string fileName, out IList<FileInformation> streams, DokanFileInfo info)
        {
            streams = new FileInformation[0];
            return Trace(nameof(FindStreams), fileName, info, DokanResult.NotImplemented);
        }

        protected virtual void File_SetLastAccessTime(string filePath, DateTime value)
        {
            File.SetLastAccessTimeUtc(filePath, value);
        }

        protected virtual void File_SetCreationTime(string filePath, DateTime value)
        {
            File.SetCreationTimeUtc(filePath, value);
        }

        protected virtual void File_SetLastWriteTime(string filePath, DateTime value)
        {
            File.SetLastWriteTimeUtc(filePath, value);
        }

        protected static bool IsDirectory(uint attrs)
        {
            return (attrs & (uint)FileAttributes.Directory) != 0;
        }

        protected const uint FileAttributes_NotFound = 0xFFFFFFFF;

        protected static Func<string, bool> GetMatcher(string searchPattern)
        {
            if (searchPattern == "*") return (k) => true;
            if (searchPattern.IndexOf('?') == -1 && searchPattern.IndexOf('*') == -1) return key => key.Equals(searchPattern, StringComparison.OrdinalIgnoreCase);
            var regex = "^" + Regex.Escape(searchPattern).Replace(@"\*", ".*").Replace(@"\?", ".") + "$";
            return key => Regex.IsMatch(key, regex, RegexOptions.IgnoreCase);
        }

        protected virtual void OnFileChanged(string fileName)
        {
        }

        protected virtual void OnFileRead(string fileName)
        {
        }

        public virtual NtStatus SetAllocationSize(string fileName, long length, DokanFileInfo info)
        {
            OnFileChanged(fileName);
            try
            {
                ((Stream)(info.Context)).SetLength(length);
                return Trace(nameof(SetAllocationSize), fileName, info, DokanResult.Success,
                    length.ToString(CultureInfo.InvariantCulture));
            }
            catch (IOException)
            {
                return Trace(nameof(SetAllocationSize), fileName, info, DokanResult.DiskFull,
                    length.ToString(CultureInfo.InvariantCulture));
            }
        }

        public NtStatus FindFiles(string fileName, out IList<FileInformation> files, DokanFileInfo info)
        {
            // This function is not called because FindFilesWithPattern is implemented
            // Return DokanResult.NotImplemented in FindFilesWithPattern to make FindFiles called
            files = FindFilesHelper(fileName, "*");

            return Trace(nameof(FindFiles), fileName, info, DokanResult.Success);
        }


        public NtStatus FindFilesWithPattern(string fileName, string searchPattern, out IList<FileInformation> files,
            DokanFileInfo info)
        {
            files = FindFilesHelper(fileName, searchPattern);

            return Trace(nameof(FindFilesWithPattern), fileName, info, DokanResult.Success);
        }

        protected abstract IList<FileInformation> FindFilesHelper(string fileName, string searchPattern);


        protected virtual NtStatus Trace(string method, string fileName, DokanFileInfo info, NtStatus result,
            params object[] parameters)
        {
#if TRACE
            var extraParameters = parameters != null && parameters.Length > 0
                ? ", " + string.Join(", ", parameters.Select(x => string.Format(DefaultFormatProvider, "{0}", x)))
                : string.Empty;

            logger.Debug(DokanFormat($"{method}('{fileName}', {info}{extraParameters}) -> {result}"));
#endif

            return result;
        }

        protected virtual NtStatus Trace(string method, string fileName, DokanFileInfo info,
            DokanNet.FileAccess access, FileShare share, FileMode mode, FileOptions options, FileAttributes attributes,
            NtStatus result)
        {
#if TRACE
            logger.Debug(
                DokanFormat(
                    $"{method}('{fileName}', {info}, [{access}], [{share}], [{mode}], [{options}], [{attributes}]) -> {result}"));
#endif

            return result;
        }


        public virtual NtStatus GetDiskFreeSpace(out long free, out long total, out long used, DokanFileInfo info)
        {
            free = 0;
            total = 0;
            used = 0;
            return NtStatus.NotImplemented;
        }


        protected virtual bool Directory_Exists(string path)
        {
            return Directory.Exists(path);
        }
        protected virtual bool File_Exists(string path)
        {
            return File.Exists(path);
        }

        protected virtual void File_Move(string src, string dest)
        {
            File.Move(src, dest);
        }
        protected virtual void Directory_Move(string src, string dest)
        {
            Directory.Move(src, dest);
        }

        protected virtual void File_Delete(string path)
        {
            File.Delete(path);
        }
        protected virtual void File_SetAttributes(string path, FileAttributes attr)
        {
            File.SetAttributes(path, attr);
        }

        protected virtual FileAttributes File_GetAttributes(string path)
        {
            return File.GetAttributes(path);
        }

        protected static void Mount(char letter, DokanOptions options, FileSystemBase fs)
        {
            DokanNet.Dokan.Unmount(letter);

            fs.Letter = letter;
            var t = Task.Run(() => DokanNet.Dokan.Mount(fs, letter + ":", options, 1));
            var e = Stopwatch.StartNew();
            while (e.ElapsedMilliseconds < 3000)
            {
                Thread.Sleep(200);
                if (t.Exception != null) throw t.Exception;
                if (Directory.Exists(letter + ":\\")) return;
            }
        }


        public void Mount(string mountpoint, DokanOptions options)
        {
            var t = Task.Run(() => DokanNet.Dokan.Mount(this, mountpoint, options, 1));
            var e = Stopwatch.StartNew();
            while (e.ElapsedMilliseconds < 3000)
            {
                Thread.Sleep(200);
                if (t.Exception != null) throw t.Exception;
                if (Directory.Exists(mountpoint)) return;
            }
        }

        protected char Letter;

        public virtual NtStatus GetFileSecurity(string fileName, out FileSystemSecurity security, AccessControlSections sections, DokanFileInfo info)
        {
            security = null;
            return NtStatus.NotImplemented;
        }

        public virtual NtStatus ReadFile(string fileName, byte[] buffer, out int bytesRead, long offset, DokanFileInfo info)
        {
            var stream = (Stream)info.Context;
            lock (stream)
            {
                stream.Position = offset;
                bytesRead = 0;
                while (bytesRead != buffer.Length)
                {
                    var b = stream.Read(buffer, bytesRead, buffer.Length - bytesRead);
                    if (b == 0) break;
                    bytesRead += b;
                }
                return NtStatus.Success;
            }
        }


        protected static bool IsBadName(string fileName)
        {
            return
                fileName.IndexOf('*') != -1 ||
                fileName.IndexOf('?') != -1 ||
                fileName.IndexOf('>') != -1 ||
                fileName.IndexOf('<') != -1;
        }

        protected const FileAccess ModificationAttributes =
        FileAccess.AccessSystemSecurity |
                FileAccess.AppendData |
                FileAccess.ChangePermissions |
                FileAccess.Delete |
                FileAccess.DeleteChild |
                FileAccess.GenericAll |
                FileAccess.GenericWrite |
                FileAccess.MaximumAllowed |
                FileAccess.SetOwnership |
                FileAccess.WriteAttributes |
                FileAccess.WriteData |
                FileAccess.WriteExtendedAttributes;

        protected static void NormalizeSearchPattern(ref string searchPattern)
        {
            searchPattern = searchPattern.Replace('>', '?');
            searchPattern = searchPattern.Replace('<', '*');
        }

        protected class FsNode<T>
        {
            public object Tag { get; set; }
            public T Info { get; set; }
            private List<FsNode<T>> _children;
            public List<FsNode<T>> Children
            {
                get
                {
                    if (_children != null) return _children;
                    if (GetChildrenDelegate != null)
                    {
                        _children = GetChildrenDelegate();
                        return _children;
                    }
                    return null;
                }
                set
                {
                    _children = value;
                }
            }
            public Func<List<FsNode<T>>> GetChildrenDelegate { get; set; }
            public string Name { get; set; }
            public string FullName { get; set; }

            public override string ToString()
            {
                return Name;
            }
        }

        protected static FsNode<T> GetNode<T>(FsNode<T> root, string path)
        {
            var components = path.SplitFast('\\', StringSplitOptions.RemoveEmptyEntries);

            var current = root;
            foreach (var item in components)
            {
                if (current.Children == null) return null;
                current = current.Children.FirstOrDefault(x => x.Name.Equals(item, StringComparison.OrdinalIgnoreCase));
                if (current == null) return null;
            }
            return current;
        }

        protected static FsNode<T> CreateTree<T>(IEnumerable<T> allfiles, Func<T, string> getPath, Func<T, bool> isDirectory = null, Action<FsNode<T>> postprocess = null)
        {
            var directories = new Dictionary<string, FsNode<T>>(StringComparer.OrdinalIgnoreCase);

            var root = new FsNode<T>();
            root.Name = "(Root)";
            directories[string.Empty] = root;
            foreach (var file in allfiles)
            {
                string name;
                var path = getPath(file);
                var directory = GetDirectory(path, directories, out name);
                if (directory.Children == null) directory.Children = new List<FsNode<T>>();

                FsNode<T> f;

                if (isDirectory != null && isDirectory(file))
                {
                    if (directories.TryGetValue(path, out var ff))
                    {
                        f = ff;
                    }
                    else
                    {
                        f = new FsNode<T>();
                        directories[path] = f;
                        directory.Children.Add(f);
                    }
                }
                else
                {
                    f = new FsNode<T>();
                    directory.Children.Add(f);
                }

                f.Name = name;
                f.Info = file;
                f.FullName = path;
                if (postprocess != null) postprocess(f);

            }
            return root;
        }

        private static FsNode<T> GetDirectory<T>(string path, Dictionary<string, FsNode<T>> dict, out string filename)
        {
            var lastSlash = path.LastIndexOf('\\');
            if (lastSlash == -1) lastSlash = 0;
            var directoryPath = path.Substring(0, lastSlash);
            filename = lastSlash != 0 ? path.Substring(lastSlash + 1) : path;


            if (!dict.TryGetValue(directoryPath, out var directory))
            {
                string currname;
                var parent = GetDirectory(directoryPath, dict, out currname);
                if (parent.Children == null) parent.Children = new List<FsNode<T>>();
                directory = new FsNode<T>();
                directory.Name = currname;
                if (directory.Name.Length == 2 && directory.Name[1] == ':')
                    directory.Name = currname[0].ToString();
                parent.Children.Add(directory);
                dict[directoryPath] = directory;
            }

            return directory;
        }

        public abstract string SimpleMountName { get; }

        public string MountSimple(int threadCount = 1)
        {
            if (Debugger.IsAttached) threadCount = 1;
            var mountname = SimpleMountName.Replace(":", "").Replace("\\", "-").Replace("/", "-");
            var mountpath = Path.Combine(Configuration_DokanFsRoot, mountname);

            Directory.CreateDirectory(mountpath);

            Console.WriteLine("Mounted.");
            Console.WriteLine(mountpath);

            var task = Task.Run(() => this.Mount(mountpath, DokanOptions.NetworkDrive, threadCount));
            var attempts = 0;
            while (attempts < 30)
            {
                Thread.Sleep(100);
                attempts++;
                if (task.IsFaulted) throw task.Exception;
                try
                {
                    if (!JunctionPoint.Exists(mountpath)) continue;
                    Directory.GetFiles(mountpath);
                    return mountpath;
                }
                catch (Exception)
                {
                }
            }
            throw new TimeoutException();
        }



        [Configuration]
        private static string Configuration_DokanFsRoot = @"C:\DokanFs";

    }
}
