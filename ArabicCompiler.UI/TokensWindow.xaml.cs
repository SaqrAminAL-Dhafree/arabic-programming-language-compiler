using System;
using System.Collections.Generic;
using System.Windows;
using ArabicCompiler;

namespace ArabicCompiler.UI
{
    public partial class TokensWindow : Window
    {
        public TokensWindow(string sourceText)
        {
            InitializeComponent();
            try
            {
                var service = new CompilerService();
                var tokens = service.GetTokens(sourceText);
                TokensGrid.ItemsSource = tokens;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error extracting tokens: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
