using System.Collections.Generic;
using System.Linq;
using Darklands.Application.Common;

namespace Darklands.Application.Infrastructure.Logging;

/// <summary>
/// Composite output that forwards writes to multiple child outputs.
/// </summary>
public sealed class CompositeLogOutput : ILogOutput
{
    private readonly List<ILogOutput> _outputs = new();

    public void AddOutput(ILogOutput output) => _outputs.Add(output);
    public void RemoveOutput(ILogOutput output) => _outputs.Remove(output);

    public void WriteLine(LogLevel level, string category, string message, string formattedMessage)
    {
        foreach (var output in _outputs.Where(o => o.IsEnabled))
        {
            output.WriteLine(level, category, message, formattedMessage);
        }
    }

    public void Flush()
    {
        foreach (var output in _outputs)
        {
            output.Flush();
        }
    }

    public void Dispose()
    {
        foreach (var output in _outputs)
        {
            output.Dispose();
        }
        _outputs.Clear();
    }

    public bool IsEnabled { get; set; } = true;
}


