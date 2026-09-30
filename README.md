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

`tests/Sotype.IntegrationTests` runs the whole app (menu, test, results) against scripted
keypresses and a manual clock. Add a scenario there to check a change without sitting
through a real typing test.

## Architecture

- `Sotype.Domain`: the domain layer, totally independent of any runtime logic
- `Sotype.Infrastructure`: implementation of domain level interfaces for things like word lists
- `Sotype.Console`: The UI layer, runs as a console app

## Packaging

Pushing a `v*` tag (e.g. `git tag v0.1.0 && git push origin v0.1.0`) runs the release
workflow, which builds an Arch Linux package from [`packaging/PKGBUILD`](packaging/PKGBUILD)
and attaches it to a GitHub release. Install it with:

```sh
sudo pacman -U sotype-<version>-1-x86_64.pkg.tar.zst
```

## License

[MIT](LICENSE)
