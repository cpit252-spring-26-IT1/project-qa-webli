# Architecture & Design Patterns

This project follows Domain-Driven Design (DDD) principles. The **domain model** in `QaWebli.Core` lives under [`src/QaWebli.Core/Domain/`](src/QaWebli.Core/Domain/), with core types grouped in **`Entities`**: [`Option`](src/QaWebli.Core/Domain/Entities/Option.cs), [`Question`](src/QaWebli.Core/Domain/Entities/Question.cs), [`Quiz`](src/QaWebli.Core/Domain/Entities/Quiz.cs), and [`Session`](src/QaWebli.Core/Domain/Entities/Session.cs). [`QaWebli.TerminalUI`](src/QaWebli.TerminalUI/) exercises these types in a console-first CLI presenter.

[`QaWebli.Infrastructure`](src/QaWebli.Infrastructure/) handles parsing quiz files from disk (splitting question text into `ContentBlock` segments via `ContentBlockParser`), writing audit logs, and hosting an embedded join page + WebSocket hub so students can join from their phones. [`QaWebli.TerminalUI`](src/QaWebli.TerminalUI/) exercises the domain in a Spectre.Console terminal presenter with the Strategy + Composite patterns for rendering.

**Why this layout:** `Option`, `Question`, `Quiz`, and `Session` are small, invariant-focused types. `Question` holds a single prompt and its choices; `Quiz` aggregates multiple `Question` instances under a title; `Session` tracks the live state of a quiz in progress (which question is active, who is connected, navigation). Builders validate construction so invalid objects never reach the rest of the app. Keeping everything under `Domain/Entities` keeps the model easy to reuse without pulling in infrastructure.

**Value objects:** `ContentBlock` is an abstract record with two sealed subtypes (`PlainText` and `CodeBlock`) forming a discriminated union. A question's body is a sequence of these blocks, parsed from Markdown by `ContentBlockParser` so the terminal can render each segment differently.

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

* **Location:** [`src/QaWebli.Core/Application/Interfaces/ISessionObserver.cs`](src/QaWebli.Core/Application/Interfaces/ISessionObserver.cs) (contract), [`src/QaWebli.Core/Application/Services/SessionFacade.cs`](src/QaWebli.Core/Application/Services/SessionFacade.cs) (subject)
* **Concrete Observers:** [`AuditLogger.cs`](src/QaWebli.Infrastructure/Logging/AuditLogger.cs), [`PollingHub.cs`](src/QaWebli.Infrastructure/Server/PollingHub.cs), [`ConsoleObserver.cs`](src/QaWebli.TerminalUI/Presentation/ConsoleObserver.cs)
* **Domain Events:** [`src/QaWebli.Core/Domain/Events/DomainEvents.cs`](src/QaWebli.Core/Domain/Events/DomainEvents.cs) — `QuestionChangedEvent`, `VoteReceivedEvent`, `StudentPresenceEvent`
* **Rationale:** When a quiz session changes (question navigated, vote cast, student joins), multiple independent things must happen: the audit logger writes the event to disk, the join hub broadcasts updates to connected students, and the `ConsoleObserver` triggers a live terminal UI re-render for the instructor. Without the Observer pattern, `SessionFacade` would need to directly call methods on each class, creating tight coupling. With the Observer pattern, the facade simply publishes an event, and each observer reacts independently. The facade does not know who is listening or what they do.
* **How it works step-by-step:**
  1. **The Contract:** `ISessionObserver` defines the methods each observer must implement (`OnQuestionChangedAsync`, `OnVoteReceivedAsync`, `OnStudentPresenceChangedAsync`).
  2. **The Subject:** `SessionFacade` maintains a private list of observers. `Program.cs` calls `facade.Subscribe(observer)` at startup to register them.
  3. **Event Firing:** When the domain state changes (e.g., `facade.NextQuestionAsync()`), the facade calls its internal `PublishAsync()` method, which iterates over all subscribed observers and fires the event.
  4. **Observer 1 (`AuditLogger`):** Receives the event and writes a thread-safe log entry to disk so there is a persistent record.
  5. **Observer 2 (`PollingHub`):** Receives the event and pushes the new state over WebSockets to all connected student phones.
  6. **Observer 3 (`ConsoleObserver`):** Receives the event and triggers a Spectre.Console UI re-render in the terminal so the instructor immediately sees the new vote counts or question state.

