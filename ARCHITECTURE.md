# Architecture & Design Patterns

This project follows Domain-Driven Design (DDD) principles. The **domain model** in `QaWebli.Core` lives under [`src/QaWebli.Core/Domain/`](src/QaWebli.Core/Domain/), with core types grouped in **`Entities`**: [`Option`](src/QaWebli.Core/Domain/Entities/Option.cs), [`Question`](src/QaWebli.Core/Domain/Entities/Question.cs), [`Quiz`](src/QaWebli.Core/Domain/Entities/Quiz.cs), and [`Session`](src/QaWebli.Core/Domain/Entities/Session.cs). [`QaWebli.TerminalUI`](src/QaWebli.TerminalUI/) exercises these types in a console-first CLI presenter.

[`QaWebli.Infrastructure`](src/QaWebli.Infrastructure/) handles parsing quiz files from disk (splitting question text into `ContentBlock` segments via `ContentBlockParser`), writing audit logs, and hosting an embedded join page + WebSocket hub so students can join from their phones. [`QaWebli.TerminalUI`](src/QaWebli.TerminalUI/) exercises the domain in a Spectre.Console terminal presenter with the Strategy + Composite patterns for rendering.

**Why this layout:** `Option`, `Question`, `Quiz`, and `Session` are small, invariant-focused types. `Question` holds a single prompt and its choices; `Quiz` aggregates multiple `Question` instances under a title; `Session` tracks the live state of a quiz in progress (which question is active, who is connected, navigation). Builders validate construction so invalid objects never reach the rest of the app. Keeping everything under `Domain/Entities` keeps the model easy to reuse without pulling in infrastructure.

**Value objects:** `ContentBlock` is an abstract record with three sealed subtypes (`PlainText`, `CodeBlock`, and `MathBlock`) forming a discriminated union. A question's body is a sequence of these blocks, parsed from Markdown by `ContentBlockParser` so the terminal can render each segment differently.

---

## Implemented patterns (CPIT-252)

Course-required Gang of Four (GoF) patterns stay **next to the code they construct**, not in a generic `Patterns` folder.

### 1. Creational: Builder pattern (`Question`)

* **Location:** [`src/QaWebli.Core/Domain/Entities/Question.cs`](src/QaWebli.Core/Domain/Entities/Question.cs) — nested type `Question.Builder`
* **Rationale:** `Question` is immutable with a private constructor. The builder sets number, prompt text, content blocks, and options; `Build()` requires non-empty text and at least one option before returning a valid `Question`.

### 2. Creational: Builder pattern (`Quiz`)

* **Location:** [`src/QaWebli.Core/Domain/Entities/Quiz.cs`](src/QaWebli.Core/Domain/Entities/Quiz.cs) — nested type `Quiz.Builder`
* **Rationale:** A `Quiz` is immutable and composes existing `Question` instances. `Quiz.Builder` collects a title and questions; `Build()` enforces a non-empty title and at least one question. This mirrors the `Question` builder style and keeps aggregate construction consistent.

### 3. Creational: Builder pattern (`Session`)

* **Location:** [`src/QaWebli.Core/Domain/Entities/Session.cs`](src/QaWebli.Core/Domain/Entities/Session.cs) — nested type `Session.Builder`
* **Rationale:** A `Session` wraps a `Quiz` and tracks which question is currently active. `Session.Builder` auto-generates an 8-character ID, requires a valid `Quiz`, and validates the start index. The private constructor ensures sessions are always created in a valid state.

### 4. Structural: Facade pattern

* **Location:** [`src/QaWebli.Core/Application/Services/SessionFacade.cs`](src/QaWebli.Core/Application/Services/SessionFacade.cs)
* **Rationale:** `Session` has low-level methods like `TryMoveNext()` and `TryMovePrevious()`. Without a facade, every caller would need to call these methods directly AND manually fire events. `SessionFacade` wraps this behind `NextQuestionAsync()` and `PreviousQuestionAsync()`, which both update the session AND publish the appropriate event. It also exposes `AddStudentAsync()` / `RemoveStudentAsync()` so the embedded join server can register student connections through the same event pipeline. The caller does one method call instead of two.

