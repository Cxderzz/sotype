# sotype

A full-screen terminal typing test in the style of [monkeytype](https://monkeytype.com):
live per-character coloring, timed and word-count modes, themes, and run history — all
in your terminal.

## Running from source

Requires the [.NET 10 SDK](https://dotnet.microsoft.com/).

```sh
dotnet run --project src/Sotype.Console
```

`Tab` restarts a test, `Esc` returns to the menu (or quits from the results screen),
`Enter`/`m` on the results screen goes back to the menu.

## Building

```sh
dotnet build
dotnet test
```

## Architecture

- `Sotype.Domain` — the typing-test rules themselves (`TypingSession`, the aggregate root;
  `Word`, `TestResult`, and other value objects; the WPM/accuracy formulas). No dependency
  on the console or any I/O — fully unit-testable in isolation.
- `Sotype.Infrastructure` — implements the domain's repository/provider interfaces: the
  embedded word list, and JSON-file-backed history and preferences.
- `Sotype.Console` — the terminal UI: Spectre.Console rendering, raw keystroke handling,
  and the alternate-screen-buffer lifecycle. The composition root (`Program.cs`) wires
  everything together.

## Packaging

An Arch Linux `PKGBUILD` is at [`packaging/PKGBUILD`](packaging/PKGBUILD) — a
framework-dependent build (depends on `dotnet-runtime`) that publishes
`src/Sotype.Console`. Its `url`/`source` fields are placeholders until this repo has a
tagged release to build from.

## License

[MIT](LICENSE)
