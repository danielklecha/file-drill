using System.CommandLine;

namespace FileDrill.Extensions;
public static class CommandExtensions
{
    public static IEnumerable<Command> GetDescendantCommands(this Command command)
    {
        foreach (Command child in command.Subcommands)
        {
            yield return child;
            foreach (Command grandchild in GetDescendantCommands(child))
                yield return grandchild;
        }
    }
}