
using System.Diagnostics.CodeAnalysis;
using System.IO.Abstractions;
using System.Text.RegularExpressions;

namespace SierraSam.Core;

[SuppressMessage("GeneratedRegex", "SYSLIB1045:Convert to \'GeneratedRegexAttribute\'.")]
public sealed class Migration
{
    private readonly IFileInfo _fileInfo;

    public Migration(IFileInfo fileInfo)
    {
        _fileInfo = fileInfo
            ?? throw new ArgumentNullException(nameof(fileInfo));
    }

    public string Prefix => Regex.Match
        (_fileInfo.Name, "^([A-z]+)(?=\\d)").Value;

    public string Version => Regex.Match
        (_fileInfo.Name, "(\\d+\\.?)+").Value;

    public string Separator => Regex.Match
        (_fileInfo.Name, "(?<=\\d)[^\\d\\.]{2,}?").Value;

    public string Description => Regex.Match
        (_fileInfo.Name, "(?<=(?<=\\d)[^\\d\\.]{2,}?).+(?=\\.\\w)").Value;

    public string Suffix => _fileInfo.Extension;

    public string Filename => $"{_fileInfo.Name}{_fileInfo.Extension}";
}