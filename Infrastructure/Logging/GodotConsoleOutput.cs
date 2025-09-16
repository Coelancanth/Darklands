using System;
using System.Collections.Generic;
using Darklands.Application.Common;
using Darklands.Application.Infrastructure.Logging;
using Godot;

namespace Darklands;

/// <summary>
/// Godot console output using GD.PrintRich with level/category coloring.
/// Lives in Godot project to avoid Godot references in Core.
/// </summary>
public sealed class GodotConsoleOutput : ILogOutput
{
    private readonly Dictionary<LogLevel, string> _levelColors = new()
    {
        { LogLevel.Debug, "#808080" },
        { LogLevel.Information, "#FFFFFF" },
        { LogLevel.Warning, "#FFD700" },
        { LogLevel.Error, "#FF4444" }
    };

    private readonly Dictionary<string, string> _categoryColors = new()
    {
        { "Combat", "#FF6B6B" },
        { "Movement", "#4ECDC4" },
        { "Vision", "#95E1D3" },
        { "AI", "#F38181" },
        { "System", "#AA96DA" },
        { "Network", "#8785A2" },
        { "Performance", "#FFD93D" },
        { "Developer", "#6BCB77" }
    };

    public void WriteLine(LogLevel level, string category, string message, string formattedMessage)
    {
        var levelColor = _levelColors.GetValueOrDefault(level, "#FFFFFF");
        var categoryColor = _categoryColors.GetValueOrDefault(category, "#CCCCCC");
        
        // Highlight actors in cyan and coordinates in yellow
        var highlightedMessage = HighlightForGodot(message);

        var richText = $"[color={levelColor}]" +
                      $"[{DateTime.Now:HH:mm:ss}] " +
                      $"[{level.ToString()[..3].ToUpper()}][/color] " +
                      $"[color={categoryColor}][{category}][/color] " +
                      $"[color={levelColor}]{highlightedMessage}[/color]";

        GD.PrintRich(richText);
    }
    
    /// <summary>
    /// Highlights actors and coordinates using Godot's rich text format.
    /// </summary>
    private static string HighlightForGodot(string message)
    {
        var result = message;
        
        // Highlight actor IDs in cyan
        result = System.Text.RegularExpressions.Regex.Replace(result, 
            @"(Actor_[a-f0-9]{8})", "[color=#00FFFF]$1[/color]");
            
        // Highlight coordinates in yellow
        result = System.Text.RegularExpressions.Regex.Replace(result, 
            @"(\(\d+,\s*\d+\))", "[color=#FFFF00]$1[/color]");
        
        return result;
    }

    public void Flush() { }
    public void Dispose() { }
    public bool IsEnabled { get; set; } = true;
}


