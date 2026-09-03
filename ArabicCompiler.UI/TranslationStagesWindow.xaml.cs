using System.Windows;

namespace ArabicCompiler.UI
{
    public partial class TranslationStagesWindow : Window
    {
        public TranslationStagesWindow()
        {
            InitializeComponent();
        }

        public void SetContent(string title, string content)
        {
            TitleText.Text = title;
            ContentBox.Text = content;
        }
    }
}
