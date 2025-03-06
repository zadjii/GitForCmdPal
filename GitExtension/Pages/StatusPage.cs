// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GitExtension.Pages;
using LibGit2Sharp;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;
using Windows.Foundation;
using Windows.Storage.Streams;

namespace GitExtension;

internal sealed partial class StatusPage : ListPage, System.IDisposable
{
    private readonly RepoData _repoData;
    private readonly FileSystemWatcher _watcher;
    private readonly Repository _repo;

    private Timer? _debounceTimer;
    private const int DebounceDelay = 250; // milliseconds
    private readonly Lock _timerLock = new();

    internal event TypedEventHandler<StatusPage, object>? StatusChanged;

    public StatusPage(RepoData repoData, Repository repo)
    {
        _repoData = repoData;
        _repo = repo;

        Icon = Icons.AppIcon;
        Name = "Open";
        ShowDetails = true;

        var repoPath = _repoData.Path;

        _repo = new(repoPath);

        Title = $"{repoData.Name} {GitExtensionPage.BranchString(_repo)}";

        _watcher = new FileSystemWatcher
        {
            Path = repoPath,
            Filter = "*.*",
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
        };

        _watcher.Changed += OnFilesystemChanged;
        _watcher.Created += OnFilesystemChanged;
        _watcher.Deleted += OnFilesystemChanged;
        _watcher.Renamed += OnFilesystemChanged;

        _watcher.EnableRaisingEvents = true;

        var emptyCommands = GitExtensionCommandsProvider.ContextMenuForRepo(_repoData);

        EmptyContent = new CommandItem()
        {
            Title = "No changes",
            Subtitle = "nothing to commit, working tree clean",
            Icon = Icons.Completed,
            Command = emptyCommands.First().Command,
            MoreCommands = emptyCommands.Skip(1).ToArray()
        };
    }

    public override IListItem[] GetItems()
    {
        IsLoading = true;
        var status = _repo.RetrieveStatus();

        var modifiedItems = status
            .Where(entry => entry.State is not FileStatus.Unaltered and not FileStatus.Ignored);
        if (!modifiedItems.Any())
        {
            IsLoading = false;
            return [];
        }
        // Get the HEAD commit's tree to compare against the working directory.
        var headTree = _repo.Head.Tip.Tree;

        // Generate a patch comparing the HEAD commit to the working directory for the specific file.
        Patch? patch;
        try
        {
            patch = _repo.Diff.Compare<Patch>(
            headTree,
            DiffTargets.WorkingDirectory
        );
        }
        catch (LibGit2SharpException diffException)
        {
            ExtensionHost.LogMessage(diffException.Message);
            return [];
        }

        var items = modifiedItems
            .OrderBy(entry => entry.State)
            .Select(file => FileTolistItem(file, patch))
            .Where(item => item != null)
            .Select(i => i!);

        List<ListItem> allCommands = [];

        var unstagedChanges = status.Where(IsUnstagedChange);
        var stagedChanges = status.Where(IsStagedChange);

        if (unstagedChanges.Any())
        {
            allCommands.Add(new(new AddAllCommand(_repo)));
        }
        if (stagedChanges.Any())
        {
            //allCommands.Add(new(new CommitPage(_repo)) { Subtitle = $"{StagedSubtitle(status)} {BranchString(_repo)}" });
            allCommands.Add(new(new UnstageAllCommand(_repo)));
        }

        allCommands.AddRange(items);

        IsLoading = false;
        return allCommands.ToArray();
    }

    internal static string StagedSubtitle(RepositoryStatus status)
    {

        return string.Format(CultureInfo.InvariantCulture,
                                "Added {0}, Modified {1}, Removed {2} ",
                                status.Added.Count(),
                                status.Staged.Count(),
                                status.Removed.Count());

    }

    private static bool IsUnstagedChange(StatusEntry file)
    {
        var state = file.State;
        return
            state.HasFlag(FileStatus.NewInWorkdir) ||
            state.HasFlag(FileStatus.ModifiedInWorkdir) ||
            state.HasFlag(FileStatus.DeletedFromWorkdir) ||
            state.HasFlag(FileStatus.TypeChangeInWorkdir) ||
            state.HasFlag(FileStatus.RenamedInWorkdir);
    }
    private static bool IsStagedChange(StatusEntry file)
    {
        var state = file.State;
        return
            state.HasFlag(FileStatus.NewInIndex) ||
            state.HasFlag(FileStatus.ModifiedInIndex) ||
            state.HasFlag(FileStatus.DeletedFromIndex) ||
            state.HasFlag(FileStatus.RenamedInIndex) ||
            state.HasFlag(FileStatus.TypeChangeInIndex);
    }

