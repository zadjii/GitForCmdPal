// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using GitExtension.Pages;
using LibGit2Sharp;
using Microsoft.CommandPalette.Extensions;
using Microsoft.CommandPalette.Extensions.Toolkit;

namespace GitExtension;

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
        { FileStatus.ModifiedInWorkdir, new Microsoft.CommandPalette.Extensions.Toolkit.Tag("modified") { Foreground = StateColors[FileStatus.ModifiedInWorkdir], Icon = Icons.ModifiedInWorkdir_File_Icon }},
        { FileStatus.DeletedFromWorkdir, new Microsoft.CommandPalette.Extensions.Toolkit.Tag("deleted") { Foreground = StateColors[FileStatus.DeletedFromWorkdir], Icon = Icons.DeletedFromWorkdir_File_Icon }},
        { FileStatus.TypeChangeInWorkdir, new Microsoft.CommandPalette.Extensions.Toolkit.Tag("renamed") { Foreground = StateColors[FileStatus.TypeChangeInWorkdir], Icon = Icons.TypeChangeInWorkdir_File_Icon }},
        { FileStatus.RenamedInWorkdir, new Microsoft.CommandPalette.Extensions.Toolkit.Tag("changed") { Foreground = StateColors[FileStatus.RenamedInWorkdir], Icon = Icons.RenamedInWorkdir_File_Icon }},
    
        // conflicts
        { FileStatus.Conflicted, new Microsoft.CommandPalette.Extensions.Toolkit.Tag("conflics") { Foreground = StateColors[FileStatus.Conflicted], Icon = Icons.Conflicted_File_Icon }},
 };
    public static readonly Microsoft.CommandPalette.Extensions.Toolkit.Tag UhOhTag = new("uh oh edge case");
}
