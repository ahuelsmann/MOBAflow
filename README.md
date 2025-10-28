# MOBAflow

An app for model railway automation and control workflows.

## Overview

MOBAflow is a solution for managing and automating model railway operations. 

## Features

- **Workflows**: Define and execute sequences of actions based on incomming events from the digital control centre.
- **Sound**: Various audio outputs or loudspeaker announcements from the perspective of a train or from a station or platform.
- **Commands**: Sending digital commands to the digital control centre.

## Project Structure

The solution consists of the following projects:

- **Backend**: Core business logic and workflow engine
  - Action system with `IAction` interface
  - Command pattern implementation
  - Workflow orchestration
- **WinUI**: Windows desktop application with modern UI
- **SharedUI**: Reusable UI components and controls
- **Sound**: Audio playback
- **Test**: Unit and integration tests

## Requirements

- .NET 10 SDK
- Windows 10/11 (for WinUI application)
- Visual Studio 2022 or later (recommended)

## Getting Started

1. Clone the repository
2. Open `MOBAflow.slnx` in Visual Studio
3. Restore NuGet packages
4. Build the solution
5. Run the WinUI project

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

## Trademark Notice

Z21 and Roco are registered trademarks of Modelleisenbahn GmbH and are used solely for product identification purposes. Their mention is made without any promotional intent.

## Contributing

Contributions are welcome! Please feel free to submit issues or pull requests.

## Author

Andreas Huelsmann