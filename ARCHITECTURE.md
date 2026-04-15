# Architecture & Design Patterns

This project follows Domain-Driven Design (DDD) principles. We organize code by **Feature** (e.g., Sessions, Questions) rather than by **Type** (e.g., Models, Builders, Interfaces). 

**Why we use this approach:**
In traditional "Folder-by-Type" structures, changing how a Session works requires jumping between a `Models` folder and a `Builders` folder. By grouping files by their domain (`src/QaWebli.Core/Domain/Sessions/`), all related logic is kept in one place. This creates high cohesion, makes the codebase easier to navigate, and aligns with modern C# best practices.

---

## Implemented Patterns (CPIT-252)

To satisfy course requirements without breaking the DDD structure, Gang of Four (GoF) design patterns are placed directly inside their relevant domain folders instead of an isolated "Patterns" directory.

### 1. Creational: Builder Pattern
* **Location:** [`src/QaWebli.Core/Domain/Sessions/SessionBuilder.cs`](src/QaWebli.Core/Domain/Sessions/SessionBuilder.cs)
* **Rationale:** While modern C# can easily build basic objects, creating a live polling `Session` requires specific business rules. The system must generate a unique `JoinCode` and correctly stamp the UTC start time. The Builder pattern handles this messy setup logic behind the scenes. This ensures that every time a session is created, it is completely valid and secure, preventing developers from accidentally launching a broken session.