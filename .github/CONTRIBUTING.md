# Contributing to StreamMaster

Thank you for your interest in contributing 🤗! This document provides guidelines and steps for contributing to the project.

## Getting Started

1. [Fork the repository](https://github.com/carlreid/StreamMaster/fork)
2. Clone your fork: `git clone https://github.com/[your-github-username]/StreamMaster.git`
3. Create a new branch: `git checkout -b [your-feature-name]`

## Prerequisites

- **Git**: For version control
- **Docker**: For running the development container (recommended)
- **.NET SDK**: For C# backend development (if not using Dev Container)
- **Node.js**: For React frontend development (if not using Dev Container)

## Development Environment Setup

### Option 1: Using the DevContainer (Recommended)

StreamMaster provides a DevContainer configuration for a consistent development experience:

1. **Install Prerequisites**:
   - [Visual Studio Code](https://code.visualstudio.com/)
   - [Docker](https://www.docker.com/) installed locally or a remote environment (e.g. [Podman Desktop](https://podman-desktop.io/), [Docker Desktop](https://www.docker.com/products/docker-desktop/), Docker in WSL2, etc)

2. **Open in Visual Studio Code with DevContainer**:
   - Open the project in VS Code
   - When prompted, select "Reopen in Container"
   - Alternatively, use the command palette (F1) and select "Dev Containers: Open Folder in Container..."

3. **Launch 🚀**:
   - `CTRL+SHIFT+D` to view the "Run and Debug" panel
   - At the top, you can select from various Launch Profiles. It's likely that `Full Stack: Run` profile will be best to start with
     - Polling is there if your environment is not working with Hot Reloading for Front End development (e.g. in WSL2)

### Option 2: Manual Setup

There are many ways that could be detailed here. It's simplest to get Visual Studio to launch the `.sln` file. Then use Visual Studio Code for working with the Front End.

However based on command line functions, it would be;

1. **Backend Setup**:

```bash
dotnet build
dotnet watch run --project src/StreamMaster.API/StreamMaster.API.cspro
```

Access API at: http://localhost:7095/swagger

2. **Frontend Setup**:

```bash
cd src/StreamMaster.WebUI
npm install
npm run dev
```

Access UI at: http://localhost:3000

### Basic Project Structure

- `/src/`: Contains all source code
- `/src/StreamMaster.API`: C# based API, this is the "host" that runs everything backend wise
- `/src/StreamMaster*`: Other C# project folders for various components of th ebackend
- `/stc/StreamMaster.WebUI`: React frontend (Vite-based)

## Development Process

1. Make your changes on your own fork and branch
2. Follow the existing code style and formatting
3. Add tests if applicable
4. Ensure all tests pass
5. Update documentation as needed

## Pull Request (PR) Process

1. Ensure your PR description clearly describes the problem and solution
2. Reference any related issues in your PR description
3. Make sure your branch is up to date with the `main` branch before submitting (*rebase is required*)

## Code Style Guidelines

- Use meaningful variable and function names
- Comment complex logic
- Keep functions focused and concise
- Follow existing project patterns and conventions

## Commit Guidelines

- Follow the [Conventional Commits](https://www.conventionalcommits.org/) naming conventions
- Separate subject from body with a blank line
- Subject should be limited to 50 characters
- Capitalize the subject line
- Do not end the subject line with a period
- Use the body to add additonal context (what, why, how)

## Reporting Issues

- Use the [GitHub issue tracker](https://github.com/carlreid/StreamMaster/issues)
- Check existing issues before creating new ones
- Provide clear steps to reproduce bugs
- Include relevant system information and logs

## Code of Conduct

By participating in this project, you agree to maintain a respectful and inclusive environment for all contributors.

## Questions?

Feel free to [open a discussion](https://github.com/carlreid/StreamMaster/discussions) for any questions not covered by this guide.