### 5. Behavioral: Observer pattern

* **Location:** [`src/QaWebli.Core/Application/Interfaces/IsessionObserver.cs`](src/QaWebli.Core/Application/Interfaces/IsessionObserver.cs) (contract), [`src/QaWebli.Core/Application/Services/SessionFacade.cs`](src/QaWebli.Core/Application/Services/SessionFacade.cs) (subject)
* **Concrete Observers:** [`AuditLogger.cs`](src/QaWebli.Infrastructure/Logging/AuditLogger.cs), [`PollingHub.cs`](src/QaWebli.Infrastructure/Server/PollingHub.cs), [`ConsoleObserver.cs`](src/QaWebli.TerminalUI/Presentation/ConsoleObserver.cs)
* **Domain Events:** [`src/QaWebli.Core/Domain/Events/DomainEvents.cs`](src/QaWebli.Core/Domain/Events/DomainEvents.cs) — `QuestionChangedEvent`, `VoteReceivedEvent`, `StudentPresenceEvent`, `AnswerRevealedEvent`, `GameFinishedEvent`
* **Rationale:** When a quiz session changes (question navigated, vote cast, student presence updates, answer revealed), multiple independent things must happen: the audit logger writes the event to disk, the join hub broadcasts updates to connected students, and the `ConsoleObserver` triggers a live terminal UI re-render for the instructor. Without the Observer pattern, `SessionFacade` would need to directly call methods on each class, creating tight coupling. With the Observer pattern, the facade simply publishes an event, and each observer reacts independently. The facade does not know who is listening or what they do.
* **How it works step-by-step:**
  1. **The Contract:** `ISessionObserver` defines the methods each observer must implement (`OnQuestionChangedAsync`, `OnVoteReceivedAsync`, `OnStudentPresenceChangedAsync`, `OnAnswerRevealedAsync`).
  2. **The Subject:** `SessionFacade` maintains a private list of observers. `Program.cs` calls `facade.Subscribe(observer)` at startup to register them.
  3. **Event Firing:** When the domain state changes (e.g., `facade.NextQuestionAsync()` or `facade.RevealAnswerAsync()`), the facade calls its internal `PublishAsync()` method, which iterates over all subscribed observers and fires the event.
  4. **Observer 1 (`AuditLogger`):** Receives the event and writes a thread-safe log entry to disk so there is a persistent record. Logs are written for started/ended sessions, question navigation, student presence changes, cast votes, and revealed answers.
  5. **Observer 2 (`PollingHub`):** Receives the event and pushes the new state or the reveal signal over WebSockets to all connected student phones.
  6. **Observer 3 (`ConsoleObserver`):** Receives the event and triggers a Spectre.Console UI re-render in the terminal so the instructor immediately sees the new vote counts, student presence, or correct answer details.

### 6. Creational: Singleton pattern

* **Location:** [`src/QaWebli.Infrastructure/Logging/AuditLogger.cs`](src/QaWebli.Infrastructure/Logging/AuditLogger.cs)
* **Rationale:** The audit logger writes to a single file on disk. If multiple instances existed, they would fight over the file handle and corrupt the log. The Singleton pattern guarantees exactly one `AuditLogger` instance exists for the entire lifetime of the application.
* **How it works step-by-step:**
  1. The class has a `private` constructor, so no external code can call `new AuditLogger()`.
  2. `Initialize(Session)` creates the single active instance for the current run.
  3. The public `static AuditLogger Instance` property exposes that initialized instance and throws if startup skipped initialization.
  4. The constructor automatically creates a `logs/` directory under the solution root and opens a timestamped log file. The filename prefix is `qa-session-` in normal mode and `game-session-` in game mode. The timestamp is derived in **GMT+3** (`Asia/Riyadh`) so the log name matches the instructor's local time (e.g., `game-session-20260519-114723.log`).
  5. All write operations use a `lock (_lock)` block to ensure thread safety when multiple observers fire events concurrently.
  6. `OnGameFinishedAsync` logs the final score table as a ranked list so the audit trail records the complete game outcome.

