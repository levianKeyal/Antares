# Arithmetic Practice Generator (Unity / C#)

## Overview

This project implements a pedagogically controlled arithmetic exercise generator designed for educational environments (high school initially, scalable to secondary and primary levels).

It supports:

* Addition
* Subtraction
* Multiplication
* Division
* Exact validation
* Truncated validation
* Ceiling validation
* Mixed validation mode
* Configurable decimal precision
* Configurable operand sign rules

The generator ensures all exercises are:

* mathematically correct
* readable for students
* consistent with validation mode
* free of floating-point artifacts
* scalable across education levels

---

# Features

## Supported Operations

* Addition
* Subtraction
* Multiplication
* Division

## Supported Validation Modes

| Mode      | Description                                 |
| --------- | ------------------------------------------- |
| ExactOnly | User must enter the exact result            |
| Truncated | User must truncate to selected decimals     |
| Ceil      | User must round upward to selected decimals |
| All       | Any valid transformation accepted           |

## Supported Sign Modes

| Mode         | Behavior                  |
| ------------ | ------------------------- |
| PositiveOnly | Both operands positive    |
| NegativeOnly | Both operands negative    |
| Mixed        | Operand signs independent |

---

# Pedagogical Guarantees

The generator enforces:

* exact arithmetic correctness
* maximum 6 decimal digits
* readable operands
* meaningful truncation exercises
* meaningful ceiling exercises
* no trailing-zero decimals
* no floating precision artifacts

Example rejected:

```
5.120 ÷ 0.400
```

Example accepted:

```
5.125 ÷ 0.375
```

---

# Generation Strategies

Different operations use different generation directions:

| Operation      | Strategy                    |
| -------------- | --------------------------- |
| Addition       | result → operands           |
| Subtraction    | result → operands           |
| Multiplication | operands → result           |
| Division       | result → divisor → dividend |

This ensures exact decimal consistency.

---

# Exact Division Configuration

Exact division readability can be controlled from Unity Inspector:

```
GameSettings.maxDivisionExactOperandDecimals
```

Example:

```
Primary school → 0
Secondary school → 1–2
High school → 3+
```

---

# Validation Mode Precision Rule

For **Truncated** and **Ceil**:

```
Generated result decimals ≥ selectedDecimals + 1
```

This guarantees the transformation actually modifies the number.

---

# Designed For Educational Scaling

Current target:

```
High school
```

Expandable to:

```
Secondary school
Primary school
```

---

# Technical Structure

Core scripts:

```
MathExercise.cs
MathValidator.cs
GameSettings.cs
RoundUpController.cs
```

Responsibilities:

| Script            | Role                       |
| ----------------- | -------------------------- |
| MathExercise      | exercise generation engine |
| MathValidator     | answer validation          |
| GameSettings      | runtime configuration      |
| RoundUpController | UI interaction logic       |

---

# Future Extensions

Planned scalability:

* difficulty progression engine
* adaptive learning mode
* level-based operand ranges
* curriculum presets
* student performance tracking

---

# License

Internal academic project (adjust as needed).
