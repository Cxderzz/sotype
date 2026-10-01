using Sotype.Cli.App;
using Sotype.Cli.Screens;
using Sotype.Cli.Terminal;
using Sotype.Infrastructure.Configuration;
using Sotype.Infrastructure.History;
using Sotype.Infrastructure.Theme;
using Sotype.Infrastructure.Words;
using Spectre.Console;

namespace Sotype.Cli;

public static class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            using var terminalSession = new TerminalSession();

            var app = new SotypeApp(
                AnsiConsole.Console,
                TimeProvider.System,
                new EmbeddedWordListProvider(),
                new JsonHistoryRepository(),
                new JsonPreferencesRepository(),
                new JsonThemeRepository());

            await app.RunAsync();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
        }
    }
}
