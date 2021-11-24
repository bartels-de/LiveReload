using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiveReload.Models;

namespace LiveReload
{
    public class LiveReloadFileWatcher
    {
        private static FileSystemWatcher? _fileWatcher;
        private static FileSystemWatcher? _folderWatcher;
        private static string? _folderToMonitorPath;
        private static string? _folderToMonitorName;
        private static bool _isFolderCreated;
        private static ThrottlingTimer? _throttler = new();

        private static List<string>? _extensionList;
        private static readonly object _loadLock = new();

        public static void StartFileWatcher()
        {
            if (!LiveReloadConfiguration.Current.LiveReloadEnabled)
                return;

            var path = LiveReloadConfiguration.Current.FolderToMonitor;

            _folderToMonitorPath = Path.GetFullPath(path);
            _folderToMonitorName = Path.GetFileName(_folderToMonitorPath);

            StartFilesWatcher();
            StartFolderWatcher();
        }

        public void StopFileWatcher()
        {
            DisposeFilesWatcher();
            DisposeFolderWatcher();
            _throttler = null;
        }

        private static void StartFilesWatcher()
        {
            _fileWatcher = new FileSystemWatcher(_folderToMonitorPath);
            _fileWatcher.Filter = "*.*";
            _fileWatcher.EnableRaisingEvents = true;
            _fileWatcher.IncludeSubdirectories = true;

            _fileWatcher.NotifyFilter = NotifyFilters.LastWrite
                                        | NotifyFilters.FileName
                                        | NotifyFilters.DirectoryName;

            _fileWatcher.Changed += FileWatcher_Changed;
            _fileWatcher.Created += FileWatcher_Changed;
            _fileWatcher.Renamed += FileWatcher_Renamed;
        }

        private static void StartFolderWatcher()
        {
            var parentPath = Path.GetDirectoryName(_folderToMonitorPath);
            var folderName = Path.GetFileName(_folderToMonitorPath);
            _folderWatcher = new FileSystemWatcher(parentPath);
            _folderWatcher.Filter = folderName;
            _folderWatcher.EnableRaisingEvents = true;
            _folderWatcher.IncludeSubdirectories = false;


            _folderWatcher.Created += FolderWatcher_Created;
            _folderWatcher.Deleted += FolderWatcher_Deleted;
            _folderWatcher.Renamed += FolderWatcher_Renamed;
        }


        private static void DisposeFilesWatcher()
        {
            if (_fileWatcher != null)
            {
                _fileWatcher.Changed -= FileWatcher_Changed;
                _fileWatcher.Created -= FileWatcher_Changed;
                _fileWatcher.Renamed -= FileWatcher_Renamed;
                _fileWatcher.EnableRaisingEvents = false;
                _fileWatcher?.Dispose();
                _fileWatcher = null;
            }
        }

        private static void DisposeFolderWatcher()
        {
            if (_folderWatcher == null)
            {
                _folderWatcher.Created -= FolderWatcher_Created;
                _folderWatcher.Deleted -= FolderWatcher_Deleted;
                _folderWatcher.Renamed -= FolderWatcher_Renamed;
                _folderWatcher.EnableRaisingEvents = false;
                _folderWatcher?.Dispose();
                _folderWatcher = null;
            }
        }

        private static void FileChanged(string filename)
        {
            // this should really never happen - but just in case
            if (!LiveReloadConfiguration.Current.LiveReloadEnabled)
                return;

            if (string.IsNullOrEmpty(filename) || filename.Contains("\\node_modules\\"))
                return;

            var ext = Path.GetExtension(filename);
            if (string.IsNullOrEmpty(ext))
                return; // we don't care about extensionless files

            var inclusionMode = FileInclusionModes.ContinueProcessing;
            if (LiveReloadConfiguration.Current.FileInclusionFilter is Func<string, FileInclusionModes> filter)
            {
                inclusionMode = filter.Invoke(filename);
                if (inclusionMode == FileInclusionModes.DontRefresh)
                    return;
            }

            if (_extensionList == null)
                lock (_loadLock)
                {
                    if (_extensionList == null)
                        _extensionList = LiveReloadConfiguration.Current.ClientFileExtensions
                            .Split(',', StringSplitOptions.RemoveEmptyEntries)
                            .ToList();
                }

            if (inclusionMode == FileInclusionModes.ForceRefresh ||
                _extensionList.Contains(ext, StringComparer.OrdinalIgnoreCase))
            {
                // Razor Pages don't restart the server, so we need a slight delay
                var delayed = LiveReloadConfiguration.Current.ServerRefreshTimeout > 0 &&
                              (ext == ".cshtml" || ext == ".razor");

                if (_isFolderCreated)
                    _throttler.Debounce(2000, param =>
                    {
                        _ = LiveReloadMiddleware.RefreshWebSocketRequest(delayed);
                        _isFolderCreated = false;
                    });
                else
                    _ = LiveReloadMiddleware.RefreshWebSocketRequest(delayed);
            }
        }

        private static void FileWatcher_Renamed(object sender, RenamedEventArgs e)
        {
            FileChanged(e.FullPath);
        }

        private static void FileWatcher_Changed(object sender, FileSystemEventArgs e)
        {
            FileChanged(e.FullPath);
        }

        private static void FolderWatcher_Created(object sender, FileSystemEventArgs e)
        {
            _isFolderCreated = true;
            StartFilesWatcher();
        }

        private static void FolderWatcher_Deleted(object sender, FileSystemEventArgs e)
        {
            DisposeFilesWatcher();
        }

        private static void FolderWatcher_Renamed(object sender, RenamedEventArgs e)
        {
            if (string.Compare(e.Name, _folderToMonitorName, StringComparison.OrdinalIgnoreCase) == 0)
                StartFileWatcher();
            else if (string.Compare(e.OldName, _folderToMonitorName, StringComparison.OrdinalIgnoreCase) == 0) DisposeFilesWatcher();
        }
    }
}