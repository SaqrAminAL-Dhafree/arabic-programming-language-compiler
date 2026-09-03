# Arabic Programming Language Compiler

A custom Arabic programming language and compiler built with **C#**, **.NET 6**, and **ANTLR4**.

The project demonstrates the design and implementation of a programming language with Arabic keywords, data types, control structures, procedures, collections, records, semantic analysis, symbol-table management, and dynamic CIL code generation for execution on the .NET runtime.

---

## Overview

This project was developed to explore the internal architecture of programming languages and compilers by designing a custom programming language whose syntax is based on the Arabic language.

Programs are written using Arabic keywords and identifiers, then processed through a compiler pipeline that transforms the source code into executable **CIL/IL instructions**.

The compiler performs several major stages:

```text
Arabic Source Code
        ↓
ANTLR Lexer
        ↓
Tokens
        ↓
ANTLR Parser
        ↓
Parse Tree
        ↓
Semantic Analysis
        ↓
Symbol Table
        ↓
CIL / IL Code Generation
        ↓
.NET Runtime / JIT
        ↓
Program Execution
```

This architecture provides a practical implementation of the fundamental stages involved in compiler construction.

---

## Language Example

A program can be written using Arabic keywords and identifiers.

Example:

```text
برنامج مثال;

متغير
    العدد : صحيح;

ابدأ
    العدد := 10;
    اطبع العدد;
.
```

The language supports Arabic identifiers and keywords, allowing source code to be written naturally using Arabic syntax.

---

## Key Features

### Language Design

* Arabic programming keywords
* Arabic identifiers
* Variables
* Constants
* Primitive data types
* Lists
* Records
* Procedures
* Parameters passed by value
* Parameters passed by reference
* Arithmetic expressions
* Logical expressions
* Relational expressions
* Assignments
* Conditional statements
* Repetition and loop constructs
* Input and output operations
* Nested instruction blocks

### Compiler Pipeline

* Lexical analysis using ANTLR4
* Syntax analysis using an ANTLR-generated parser
* Parse tree generation
* Semantic analysis
* Symbol table management
* Type and symbol validation
* Custom visitor-based compiler logic
* Dynamic CIL/IL code generation
* Runtime execution through .NET

### Developer Interface

The project also includes a WPF-based graphical interface providing tools for inspecting different compiler stages, including:

* Tokens
* Parse Tree
* Symbol Table
* Translation / Compilation Stages

---

## Compiler Architecture

The compiler is organized into several major stages.

### 1. Source Code

The developer writes a program using the custom Arabic programming language.

```text
برنامج ...
```

The source code is provided to the compiler as input.

---

### 2. Lexical Analysis

The language grammar is defined using **ANTLR4**.

ANTLR analyzes the source code and converts it into a stream of tokens.

For example, Arabic keywords such as:

```text
برنامج
متغير
صحيح
اذا
فان
والا
اطبع
اقرا
```

are recognized according to the language grammar.

---

### 3. Syntax Analysis

The generated ANTLR parser analyzes the token stream according to the grammar defined in:

```text
ArabicCompiler/Expr.g4
```

The parser constructs a **Parse Tree** representing the syntactic structure of the program.

---

### 4. Semantic Analysis

After syntactic analysis, the compiler performs semantic processing using custom visitor logic.

This stage is responsible for analyzing the meaning and validity of program constructs beyond grammar matching.

The project contains a dedicated:

```text
SemanticAnalyzerVisitor
```

for this stage.

---

### 5. Symbol Table

The compiler maintains information about identifiers through a custom symbol-table implementation.

The project includes:

```text
Symbol.cs
SymbolTable.cs
```

The symbol table is used to maintain information about declared program elements during compilation.

---

### 6. Code Generation

After analysis, the compiler generates **CIL (Common Intermediate Language)** instructions dynamically using .NET facilities such as:

```text
System.Reflection.Emit
```

The generated instructions can include operations for:

* Loading values
* Arithmetic operations
* Logical operations
* Comparisons
* Branching
* Method calls
* Returning from methods
* Console output

---

### 7. .NET Runtime Execution

The generated CIL is placed into a dynamic .NET assembly.

The program is then executed through the .NET runtime.

The .NET runtime is responsible for the subsequent execution process, including JIT compilation of the generated IL into machine code when required.

Therefore, the compiler's responsibility is to generate CIL/IL, while the .NET runtime and JIT handle the lower-level execution.

---

## Technology Stack

