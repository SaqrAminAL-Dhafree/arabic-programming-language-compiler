using System;
using System.Collections.Generic;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;

namespace ArabicCompiler
{
    // Lightweight compiler service to expose compilation phases to the UI
    public class CompilerService
    {
    public record TokenInfo(string TokenType, string Text, int Line, int Column);

    /// <summary>
    /// Analyze semantics and return a list of symbol table entries (name, type, scope).
    /// </summary>
    public List<SymbolDumpEntry> GetSymbolTableDump(string source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        var input = new AntlrInputStream(source);
        var lexer = new ExprLexer(input);
        var tokens = new CommonTokenStream(lexer);
        var parser = new ExprParser(tokens);
        var tree = parser.program();

        var symbolTable = new SymbolTable();
    var codeGen = new CodeGenerator("InMemoryAssembly");
        var semantic = new SemanticAnalyzerVisitor(symbolTable, codeGen);
        semantic.Visit(tree);
        return symbolTable.GetAllSymbols();
    }

    /// <summary>
    /// Parse the given source and return a pretty textual representation of the parse tree.
    /// </summary>
    public string GetParseTreeText(string source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        var input = new AntlrInputStream(source);
        var lexer = new ExprLexer(input);
        var tokens = new CommonTokenStream(lexer);
        var parser = new ExprParser(tokens);
        var tree = parser.program();
        // By default return a formatted indented representation
        return GetParseTreeFormatted(tree, parser);
    }

    // Produce an indented, human-friendly parse tree with node types and token text
    private string GetParseTreeFormatted(Antlr4.Runtime.Tree.IParseTree node, ExprParser parser)
    {
        var sb = new System.Text.StringBuilder();
        void Walk(Antlr4.Runtime.Tree.IParseTree t, int depth)
        {
            var indent = new string(' ', depth * 2);
            var nodeText = t.GetType().Name;
            // If it's a terminal node, show token type and text
            if (t is Antlr4.Runtime.Tree.ITerminalNode term)
            {
                var symbol = term.Symbol;
                var typeName = ExprLexer.DefaultVocabulary.GetSymbolicName(symbol.Type) ?? symbol.Type.ToString();
                sb.AppendLine($"{indent}- {typeName}: '{symbol.Text.Replace("\n", "\\n")}' (line {symbol.Line}, col {symbol.Column})");
            }
            else
            {
                // non-terminal: show rule name if possible
                try
                {
                    var ruleIndexProp = t.GetType().GetProperty("RuleIndex");
                    string ruleName = string.Empty;
                    if (ruleIndexProp != null)
                    {
                        var idx = (int)ruleIndexProp.GetValue(t)!;
                        ruleName = parser.RuleNames[idx];
                    }
                    if (!string.IsNullOrEmpty(ruleName))
                        sb.AppendLine($"{indent}+ {ruleName}");
                    else
                        sb.AppendLine($"{indent}+ {t.GetType().Name}");
                }
                catch
                {
                    sb.AppendLine($"{indent}+ {t.GetType().Name}");
                }
                for (int i = 0; i < t.ChildCount; i++)
                {
                    Walk(t.GetChild(i), depth + 1);
                }
            }
        }

        Walk(node, 0);
        return sb.ToString();
    }

    /// <summary>
    /// Return a nicely formatted symbol table text (columns: Name, Type, Scope, Extras).
    /// </summary>
    public string GetSymbolTableText(string source)
    {
        var symbols = GetSymbolTableDump(source);
        if (symbols == null || symbols.Count == 0) return "(no symbols)";

        // Determine column widths
    int nameW = Math.Max(4, symbols.Max(s => s.Name?.Length ?? 0));
    int typeW = Math.Max(4, symbols.Max(s => s.Type.ToString().Length));
        int scopeW = Math.Max(5, symbols.Max(s => s.Scope?.Length ?? 0));

        var fmt = $"{{0,-{nameW}}}  {{1,-{typeW}}}  {{2,-{scopeW}}}";
        var sb = new System.Text.StringBuilder();
        sb.AppendLine(string.Format(fmt, "Name", "Type", "Scope"));
        sb.AppendLine(new string('-', nameW + typeW + scopeW + 4));
        foreach (var s in symbols)
        {
            sb.AppendLine(string.Format(fmt, s.Name, s.Type.ToString(), s.Scope));
        }
        return sb.ToString();
    }

