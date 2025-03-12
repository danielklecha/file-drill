using System;
using System.Collections.Generic;
using System.CommandLine;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileDrill.Commands;
public class MainCommand : RootCommand
{
    public MainCommand() : base("Console line interface for file-drill")
    {
        Name = "file-drill";
        AddCommand(new ConfigCommand());
        AddCommand(new ReadCommand());
        AddCommand(new ClassifyCommand());
    }
}
