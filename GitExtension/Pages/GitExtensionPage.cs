// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LibGit2Sharp;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace GitExtension;

internal sealed partial class GitExtensionPage : ListPage, System.IDisposable
{
    private readonly Repository _repo;
    private readonly FileSystemWatcher _watcher;

    public GitExtensionPage()
    {
        Icon = IconHelpers.FromRelativePath("Assets\\StoreLogo.png");
        Title = "Git for CmdPal";
        Name = "Open";

        var repoPath = "d:\\dev\\public\\powertoys";

        _repo = new(repoPath);

        _watcher = new FileSystemWatcher
        {
            Path = repoPath,
            Filter = "*.*",
            IncludeSubdirectories = true,
            NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.DirectoryName
        };

        _watcher.Changed += OnRepoChanged;
        _watcher.Created += OnRepoChanged;
        _watcher.Deleted += OnRepoChanged;
        _watcher.Renamed += OnRepoChanged;

        _watcher.EnableRaisingEvents = true;
    }

    public override IListItem[] GetItems()
    {
        var status = _repo.RetrieveStatus();

        var items = status
            .Where(entry => entry.State is not FileStatus.Unaltered and not FileStatus.Ignored)
            .Select(FileTolistItem)
            .Where(item => item != null)
            .Select(i => i!);


        return items.ToArray();
    }

    private ListItem? FileTolistItem(StatusEntry file)
    {
        try
        {

            var state = file.State;

            var tag = Statics.StateTags.TryGetValue(state, out var value) ? value : Statics.UhOhTag;
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

            // var addedLinesTag = file. new Microsoft.CommandPalette.Extensions.Toolkit.Tag("untracked")

            if (file.IndexToWorkDirRenameDetails is RenameDetails rename)
            {
                subtitle += $": {rename.OldFilePath} -> {rename.NewFilePath}";
            }

            Command command = state switch
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

                _ => new NoOpCommand(),
            };

            return new ListItem(command)
            {
                Title = title,
                Subtitle = subtitle,
                Tags = [tag],
            };
        }
        catch (Exception e)
        {
            ExtensionHost.LogMessage((ILogMessage)e);
        }
        return null;
    }

    public void Dispose() => throw new System.NotImplementedException();


    private void OnRepoChanged(object sender, FileSystemEventArgs e) =>
        RaiseItemsChanged(-1);
}

internal sealed partial class StageCommand : InvokableCommand
{
    private readonly Repository _repo;
    private readonly StatusEntry _file;
    internal StageCommand(Repository repo, StatusEntry file)
    {
        Name = "Stage";
        _repo = repo;
        _file = file;
    }
    public override ICommandResult Invoke()
    {
        Commands.Stage(_repo, _file.FilePath);

        return CommandResult.KeepOpen();
    }
}
internal sealed partial class UnstageCommand : InvokableCommand
{
    private readonly Repository _repo;
    private readonly StatusEntry _file;
    internal UnstageCommand(Repository repo, StatusEntry file)
    {
        Name = "Unstage";
        _repo = repo;
        _file = file;
    }
    public override ICommandResult Invoke()
    {
        Commands.Unstage(_repo, _file.FilePath);

        return CommandResult.KeepOpen();
    }
}


internal sealed class Icons
{

    public static readonly IconInfo Nonexistent_File_Icon = new("");
    public static readonly IconInfo Unaltered_File_Icon = new("");
    public static readonly IconInfo NewInIndex_File_Icon = new("");
    public static readonly IconInfo ModifiedInIndex_File_Icon = new("");
    public static readonly IconInfo DeletedFromIndex_File_Icon = new("");
    public static readonly IconInfo RenamedInIndex_File_Icon = new("");
    public static readonly IconInfo TypeChangeInIndex_File_Icon = new("");
    public static readonly IconInfo NewInWorkdir_File_Icon = new("");
    public static readonly IconInfo ModifiedInWorkdir_File_Icon = new("");
    public static readonly IconInfo DeletedFromWorkdir_File_Icon = new("");
    public static readonly IconInfo TypeChangeInWorkdir_File_Icon = new("");
    public static readonly IconInfo RenamedInWorkdir_File_Icon = new("");
    public static readonly IconInfo Conflicted_File_Icon = new("");

}


internal sealed class Statics
{

