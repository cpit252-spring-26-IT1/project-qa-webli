# Architecture & Design Patterns

This project follows Domain-Driven Design (DDD) principles. The **domain model** in `QaWebli.Core` lives under [`src/QaWebli.Core/Domain/`](src/QaWebli.Core/Domain/), with core types grouped in **`Entities`**: [`Option`](src/QaWebli.Core/Domain/Entities/Option.cs), [`Question`](src/QaWebli.Core/Domain/Entities/Question.cs), [`Quiz`](src/QaWebli.Core/Domain/Entities/Quiz.cs), and [`Session`](src/QaWebli.Core/Domain/Entities/Session.cs). [`QaWebli.TerminalUI`](src/QaWebli.TerminalUI/) exercises these types in a small console demo; higher-level concerns (persistence, web UI) will live in other layers as they are implemented.

[`QaWebli.Infrastructure`](src/QaWebli.Infrastructure/) handles parsing quiz files from disk (splitting question text into `ContentBlock` segments via `ContentBlockParser`), writing audit logs, and hosting an embedded web server so students can join from their phones. [`QaWebli.TerminalUI`](src/QaWebli.TerminalUI/) exercises the domain in a Spectre.Console terminal demo with the Strategy + Composite patterns for rendering; higher-level concerns (web UI) will live in other layers as they are implemented.

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
* **Rationale:** `Session` has low-level methods like `TryMoveNext()` and `TryMovePrevious()`. Without a facade, every caller would need to call these methods directly AND manually fire events. `SessionFacade` wraps this behind `NextQuestionAsync()` and `PreviousQuestionAsync()`, which both update the session AND publish the appropriate event. It also exposes `AddStudentAsync()` / `RemoveStudentAsync()` so the web server can register student connections through the same event pipeline. The caller does one method call instead of two.

### 5. Behavioral: Observer pattern

* **Location:** [`src/QaWebli.Core/Application/Interfaces/ISessionObserver.cs`](src/QaWebli.Core/Application/Interfaces/ISessionObserver.cs) (contract), [`src/QaWebli.Core/Application/Services/SessionFacade.cs`](src/QaWebli.Core/Application/Services/SessionFacade.cs) (subject)
* **Concrete Observers:** [`AuditLogger.cs`](src/QaWebli.Infrastructure/Logging/AuditLogger.cs), [`PollingHub.cs`](src/QaWebli.Infrastructure/Server/PollingHub.cs)
* **Domain Events:** [`src/QaWebli.Core/Domain/Events/DomainEvents.cs`](src/QaWebli.Core/Domain/Events/DomainEvents.cs) — `QuestionChangedEvent`, `VoteReceivedEvent`, `StudentPresenceEvent`
* **Rationale:** When a quiz session changes (question navigated, vote cast, student joins), multiple independent things must happen: the audit logger writes the event to disk, the web hub broadcasts updates to connected students. Without the Observer pattern, `SessionFacade` would need to directly call methods on each class, creating tight coupling. With the Observer pattern, the facade simply publishes an event, and each observer reacts independently. The facade does not know who is listening or what they do.

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
* **Rationale:** Different types of content require different rendering logic. Plain text just needs escaping and display; code blocks need their own panel with a language tag. The Strategy pattern encapsulates each rendering algorithm in its own class, so the caller just invokes `Render(block)` without knowing which strategy is active. Adding a new content type means adding a new strategy class — no existing code changes.
* **How it works step-by-step:**
  1. `IContentRendererStrategy` defines two methods: `CanRender(ContentBlock)` returns `true` if this strategy handles the given block type, and `Render(ContentBlock)` returns a Spectre.Console `IRenderable`.
  2. `PlainTextRendererStrategy` handles `ContentBlock.PlainText` blocks, rendering them as escaped white Spectre markup.
  3. `CodeBlockRendererStrategy` handles `ContentBlock.CodeBlock` blocks, rendering them in a grey rounded panel with an optional language tag header.

### 8. Structural: Composite pattern

* **Location:** [`src/QaWebli.TerminalUI/Presentation/CompositeQuestionRenderer.cs`](src/QaWebli.TerminalUI/Presentation/CompositeQuestionRenderer.cs)
* **Rationale:** A question can contain multiple content blocks in any order: plain text, then a code block, then more text. The Composite pattern lets us treat the collection of renderers as a single unit. `Program.cs` just calls `renderer.RenderQuestion(question)` and gets back one `IRenderable` — it does not need to know how many blocks exist or what types they are.
* **How it works step-by-step:**
  1. `CompositeQuestionRenderer` is constructed with a list of `IContentRendererStrategy` objects.
  2. When `RenderQuestion(question)` is called, it iterates over every `ContentBlock` in the question.
  3. For each block, it finds the first strategy whose `CanRender(block)` returns `true`.
  4. It calls that strategy's `Render(block)` method and collects the result.
  5. All results are combined into a single `Rows` renderable and returned.

