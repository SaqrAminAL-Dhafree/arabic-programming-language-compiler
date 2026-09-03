<!-- // -->
ArabicCompiler.UI - Prototype WPF host for ArabicCompiler

This is a small WPF prototype that embeds AvalonEdit as a code editor and provides buttons to save and run your `program.arb` using the ArabicCompiler project in the same workspace.

Usage:

- Open Visual Studio or run `dotnet build` then run the project from the IDE.
- Edit code in the editor, press Run. The UI saves to `ArabicCompiler/program.arb` and runs `dotnet run --project ArabicCompiler/ArabicCompiler.csproj` capturing the output.

Notes:

- This is a prototype. Later improvements: diagnostics highlighting, REPL, in-process API call instead of launching dotnet run, packaging the app.
