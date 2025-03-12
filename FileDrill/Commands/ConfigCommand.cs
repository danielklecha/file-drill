using System.CommandLine;

namespace FileDrill.Commands;
internal class ConfigCommand : Command
{
    public ConfigCommand() : base("config", "Configuration operations")
    {
        AddCommand(new ConfigSetCommand());
        AddCommand(new ConfigSeedCommand());
        AddCommand(new ConfigShowCommand());
        AddCommand(new ConfigExportCommand());
        AddCommand(new ConfigMergeCommand());
        AddCommand(new ConfigClearCommand());
    }
}
