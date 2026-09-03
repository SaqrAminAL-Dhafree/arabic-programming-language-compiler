using ICSharpCode.AvalonEdit.Highlighting;
using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;

namespace ArabicCompiler.UI
{
    public partial class MainWindow : Window
    {
        private readonly string workspaceRoot = @"c:\Users\altur\OneDrive\COMPILER";
        private readonly string targetSourcePath;

        public MainWindow()
        {
            InitializeComponent();
            // path where ArabicCompiler expects program.arb
            targetSourcePath = System.IO.Path.Combine(workspaceRoot, "ArabicCompiler", "program.arb");
            // set default sample
            Editor.Text = "// اكتب برنامجك هنا ثم اضغط Run" + Environment.NewLine;
        }

        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            Editor.Text = string.Empty;
            OutputBox.Text = string.Empty;
        }

        private void BtnOpen_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog();
            ofd.Filter = "Arabic source (*.arb)|*.arb|All files (*.*)|*.*";
            if (ofd.ShowDialog() == true)
            {
                Editor.Text = File.ReadAllText(ofd.FileName, Encoding.UTF8);
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            var sfd = new SaveFileDialog();
            sfd.Filter = "Arabic source (*.arb)|*.arb|All files (*.*)|*.*";
            sfd.FileName = "program.arb";
            if (sfd.ShowDialog() == true)
            {
                File.WriteAllText(sfd.FileName, Editor.Text, Encoding.UTF8);
            }
        }

        private async void BtnRun_Click(object sender, RoutedEventArgs e)
        {
            OutputBox.Text = string.Empty;
            try
            {
                // Ensure the target source file is written to ArabicCompiler folder
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(targetSourcePath)!);
                File.WriteAllText(targetSourcePath, Editor.Text, Encoding.UTF8);

                await RunCompilerAndCaptureOutput();
            }
            catch (Exception ex)
            {
                OutputBox.Text = "Failed: " + ex.Message;
            }
        }

        private Task RunCompilerAndCaptureOutput()
        {
            return Task.Run(() =>
            {
                var psi = new ProcessStartInfo("dotnet", $"run --project \"{Path.Combine(workspaceRoot, "ArabicCompiler", "ArabicCompiler.csproj")}\"")
                {
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.Combine(workspaceRoot, "ArabicCompiler")
                };

                var p = new Process();
                p.StartInfo = psi;
                p.OutputDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                    {
                        Dispatcher.Invoke(() => OutputBox.AppendText(e.Data + Environment.NewLine));
                    }
                };
                p.ErrorDataReceived += (s, e) =>
                {
                    if (e.Data != null)
                    {
                        Dispatcher.Invoke(() => OutputBox.AppendText("ERR: " + e.Data + Environment.NewLine));
                    }
                };

                p.Start();
                p.BeginOutputReadLine();
                p.BeginErrorReadLine();
                p.WaitForExit();
            });
        }

        private void BtnTokens_Click(object sender, RoutedEventArgs e)
        {
            // Show tokens for current editor text
            var tokensWindow = new TokensWindow(Editor.Text);
            tokensWindow.Owner = this;
            tokensWindow.ShowDialog();
        }

        private void BtnParseTree_Click(object sender, RoutedEventArgs e)
        {
            var parseTreeWindow = new ParseTreeWindow(Editor.Text);
            parseTreeWindow.Owner = this;
            parseTreeWindow.ShowDialog();
        }

        // Menu handlers referenced from MainWindow.xaml
        private void Menu_ShowTokens_Click(object sender, RoutedEventArgs e)
        {
            var tokensWindow = new TokensWindow(Editor.Text);
            tokensWindow.Owner = this;
            tokensWindow.ShowDialog();
        }

        private void Menu_ShowParseTree_Click(object sender, RoutedEventArgs e)
        {
            var parseTreeWindow = new ParseTreeWindow(Editor.Text);
            parseTreeWindow.Owner = this;
            parseTreeWindow.ShowDialog();
        }

        private void Menu_ShowSymbolTable_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var win = new SymbolTableWindow(Editor.Text);
                win.Owner = this;
                win.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error showing symbol table: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Menu_ShowIL_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var service = new ArabicCompiler.CompilerService();
                var ilText = service.GetILText(Editor.Text);
                var win = new TranslationStagesWindow();
                win.TitleText.Text = "Generated IL / CodeGen Log";
                win.ContentBox.FontFamily = new System.Windows.Media.FontFamily("Consolas");
                win.ContentBox.Text = ilText;
                win.Owner = this;
                win.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error showing IL: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Menu_GenerateCode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var service = new ArabicCompiler.CompilerService();
                var ilText = service.GetILText(Editor.Text);
                var win = new TranslationStagesWindow();
                win.TitleText.Text = "CodeGen / IL";
                win.ContentBox.FontFamily = new System.Windows.Media.FontFamily("Consolas");
                win.ContentBox.Text = ilText;
                win.Owner = this;
                win.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error generating code: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Menu_GenerateSimpleCode_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var service = new ArabicCompiler.CompilerService();
                var simple = service.GetSimpleCode(Editor.Text);
                var win = new TranslationStagesWindow();
                win.TitleText.Text = "Simple Generated Code";
                win.ContentBox.FontFamily = new System.Windows.Media.FontFamily("Consolas");
                win.ContentBox.Text = simple;
                win.Owner = this;
                win.ShowDialog();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error generating simple code: " + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