### 7. Behavioral: Strategy pattern

* **Location:** [`src/QaWebli.TerminalUI/Presentation/IContentRendererStrategy.cs`](src/QaWebli.TerminalUI/Presentation/IContentRendererStrategy.cs) (interface), [`src/QaWebli.TerminalUI/Presentation/PlainTextRendererStrategy.cs`](src/QaWebli.TerminalUI/Presentation/PlainTextRendererStrategy.cs), [`src/QaWebli.TerminalUI/Presentation/CodeBlockRendererStrategy.cs`](src/QaWebli.TerminalUI/Presentation/CodeBlockRendererStrategy.cs), and [`src/QaWebli.TerminalUI/Presentation/MathBlockRendererStrategy.cs`](src/QaWebli.TerminalUI/Presentation/MathBlockRendererStrategy.cs) (concrete)
* **Rationale:** Different types of content require different rendering logic. Plain text needs escaping and simple rendering; code blocks need syntax highlighting and their own panel with a language tag; math blocks need advanced LaTeX formatting and their own cyan panel with an 'expression' tag. The Strategy pattern encapsulates each rendering algorithm in its own class, so the caller just invokes `Render(block)` without knowing which strategy is active. Adding a new content type means adding a new strategy class — no existing code changes (Open/Closed Principle).
* **How it works step-by-step:**
  1. `IContentRendererStrategy` defines two methods: `CanRender(ContentBlock)` returns `true` if this strategy handles the given block type, and `Render(ContentBlock)` returns a Spectre.Console `IRenderable`.
  2. `PlainTextRendererStrategy` handles `ContentBlock.PlainText` blocks by escaping any Spectre markup characters and rendering the text directly as standard copy.
  3. `CodeBlockRendererStrategy` handles `ContentBlock.CodeBlock` blocks. Internally it delegates to the **escape-first syntax highlighting pipeline** (under `Presentation/SyntaxHighlighting/`) which colours keywords, strings, comments, numbers, and type names per language. The strategy then wraps the highlighted output in a grey rounded panel with a language tag header.
  4. `MathBlockRendererStrategy` handles `ContentBlock.MathBlock` blocks. It parses LaTeX notation (recursively formatting fractions and replacing integrals, derivatives, limits, and Greek symbols with clean Unicode representations) and displays them inside a styled cyan panel with an 'expression' header.

### 8. Structural: Composite pattern

* **Location:** [`src/QaWebli.TerminalUI/Presentation/CompositeQuestionRenderer.cs`](src/QaWebli.TerminalUI/Presentation/CompositeQuestionRenderer.cs)
* **Rationale:** A question can contain multiple content blocks in any order: plain text, then a code block, then more text. The Composite pattern lets us treat the collection of renderers as a single unit. `Program.cs` just calls `renderer.RenderQuestion(question)` and gets back one `IRenderable` — it does not need to know how many blocks exist or what types they are.
* **How it works step-by-step:**
  1. `CompositeQuestionRenderer` is constructed with a list of `IContentRendererStrategy` objects.
  2. When `RenderQuestion(question)` is called, it iterates over every `ContentBlock` in the question.
  3. For each block, it finds the first strategy whose `CanRender(block)` returns `true`.
  4. It calls that strategy’s `Render(block)` method and collects the result.
  5. All results are combined into a single `Rows` renderable and returned.

### 9. Creational: Factory Method pattern

