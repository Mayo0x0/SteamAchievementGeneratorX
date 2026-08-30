<p align="center">
  <img src="https://github.com/user-attachments/assets/c9daffa1-1634-4be9-b665-c8999cc30647" alt="AchievementsGenLogo" width="400"/>
</p>

# Steam Achievement Generator X

Turns a **saved SteamDB stats page** into a ready to use `steam_settings` folder for the
[gbe_fork](https://github.com/Detanup01/gbe_fork) Goldberg Steam emulator.

This is a reworked fork of [jeremanteca/SteamAchievementGenerator](https://github.com/jeremanteca/SteamAchievementGenerator).

## What is different from the original

| | original | this fork |
|---|---|---|
| Input | "Webpage, complete" only (HTML + `_files` folder) | **single file HTML** (WebScrapBook), "webpage complete", and raw HTML |
| Inline images | not supported - broke on `data:` URIs | decoded straight out of the page |
| Missing icons | silently lost | refetched from the Steam CDN using the App ID |
| Stats | not exported | **`stats.json` for gbe_fork**, with an editable type/default/global grid |
| Translations | English only | **official Steam translations** merged in from a community page, editable in the grid |
| `achievements.json` | Goldberg legacy key only | `icon` + `icon_gray` + `icongray` |
| App ID | picked the first `data-appid` on the page (often the wrong game) | reads the page's own app scope |
| Interface | fixed size window | resizable, drag & drop, achievement/stat/log tabs |
| Automation | none | command line mode for batch conversion |
| Build | .NET Framework 4.8 / packages.config | SDK project, `net48` **and** `net8.0-windows` |

## Getting the input pages

### The SteamDB stats page (required)

1. Open the **Stats** tab of the game on SteamDB, e.g. `https://steamdb.info/app/3024040/stats/`.
2. Scroll through the whole achievement list once. SteamDB only loads the greyscale
   ("locked") icons while a row is hovered, so a page saved without scrolling stores
   placeholders for most of them - the generator can refetch those from the Steam CDN,
   but scrolling first makes the run fully offline.
3. Save the page:
   * **WebScrapBook** (recommended): *Capture page* - one self contained `.html` file.
   * or the browser's own *Save as -> Webpage, complete*, keeping the `_files` folder next to the HTML.

### The localized achievement page (optional)

SteamDB only ever shows English. The official translations live on the Steam Community, in
whatever language your browser asks for:

1. Set your browser (or your Steam profile) to the language you want.
2. Open `https://steamcommunity.com/stats/<App ID>/achievements/` and save it the same way.
3. Hand that file to the tool as well.

The community page never prints API names, so the two pages are joined on the Steam icon file
name, which both of them reference. Whatever cannot be matched that way is retried on the global
unlock rate, but only where that rate is unique on both sides - a wrong translation is worse than
a missing one. Everything still missing afterwards is listed by API name so you can fill it in.

## Using it

### Window

1. Start `SteamAchievementGenerator.exe`, then pick the HTML file, or drag it onto the window.
2. Optionally pick the saved community page as **Translations**. The language is read from the
   page (`lang="de"` becomes `german`); if the page does not say, you are asked. Achievements with
   no translation are highlighted in the **Achievements** tab and can be typed straight into the
   grid - the generated file then contains those too. Picking a language without loading a page
   works as well, if you want to write all the text yourself.
3. Check the **Achievements** and **Stats** tabs.
   SteamDB does not publish whether a stat is an `int`, a `float` or an `avgrate` - the type is
   guessed from the default value, and the **Type** column is editable if the game needs
   something else.
4. Pick the output folder (defaults to `steam_settings` next to the HTML file) and press
   **Generate steam_settings**.

### Command line

```
SteamAchievementGenerator.exe --input <steamdb.html> [--output <folder>] [options]
```

| option | meaning |
|---|---|
| `--output <folder>` | target folder (default: `steam_settings` next to the HTML) |
| `--translation <file>` | saved steamcommunity.com achievements page with the official translations |
| `--language <name>` | Steam language name for `--translation` (e.g. `german`); detected from the page when omitted |
| `--icon-names api\|steam` | `images/ACH_NAME.jpg` (default) or the original Steam hash file name |
| `--no-stats` | do not write `stats.json` |
| `--no-achievements` | do not write `achievements.json` or icons |
| `--no-download` | never contact the Steam CDN, use only what the page contains |
| `--plain-text` | write `displayName`/`description` as plain strings instead of `{"english": ...}` |
| `--clean` | delete an existing `images` folder before writing |

Passing a single file path without any switch opens that file in the window, so the exe can be
used as an Explorer file association or a drop target.

## What gets generated

```
steam_settings/
  steam_appid.txt        the App ID
  achievements.json      one entry per achievement
  stats.json             one entry per stat
  images/                unlocked and locked icons
```

`achievements.json`:

```json
[
  {
    "name": "ACHIEVEMENT_Store_Food_1000",
    "displayName": {
      "english": "Lord v. Food",
      "german": "Burgherr vs. Nahrung"
    },
    "description": {
      "english": "Store 1,000 food in a single game",
      "german": "Lagert 1.000 Nahrung in einem einzigen Spiel"
    },
    "hidden": "0",
    "icon": "images/ACHIEVEMENT_Store_Food_1000.jpg",
    "icon_gray": "images/ACHIEVEMENT_Store_Food_1000_gray.jpg",
    "icongray": "images/ACHIEVEMENT_Store_Food_1000_gray.jpg"
  }
]
```

* `icon_gray` is what current gbe_fork reads, `icongray` is kept for the original Goldberg
  emulator and for Achievement Watcher.
* The localized object form is resolved by gbe_fork at load time: it picks the language the game
  asked for, falls back to `english`, and only then to the first entry. `--plain-text` collapses it
  back to a plain string, but is ignored for achievements that actually carry a translation.
* SteamDB's global unlock rate is shown in the **Achievements** tab for reference, but it is not
  written to the file - no emulator needs it (gbe_fork would read an `unlock_percentage` field for
  `GetAchievementUnlockPercentage`, and returns `-1.0` when it is absent).

`stats.json`:

```json
[
  { "name": "stat_Units_Killed", "type": "int", "default": "0", "global": "0" }
]
```

`type` is one of `int`, `float`, `avgrate`. `default` and `global` are strings, as gbe_fork expects.
SteamDB never shows a global value, so it is always `"0"` unless you edit it in the grid.

## Installing the result

1. Copy the generated `steam_settings` folder next to the game's `steam_api64.dll` /
   `steam_api.dll` (for Unreal Engine titles that is usually
   `<Game>\Engine\Binaries\ThirdParty\Steamworks\Steamv1xx\Win64\`).
2. Create `%appdata%\GSE Saves\<App ID>\` (older builds: `Goldberg SteamEmu Saves`) so unlocked
   achievements have somewhere to live.

## Building

Requires the .NET 8 SDK (or Visual Studio 2022 with the ".NET desktop development" workload).

```bash
dotnet build SteamAchievementGenerator.sln -c Release
```

Two builds are produced:

* `bin/Release/net48/` - .NET Framework 4.8, runs on any current Windows without extra installs
* `bin/Release/net8.0-windows/` - .NET 8, needs the .NET Desktop Runtime

A portable single file build:

```bash
dotnet publish -c Release -f net8.0-windows -r win-x64 --self-contained true -p:PublishSingleFile=true
```

## Troubleshooting

**"No achievement entries found"** - the saved page is not the `/stats/` tab, or the save only
captured a partial DOM. Re-save with WebScrapBook's *Capture page*.

**Many missing icons** - the page was saved without scrolling through the list and the App ID
lookup failed, or you are offline with `--no-download`. Check the App ID shown in the window;
icons are fetched from
`https://cdn.cloudflare.steamstatic.com/steamcommunity/public/images/apps/<App ID>/<file>.jpg`.

**A single missing icon** - very recently added achievements are sometimes not on the CDN yet.
The generator falls back to the unlocked icon and says so in the log.

**Achievements without a translation** - the community page shows fewer rows than SteamDB (it hides
nothing, but SteamDB sometimes lists achievements Steam has not published yet), or the icons were
replaced between the two saves. The missing ones are named in the dialog and in the Log tab, and
their cells are highlighted in the Achievements tab so you can type the text in yourself.

## License

MIT - see [LICENSE](LICENSE).
