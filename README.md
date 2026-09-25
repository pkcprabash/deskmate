# Deskmate

Deskmate is a small animated coworker that lives in the corner of your screen.
It types along when you type, takes little breaks when you pause, falls asleep
when you're away, nudges you to rest after long stretches of work, greets you
by name each morning, and reminds you about the things that matter, the day
before and on the day.

## Install

Download the latest installer for Windows or macOS from
[GitHub Releases](https://github.com/pkcprabash/deskmate/releases). Deskmate updates
itself in the background; updates apply the next time it starts.

## Building and releasing

```
dotnet build
dotnet test
```

Installers are built with [Velopack](https://velopack.io) (`dotnet tool install -g vpk`):

```
build/pack-macos.sh 1.0.0        # macOS
build/pack-windows.ps1 -Version 1.0.0   # Windows
```

Without signing credentials these produce unsigned local builds. To ship a release, push a
tag (`git tag v1.0.0 && git push origin v1.0.0`); the Release workflow builds, signs,
notarizes and publishes both installers. Signing uses these repository secrets:

| Secret | Purpose |
| --- | --- |
| `WINDOWS_SIGN_PARAMS` | Parameters passed to signtool for Windows code signing |
| `APPLE_CERTIFICATE_P12`, `APPLE_CERTIFICATE_PASSWORD` | Base64 Developer ID certificates (application + installer) |
| `APPLE_APP_IDENTITY`, `APPLE_INSTALL_IDENTITY` | Certificate names for app / installer signing |
| `APPLE_ID`, `APPLE_TEAM_ID`, `APPLE_APP_PASSWORD` | Credentials for notarization |
