# GitHub Copilot Prompts

This directory contains reusable prompt files for common MOBAflow development tasks.

## Usage

In GitHub Copilot Chat, type `#prompt:` to reference any prompt file from this folder.

## Available Prompts

*(To be added as needed)*

Examples:
- `add-new-service.prompt.md` - Template for adding a new service with DI
- `create-viewmodel.prompt.md` - Template for creating platform-specific ViewModels
- `add-z21-command.prompt.md` - Template for adding new Z21 protocol commands

## Creating Prompt Files

1. Create a markdown file with `.prompt.md` extension
2. Write your prompt using `#` references to include context (files, methods, classes)
3. Save it in this directory
4. Reference it in chat with `#prompt:filename`

For more information, see: https://learn.microsoft.com/en-us/visualstudio/ide/copilot-chat-context?view=vs-2022#use-prompt-files
