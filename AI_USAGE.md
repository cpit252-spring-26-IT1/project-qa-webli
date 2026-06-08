# Generative AI Declaration

This is my disclosure for how I used generative AI tools while building qa-cli.

My AI setup is not a simple "I used one chatbot once" situation. I use many AI tools in parallel: browser chat LLMs, IDE completions, repo-aware coding agents, APIs through services such as OpenRouter, local agents, and CLI agents. Early in the project, I mostly used chat LLMs to ask questions, understand errors, and brainstorm structure. As the project became larger, I leaned more on system agents because they could read the repository context and help me move faster while I was under time pressure from other subjects, especially after expected group support fell through late in the work.

I am disclosing that clearly because the usage was real and significant. At the same time, I did not treat AI output as something to submit blindly. I reviewed the code, changed parts I did not like, removed things that were not useful, ran the project manually during development, and made the final decisions about what stayed in the submission.

## Context

This project became more demanding than expected because the workload effectively shifted onto me late in the semester. I also tend to be very perfectionistic with coursework, so when the scope grew I used AI tools not only to write or refactor code, but also to keep myself oriented: what files changed, what still needed review, what a clean commit should include, and what parts of the architecture were becoming too coupled.

That context matters because my AI use was not one isolated shortcut. It became a working environment: sometimes a tutor, sometimes a debugger, sometimes a refactoring assistant, sometimes a code generator for support features, and sometimes a documentation/commit-planning helper.

## How I Used AI

### Chat LLMs

These were mostly used earlier, when the project was smaller and I needed explanations, examples, or quick feedback more than full repository edits.

Examples of use:

- asking for explanations of C#/.NET errors
- asking how a design pattern could fit a feature
- asking for examples of WebSocket, Kestrel, QR, parser, or terminal UI code
- pasting terminal errors or one source file and asking what was wrong
- brainstorming better names, commit messages, and project structure
- learning C# and .NET conventions, such as why interfaces are commonly named with an initial `I` and how `:` can represent inheritance or interface implementation depending on the type

Tools in this category included ChatGPT, Gemini, Claude, z.ai, DeepSeek-style models through API/OpenRouter/browser interfaces, AnythingLLM, Odysseus, and other chat interfaces I was experimenting with.

The chat tools were especially useful for learning and explanation. For example, they helped me understand conventions like interface names starting with `I`, the difference between inheritance and interface implementation in C# syntax, when a `record` is useful, and why splitting responsibilities across smaller classes makes a larger project easier to maintain.

### System Agents and IDE Assistants

These became more useful later because the project had many files and the context mattered. A repo-aware agent could inspect the structure, read the related classes, and suggest a smaller change than a normal chatbot would.

Examples of use:

- refactoring code while preserving existing behavior
- splitting large presenter logic into smaller classes
- drafting support classes for unfamiliar libraries
- debugging test failures and runtime errors
- planning commit groups and documentation updates
- simplifying tests after I decided the first generated test suite was too artificial

Tools in this category included Codex CLI, Cursor/Composer-style tools, GitHub Copilot, Antigravity/CLI-style agents, and local or API-backed agents.

Because I used several tools in parallel, I did not retain exact model names or exact dates for every interaction. When I hit student-rate limits or context limits, I sometimes switched tools or used a simple handoff/compact workflow, including notes such as `HANDOVER.md`, so the next agent could continue with less lost context. The sequence below follows the project development order, but it should not be read as an exact transcript.

These agents were more important later because the project context was too large to keep pasting into a chat window. I often used them to inspect the repository, point out which method or file was actually causing a problem, and suggest a focused change. I still decided whether the change made sense before keeping it.

## Project Sequence

| Project Stage | Work Area | AI Use |
| --- | --- | --- |
| Early parser and domain work | Markdown quiz loading, `ContentBlock`, and basic quiz/session entities | AI helped with parser ideas, regular expressions, debugging malformed quiz input, and thinking through how plain text/code/math blocks should be separated. |
| Student voting and hosted UI | Kestrel server, WebSocket hub, browser client, and live voting | AI helped with WebSocket examples, JSON message flow, browser-client updates, and student connection handling. |
| Presenter refactor and public joining | terminal UI refactor, QR code, syntax highlighting, and ngrok integration | AI helped heavily with QR generation, ngrok process handling, syntax highlighting support, and presenter refactoring. |
| Reveal/logging/math additions | LaTeX display, answer reveal, vote logs, and observer/event flow | AI helped with formatting, render strategy ideas, answer reveal flow, and logging/vote update behavior. |
| Game mode | lobby, countdown timer, scoring, leaderboard, display names, and game audit logs | AI helped heavily with the game-mode implementation, scoring/timer flow, lobby, leaderboard, and game audit logging. |
| Tests and cleanup | xUnit tests, test simplification, vote-change fixes, and behavior checks | AI helped draft the first test suite, then helped simplify it after I decided the original suite was too artificial. |
| Arabic support | terminal Arabic shaping, RTL alignment, browser direction, and Arabic sample quiz | AI helped with Arabic shaping/RTL display and integration with the rendering pipeline. This was added later to make Arabic quiz text readable and to support class use cases such as Quran recitation rings. |
| Hosting reliability and QR refinement | Linux inotify fix and compact QR rendering for ngrok URLs | AI helped debug the startup issue and implement smaller QR rendering for long ngrok URLs. |
| Documentation and final submission preparation | README screenshots, architecture notes, AI declaration, and commit planning | AI helped organize screenshots, rewrite documentation, and make the submission easier to review. |

