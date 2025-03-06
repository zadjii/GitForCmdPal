// Copyright (c) Microsoft Corporation
// The Microsoft Corporation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.Text;
using System.Text.RegularExpressions;
using LibGit2Sharp;

namespace GitExtension;

internal sealed partial class GitDiffDetails : VirtualDetails
{
    private readonly PatchEntryChanges _fileDiff;
    private readonly Lazy<string> _bodyText;

    internal GitDiffDetails(PatchEntryChanges fileDiff)
    {
        _fileDiff = fileDiff;
        _bodyText = new(GenerateBody);
    }

    private string GenerateBody()
    {
        // Retrieve the full diff text.
        var patchText = _fileDiff.Patch;

        // Split the diff text into chunks. 
        // The regex splits on lines that start with @@, preserving those lines with a positive lookahead.
        var hunks = Regex.Split(patchText, @"(?=^@@)", RegexOptions.Multiline);

        var markdownDiff = new StringBuilder();

        // For each chunk, wrap it in its own Markdown code block.
        foreach (var hunk in hunks)
        {
            // Trim to avoid empty code blocks.
            if (string.IsNullOrWhiteSpace(hunk))
            {
                continue;
            }

            markdownDiff.AppendLine("```diff");
            markdownDiff.AppendLine(hunk.TrimEnd());
            markdownDiff.AppendLine("```");
            markdownDiff.AppendLine();
        }
        return markdownDiff.ToString();

    }

    public override string Body
    {
        get => _bodyText.Value;
        set => base.Body = value;
    }

}
