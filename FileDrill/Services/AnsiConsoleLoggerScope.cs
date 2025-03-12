using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileDrill.Services;
public class AnsiConsoleLoggerScope(Action onDispose) : IDisposable
{
    public void Dispose() => onDispose?.Invoke();
}