### 6. Creational: Singleton pattern

* **Location:** [`src/QaWebli.Infrastructure/Logging/AuditLogger.cs`](src/QaWebli.Infrastructure/Logging/AuditLogger.cs)
* **Rationale:** The audit logger writes to a single file on disk. If multiple instances existed, they would fight over the file handle and corrupt the log. The Singleton pattern guarantees exactly one `AuditLogger` instance exists for the entire lifetime of the application.
* **How it works step-by-step:**
  1. The class has a `private` constructor, so no external code can call `new AuditLogger()`.
  2. A `private static readonly Lazy<AuditLogger>` field holds the single instance. `Lazy<T>` guarantees thread-safe initialization — even if two threads access `Instance` at the exact same time, the constructor runs only once.
  3. The public `static AuditLogger Instance` property exposes the single instance.
  4. The constructor automatically creates a `logs/` directory under the solution root and opens a timestamped log file (e.g., `logs/qa-session-20260502-091400.log`).
  5. All write operations use a `lock (_lock)` block to ensure thread safety when multiple observers fire events concurrently.

### 7. Behavioral: Strategy pattern

* **Location:** [`src/QaWebli.TerminalUI/Presentation/IContentRendererStrategy.cs`](src/QaWebli.TerminalUI/Presentation/IContentRendererStrategy.cs) (interface), [`src/QaWebli.TerminalUI/Presentation/PlainTextRendererStrategy.cs`](src/QaWebli.TerminalUI/Presentation/PlainTextRendererStrategy.cs) and [`src/QaWebli.TerminalUI/Presentation/CodeBlockRendererStrategy.cs`](src/QaWebli.TerminalUI/Presentation/CodeBlockRendererStrategy.cs) (concrete)
* **Rationale:** Different types of content require different rendering logic. Plain text just needs escaping and display; code blocks need syntax highlighting and their own panel with a language tag. The Strategy pattern encapsulates each rendering algorithm in its own class, so the caller just invokes `Render(block)` without knowing which strategy is active. Adding a new content type means adding a new strategy class — no existing code changes (Open/Closed Principle).
* **How it works step-by-step:**
  1. `IContentRendererStrategy` defines two methods: `CanRender(ContentBlock)` returns `true` if this strategy handles the given block type, and `Render(ContentBlock)` returns a Spectre.Console `IRenderable`.
  2. `PlainTextRendererStrategy` handles `ContentBlock.PlainText` blocks, rendering them as escaped white Spectre markup.
  3. `CodeBlockRendererStrategy` handles `ContentBlock.CodeBlock` blocks. Internally it delegates to the **escape-first syntax highlighting pipeline** (under `Presentation/SyntaxHighlighting/`) which colours keywords, strings, comments, numbers, and type names per language. The strategy then wraps the highlighted output in a grey rounded panel with a language tag header. This demonstrates how the Strategy pattern isolates complex rendering logic — the caller (`CompositeQuestionRenderer`) sees only `CanRender` and `Render`, while the entire syntax highlighting pipeline is encapsulated inside the code block strategy.

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
  2. The factory method creates instances of `PlainTextRendererStrategy` and `CodeBlockRendererStrategy` and passes them to the constructor.
  3. The caller receives a fully configured renderer without needing to know what strategies exist internally.

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
  1. `WebServer` starts Kestrel (listening on the chosen port; default 8080).
  2. When a student opens the URL in their browser, Kestrel serves the embedded `client.html` — a self-contained page with HTML + CSS + JS in one file.
  3. The browser opens a WebSocket to `/hub`. The `PollingHub` registers the connection and calls `_manager.AddStudentAsync()`.
  4. `PollingHub` implements `ISessionObserver`, so when the presenter navigates to a new question, the hub receives the `QuestionChangedEvent` and broadcasts the new question as JSON to every connected student.
  5. When the presenter quits, `PollingHub.CloseAllAsync()` sends a "session_ended" message to every student and closes their WebSocket connections gracefully.

