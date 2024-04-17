using System;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Win32;
using iText.Kernel.Pdf;
using iText.Kernel.Pdf.Canvas.Parser;
using iText.Kernel.Pdf.Canvas.Parser.Listener;
using System.Text.RegularExpressions;
using System.Text;

namespace RenamePDF
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            AllowDrop = true;
            Drop += MainWindow_Drop;
        }

        private async void MainWindow_Drop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files.Length > 0)
                {
                    FilePathTextBox.Text = files[0];
                    await ProcessPDFFile(files[0]);
                }
            }
        }

        private void StartButton_Click(object sender, RoutedEventArgs e)
{
    OpenFileDialog openFileDialog = new OpenFileDialog
    {
        Multiselect = true,
        Filter = "PDF Files|*.pdf"
    };

    if (openFileDialog.ShowDialog() == true)
    {
        foreach (string fileName in openFileDialog.FileNames)
        {
            FilePathTextBox.Text += fileName + Environment.NewLine;
        }
    }
}

        private async void StartRecognition_Click(object sender, RoutedEventArgs e)
        {
            string[] files = FilePathTextBox.Text.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string file in files)
            {
                if (file.Contains("Заказ") || file.Contains("заказ"))
                {
                    await ProcessPDFFile(file);
                }
            }
        }

        private async Task ProcessPDFFile(string filePath)
        {
            await Task.Run(() =>
            {
                string text = ExtractTextFromPDF(filePath);
                if (text.Contains("заказ", StringComparison.OrdinalIgnoreCase) || text.Contains("заказ", StringComparison.OrdinalIgnoreCase))
                {
                    string newName = RenameFile(filePath, text);
                    if (newName != null)
                    {
                        try
                        {
                            File.Move(filePath, newName);
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"Ошибка при переименовании файла: {ex.Message}");
                        }
                    }
                }
                else
                {
                    MessageBox.Show("Файл не содержит ключевого слова 'заказ' в названии.");
                }
            });
        }

        private string ExtractTextFromPDF(string filePath)
        {
            using (PdfReader pdfReader = new PdfReader(filePath))
            {
                using (PdfDocument pdfDocument = new PdfDocument(pdfReader))
                {
                    StringBuilder textBuilder = new StringBuilder();

                    for (int i = 1; i <= pdfDocument.GetNumberOfPages(); i++)
                    {
                        PdfPage page = pdfDocument.GetPage(i);
                        LocationTextExtractionStrategy strategy = new LocationTextExtractionStrategy();
                        PdfCanvasProcessor parser = new PdfCanvasProcessor(strategy);
                        parser.ProcessPageContent(page);

                        textBuilder.Append(strategy.GetResultantText());
                    }

                    return textBuilder.ToString();
                }
            }
        }

        private string RenameFile(string filePath, string text)
        {
            string directory = Path.GetDirectoryName(filePath);
            string fileName = Path.GetFileNameWithoutExtension(filePath);
            string extension = Path.GetExtension(filePath);

            string numberPlateText = ExtractNumberPlateText(text);
            if (numberPlateText != null)
            {
                // Удаление всех пробелов из текста номерного знака
                numberPlateText = numberPlateText.Replace(" ", "");
                // Добавление пробела после текста номерного знака
                string newName = $"{numberPlateText} {fileName}{extension}";
                return Path.Combine(directory, newName);
            }
            else
            {
                MessageBox.Show("Не удалось найти номерной знак в тексте файла.");
                return null;
            }
        }

        private string ExtractNumberPlateText(string text)
        {
            int index = text.IndexOf("гос. номер:", StringComparison.OrdinalIgnoreCase);
            if (index != -1)
            {
                index += "гос. номер:".Length;
                int endIndex = Math.Min(index + 12, text.Length);
                return text.Substring(index, endIndex - index).Trim();
            }
            else
            {
                return null;
            }
        }
    }

}