* **Location:** [`src/QaWebli.TerminalUI/Presentation/CompositeQuestionRenderer.cs`](src/QaWebli.TerminalUI/Presentation/CompositeQuestionRenderer.cs) — the `Default()` static method.
* **Rationale:** Creating a `CompositeQuestionRenderer` requires knowing which strategies exist and in what order to check them. Instead of forcing `Program.cs` to manually construct and inject all strategies, the `Default()` factory method encapsulates this knowledge and returns a fully configured renderer.
* **How it works step-by-step:**
  1. `Program.cs` calls `CompositeQuestionRenderer.Default()`.
  2. The factory method creates instances of `PlainTextRendererStrategy`, `CodeBlockRendererStrategy`, and `MathBlockRendererStrategy` and passes them to the constructor.
  3. The caller receives a fully configured renderer without needing to know what strategies exist internally.

### 10. Behavioral: Strategy pattern (`IQuizEngine`)

* **Location:** [`src/QaWebli.TerminalUI/Presentation/IQuizEngine.cs`](src/QaWebli.TerminalUI/Presentation/IQuizEngine.cs)
* **Concrete strategies:** [`ManualQuizEngine.cs`](src/QaWebli.TerminalUI/Presentation/ManualQuizEngine.cs), [`AutomatedGameQuizEngine.cs`](src/QaWebli.TerminalUI/Presentation/AutomatedGameQuizEngine.cs)
* **Rationale:** The presenter has two fundamentally different execution flows: in normal mode the instructor drives navigation manually with arrow keys; in game mode the engine drives itself — a countdown runs per question, the answer is auto-revealed when the timer expires, an intermediate leaderboard is shown between questions, and a final leaderboard is displayed at the end. `IQuizEngine` defines a single `RunAsync(session, facade, requestRender)` contract and two bool state properties (`InLobby`, `IsFinished`). `PresenterController` selects the correct strategy at construction time based on `session.IsGameMode` — no if/else chains scattered through the rendering code.
* **How it works step-by-step:**
  1. `PresenterController` checks `session.IsGameMode` and assigns either `AutomatedGameQuizEngine` or `ManualQuizEngine` to its `IQuizEngine` field.
  2. `RunAsync` is called once and drives the entire session lifetime.
  3. `InLobby` and `IsFinished` are read by `PresenterController.RequestRender()` to decide which view to draw (lobby → question → leaderboard).

---

## Syntax highlighting (presenter)

Terminal-side code colouring uses an **escape-first** pipeline to guarantee Spectre.Console markup safety. The implementation is split into focused single-responsibility classes under [`src/QaWebli.TerminalUI/Presentation/SyntaxHighlighting/`](src/QaWebli.TerminalUI/Presentation/SyntaxHighlighting/):

| Class | File | Responsibility |
|-------|------|----------------|
| `SyntaxHighlightColors` | [`SyntaxHighlightColors.cs`](src/QaWebli.TerminalUI/Presentation/SyntaxHighlighting/SyntaxHighlightColors.cs) | Spectre colour names for each token category (keyword, string, comment, number, type). |
| `LanguageKeywordSets` | [`LanguageKeywordSets.cs`](src/QaWebli.TerminalUI/Presentation/SyntaxHighlighting/LanguageKeywordSets.cs) | Static `HashSet<string>` for C#, Python, Java, JS/TS, SQL + `GetKeywords(lang)` lookup. |
| `CommentDetector` | [`CommentDetector.cs`](src/QaWebli.TerminalUI/Presentation/SyntaxHighlighting/CommentDetector.cs) | `IsSingleLineComment(trimmed, lang)` — detects `//`, `#`, `--` per language. |
| `StringLiteralScanner` | [`StringLiteralScanner.cs`](src/QaWebli.TerminalUI/Presentation/SyntaxHighlighting/StringLiteralScanner.cs) | `FindStringRanges(line)` — finds `"` / `'` literal spans with escape handling. |
| `EscapeFirstSyntaxHighlighter` | [`EscapeFirstSyntaxHighlighter.cs`](src/QaWebli.TerminalUI/Presentation/SyntaxHighlighting/EscapeFirstSyntaxHighlighter.cs) | `HighlightSafe(code, lang)` → `IRenderable` — the full pipeline (see algorithm below). |

