### Step 1: Create `docs/quiz-format.md`

```bash
mkdir -p docs
```

```markdown
# Quiz Markdown Format

Qa-webli reads quiz files written in standard Markdown. Here's how to format them.

## Title

Start the file with a level-1 heading — this becomes the quiz title.

```markdown
# CPIT 252 — Design Patterns
```

## Questions

Each question is a level-2 heading starting with `Question` followed by a number and colon.

```markdown
## Question 1: Which pattern ensures an object is valid before it exists?
```

## Options

List options under each question using `- [ ]` (wrong) or `- [x]` (correct). Use `A)`, `B)`, `C)`, `D)` prefix for labels.

```markdown
- [x] A) Builder
- [ ] B) Singleton
- [ ] C) Factory Method
- [ ] D) Prototype
```

## Full Example

```markdown
# My Quiz Title

## Question 1: What is the capital of France?
- [x] A) Paris
- [ ] B) London
- [ ] C) Berlin

## Question 2: What is the capital of Japan?
- [ ] A) Seoul
- [ ] B) Beijing
- [x] C) Tokyo
```

## Rules

- **One title** per file (the `#` heading)
- **At least one question** or the parser will reject it
- **At least one option** per question
- **Exactly one correct option** per question (mark with `[x]`)
- Option labels should be `A)`, `B)`, `C)`, `D)` etc.
```

### Step 2: Update README.md — replace just the Usage section

```markdown
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
```
