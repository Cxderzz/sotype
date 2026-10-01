using Spectre.Console;

namespace Sotype.Cli.Rendering.Charts;

/// <summary>
/// One named line on a <see cref="LineChart"/>, with values in order from oldest to newest.
/// </summary>
public sealed record ChartSeries(string Name, IReadOnlyList<double> Values, Color Colour);