    private ListItem? FileTolistItem(StatusEntry file, Patch patch)
    {
        List<Microsoft.CommandPalette.Extensions.Toolkit.Tag> tags = [];
        List<Microsoft.CommandPalette.Extensions.Toolkit.Command> commands = [];

        foreach (var state in GetFlags(file.State))
        {
            try
            {

                var tag = Statics.StateTags.TryGetValue(state, out var value) ? value : Statics.UhOhTag;
                tags.Add(tag);

                Command? command = state switch
                {
                    FileStatus.NewInIndex => new UnstageCommand(_repo, file),
                    FileStatus.ModifiedInIndex => new UnstageCommand(_repo, file),
                    FileStatus.DeletedFromIndex => new UnstageCommand(_repo, file),
                    FileStatus.RenamedInIndex => new UnstageCommand(_repo, file),
                    FileStatus.TypeChangeInIndex => new UnstageCommand(_repo, file),

                    FileStatus.NewInWorkdir => new StageCommand(_repo, file),
                    FileStatus.ModifiedInWorkdir => new StageCommand(_repo, file),
                    FileStatus.DeletedFromWorkdir => new StageCommand(_repo, file),
                    FileStatus.TypeChangeInWorkdir => new StageCommand(_repo, file),
                    FileStatus.RenamedInWorkdir => new StageCommand(_repo, file),

                    _ => null,
                };

                if (command != null)
                {
                    commands.Add(command);
                }
            }
            catch { }

        }

        try
        {

            var state = file.State;

            var title = file.FilePath;

            var subtitle = state switch
            {
                FileStatus.NewInIndex => "Staged",
                FileStatus.ModifiedInIndex => "Staged",
                FileStatus.DeletedFromIndex => "Staged",
                FileStatus.RenamedInIndex => "Staged",
                FileStatus.TypeChangeInIndex => "Staged",

                _ => string.Empty,
            };

            if (file.IndexToWorkDirRenameDetails is RenameDetails rename)
            {
                subtitle += $": {rename.OldFilePath} -> {rename.NewFilePath}";
            }
            else if (file.HeadToIndexRenameDetails is RenameDetails rename2)
            {
                subtitle += $": {rename2.OldFilePath} -> {rename2.NewFilePath}";
            }

            var fullPath = Path.Combine(_repo.Info.WorkingDirectory, file.FilePath);
            fullPath = Path.GetFullPath(fullPath);

            var defaultCommand = commands.FirstOrDefault(new NoOpCommand());
            var remaining = commands.Skip(1);

            IDetails? details = null;
            // Retrieve the diff for the specified file.            
            if (patch[file.FilePath] is PatchEntryChanges fileDiff)
            {
                details = new GitDiffDetails(fileDiff);
            }
            var li = new ListItem(defaultCommand)
            {
                Title = title,
                Subtitle = subtitle,
                Tags = tags.ToArray(),
                Details = details,
                // Icon = GetFileIcon(file),
                MoreCommands = [
                    ..remaining.Select(c => new CommandContextItem(c)),
                    new CommandContextItem(new OpenUrlCommand(fullPath)) { Title = "Open file" },
                    new CommandContextItem(new CopyTextCommand(fullPath)) { Title = "Copy full path" },
                    new CommandContextItem(new CopyTextCommand(file.FilePath)) { Title = "Copy relative path" },
                    new CommandContextItem(new ShowFileInFolderCommand(fullPath)) { Title = "Open in explorer" },
                ],
            };

            _ = Task.Run(() => { li.Icon = GetFileIcon(file); });
            return li;
        }
        catch (Exception e)
        {
            ExtensionHost.LogMessage(e.ToString());
        }
        return null;
    }

    public void Dispose()
    {
        _watcher.Dispose();
        _debounceTimer?.Dispose();
        _debounceTimer = null;
    }

    private static IEnumerable<FileStatus> GetFlags(FileStatus input)
    {
        foreach (var value in Enum.GetValues<FileStatus>())
        {
            if (value == FileStatus.Unaltered)
            {
                continue;
            }

            if (input.HasFlag(value))
            {
                yield return value;
            }
        }
    }


    private void OnFilesystemChanged(object sender, FileSystemEventArgs e)
    {
        try
        {
            // Convert the absolute path to a relative path based on the working directory.
            var relativePath = Path.GetRelativePath(_repo.Info.WorkingDirectory, e.FullPath);

            // Check if the file is ignored.
            if (_repo.Ignore.IsPathIgnored(relativePath))
            {
                // Skip processing if the file is ignored.
                return;
            }
        }
        catch { }

        // Reset the debounce timer each time an event is fired
        lock (_timerLock)
        {
            if (_debounceTimer != null)
            {
                _debounceTimer.Change(DebounceDelay, Timeout.Infinite);
            }
            else
            {
                _debounceTimer = new Timer(OnDebounceTimerElapsed, null, DebounceDelay, Timeout.Infinite);
            }
        }
    }

    private void OnDebounceTimerElapsed(object? state)
    {
        // Safely dispose of the timer and set to null
        lock (_timerLock)
        {
            _debounceTimer?.Dispose();
            _debounceTimer = null;
        }

        // Now, handle the debounced event (250ms after the last event)
        // Place additional event handling logic here.
        OnRepoChanged();
    }

    private void OnRepoChanged()
    {
        Title = $"{_repoData.Name} {GitExtensionPage.BranchString(_repo)}";
        RaiseItemsChanged(-1);
        StatusChanged?.Invoke(this, EventArgs.Empty);
    }

    private IconInfo? GetFileIcon(StatusEntry file)
    {

        if (IsStagedChange(file))
        {
            return Icons.StagedFile;
        }

        var path = Path.Combine(_repo.Info.WorkingDirectory, file.FilePath);
        path = Path.GetFullPath(path);
        var t = ThumbnailHelper.GetThumbnail(path);
        t.ConfigureAwait(false);
        var stream = t.Result;
        if (stream != null)
        {
            var data = new IconData(RandomAccessStreamReference.CreateFromStream(stream));
            var icon = new IconInfo(data, data);
            return icon;
        }
        return null;
    }
}