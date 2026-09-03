# Arabic Programming Language Compiler

A compiler implementation for a custom **Arabic programming language**, built with **C# and .NET 6**, using **ANTLR 4** for grammar-driven lexical and syntactic analysis.

The project explores the core stages of compiler construction, including parsing, semantic analysis, symbol-table management, and code generation, with a dedicated desktop interface for interacting with the compiler and inspecting its internal translation stages.

---

## Overview

**Arabic Programming Language Compiler** is a compiler project designed to process a programming language whose syntax and keywords are expressed in Arabic.

The language supports structured programming concepts including:

* Variables and constants
* Custom data types
* Lists and records
* Procedures
* Parameters passed by value or by reference
* Conditional statements
* Loops
* Arithmetic and logical expressions
* Input and output operations
* Variable indexing and record-field access
* Arabic identifiers

The project demonstrates how the different stages of a compiler can work together to transform source code into a structured representation and generated output.

---

## Compiler Pipeline

The project is organized around the major stages of compiler processing:

```text
Arabic Source Code
        │
        ▼
     Lexical Analysis
        │
        ▼
   Syntax / Parsing
        │
        ▼
   Parse Tree / AST
        │
        ▼
 Semantic Analysis
        │
        ▼
   Symbol Table
        │
        ▼
  Code Generation
        │
        ▼
 Generated Output
```

This architecture separates the responsibilities of each stage and provides a clear foundation for extending the language and compiler.

---

## Language Features

### Declarations

The language supports Arabic declarations for:

* Constants
* Variables
* User-defined types
* Procedures

Example:

```text
برنامج example؛
...
.
```

### Data Types

Built-in data types include:

```text
صحيح
حقيقي
منطقي
حرفي
خيط_رمزي
```

The language also supports user-defined types.

### Composite Types

The grammar supports:

* Lists
* Records
* Structured fields
* Indexed access
* Record-field access

### Procedures

Procedures support formal parameters with:

```text
بالقيمة
بالمرجع
```

allowing parameters to be passed by value or by reference.

### Control Flow

The language includes:

* Conditional statements
* `اذا / فان / والا`
* `كرر`
* `طالما`
* `اعد ... حتى`

### Expressions

The expression grammar supports:

* Arithmetic operations
* Relational operations
* Logical operations
* Boolean expressions
* Unary signs
* Variable access
* Indexed access
* Record-field access

### Input & Output

The language provides Arabic commands for:

```text
اقرا
اطبع
```

for input and output operations.

### Arabic Identifiers

Identifiers are designed to support Arabic letters directly, allowing source programs to be written using Arabic-oriented naming conventions.

---

## Compiler Components

### Grammar & Parsing

The language grammar is defined using **ANTLR 4** in:

```text
Expr.g4
```

The grammar contains both:

* Parser rules
* Lexer rules

ANTLR-generated lexer and parser components are used to process the source language.

### Semantic Analysis

The project includes:

```text
SemanticAnalyzerVisitor.cs
```

which provides the semantic-analysis stage of the compiler.

### Symbol Table

Symbol management is implemented through:

```text
Symbol.cs
SymbolTable.cs
```

These components provide the foundation for tracking symbols and their associated information during compilation.

### Code Generation

Code generation is implemented through:

```text
CodeGenerator.cs
```

and works as part of the compiler pipeline to produce generated output from the processed source program.

### Compiler Service

The compiler workflow is coordinated through:

```text
CompilerService.cs
```

providing a central service layer for compiler operations.

---

## Desktop User Interface

The project also contains a dedicated desktop UI:

```text
ArabicCompiler.UI
```

The interface includes dedicated views for inspecting compiler information, including:

* Tokens
* Parse Tree
* Symbol Table
* Translation Stages

This makes the compiler easier to demonstrate, inspect, and understand during development and testing.

---

## Technology Stack

| Technology                          | Purpose                               |
| ----------------------------------- | ------------------------------------- |
| C#                                  | Core implementation language          |
| .NET 6                              | Application framework                 |
| ANTLR 4.13.1                        | Lexer and parser generation           |
| WPF / XAML                          | Desktop user interface                |
| Lokad.ILPack                        | IL-related packaging support          |
| System.Reflection.Emit.ILGeneration | IL generation support                 |
| Visual Studio                       | Development environment               |
| Git & GitHub                        | Version control and source management |

---

## Project Structure

```text
COMPILER/
│
├── ArabicCompiler/
│   ├── .antlr/
│   ├── Generated/
│   ├── examable/
│   ├── Expr.g4
│   ├── ArabicVisitor.cs
│   ├── CodeGenerator.cs
│   ├── CompilerService.cs
│   ├── Program.cs
│   ├── SemanticAnalyzerVisitor.cs
│   ├── Symbol.cs
│   ├── SymbolTable.cs
│   └── ArabicCompiler.csproj
│
├── ArabicCompiler.UI/
│   ├── App.xaml
│   ├── MainWindow.xaml
│   ├── ParseTreeWindow.xaml
│   ├── SymbolTableWindow.xaml
│   ├── TokensWindow.xaml
│   ├── TranslationStagesWindow.xaml
│   └── ArabicCompiler.UI.csproj
│
├── .vscode/
│
├── COMPILER.sln
└── .gitignore
```

---

## Example Programs

The repository includes multiple `.arb` example programs covering different language constructs, including:

```text
example01_numbers.arb
example02_strings.arb
example03_arithmetic.arb
example04_logical.arb
example05_conditionals.arb
example06_loops.arb
example07_assignment.arb
example08_final.arb
```

These examples provide practical test cases for the language grammar and compiler pipeline.

---

## Getting Started

### Prerequisites

Install:

* .NET 6 SDK
* Visual Studio 2022
* Git

### Clone the Repository

```bash
git clone git@github.com:SaqrAminAL-Dhafree/arabic-programming-language-compiler.git
```

### Navigate to the Project

```bash
cd arabic-programming-language-compiler
```

### Build the Solution

```bash
dotnet build COMPILER.sln
```

### Run the Compiler

The main compiler project can be run with:

```bash
dotnet run --project ArabicCompiler/ArabicCompiler.csproj
```

The desktop UI can be opened through the `ArabicCompiler.UI` project in Visual Studio.

---

## Design Goals

The project was developed with the following goals:

* Explore compiler construction fundamentals.
* Design a programming language using Arabic syntax.
* Build a grammar-driven parser using ANTLR.
* Separate syntax analysis from semantic analysis.
* Implement symbol-table management.
* Implement a dedicated code-generation stage.
* Provide a visual interface for inspecting compiler stages.
* Create a structured foundation for extending the language.

---

## Engineering Highlights

This project demonstrates practical experience with:

* Compiler architecture
* Language grammar design
* ANTLR4
* Lexical analysis
* Syntax analysis
* Parse trees
* Visitor-based processing
* Semantic analysis
* Symbol tables
* Code generation
* C# and .NET
* WPF desktop application development
* Structured software architecture

---

## Author

**Saqr Ameen Al-Dhafree**

Full Stack Developer | Flutter & Dart | Backend Development

* LinkedIn: [https://www.linkedin.com/in/saqraldhafree](https://www.linkedin.com/in/saqraldhafree)
* GitHub: [https://github.com/SaqrAminAL-Dhafree](https://github.com/SaqrAminAL-Dhafree)

---

## License

This project is maintained as a portfolio and educational compiler-development project.