    /// <summary>
    /// Run semantic analysis and code generation and return any generated IL/log text.
    /// </summary>
    public string GetILText(string source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        try
        {
            var input = new AntlrInputStream(source);
            var lexer = new ExprLexer(input);
            var tokens = new CommonTokenStream(lexer);
            var parser = new ExprParser(tokens);
            var tree = parser.program();

            var symbolTable = new SymbolTable();
            var codeGen = new CodeGenerator("InMemoryAssembly");

            // run semantic analysis (may populate symbol table and prepare procedures)
            var semantic = new SemanticAnalyzerVisitor(symbolTable, codeGen);
            semantic.Visit(tree);

            // run code generation visitor to emit IL/logs
            var generatorVisitor = new ArabicVisitor(codeGen, symbolTable);
            generatorVisitor.Visit(tree);

            // return codegen log if any
            return codeGen.GetLogText() ?? string.Empty;
        }
        catch (Exception ex)
        {
            return "Error during IL generation: " + ex.Message;
        }
    }

    /// <summary>
    /// Generate a small, human-readable pseudo-code from the parse tree.
    /// This is heuristic and intended as a simple readable output, not full codegen.
    /// </summary>
    public string GetSimpleCode(string source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));
        var input = new AntlrInputStream(source);
        var lexer = new ExprLexer(input);
        var tokens = new CommonTokenStream(lexer);
        var parser = new ExprParser(tokens);
        var tree = parser.program();

        var sb = new System.Text.StringBuilder();
        void Walk(IParseTree node, int depth)
        {
            string indent = new string(' ', depth * 2);
            if (node is Antlr4.Runtime.Tree.ITerminalNode t)
            {
                sb.Append(t.GetText());
                return;
            }
            // try to get rule name
            string ruleName = node.GetType().Name;
            try
            {
                var prop = node.GetType().GetProperty("RuleIndex");
                if (prop != null)
                {
                    var idx = (int)prop.GetValue(node)!;
                    ruleName = parser.RuleNames[idx];
                }
            }
            catch { }

            // handle a few common constructs heuristically
            if (ruleName.Contains("assignment") || ruleName.Contains("assign"))
            {
                // join children as a single line
                var parts = new List<string>();
                for (int i = 0; i < node.ChildCount; i++) parts.Add(GetText(node.GetChild(i)));
                sb.AppendLine(indent + string.Join(" ", parts) + ";");
                return;
            }
            if (ruleName.Contains("output") || ruleName.Contains("print"))
            {
                var parts = new List<string>();
                for (int i = 0; i < node.ChildCount; i++) parts.Add(GetText(node.GetChild(i)));
                sb.AppendLine(indent + "print(" + string.Join(" ", parts) + ");");
                return;
            }
            if (ruleName.Contains("procedure") || ruleName.Contains("function"))
            {
                // header + body
                var header = GetText(node.GetChild(0));
                sb.AppendLine(indent + header + " { ");
                for (int i = 1; i < node.ChildCount; i++) Walk(node.GetChild(i), depth + 1);
                sb.AppendLine(indent + "}");
                return;
            }

            // default: recurse children
            for (int i = 0; i < node.ChildCount; i++)
            {
                Walk(node.GetChild(i), depth);
            }
        }

        string GetText(IParseTree n)
        {
            if (n is Antlr4.Runtime.Tree.ITerminalNode tn) return tn.GetText();
            var parts = new List<string>();
            for (int i = 0; i < n.ChildCount; i++) parts.Add(GetText(n.GetChild(i)));
            return string.Join(" ", parts).Trim();
        }

        Walk(tree, 0);
        var outText = sb.ToString();
        if (string.IsNullOrWhiteSpace(outText)) return "(no simple code produced)";
        return outText;
    }

    /// <summary>
    /// Tokenize the given source using the ANTLR-generated lexer and return a list
    /// of token entries (type, text, line, column).
    /// </summary>
    public List<TokenInfo> GetTokens(string source)
    {
        if (source == null) throw new ArgumentNullException(nameof(source));

        var input = new AntlrInputStream(source);
        var lexer = new ExprLexer(input);

        // collect all tokens
        var tokens = new List<TokenInfo>();
        var all = lexer.GetAllTokens();
        foreach (var t in all)
        {
            var typeName = ExprLexer.DefaultVocabulary.GetSymbolicName(t.Type) ?? t.Type.ToString();
            tokens.Add(new TokenInfo(typeName, t.Text, t.Line, t.Column));
        }

        return tokens;
    }
    }
}