### Algorithm (per line)

1. Detect full-line comments → grey italic escaped line.
2. Extract string literals to GUID-keyed placeholders → `Markup.Escape()` the remainder.
3. Word-boundary regex pass: keywords (per language set), PascalCase type names, numbers.
4. Restore string placeholders with gold colour.
5. Wrap in `new Markup(result)` with try/catch fallback to plain `Text` if markup is still invalid.

The thin [`CodeBlockRendererStrategy`](src/QaWebli.TerminalUI/Presentation/CodeBlockRendererStrategy.cs) delegates to `EscapeFirstSyntaxHighlighter.HighlightSafe()` and wraps the result in a grey `Panel` with a language header. No changes were needed to the Core/Infrastructure parsing — `ContentBlock.CodeBlock` already carries `Language` + `Code`. The student `client.html` keeps Highlight.js; this is **presenter-only** terminal polish.

---

## Terminal QR code

* **Location:** [`src/QaWebli.TerminalUI/Presentation/QRGenerator.cs`](src/QaWebli.TerminalUI/Presentation/QRGenerator.cs)
* **Dependency:** [QRCoder 1.6.0](https://www.nuget.org/packages/QRCoder/) — added to [`QaWebli.TerminalUI.csproj`](src/QaWebli.TerminalUI/QaWebli.TerminalUI.csproj).
* **Purpose:** Renders an ASCII QR code in the presenter status panel encoding the `joinUrl` (LAN IP or ngrok public URL). Students scan with their phone camera to open the join page instantly.
  - `GenerateCompact(url)` — borderless, ECC M, for the side-by-side status panel.
  - For **ngrok** URLs specifically, `GenerateCompact(url)` switches to a half-block Unicode rendering with lower QR error correction so long public URLs take less space in the terminal footer.
  - `Generate(url)` — with quiet zones for wide terminals.
  - On failure → returns `"[QR unavailable]"` (no crash).
* **Presenter layout:** `Program.cs` renders a transparent `Grid` with `Expand()` at the bottom of the screen. The left column (navigation, stats, join URL) fills available space while the right column (QR code) is pinned **hard-right** at the terminal edge without taking up unnecessary vertical space or generating distracting borders.
* **Rules:**
  - QR + URL row only when student UI is enabled (`joinUrl` non-empty).
  - `--no-student-ui`: no QR, no join URL — minimal footer only.
  - `--ngrok` success: QR encodes the **HTTPS ngrok URL** (solves long-URL sharing problem).
  - All URLs are `Markup.Escape()`-d inside `[link]` tags to prevent Spectre markup crashes.

---

## Embedded Join Page (Kestrel + WebSocket)

Not a GoF pattern — this is an infrastructure feature used to let participants join from their own devices while the presenter stays in the terminal.

* **Location:** [`src/QaWebli.Infrastructure/Server/WebServer.cs`](src/QaWebli.Infrastructure/Server/WebServer.cs), [`src/QaWebli.Infrastructure/Server/PollingHub.cs`](src/QaWebli.Infrastructure/Server/PollingHub.cs), [`src/QaWebli.Infrastructure/Server/Resources/client.html`](src/QaWebli.Infrastructure/Server/Resources/client.html)
* **How it works:**
  1. `WebServer` disables configuration reload watchers, then starts Kestrel (listening on the chosen port; default 8080). The server is configured entirely in code, so file-watch reloads add no value and can exhaust Linux `inotify` limits on busy development machines.
  2. When a student opens the URL in their browser, Kestrel serves the embedded `client.html` — a self-contained page with HTML + CSS + JS in one file.
  3. The browser opens a WebSocket to `/hub`. The `PollingHub` registers the connection and calls `_manager.AddStudentAsync()`.
  4. `PollingHub` implements `ISessionObserver`, so when the presenter navigates to a new question, the hub receives the `QuestionChangedEvent` and broadcasts the new question as JSON to every connected student.
  5. When the presenter quits, `PollingHub.CloseAllAsync()` sends a "session_ended" message to every student and closes their WebSocket connections gracefully. Close/send calls use short cancellation windows so shutdown does not hang on dead sockets.

---

## LaTeX Math Formula Support

* **Location:** [`src/QaWebli.Core/Domain/ValueObjects/ContentBlock.cs`](src/QaWebli.Core/Domain/ValueObjects/ContentBlock.cs) (Domain), [`src/QaWebli.Infrastructure/Parsing/ContentBlockParser.cs`](src/QaWebli.Infrastructure/Parsing/ContentBlockParser.cs) (Parser), [`src/QaWebli.TerminalUI/Presentation/MathBlockRendererStrategy.cs`](src/QaWebli.TerminalUI/Presentation/MathBlockRendererStrategy.cs) (Renderer Strategy), [`src/QaWebli.Infrastructure/Server/Resources/client.html`](src/QaWebli.Infrastructure/Server/Resources/client.html) (Student client)
* **Purpose:** Enables quiz authors to write mathematical formulations using LaTeX syntax and display them properly in both the terminal UI and the student web page.
* **Terminal UI Strategy & Unicode Mapping:**
  - Standard Spectre markup does not support LaTeX rendering natively.
  - Using the **Strategy Pattern**, we extended the quiz domain with `ContentBlock.MathBlock`. The `ContentBlockParser` detects display math fences (`$$`) and extracts them into dedicated `MathBlock`s, leaving plain text blocks untouched.
  - The `MathBlockRendererStrategy` executes specifically for `MathBlock`s. It formats LaTeX tokens (e.g. `\sigma`, `\pi`, `\bowtie`, `\land`, `\lor`, `\geq`, `\leq`, `\rightarrow`, `\subset`, `\subseteq`, `\cup`, `\cap`, `\setminus`, `\div`) to their Unicode equivalents (e.g. `σ`, `π`, `⋈`, `∧`, `∨`, `≥`, `≤`, `→`, `⊂`, `⊆`, `∪`, `∩`, `∖`, `÷`), strips formatting markers, and wraps the result in a styled cyan panel to clearly distinguish math expressions from plain text and code blocks.
* **Student Web Client KaTeX Integration:**
  - The student web client embeds KaTeX stylesheets and libraries from a CDN dynamically.
  - When the web client receives the question content, it runs `tryRenderMath()`, which calls KaTeX's `renderMathInElement()` to automatically discover math delimiters `$$...$$` (display mode) or `$..$` (inline mode) and render them into high-quality mathematical representations in the browser.

---

## CLI entry point and hosting

* **Location:** [`src/QaWebli.TerminalUI/Program.cs`](src/QaWebli.TerminalUI/Program.cs) (orchestration entry point), [`src/QaWebli.TerminalUI/Hosting/CliOptions.cs`](src/QaWebli.TerminalUI/Hosting/CliOptions.cs) (CLI options parser)
* **Responsibilities:** `CliOptions.cs` parses the CLI flags (`--port`, `--bind`, `--no-student-ui`, `--https`, `--ngrok`, `--ngrok-authtoken`, `--quiz`, `-g` / `--game`, `--timer`, `-h` / `--help`). `Program.cs` acts as the thin entry point: it resolves the quiz path, instantiates domain entities, registers the singleton `AuditLogger` observer, sets up background local/tunnel networking, and hands execution over to `PresenterController` to start the presenter loop.

---

## ngrok integration (optional)

* **Location:** [`src/QaWebli.TerminalUI/Hosting/NgrokTunnel.cs`](src/QaWebli.TerminalUI/Hosting/NgrokTunnel.cs)
* **Purpose:** When `--ngrok` is set, spawn the **`ngrok`** CLI (`ngrok http <port>`) as a subprocess. If `--ngrok-authtoken` is provided, write a **temporary** minimal YAML config (version + authtoken) so the user's global ngrok config is not overwritten; otherwise rely on `NGROK_AUTHTOKEN` or ngrok's already-configured authtoken.
* **Structured result:** `NgrokTunnel.StartAsync()` returns a `NgrokStartResult` record with `Success`, `PublicUrl`, and `ErrorMessage`. This enables `Program.cs` to fail fast with a descriptive error when `--ngrok` is passed but the tunnel cannot be established.
* **Preflight:** Checks `ngrok` is on PATH by running `ngrok version` before attempting to start a tunnel.
* **Discovery:** Poll `http://127.0.0.1:4040/api/tunnels` (ngrok's local inspector API) until a tunnel matching the requested port exposes `public_url`, prefer HTTPS when available.
* **Shutdown:** On presenter exit, kill the ngrok process tree and delete the temp config file (best-effort).

This is **not** a NuGet dependency: it assumes the `ngrok` binary is installed and on `PATH`.

---

## Game Mode (`-g` / `--game`)

* **CLI flags:** `-g` / `--game` to enable; `--timer <seconds>` for countdown duration (default **10 s**).
* **Domain changes:** `Session` gains `IsGameMode`, `GameTimerSeconds`, `StudentScores` (a `ConcurrentDictionary<string,int>`), and per-question start times tracked by `_questionStartTimes`. `RecordVote` awards time-decayed points on the first correct vote within the timer window: `points = max(0, maxCentiseconds − elapsedCentiseconds)`.
* **`ISessionObserver` extension:** `GameFinishedEvent` added to [`DomainEvents.cs`](src/QaWebli.Core/Domain/Events/DomainEvents.cs); `ISessionObserver` extended with `OnGameFinishedAsync`. All three observers (`AuditLogger`, `PollingHub`, `ConsoleObserver`) implement it.
* **`SessionFacade`:** added `StartGameAsync()` (closes the lobby, starts the first timer) and `FinishGameAsync(scores)` (fires `GameFinishedEvent` with final rankings).
* **Presenter views:**
  - `LobbyConsoleView` — waiting screen with live player count and QR code; shown until the presenter presses Enter.
  - `QuestionConsoleView` — existing question view extended with a live countdown timer line in game mode.
  - `LeaderboardRenderer` — podium layout for top-3 + ranked table for the rest; shown between questions and as the final screen.
* **Engine flow (`AutomatedGameQuizEngine`):**
  1. Wait in lobby until Enter is pressed → `facade.StartGameAsync()`.
  2. Per question: tick countdown, re-render each second; break early if all students voted.
  3. `facade.RevealAnswerAsync()` → show answer for 5 s.
  4. Show intermediate leaderboard for 5 s → advance to next question.
  5. After last question: `facade.FinishGameAsync(scores)` → final leaderboard until Q is pressed.
* **Student web client:** `client.html` updated with per-question score feedback after reveal, rank display, and a final leaderboard screen.
* **Player display names:** `Session.DisplayNames` (`ConcurrentDictionary<string,string>`) maps each connection ID to a chosen name. Students set their name in the lobby via a text input; the client sends a `setName` WebSocket message which `PollingHub` forwards to `Session.SetDisplayName`. `GetDisplayName(id)` falls back to the raw connection ID when no name is set. `LeaderboardRenderer` calls `GetDisplayName` so the terminal podium shows human-readable names.

---

## Libraries and dependencies

### NuGet / framework (compiled into the app)

| Area | Package / framework | Role |
|------|----------------------|------|
| Terminal UI | [Spectre.Console](https://github.com/spectreconsole/spectre.console) | Panels, markup, keyboard-driven presenter output. |
| Arabic shaping | [BidiReshapeSharp](https://www.nuget.org/packages/BidiReshapeSharp/) | Reshapes Arabic text for cleaner terminal display before Spectre renders it. |
| QR code | [QRCoder 1.6.0](https://www.nuget.org/packages/QRCoder/) | Generates ASCII QR codes for the terminal status panel (`AsciiQRCode`). |
| Web host | `Microsoft.AspNetCore.App` ([`FrameworkReference`](src/QaWebli.Infrastructure/QaWebli.Infrastructure.csproj) on `QaWebli.Infrastructure`) | Kestrel + WebSockets for embedded `client.html` and `/hub`. |
| Runtime | **.NET 8** (`net8.0`) | Target framework for all projects. |

### External tools (optional at runtime)

| Tool | Role |
|------|------|
| **ngrok** | TCP tunnel to expose local `--port` on a public HTTPS URL; used only when `--ngrok` is passed or when you run ngrok manually alongside QA-CLI. |

### Student client (embedded asset, not a NuGet package)

| Asset | Notes |
|-------|--------|
| [`client.html`](src/QaWebli.Infrastructure/Server/Resources/client.html) | Embedded resource; uses CDN scripts (e.g. Highlight.js) for code in question text; talks to `/hub` over WebSockets. |

---

## Arabic RTL support

* **Location:** [`src/QaWebli.TerminalUI/Presentation/ArabicHelper.cs`](src/QaWebli.TerminalUI/Presentation/ArabicHelper.cs), [`src/QaWebli.TerminalUI/Presentation/PlainTextRendererStrategy.cs`](src/QaWebli.TerminalUI/Presentation/PlainTextRendererStrategy.cs), [`src/QaWebli.TerminalUI/Presentation/CompositeQuestionRenderer.cs`](src/QaWebli.TerminalUI/Presentation/CompositeQuestionRenderer.cs), [`src/QaWebli.TerminalUI/Presentation/QuestionConsoleView.cs`](src/QaWebli.TerminalUI/Presentation/QuestionConsoleView.cs), [`src/QaWebli.Infrastructure/Server/Resources/client.html`](src/QaWebli.Infrastructure/Server/Resources/client.html)
* **Purpose:** Improve readability for Arabic quiz text in both the terminal presenter and the student browser UI.
* **How it works:**
  1. `ArabicHelper.ContainsArabic(...)` detects Arabic Unicode ranges.
  2. `ArabicHelper.Reshape(...)` uses `BidiReshapeSharp` and prepends an RLM marker so the terminal displays joined glyphs more naturally.
  3. Presenter renderers right-align Arabic question text and option rows while leaving Latin/code content unchanged.
  4. The student web page uses `dir="auto"` and broader font fallbacks so Arabic text can render without a separate page mode.

---

## Testing approach

The automated test suite is intentionally small and centered on the largest user-facing behaviors rather than every helper method.

- `QuizParserTests` validates successful Markdown quiz loading and rejection of invalid quiz input.
- `SessionScoringTests` covers vote counting, score awarding, late answers, vote changes, and display-name behavior in game mode.
- `CliOptionsTests` verifies the user-facing `-g` and `--ngrok` modes are parsed correctly.
- `PresentationPatternTests` adds one small Observer check (`ConsoleObserver`) and one small Strategy check (`PlainTextRendererStrategy`) so the documented patterns are exercised without turning the suite into pattern-only tests.

At the time of writing, the suite contains **16 tests** and is designed to protect the project’s main runtime flows.

---

## Terminal demo

Run:

```bash
dotnet run --project src/QaWebli.TerminalUI -- sample-quiz.md
```

With a custom port and advertised bind address:

```bash
dotnet run --project src/QaWebli.TerminalUI -- sample-quiz.md --port 9090 --bind 0.0.0.0
```

With ngrok (one command):

```bash
dotnet run --project src/QaWebli.TerminalUI -- sample-quiz.md --ngrok
```
