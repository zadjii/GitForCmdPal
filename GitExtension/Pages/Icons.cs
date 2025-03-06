// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.CommandPalette.Extensions.Toolkit;

namespace GitExtension.Pages;

internal sealed class Icons
{
    public static readonly IconInfo AppIcon = IconHelpers.FromRelativePath("Assets\\StoreLogo.scale-200.png");
    public static readonly IconInfo VsCodeIcon = IconHelpers.FromRelativePath("Assets\\visual-studio-code-icons\\vscode.svg");
    public static readonly IconInfo ExplorerIcon = new("c:\\Windows\\explorer.exe");
    public static readonly IconInfo AddRepoIcon = IconHelpers.FromRelativePath("Assets\\svg\\OpenLocalGitRepo.svg");

    public static readonly IconInfo TerminalIcon = IconHelpers.FromRelativePath("Assets\\svg\\Terminal.svg");
    public static readonly IconInfo VisualStudioIcon = IconHelpers.FromRelativePath("Assets\\svg\\Visual_Studio_Icon_2019.svg");

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


    public static readonly IconInfo Add = new("\uE710"); // Add
    public static readonly IconInfo AddAll = new("\uED0E"); // SubscriptionAdd
    public static readonly IconInfo AddNewRepo = new("\uED0E"); // SubscriptionAdd
    public static readonly IconInfo StagedFile = new("\uE73A"); // CheckboxComposite

    // IconHelpers.FromRelativePath("Assets\\svg\\Commit.svg");
    public static readonly IconInfo Commit = new("\uE78C"); // SaveLocal
    public static readonly IconInfo CreateBranch = IconHelpers.FromRelativePath("Assets\\svg\\NewBranch.svg");

    public static readonly IconInfo Push = new("\uE898"); // Upload
    public static readonly IconInfo Pull = new("\uEBD3"); // CloudDownload

    public static readonly IconInfo Checkout = new("\uE8AB"); // Switch
    public static readonly IconInfo CurrentBranch = new("\uE73D"); // CheckboxCompositeReversed

    public static readonly IconInfo Completed = new("\uE930"); // Completed

}
