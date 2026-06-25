# Kron4n CoH2 Launcher v0.1

A small launch options helper for **Company of Heroes 2**.

I made this because I wanted a simple way to launch CoH2 with common startup options without typing them manually every time, and also to quickly open the usual CoH2 folders.

## What it does

- Launches Company of Heroes 2 through Steam with selected launch options
- Copies the selected launch options so they can be pasted manually into Steam
- Opens useful CoH2 folders:
  - Documents
  - Replays
  - Mods
  - Workshop
  - Install folder
- Creates a backup of the CoH2 settings folder
- Includes a default safe modern launch profile

## Default Safe Modern Fix

```txt
-nomovies -window -fullwindow -lockmouse -refresh 144 -novsync -notriplebuffer -forceactive
```

You can change the refresh rate or use your own custom arguments in the launcher.

## What it does not do

This tool does **not** modify Company of Heroes 2 game files.

It does not patch the game, install mods, edit balance files, or replace any CoH2 files. It only launches the game through Steam with selected launch arguments and provides shortcuts to common folders.

## Builds

There are two builds.

### Full build

The Full build is larger, but it is self-contained.

Use this version if you do not want to install .NET separately.

### Lite build

The Lite build is much smaller, but it requires:

```txt
.NET 10 Desktop Runtime x64
```

Use this version if you already have the runtime installed or do not mind installing it.

## Source structure

```txt
Kron4n-CoH2-Launcher/
├── Kron4n_CoH2_Launcher/
│   └── Full self-contained project
│
├── Kron4n_CoH2_Launcher_v0.1_Lite/
│   └── Lite framework-dependent project
│
├── README.md
├── LICENSE
└── .gitignore
```

## Notes

Random EXE files from the internet can look suspicious, and that is fair.

That is why the source code is included here, so people can check what the launcher does before running it.

## License

MIT License.
