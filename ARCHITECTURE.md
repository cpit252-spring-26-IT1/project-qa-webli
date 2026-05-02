```markdown
# Architecture & Design Patterns

This project follows Domain-Driven Design (DDD) principles. The **domain model** in `QaWebli.Core` lives under [`src/QaWebli.Core/Domain/`](src/QaWebli.Core/Domain/), with core types grouped in **`Entities`**: [`Option`](src/QaWebli.Core/Domain/Entities/Option.cs), [`Question`](src/QaWebli.Core/Domain/Entities/Question.cs), [`Quiz`](src/QaWebli.Core/Domain/Entities/Quiz.cs), and [`Session`](src/QaWebli.Core/Domain/Entities/Session.cs). [`QaWebli.TerminalUI`](src/QaWebli.TerminalUI/) exercises these types in a small console demo; higher-level concerns (persistence, web UI) will live in other layers as they are implemented.

[`QaWebli.Infrastructure`](src/QaWebli.Infrastructure/) handles parsing quiz files from disk. [`QaWebli.TerminalUI`](src/QaWebli.TerminalUI/) exercises the domain in a console demo; higher-level concerns (web UI) will live in other layers as they are implemented.

**Why this layout:** `Option`, `Question`, `Quiz`, and `Session` are small, invariant-focused types. `Question` holds a single prompt and its choices; `Quiz` aggregates multiple `Question` instances under a title; `Session` tracks the live state of a quiz in progress (which question is active, navigation). Builders validate construction so invalid objects never reach the rest of the app. Keeping everything under `Domain/Entities` keeps the model easy to reuse without pulling in infrastructure.

---

## Implemented patterns (CPIT-252)

Course-required Gang of Four (GoF) patterns stay **next to the code they construct**, not in a generic `Patterns` folder.

### 1. Creational: Builder pattern (`Question`)

* **Location:** [`src/QaWebli.Core/Domain/Entities/Question.cs`](src/QaWebli.Core/Domain/Entities/Question.cs) — nested type `Question.Builder`
* **Rationale:** `Question` is immutable with a private constructor. The builder sets number, prompt text, and options; `Build()` requires non-empty text and at least one option before returning a valid `Question`.

### 2. Creational: Builder pattern (`Quiz`)

* **Location:** [`src/QaWebli.Core/Domain/Entities/Quiz.cs`](src/QaWebli.Core/Domain/Entities/Quiz.cs) — nested type `Quiz.Builder`
* **Rationale:** A `Quiz` is immutable and composes existing `Question` instances. `Quiz.Builder` collects a title and questions; `Build()` enforces a non-empty title and at least one question. This mirrors the `Question` builder style and keeps aggregate construction consistent.

### 3. Creational: Builder pattern (`Session`)

* **Location:** [`src/QaWebli.Core/Domain/Entities/Session.cs`](src/QaWebli.Core/Domain/Entities/Session.cs) — nested type `Session.Builder`
* **Rationale:** A `Session` wraps a `Quiz` and tracks which question is currently active. `Session.Builder` auto-generates an 8-character ID, requires a valid `Quiz`, and validates the start index. The private constructor ensures sessions are always created in a valid state.

### 4. Structural: Facade pattern

* **Location:** [`src/QaWebli.Core/Application/Services/SessionFacade.cs`](src/QaWebli.Core/Application/Services/SessionFacade.cs)
* **Rationale:** `Session` has low-level methods like `MoveNext()` and `MovePrevious()`. Without a facade, every caller would need to call these methods directly AND manually fire events. `SessionFacade` wraps this behind `NextQuestionAsync()` and `PreviousQuestionAsync()`, which both update the session AND publish the appropriate event. The caller does one method call instead of two.

### 5. Behavioral: Observer pattern

* **Location:** [`src/QaWebli.Core/Application/Interfaces/ISessionObserver.cs`](src/QaWebli.Core/Application/Interfaces/ISessionObserver.cs) (contract), [`src/QaWebli.Core/Application/Services/SessionFacade.cs`](src/QaWebli.Core/Application/Services/SessionFacade.cs) (subject)
* **Domain Events:** [`src/QaWebli.Core/Domain/Events/DomainEvents.cs`](src/QaWebli.Core/Domain/Events/DomainEvents.cs) — `QuestionChangedEvent`, `VoteReceivedEvent`, `StudentPresenceEvent`
* **Rationale:** When a quiz session changes (question navigated, vote cast, student joins), multiple independent things must happen: the terminal re-renders, the terminal, the audit logger (latter feature) to disk. Without the Observer pattern, `SessionFacade` would need to directly call methods on all three classes, creating tight coupling. With the Observer pattern, the facade simply publishes an event, and each observer reacts independently. The facade does not know who is listening or what they do.


---

## Terminal demo

Run:

```bash
dotnet run --project src/QaWebli.TerminalUI -- sample-quiz.md
```

Interactive session — use ← → arrow keys to navigate questions, Q to quit:

```
  Q1/10 — CPIT 252 — Design Patterns

  Which creational pattern ensures an object is fully configured and valid before it exists?

    A) Builder
    B) Singleton
    C) Factory Method
    D) Prototype

  [Event] Question changed to 2/10

  ← → Navigate | Q Quit
```

Each navigation triggers a `QuestionChangedEvent` that is published to all subscribed observers.
```
```