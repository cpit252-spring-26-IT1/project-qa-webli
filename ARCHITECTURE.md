# Architecture & Design Patterns

This project follows Domain-Driven Design (DDD) principles. The **domain model** in `QaWebli.Core` lives under [`src/QaWebli.Core/Domain/`](src/QaWebli.Core/Domain/), with core types grouped in **`Entities`**: [`Option`](src/QaWebli.Core/Domain/Entities/Option.cs) and [`Question`](src/QaWebli.Core/Domain/Entities/Question.cs). Higher-level concepts (for example live **sessions**, persistence, or UI) will live in other layers—[`QaWebli.Infrastructure`](src/QaWebli.Infrastructure/) and [`QaWebli.TerminalUI`](src/QaWebli.TerminalUI/)—as they are implemented.

**Why this layout:** `Option` and `Question` are modeled as small, focused types with clear invariants. Keeping them together under `Domain/Entities` keeps the core quiz model easy to find and reuse from Terminal UI or future web UI without pulling in infrastructure. Feature-specific folders (e.g. sessions, imports) can be added beside `Entities` when those bounded contexts grow, without scattering entity definitions.

---

## Implemented patterns (CPIT-252)

Course-required Gang of Four (GoF) patterns stay **next to the code they construct**, not in a generic `Patterns` folder.

### 1. Creational: Builder pattern

* **Location:** [`src/QaWebli.Core/Domain/Entities/Question.cs`](src/QaWebli.Core/Domain/Entities/Question.cs) — nested type `Question.Builder`
* **Rationale:** `Question` is immutable and exposes only a private constructor. The builder gathers number, prompt text, and options, then `Build()` enforces rules (non-empty text, at least one option) before returning a valid `Question`. That keeps construction errors in one place and prevents half-built questions from leaking into the rest of the app.

### 2. Value-oriented option model

* **Location:** [`src/QaWebli.Core/Domain/Entities/Option.cs`](src/QaWebli.Core/Domain/Entities/Option.cs)
* **Rationale:** Each answer choice is an immutable `record` (label, text, correctness). Records give value equality and a stable shape for lists inside `Question`.