| Technology      | Purpose                             |
| --------------- | ----------------------------------- |
| C#              | Compiler implementation             |
| .NET 6          | Runtime and application platform    |
| ANTLR4          | Lexer and parser generation         |
| Reflection.Emit | Dynamic CIL/IL generation           |
| Lokad.ILPack    | .NET assembly-related functionality |
| WPF             | Compiler graphical interface        |
| Visual Studio   | Development environment             |

---

## Project Structure

```text
COMPILER
│
├── ArabicCompiler
│   ├── .antlr
│   ├── Generated
│   ├── Expr.g4
│   ├── ArabicVisitor.cs
│   ├── CodeGenerator.cs
│   ├── CompilerService.cs
│   ├── SemanticAnalyzerVisitor.cs
│   ├── Symbol.cs
│   ├── SymbolTable.cs
│   ├── Program.cs
│   └── ...
│
├── ArabicCompiler.UI
│   ├── MainWindow.xaml
│   ├── MainWindow.xaml.cs
│   ├── TokensWindow.xaml
│   ├── ParseTreeWindow.xaml
│   ├── SymbolTableWindow.xaml
│   ├── TranslationStagesWindow.xaml
│   └── ...
│
├── COMPILER.sln
└── README.md
```

---

## Grammar

The language grammar is defined in:

```text
ArabicCompiler/Expr.g4
```

The grammar defines the lexical and syntactic rules of the Arabic programming language, including:

* Program structure
* Declarations
* Data types
* Procedures
* Lists
* Records
* Expressions
* Assignments
* Conditions
* Loops
* Input
* Output
* Arabic identifiers
* Operators
* Literals
* Comments

Arabic identifiers are supported through the language lexer rules, allowing identifiers to be written using Arabic characters.

---

## Compiler Components

The main compiler components include:

### `Expr.g4`

Defines the grammar and lexical rules of the Arabic programming language.

### `ArabicVisitor.cs`

Provides visitor-based processing of the generated parse-tree structures.

### `SemanticAnalyzerVisitor.cs`

Performs semantic analysis over the parsed program.

### `Symbol.cs`

Represents symbols maintained by the compiler.

### `SymbolTable.cs`

Provides symbol-table functionality for tracking declarations and identifiers.

### `CodeGenerator.cs`

Generates dynamic CIL/IL instructions using .NET Reflection.Emit.

### `CompilerService.cs`

Provides compiler-related service and processing logic.

### `ArabicCompiler.UI`

Provides the graphical interface for interacting with and inspecting compiler stages.

---

## Running the Project

### Requirements

* .NET 6 SDK
* Visual Studio 2022 or another compatible .NET development environment
* Windows environment for the WPF interface

### Clone

```bash
git clone git@github.com:SaqrAminAL-Dhafree/arabic-programming-language-compiler.git
```

### Open the Solution

Open:

```text
COMPILER.sln
```

in Visual Studio.

Restore the NuGet packages and build the solution.

---

## What This Project Demonstrates

This project demonstrates practical understanding of:

* Programming language design
* Compiler architecture
* Lexical analysis
* Parsing
* Grammar design
* Parse trees
* Semantic analysis
* Symbol tables
* Visitor patterns
* Intermediate language generation
* CIL/IL instructions
* Dynamic assembly generation
* .NET runtime execution
* WPF application development

---

## Compiler Execution Model

The project does not directly generate native machine code.

Instead, it generates **CIL/IL instructions** dynamically.

The execution model is:

```text
Arabic Language
      ↓
Custom Compiler
      ↓
CIL / IL
      ↓
.NET Runtime
      ↓
JIT Compilation
      ↓
Native Machine Code
      ↓
Execution
```

This approach allows the custom language to use the .NET execution environment while maintaining its own language syntax, grammar, semantic analysis, and code-generation logic.

---

## Project Goal

The primary goal of this project is to demonstrate how a programming language can be designed and processed through the major stages of a compiler.

Rather than creating another conventional application, the project focuses on understanding what happens internally between writing source code and executing a program.

---

## Future Improvements

Potential future development directions include:

* More advanced type checking
* Improved scope management
* Expanded function and procedure support
* Additional data structures
* More comprehensive error reporting
* Expanded standard library functionality
* Additional code-generation optimizations
* More language features
* Improved compiler diagnostics

---

## Author

**Saqr Ameen Al-Dhafree**

Full Stack Developer | Flutter & Dart | Backend Development

GitHub: [SaqrAminAL-Dhafree](https://github.com/SaqrAminAL-Dhafree)

LinkedIn: [Saqr Ameen Al-Dhafree](https://linkedin.com/in/saqraldhafree)

---

## License

This project is intended for educational and portfolio purposes.
