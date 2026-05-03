```markdown
# Qa-webli

*(QA = Question & Answer | Web = Web Interface | li = Command Line Interface)*

## Description
Qa-webli is an interactive polling and presentation platform. It is designed to handle complex questions—like those containing math equations or programming code—without ruining the formatting. Presenters can launch and manage live sessions using either a standard web dashboard or a fast, text-based terminal application, while participants connect easily from their own devices.

## Features
- **Easy to Use:** Clean interfaces tailored for both the presenter managing the flow and the students answering.
- **Comprehensive Logging:** Every action, state change, and system event is tracked locally to ensure session integrity and easy auditing.
- **Local & Secure:** Designed to be run locally or securely hosted by the organization, keeping data private.
- **Accurate Formatting:** Natively supports Markdown, meaning technical questions look exactly as intended.

## Usage

```bash
dotnet run --project src/QaWebli.TerminalUI -- sample-quiz.md
```

Or download a pre-built binary from the releases below.

### Quiz File Format

Quizzes are written in Markdown — see [`docs/quiz-format.md`](docs/quiz-format.md) for the full guide.

## Downloads (v1 — pre-release)

| Platform | Link |
|----------|------|
| Windows x64 | [qa-webli-v1-win-x64.zip](https://github.com/cpit252-spring-26-IT1/project-qa-webli/releases/download/v1/qa-webli-v1-win-x64.zip) |
| Linux x64 | [qa-webli-v1-linux-x64.zip](https://github.com/cpit252-spring-26-IT1/project-qa-webli/releases/download/v1/qa-webli-v1-linux-x64.zip) |
| macOS ARM | [qa-webli-v1-osx-arm64.zip](https://github.com/cpit252-spring-26-IT1/project-qa-webli/releases/download/v1/qa-webli-v1-osx-arm64.zip) |
| Sample Quiz | [sample-quiz.md](https://github.com/cpit252-spring-26-IT1/project-qa-webli/releases/download/v1/sample-quiz.md) |

Unzip, place `sample-quiz.md` next to the executable, and run:

```
./qa-webli sample-quiz.md
```

Navigate with ← → arrow keys. Press Q to quit.

## Screenshots

*(UI in development. Screenshots to follow.)*

## Project Management
* [Kanban Board](https://github.com/orgs/cpit252-spring-26-IT1/projects/31)

## License

**All Rights Reserved**

Copyright (c) 2026 Qa-webli.

This project and its source code are proprietary. Unauthorized copying, modification, distribution, or use of this software, via any medium, is strictly prohibited without explicit written permission from the authors.