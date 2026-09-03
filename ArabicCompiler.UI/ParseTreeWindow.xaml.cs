using System;
using System.Windows;
using ArabicCompiler;
using Microsoft.Win32;
using Antlr4.Runtime;
using Antlr4.Runtime.Tree;
using System.Windows.Controls;

namespace ArabicCompiler.UI
{
    public partial class ParseTreeWindow : Window
    {
        private readonly string _sourceText;
        public ParseTreeWindow(string sourceText)
        {
            _sourceText = sourceText ?? string.Empty;
            InitializeComponent();
            try
            {
                // Build ANTLR parse tree and populate TreeView with collapsible nodes
                var input = new AntlrInputStream(_sourceText);
                var lexer = new ExprLexer(input);
                var tokens = new CommonTokenStream(lexer);
                var parser = new ExprParser(tokens);
                var tree = parser.program();

                ParseTreeTreeView.Items.Clear();
                var rootItem = CreateTreeViewItem(tree, parser);
                ParseTreeTreeView.Items.Add(rootItem);
                rootItem.IsExpanded = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error extracting parse tree: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCopy_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var service = new ArabicCompiler.CompilerService();
                var text = service.GetParseTreeText(_sourceText);
                Clipboard.SetText(text);
            }
            catch { }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var sfd = new SaveFileDialog();
                sfd.Filter = "Text files (*.txt)|*.txt|All files (*.*)|*.*";
                sfd.FileName = "parsetree.txt";
                if (sfd.ShowDialog() == true)
                {
                    var service = new ArabicCompiler.CompilerService();
                    var text = service.GetParseTreeText(_sourceText);
                    System.IO.File.WriteAllText(sfd.FileName, text);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Save failed: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSemantic_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var win = new SymbolTableWindow(_sourceText);
                win.Owner = this;
                win.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error opening semantic view: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        // Recursively create TreeViewItem from an IParseTree
        private TreeViewItem CreateTreeViewItem(IParseTree node, ExprParser parser)
        {
            string header;
            if (node is ITerminalNode term)
            {
                var symbol = term.Symbol;
                var typeName = ExprLexer.DefaultVocabulary.GetSymbolicName(symbol.Type) ?? symbol.Type.ToString();
                header = $"{typeName}: '{symbol.Text.Replace("\n", "\\n")}'; (line {symbol.Line}, col {symbol.Column})";
            }
            else
            {
                // attempt to get rule name
                string ruleName = node.GetType().Name;
                try
                {
                    var ruleIndexProp = node.GetType().GetProperty("RuleIndex");
                    if (ruleIndexProp != null)
                    {
                        var idx = (int)ruleIndexProp.GetValue(node)!;
                        ruleName = parser.RuleNames[idx];
                    }
                }
                catch { }
                header = ruleName;
            }

            var item = new TreeViewItem { Header = header };
            for (int i = 0; i < node.ChildCount; i++)
            {
                var child = node.GetChild(i);
                item.Items.Add(CreateTreeViewItem(child, parser));
            }
            return item;
        }
    }
}