---

## CLI entry point and hosting

* **Location:** [`src/QaWebli.TerminalUI/Program.cs`](src/QaWebli.TerminalUI/Program.cs)
* **Responsibilities:** Parses CLI flags (`--port`, `--bind`, `--no-student-ui`, `--https`, `--ngrok`, `--ngrok-authtoken`, `--quiz`, `-h` / `--help`), resolves the quiz path, wires `SessionFacade` + observers, starts or skips `WebServer` / `PollingHub`, optionally starts ngrok (strict mode — fails fast on error), renders the presenter with syntax-highlighted code and QR status panel, and drives the Spectre.Console presenter loop.

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

## Libraries and dependencies

### NuGet / framework (compiled into the app)

| Area | Package / framework | Role |
|------|----------------------|------|
| Terminal UI | [Spectre.Console](https://github.com/spectreconsole/spectre.console) | Panels, markup, keyboard-driven presenter output. |
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

Interactive session — use ← → arrow keys to navigate questions, Q to quit. Questions are rendered through Spectre.Console using the Strategy + Composite patterns:

**Plain text question:**

```
╭─Q1 / 10──────────────────────────────────────────────────────────────╮
│ Which creational pattern ensures an object is fully configured and   │
│ valid before it exists?                                              │
╰──────────────────────────────────────────────────────────────────────╯

    A  Builder             #####---------------  1 (25%)
    B  Singleton           ####################  4 (75%)
    C  Factory Method      --------------------  0 (0%)
    D  Prototype           --------------------  0 (0%)

← → Navigate | Q Quit                                           ██████████████  ██  ██
                                                                ██          ██  ████  ██
3 student(s)  |  5 vote(s)                                      ██  ██████  ██    ████
http://192.168.1.5:8080  ← scan to join                         ██  ██████  ██  ██  ██
                                                                ██████████████  ██  ██
```

**Question with syntax-highlighted code block:**

```
╭─Q3 / 6─────────────────────────────────────────╮
│ What will this C# code output?                 │
│ ╭─────────────────────────────────────csharp─╮ │
│ │ using System;                              │ │  ← keyword: blue, type: cyan
│ │                                            │ │
│ │ var numbers = new int[] { 1, 2, 3, 4, 5 };│ │  ← numbers: green, keywords: blue
│ │ var result = 0;                            │ │
│ │ foreach (var n in numbers)                 │ │
│ │ {                                          │ │
│ │     if (n % 2 == 0)                        │ │
│ │         result += n;                       │ │
│ │ }                                          │ │
│ │ Console.WriteLine(result);                 │ │  ← type: cyan
│ ╰────────────────────────────────────────────╯ │
╰────────────────────────────────────────────────╯

    A  15                  --------------------  0 (0%)
    B  6                   ####################  3 (100%)
    C  9                   --------------------  0 (0%)
    D  0                   --------------------  0 (0%)

← → Navigate | Q Quit                                           (QR pinned right)
3 student(s)  |  3 vote(s)
https://example.ngrok-free.dev  ← scan to join
```

The footer uses a transparent, borderless `Grid` (no panel borders). The left column shows navigation controls, student count, vote count, and the join URL. The right column pins the QR code to the right edge of the terminal. When `--ngrok` is active, the QR encodes the public HTTPS URL; otherwise it encodes the LAN address.

Colors: cyan outer panel border, cyan bold question number, dim total count, grey inner code panel border with language tag, **blue keywords, gold strings, grey italic comments, green numbers, cyan type names** inside code blocks, coloured vote bars per option, bold option labels, dim navigation hint. Each navigation triggers a `QuestionChangedEvent` published to all subscribed observers. The `AuditLogger` singleton writes each event to `logs/qa-session-YYYYMMDD-HHmmss.log`. Students can open the server URL on their phone (or scan the QR code) to see questions update live.
