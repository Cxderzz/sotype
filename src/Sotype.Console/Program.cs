using Sotype.Cli.App;
using Sotype.Cli.Terminal;
using Sotype.Infrastructure.Configuration;
using Sotype.Infrastructure.History;
using Sotype.Infrastructure.Words;

namespace Sotype.Cli;

public static class Program
{
    public static async Task Main()
    {
        try
        {
            using var terminalSession = new TerminalSession();

            var app = new SotypeApp(
                new EmbeddedWordListProvider(),
                new JsonHistoryRepository(),
                new JsonPreferencesRepository());

            await app.RunAsync();
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex);
        }
    }
}