    public static readonly Dictionary<FileStatus, OptionalColor> StateColors = new()
{
    { FileStatus.Nonexistent, ColorHelpers.FromRgb(128, 128, 128) }, // Gray: not present #808080
    { FileStatus.Unaltered,  ColorHelpers.FromRgb(255, 255, 255) }, // White: no changes #FFFFFF

    { FileStatus.NewInIndex,  ColorHelpers.FromRgb(0, 255, 0) },     // Bright green: newly staged #00FF00
    { FileStatus.ModifiedInIndex, ColorHelpers.FromRgb(0, 0, 255) },   // Blue: changes staged for commit #0000FF
    { FileStatus.DeletedFromIndex, ColorHelpers.FromRgb(255, 0, 0) },  // Red: deletions staged #FF0000
    { FileStatus.RenamedInIndex, ColorHelpers.FromRgb(255, 165, 0) },  // Orange: renamed files staged #FFA500
    { FileStatus.TypeChangeInIndex, ColorHelpers.FromRgb(128, 0, 128) }, // Purple: type changes in the index #800080

    { FileStatus.NewInWorkdir, ColorHelpers.FromRgb(144, 238, 144) },  // Light green: new files in workdir #90EE90
    { FileStatus.ModifiedInWorkdir, ColorHelpers.FromRgb(255, 255, 0) }, // Yellow: modifications in workdir #FFFF00
    { FileStatus.DeletedFromWorkdir, ColorHelpers.FromRgb(139, 0, 0) },  // Dark red: deletions in workdir #8B0000
    { FileStatus.TypeChangeInWorkdir, ColorHelpers.FromRgb(255, 0, 255) }, // Magenta: type changes in workdir #FF00FF
    { FileStatus.RenamedInWorkdir, ColorHelpers.FromRgb(255, 140, 0) },   // Dark orange: renamed files in workdir #FF8C00

    { FileStatus.Conflicted, ColorHelpers.FromRgb(255, 20, 147) }        // Deep pink: conflicts need attention #FF1493
};
    public static readonly Dictionary<FileStatus, Microsoft.CommandPalette.Extensions.Toolkit.Tag> StateTags = new() {
        // unexpected:
        { FileStatus.Nonexistent, new Microsoft.CommandPalette.Extensions.Toolkit.Tag("nen-existent") { Foreground = StateColors[FileStatus.Nonexistent], Icon = Icons.Nonexistent_File_Icon }},
        
        // nothing
        { FileStatus.Unaltered, new Microsoft.CommandPalette.Extensions.Toolkit.Tag("unchanged") { Foreground = StateColors[FileStatus.Unaltered], Icon = Icons.Unaltered_File_Icon }},

        // staged
        { FileStatus.NewInIndex, new Microsoft.CommandPalette.Extensions.Toolkit.Tag("added") { Foreground = StateColors[FileStatus.NewInIndex], Icon = Icons.NewInIndex_File_Icon }},
        { FileStatus.ModifiedInIndex, new Microsoft.CommandPalette.Extensions.Toolkit.Tag("modified") { Foreground = StateColors[FileStatus.ModifiedInIndex], Icon = Icons.ModifiedInIndex_File_Icon }},
        { FileStatus.DeletedFromIndex, new Microsoft.CommandPalette.Extensions.Toolkit.Tag("deleted") { Foreground = StateColors[FileStatus.DeletedFromIndex], Icon = Icons.DeletedFromIndex_File_Icon }},
        { FileStatus.RenamedInIndex, new Microsoft.CommandPalette.Extensions.Toolkit.Tag("renamed") { Foreground = StateColors[FileStatus.RenamedInIndex], Icon = Icons.RenamedInIndex_File_Icon }},
        { FileStatus.TypeChangeInIndex, new Microsoft.CommandPalette.Extensions.Toolkit.Tag("changed") { Foreground = StateColors[FileStatus.TypeChangeInIndex], Icon = Icons.TypeChangeInIndex_File_Icon }},
    
        // unstaged
        { FileStatus.NewInWorkdir, new Microsoft.CommandPalette.Extensions.Toolkit.Tag("untracked") { Foreground = StateColors[FileStatus.NewInWorkdir], Icon = Icons.NewInWorkdir_File_Icon }},
        // { FileStatus.DeletedFromIndex | FileStatus.NewInWorkdir, new Microsoft.CommandPalette.Extensions.Toolkit.Tag("modified") { Foreground = StateColors[FileStatus.ModifiedInWorkdir], Icon = Icons.ModifiedInWorkdir_File_Icon }},
        { FileStatus.ModifiedInWorkdir, new Microsoft.CommandPalette.Extensions.Toolkit.Tag("modified") { Foreground = StateColors[FileStatus.ModifiedInWorkdir], Icon = Icons.ModifiedInWorkdir_File_Icon }},
        { FileStatus.DeletedFromWorkdir, new Microsoft.CommandPalette.Extensions.Toolkit.Tag("deleted") { Foreground = StateColors[FileStatus.DeletedFromWorkdir], Icon = Icons.DeletedFromWorkdir_File_Icon }},
        { FileStatus.TypeChangeInWorkdir, new Microsoft.CommandPalette.Extensions.Toolkit.Tag("renamed") { Foreground = StateColors[FileStatus.TypeChangeInWorkdir], Icon = Icons.TypeChangeInWorkdir_File_Icon }},
        { FileStatus.RenamedInWorkdir, new Microsoft.CommandPalette.Extensions.Toolkit.Tag("changed") { Foreground = StateColors[FileStatus.RenamedInWorkdir], Icon = Icons.RenamedInWorkdir_File_Icon }},
    
        // conflicts
        { FileStatus.Conflicted, new Microsoft.CommandPalette.Extensions.Toolkit.Tag("conflics") { Foreground = StateColors[FileStatus.Conflicted], Icon = Icons.Conflicted_File_Icon }},
 };
    public static readonly Microsoft.CommandPalette.Extensions.Toolkit.Tag UhOhTag = new("uh oh edge case");
}
