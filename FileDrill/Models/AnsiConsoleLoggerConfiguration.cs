using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileDrill.Models;
public class AnsiConsoleLoggerConfiguration
{
    public int EventId { get; set; } = 0;
    public IAnsiConsole? Console { get; set; }
}