using Sotype.Cli.App;
using Sotype.Cli.Terminal;
using Sotype.Infrastructure.Configuration;
using Sotype.Infrastructure.History;
using Sotype.Infrastructure.Words;

Exception? failure = null;

try
{
    // Scoped inside this try, not at the top level: if RunAsync throws, the `using`
    // declaration's implicit finally restores the terminal *before* unwinding reaches the
    // catch below, so the error prints to a normal shell rather than the alternate screen.
    using var terminalSession = new TerminalSession();

    var app = new SotypeApp(
        new EmbeddedWordListProvider(),
        new JsonHistoryRepository(),
        new JsonPreferencesRepository());

    await app.RunAsync();
}
catch (Exception ex)
{
    failure = ex;
}

if (failure is not null)
{
    Console.Error.WriteLine(failure);
    return 1;
}

return 0;
