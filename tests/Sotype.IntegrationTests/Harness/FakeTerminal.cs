using System.Text.RegularExpressions;
using Spectre.Console;
using Spectre.Console.Rendering;

namespace Sotype.IntegrationTests.Harness;

/// <summary>
/// An interactive, ANSI-capable console of a fixed size that reads from a
/// <see cref="ScriptedInput"/> and records everything written to it.
/// </summary>
public sealed partial class FakeTerminal : IAnsiConsole
{
    private readonly StringWriter _output = new();
    private readonly IAnsiConsole _inner;

    public FakeTerminal(ScriptedInput input, int width = 100, int height = 30)
    {
        Input = input;

        _inner = AnsiConsole.Create(new AnsiConsoleSettings
        {
            Ansi = AnsiSupport.Yes,
            ColorSystem = ColorSystemSupport.TrueColor,
            Interactive = InteractionSupport.Yes,
            Out = new AnsiConsoleOutput(_output),
            Enrichment = new ProfileEnrichment { UseDefaultEnrichers = false }
        });

        _inner.Profile.Width = width;
        _inner.Profile.Height = height;
    }

    /// <summary>Everything written so far, escape sequences included.</summary>
    public string RawOutput => _output.ToString();

    /// <summary>Everything written so far, with escape sequences stripped.</summary>
    public string Output => AnsiEscape().Replace(RawOutput, string.Empty);

    public Profile Profile => _inner.Profile;

    public IAnsiConsoleCursor Cursor => _inner.Cursor;

    public IAnsiConsoleInput Input { get; }

    public IExclusivityMode ExclusivityMode => _inner.ExclusivityMode;

    public RenderPipeline Pipeline => _inner.Pipeline;

    public void Clear(bool home) => _inner.Clear(home);

    public void Write(IRenderable renderable) => _inner.Write(renderable);

    public void WriteAnsi(Action<AnsiWriter> action) => _inner.WriteAnsi(action);

    [GeneratedRegex(@"\x1b\[[0-9;?]*[ -/]*[@-~]")]
    private static partial Regex AnsiEscape();
}
