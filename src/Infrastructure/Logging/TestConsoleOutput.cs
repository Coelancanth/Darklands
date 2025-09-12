using System;
using System.Text;
using Darklands.Core.Domain.Debug;

namespace Darklands.Core.Infrastructure.Logging;

/// <summary>
/// Simple output for test environment that writes to Console and buffers content.
/// </summary>
public sealed class TestConsoleOutput : ILogOutput
{
    private readonly StringBuilder _buffer = new();

    public void WriteLine(LogLevel level, string category, string message, string formattedMessage)
    {
        Console.WriteLine(formattedMessage);
        _buffer.AppendLine(formattedMessage);
    }

    public string GetBuffer() => _buffer.ToString();
    public void ClearBuffer() => _buffer.Clear();

    public void Flush() { }
    public void Dispose() => _buffer.Clear();
    public bool IsEnabled { get; set; } = true;
}