## Files or Areas With Heavy AI Drafting

These are the areas where AI assistance was strongest. Some files also contain source comments near the top of the file disclosing that AI assistance was used.

| File / Area | What AI Helped Build | What It Is Not |
| --- | --- | --- |
| `src/QaWebli.Infrastructure/Server/WebServer.cs` | Lightweight Kestrel host, route setup, embedded `client.html` loading, and later hosting reliability fixes. | It is infrastructure support, not the core quiz domain model. |
| `src/QaWebli.Infrastructure/Server/PollingHub.cs` | WebSocket connection lifecycle, JSON messages, broadcast logic, student connection handling, and defensive send/close behavior. | It is not where the quiz questions/options are defined. |
| `src/QaWebli.TerminalUI/Hosting/NgrokTunnel.cs` | Starting ngrok, reading the local ngrok inspector API, choosing the public URL, and cleaning up the process. | It is optional hosting support, not required for local quiz logic. |
| `src/QaWebli.TerminalUI/Presentation/QRGenerator.cs` | Terminal QR code rendering, including compact rendering for long ngrok URLs. | It is display support, not voting/scoring logic. |
| `src/QaWebli.TerminalUI/Presentation/SyntaxHighlighting/` | Token scanning, keyword lists, string/comment detection, and safe Spectre.Console markup rendering. | It is cosmetic/presentation support for code blocks. |
| `src/QaWebli.TerminalUI/Presentation/PresenterController.cs` | Refactoring presenter flow away from an over-large `Program.cs`. | It did not decide the project concept; it reorganized presentation flow. |
| `src/QaWebli.TerminalUI/Program.cs` | AI helped point out where orchestration code was becoming too crowded and suggested decoupling directions. | The entry-point flow and overall CLI/presenter direction were still manually understood and controlled by me. |
| `src/QaWebli.Infrastructure/Parsing/MarkdownQuizParser.cs` | Regex/parser debugging and parsing improvements. | The quiz file format and parser behavior were reviewed through manual runs and later covered by the final test suite. |
| `src/QaWebli.Tests/` | Initial test drafting and later help simplifying the suite while I worked through what tests were actually useful. | The final tests were not kept as a huge generated suite; I asked to remove low-value tests. |
| Arabic support files | Arabic detection, reshaping, RTL terminal alignment, and browser direction/font support. | It is a later accessibility/readability addition, not the original project skeleton. |
| Game mode files | Lobby, countdown, score timing, leaderboard display, final results, and display-name support. | The game mode was not part of the first pushed grading version; it was a later local enhancement. |
| `ARCHITECTURE.md` and documentation updates | AI and documentation-agent experiments helped draft and reorganize architecture notes in a style close to what I wanted to say. | The documentation was still reviewed and edited by me; it was not treated as an unquestioned final answer. |

Some of these areas, especially QR rendering, ngrok integration, WebSocket hosting, syntax highlighting, browser UI styling, tests, and game-mode presentation details, were much more AI-assisted than the core domain classes. This was partly because those features involved unfamiliar libraries or presentation details that were not the main design-pattern learning goal of the course.

## Core Work I Still Consider Mine

The project idea, the main quiz/presenter goal, the final feature choices, and the decision to keep the project as a terminal-first app were mine. I also built and studied the design-pattern skeleton because I wanted to understand the patterns rather than only paste them in.

AI did suggest where patterns could fit, and I used that as a learning loop. For example, Strategy became useful once question rendering grew from plain text into code, math, and Arabic-aware text. That pattern was not strictly required for the smallest version of the app, but it became a useful way to keep the renderer extensible and gave me a practical canvas to implement the idea.

The same applies to project structure. AI suggested that separating the solution into `QaWebli.Core`, `QaWebli.Infrastructure`, `QaWebli.TerminalUI`, and `QaWebli.Tests` would make future UI changes easier. I accepted that structure because it matched the direction I wanted: the domain logic could stay separate from terminal or web-specific code.

The Core/domain side of the project and the main `Program.cs` orchestration path were mostly manually understood and controlled by me, with AI used for refactoring advice, naming, and pointing out coupling:

- `Option`
- `Question`
- `Quiz`
- `Session`
- `SessionFacade`
- observer events and interfaces

Some later features edited these areas, but I reviewed those changes before keeping them.

## Other AI Uses

AI also helped with smaller but important project-workflow tasks:

- naming commits in a more professional style
- breaking large work into cleaner commit groups
- proofreading and reorganizing README/architecture text
- checking whether new features should be documented in `README.md` or `ARCHITECTURE.md`
- experimenting with documentation-agent workflows that tried to update architecture notes after code changes

The documentation-agent experiment did not perfectly copy my voice, but it helped reveal what I wanted the documentation to say. I then edited the final wording from there.

## Verification and Responsibility

AI output was treated as assistance, not as final authority. Earlier features were mostly checked by building the app and running it manually. The automated tests were added late, so `dotnet test` should be understood as part of the final submission verification, not as something that existed during every earlier feature.

- `dotnet build QaWebli.sln -c Debug`
- running early versions of the application as features were added
- manual runs of the terminal presenter
- manual game-mode checks
- Arabic sample quiz checks
- student browser UI screenshots
- ngrok/local hosting checks where available
- final automated verification with `dotnet test QaWebli.sln` after the test suite was added

This declaration is intentionally broad because my AI workflow was broad. The important point is that AI was used as an assistant during a difficult and time-constrained project, while the final review, integration, testing, and submission decisions remained mine.
