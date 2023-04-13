
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
        (_fileInfo.Name, $"^([A-Za-z]+?)\\1*(?=\\d|([^A-Za-z0-9])\\2)").Value;

    public string? Version => Regex.Match
        (_fileInfo.Name, $"(?<={Prefix})(\\d+\\.?)+").Value;

    public string Separator => Regex.Match
        (_fileInfo.Name, $"(?<={Prefix}|{Version})([^A-Za-z0-9])\\1+").Value;

    public string Description => Regex.Match
        (_fileInfo.Name, $"(?<={Separator}).+(?=\\.\\w)").Value;

    public string Suffix => _fileInfo.Extension;

    public string Filename => $"{_fileInfo.Name}";
}