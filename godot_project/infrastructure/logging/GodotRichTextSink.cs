using System;
using System.IO;
using Godot;
using Serilog.Core;
using Serilog.Events;
using Serilog.Formatting;

namespace Darklands.Presentation.Infrastructure.Logging;

/// <summary>
/// Serilog sink that writes formatted logs to Godot's RichTextLabel with BBCode markup.
/// Thread-safe: Uses CallDeferred to marshal UI updates to main thread.
///
/// STATUS: Future-use (VS_003 Phase 3 implementation, ready for VS_005+ in-game debug console).
/// Currently, DebugConsoleController uses GodotConsoleSink (Godot Output panel only).
/// This sink will be wired up when in-game RichTextLabel log display is implemented.
/// </summary>
public class GodotRichTextSink : ILogEventSink
{
    private readonly RichTextLabel _richTextLabel;
    private readonly ITextFormatter _formatter;

    /// <summary>
    /// Create sink targeting a specific RichTextLabel node.
    /// </summary>
    /// <param name="richTextLabel">Target RichTextLabel (must be in scene tree)</param>
    /// <param name="formatter">Formatter (typically GodotBBCodeFormatter)</param>
    public GodotRichTextSink(RichTextLabel richTextLabel, ITextFormatter formatter)
    {
        _richTextLabel = richTextLabel ?? throw new ArgumentNullException(nameof(richTextLabel));
        _formatter = formatter ?? throw new ArgumentNullException(nameof(formatter));
    }

    /// <summary>
    /// Emit a log event to the RichTextLabel.
    /// THREAD-SAFE: Formatting happens on background thread, UI update via CallDeferred.
    /// </summary>
    public void Emit(LogEvent logEvent)
    {
        try
        {
            // Format on background thread (cheap string manipulation)
            var writer = new StringWriter();
            _formatter.Format(logEvent, writer);
            var formatted = writer.ToString();

            // Marshal to main thread (Godot requirement: UI only on main thread)
            // CallDeferred adds to main thread message queue for next frame
            _richTextLabel.CallDeferred(
                RichTextLabel.MethodName.AppendText,
                formatted
            );
        }
        catch (Exception ex)
        {
            // Sink failures should not crash application
            // Godot's GD.PrintErr is thread-safe fallback
            GD.PrintErr($"GodotRichTextSink.Emit failed: {ex.Message}");
        }
    }
}