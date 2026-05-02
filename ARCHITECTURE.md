# Architecture & Design Patterns

This project follows Domain-Driven Design (DDD) principles. The **domain model** in `QaWebli.Core` lives under [`src/QaWebli.Core/Domain/`](src/QaWebli.Core/Domain/), with core types grouped in **`Entities`**: [`Option`](src/QaWebli.Core/Domain/Entities/Option.cs), [`Question`](src/QaWebli.Core/Domain/Entities/Question.cs), and [`Quiz`](src/QaWebli.Core/Domain/Entities/Quiz.cs). [`QaWebli.TerminalUI`](src/QaWebli.TerminalUI/) exercises these types in a small console demo; higher-level concerns (live **sessions**, persistence, web UI) will live in other layers as they are implemented.

**Why this layout:** `Option`, `Question`, and `Quiz` are small, invariant-focused types. `Question` holds a single prompt and its choices; `Quiz` aggregates multiple `Question` instances under a title. Builders validate construction so invalid quizzes or questions never reach the rest of the app. Keeping everything under `Domain/Entities` keeps the model easy to reuse without pulling in infrastructure.

---

## Implemented patterns (CPIT-252)

Course-required Gang of Four (GoF) patterns stay **next to the code they construct**, not in a generic `Patterns` folder.

### 1. Creational: Builder pattern (`Question`)

* **Location:** [`src/QaWebli.Core/Domain/Entities/Question.cs`](src/QaWebli.Core/Domain/Entities/Question.cs) — nested type `Question.Builder`
* **Rationale:** `Question` is immutable with a private constructor. The builder sets number, prompt text, and options; `Build()` requires non-empty text and at least one option before returning a valid `Question`.

### 2. Creational: Builder pattern (`Quiz`)

* **Location:** [`src/QaWebli.Core/Domain/Entities/Quiz.cs`](src/QaWebli.Core/Domain/Entities/Quiz.cs) — nested type `Quiz.Builder`
* **Rationale:** A `Quiz` is immutable and composes existing `Question` instances. `Quiz.Builder` collects a title and questions; `Build()` enforces a non-empty title and at least one question. This mirrors the `Question` builder style and keeps aggregate construction consistent.

### 3. Value-oriented option model

* **Location:** [`src/QaWebli.Core/Domain/Entities/Option.cs`](src/QaWebli.Core/Domain/Entities/Option.cs)
* **Rationale:** Each answer choice is an immutable `record` (label, text, correctness). Records provide value equality and a stable shape for lists inside `Question`.

---

## Terminal demo (this commit)

Run:

```bash
dotnet run --project src/QaWebli.TerminalUI
```

The outcome of this commit is this:

```
=== Option (immutable record) ===
  Option { Label = A, Text = Paris, IsCorrect = True }
  Value equality: True

=== Question (built via Builder) ===
  Q1: What is the capital of France?
    A) Paris ✓
    B) London 
    C) Berlin 
  Correct: Paris
Quiz: Geography Quiz (2 questions)

  Q1: What is the capital of France?
    A) Paris ✓
    B) London 
    C) Berlin 

  Q2: What is the capital of Japan?
    A) Seoul 
    B) Beijing 
    C) Tokyo ✓

```
