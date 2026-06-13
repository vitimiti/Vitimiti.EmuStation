# Vitimtii's EmuStation

A modern C# only project to create an emulators station to play multiple old consoles.

## Table of Contents

- [Supported Platforms](#supported-platforms)
- [Used Native Libraries and Supported Platforms](#used-native-libraries)
  - [Desktop](#desktop)
- [Building](#building)

## Supported Platforms

The following platforms (RIDs) are supported:

- `linux-x64`
- `linux-arm64`
- `osx-arm64`
- `win-x64`
- `win-arm64`

While other RIDs for Linux, OSX and Windows may work, this project won't support them, therefore it is up to you to provide the native libraries.

## Used Native Libraries

While the project is written on C#, native libraries are required to comunicate with the operating systems this project runs on.

### Desktop

- [SDL3 v3.4.10](https://github.com/libsdl-org/SDL/tree/release-3.4.10)
  - Windowing
  - Rendering
  - Input

## Building

This project will hold every requirements within the project files to enable automatic, easy building. The final product will be a published [NativeAOT](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/?tabs=windows%2Cnet8) single executable to be able to easily produce and distribute the emu station.

Therefore, running the publish command with an RID will produce the desired output. Currently, only desktop targets are supported:

```shell
# For windows
dotnet publish -r win-x64 -c Release;
dotnet publish -r win-arm64 -c Release;

# For Linux
dotnet publish -r linux-x64 -c Release;
dotnet publish -r linux-arm64 -c Release;

# For OSX
dotnet publish -r osx-arm64 -c Release;
```