### 9. Creational: Factory Method pattern

* **Location:** [`src/QaWebli.TerminalUI/Presentation/CompositeQuestionRenderer.cs`](src/QaWebli.TerminalUI/Presentation/CompositeQuestionRenderer.cs) — the `Default()` static method.
* **Rationale:** Creating a `CompositeQuestionRenderer` requires knowing which strategies exist and in what order to check them. Instead of forcing `Program.cs` to manually construct and inject all strategies, the `Default()` factory method encapsulates this knowledge and returns a fully configured renderer.
* **How it works step-by-step:**
  1. `Program.cs` calls `CompositeQuestionRenderer.Default()`.
  2. The factory method creates instances of `PlainTextRendererStrategy` and `CodeBlockRendererStrategy` and passes them to the constructor.
  3. The caller receives a fully configured renderer without needing to know what strategies exist internally.

---

## Web Server (Kestrel + WebSocket)

Not a GoF pattern — this is an infrastructure feature. Documented here for completeness.

* **Location:** [`src/QaWebli.Infrastructure/Server/WebServer.cs`](src/QaWebli.Infrastructure/Server/WebServer.cs), [`src/QaWebli.Infrastructure/Server/PollingHub.cs`](src/QaWebli.Infrastructure/Server/PollingHub.cs), [`src/QaWebli.Infrastructure/Server/Resources/client.html`](src/QaWebli.Infrastructure/Server/Resources/client.html)
* **How it works:**
  1. `WebServer` starts Kestrel on the presenter's local IP and a configurable port (default 8080).
  2. When a student opens the URL in their browser, Kestrel serves the embedded `client.html` — a self-contained page with HTML + CSS + JS in one file.
  3. The browser opens a WebSocket to `/hub`. The `PollingHub` registers the connection and calls `_manager.AddStudentAsync()`.
  4. `PollingHub` implements `ISessionObserver`, so when the presenter navigates to a new question, the hub receives the `QuestionChangedEvent` and broadcasts the new question as JSON to every connected student.
  5. When the presenter quits, `PollingHub.CloseAllAsync()` sends a "session_ended" message to every student and closes their WebSocket connections gracefully.

---

## Terminal demo

Run:

```bash
dotnet run --project src/QaWebli.TerminalUI -- sample-quiz.md
```

Interactive session — use ← → arrow keys to navigate questions, Q to quit. Questions are rendered through Spectre.Console using the Strategy + Composite patterns:

**Plain text question:**

```
╭─Q1 / 10──────────────────────────────────────────────────────────────╮
│ Which creational pattern ensures an object is fully configured and   │
│ valid before it exists?                                              │
╰──────────────────────────────────────────────────────────────────────╯

    A  Builder ✓          ← green (correct answer)
    B  Singleton          ← white
    C  Factory Method     ← white
    D  Prototype          ← white

  ← → Navigate | Q Quit   |   0 student(s)  http://192.168.1.5:8080
```

**Question with a code block:**

```
╭─Q3 / 6─────────────────────────────────────────╮
│ What will this C# code output?                 │
│ ╭─────────────────────────────────────csharp─╮ │
│ │ using System;                              │ │
│ │                                            │ │
│ │ var numbers = new int[] { 1, 2, 3, 4, 5 }; │ │
│ │ var result = 0;                            │ │
│ │ foreach (var n in numbers)                 │ │
│ │ {                                          │ │
│ │     if (n % 2 == 0)                        │ │
│ │         result += n;                       │ │
│ │ }                                          │ │
│ │ Console.WriteLine(result);                 │ │
│ ╰────────────────────────────────────────────╯ │
╰────────────────────────────────────────────────╯

    A  15
    B  6 ✓
    C  9
    D  0

  ← → Navigate | Q Quit   |   0 student(s)  http://192.168.1.5:8080
```

Colors: cyan outer panel border, cyan bold question number, dim total count, grey inner code panel border with language tag, green ✓ on correct option, bold option labels, dim navigation hint. Each navigation triggers a `QuestionChangedEvent` published to all subscribed observers. The `AuditLogger` singleton writes each event to `logs/qa-session-YYYYMMDD-HHmmss.log`. Students can open the server URL on their phone to see questions update live.
