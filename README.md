# sotype

A full-screen terminal typing test in the style of [monkeytype](https://monkeytype.com):
live per-character coloring, a smooth caret that glides between characters at sub-cell
resolution, timed and word-count modes, themes, and run history while all in your terminal.

## Screenshots

![sotype running a 10 word test](media/sotype.gif)

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

- `Sotype.Domain`: the domain layer, totally independent of any runtime logic
- `Sotype.Infrastructure`: implementation of domain level interfaces for things like word lists
- `Sotype.Console`: The UI layer, runs as a console app

## Packaging

Packaging is a work in progress. There is a dummy PKGBUILD for Arch Linux that needs heavy refinement.

## License

[MIT](LICENSE)
