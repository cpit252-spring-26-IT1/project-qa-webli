# CPIT-252 — Design Patterns itteration one examble quiz

## 1. What is a primary benefit of the Builder pattern?

Separates the construction of a complex object from its representation, allowing the same process to create different representations.

- [x] A) It decouples object construction from the final representation
- [ ] B) It guarantees type safety at compile time
- [ ] C) It reduces memory usage by sharing instances
- [ ] D) It adds thread safety automatically

## 2. What does the Strategy pattern enable?

The Strategy pattern defines a family of algorithms, encapsulates each one, and makes them interchangeable at runtime.

- [ ] A) Inheritance-based behavior sharing
- [x] B) Runtime algorithm substitution without changing the client
- [ ] C) Lazy initialization of expensive objects
- [ ] D) Logging cross-cutting concerns

## 3. What will this C# code output?

```csharp
using System;

var numbers = new int[] { 1, 2, 3, 4, 5 };
var result = 0;
foreach (var n in numbers)
{
    if (n % 2 == 0)
        result += n;
}
Console.WriteLine(result);
```

- [ ] A) 15
- [x] B) 6
- [ ] C) 9
- [ ] D) 0

## 4. Which pattern does this Python code demonstrate?

```python
class Shape:
    def area(self) -> float:
        raise NotImplementedError

class Circle(Shape):
    def __init__(self, radius: float):
        self.radius = radius

    def area(self) -> float:
        return 3.14159 * self.radius ** 2

class Square(Shape):
    def __init__(self, side: float):
        self.side = side

    def area(self) -> float:
        return self.side ** 2

# Client code
shapes = [Circle(5), Square(4)]
for s in shapes:
    print(s.area())
```

- [ ] A) Builder
- [ ] B) Observer
- [x] D) Template Method / Polymorphism
- [ ] C) Singleton

## 5. Which SQL query finds students with more than 3 absences?

```sql
SELECT student_id, COUNT(*) AS absences
FROM attendance
WHERE status = 'absent'
GROUP BY student_id
HAVING COUNT(*) > 3
ORDER BY absences DESC;
```

- [x] A) The one using HAVING to filter after GROUP BY
- [ ] B) The one using WHERE after GROUP BY
- [ ] C) The one using LIMIT to cap results
- [ ] D) None — SQL cannot count absences this way

## 6. What is the key difference between an interface and an abstract class?

- [ ] A) Interfaces can have implementation; abstract classes cannot
- [x] B) A class can implement multiple interfaces but can only extend one abstract class
- [ ] C) Abstract classes are faster at runtime
- [ ] D) Interfaces must be public; abstract classes can be private
