using System;
using System.Windows;
using ArabicCompiler;

namespace ArabicCompiler.UI
{
    public partial class SymbolTableWindow : Window
    {
        public SymbolTableWindow(string sourceText)
        {
            InitializeComponent();
            try
            {
                var service = new CompilerService();
                var symbols = service.GetSymbolTableDump(sourceText);
                SymbolsGrid.ItemsSource = symbols;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error extracting symbol table: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}