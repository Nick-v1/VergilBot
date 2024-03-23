using Microsoft.Extensions.DependencyInjection;

namespace Vergil.Main.Commands;

public class FactoryInteractions : IFactoryInteractions
{
    private readonly IServiceProvider _serviceProvider;

    public FactoryInteractions(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }
    
    public SlashCommands GetNewSlashCommandsInstance()
    {
        return _serviceProvider.GetRequiredService<SlashCommands>();
    }
}

public interface IFactoryInteractions
{
    SlashCommands GetNewSlashCommandsInstance();
}