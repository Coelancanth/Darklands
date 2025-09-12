using System;
using System.IO;
using System.Text;
using Darklands.Core.Domain.Debug;

namespace Darklands.Core.Infrastructure.Logging;

/// <summary>
/// Simple session-based file output. Creates a timestamped file and updates a
/// darklands-current.log alongside it for easy access.
/// </summary>
public sealed class FileLogOutput : ILogOutput
{
    private readonly string _logDirectory;
    private readonly object _lock = new();
    private StreamWriter? _writer;

    public FileLogOutput(string logDirectory = "logs")
    {
        _logDirectory = logDirectory;
        InitializeLogFile();
    }

    private void InitializeLogFile()
    {
        Directory.CreateDirectory(_logDirectory);

        var fileName = $"darklands-session-{DateTime.Now:yyyyMMdd-HHmmss}.log";
        var filePath = Path.Combine(_logDirectory, fileName);
        var currentPath = Path.Combine(_logDirectory, "darklands-current.log");

        _writer = new StreamWriter(filePath, append: false, encoding: Encoding.UTF8)
        {
            AutoFlush = true
        };

        _writer.WriteLine("═══════════════════════════════════════════════════════");
        _writer.WriteLine(" DARKLANDS SESSION LOG");
        _writer.WriteLine($" Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
        _writer.WriteLine("═══════════════════════════════════════════════════════");
        _writer.WriteLine();

        UpdateCurrentLogLink(filePath, currentPath);
    }

    public void WriteLine(LogLevel level, string category, string message, string formattedMessage)
    {
        lock (_lock)
        {
            _writer?.WriteLine(formattedMessage);
        }
    }

    public void Flush()
    {
        lock (_lock)
        {
            _writer?.Flush();
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_writer != null)
            {
                _writer.WriteLine();
                _writer.WriteLine("═══════════════════════════════════════════════════════");
                _writer.WriteLine($" SESSION ENDED: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
                _writer.WriteLine("═══════════════════════════════════════════════════════");
                _writer.Dispose();
                _writer = null;
            }
        }
    }

    public bool IsEnabled { get; set; } = true;

    private void UpdateCurrentLogLink(string source, string target)
    {
        try
        {
            if (File.Exists(target))
                File.Delete(target);
            File.Copy(source, target);
        }
        catch
        {
            // Non-critical
        }
    }
}


