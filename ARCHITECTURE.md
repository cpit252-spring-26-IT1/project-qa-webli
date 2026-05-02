```markdown
# Architecture & Design Patterns

This project follows Domain-Driven Design (DDD) principles. The **domain model** in `QaWebli.Core` lives under [`src/QaWebli.Core/Domain/`](src/QaWebli.Core/Domain/), with core types grouped in **`Entities`**: [`Option`](src/QaWebli.Core/Domain/Entities/Option.cs), [`Question`](src/QaWebli.Core/Domain/Entities/Question.cs), [`Quiz`](src/QaWebli.Core/Domain/Entities/Quiz.cs), and [`Session`](src/QaWebli.Core/Domain/Entities/Session.cs). [`QaWebli.TerminalUI`](src/QaWebli.TerminalUI/) exercises these types in a small console demo; higher-level concerns (persistence, web UI) will live in other layers as they are implemented.

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

### 4. Value-oriented option model

* **Location:** [`src/QaWebli.Core/Domain/Entities/Option.cs`](src/QaWebli.Core/Domain/Entities/Option.cs)
* **Rationale:** Each answer choice is an immutable `record` (label, text, correctness). Records provide value equality and a stable shape for lists inside `Question`.

---

## Terminal demo (this commit)

Run:

```bash
dotnet run --project src/QaWebli.TerminalUI -- sample-quiz.md
```

The outcome of this commit is this:

```
Quiz: CPIT 252 — Design Patterns (10 questions)

  Q1: Which creational pattern ensures an object is fully configured and valid before it exists?
    A) Builder ✓
    B) Singleton 
    C) Factory Method 
    D) Prototype 

  Q2: Which pattern restricts a class to exactly one instance with global access?
    A) Builder 
    B) Singleton ✓
    C) Strategy 
    D) Observer 

  Q3: Which behavioral pattern lets one object notify multiple dependents without knowing who they are?
    A) Strategy 
    B) Facade 
    C) Observer ✓
    D) Composite 

  Q4: Which structural pattern provides a unified interface to a set of interfaces in a subsystem?
    A) Adapter 
    B) Facade ✓
    C) Decorator 
    D) Proxy 

  Q5: Which pattern lets you treat a group of objects the same way you treat a single instance?
    A) Strategy 
    B) Observer 
    C) Composite ✓
    D) Bridge 

  Q6: Which pattern defines a family of algorithms and makes them interchangeable at runtime?
    A) Strategy ✓
    B) Observer 
    C) Facade 
    D) Singleton 

  Q7: Which pattern delegates object creation to a subclass instead of calling a constructor directly?
    A) Builder 
    B) Singleton 
    C) Factory Method ✓
    D) Prototype 

  Q8: In the Observer pattern, what is the object that sends notifications called?
    A) Listener 
    B) Subject ✓
    C) Strategy 
    D) Client 

  Q9: What problem does the Builder pattern solve that a constructor alone cannot?
    A) Preventing multiple instances 
    B) Constructing objects with many optional or required parameters step by step ✓
    C) Notifying observers of state changes 
    D) Hiding a complex subsystem 

  Q10: Which principle is best demonstrated by the Strategy pattern?
    A) Liskov Substitution 
    B) Open/Closed — open for extension, closed for modification ✓
    C) Single Responsibility 
    D) Dependency Inversion 
```