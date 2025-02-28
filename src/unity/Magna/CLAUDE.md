# Magna Project Coding Guidelines

## Build & Run Commands
- Start Unity project: Open project in Unity 2021.3.24f1
- Play/test: Click Play button in Unity Editor
- CSV Animation files: Attach compatible CSV files to UpdatedMove script

## Code Style Guidelines
- Naming: PascalCase for classes/methods, camelCase for fields/parameters
- Indentation: 4 spaces
- Braces: Allman style (opening brace on new line)
- Types: Use `double` for robot positions, `Vector3` for Unity positions
- Error handling: Use try/catch for network operations, log errors with Debug.Log
- Comments: Use XML documentation style for methods/classes
- Imports: System imports first, then Unity imports, then third-party libraries

## Network Communication
- UDP-based communication with ABB robots via EGM protocol
- Default ports: 6510 (EGM), 6511 (UDPCOMM), 8000 (RaspberryPi)
- Make sure Unity has proper network permissions in firewall

## Dependencies
- Google.Protobuf (3.21.7)
- Google.Protobuf.Tools (3.21.7)
- NuGetForUnity for package management