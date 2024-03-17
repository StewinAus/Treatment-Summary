using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Xps.Packaging;
using System.Windows.Documents.Serialization;
using System.Windows.Xps;
using System.Printing;


namespace Treatment_Summary
{
    /// <summary>
    /// Interaction logic for Summary.xaml
    /// </summary>
    public partial class Summary : Window
    {
        public Summary()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

            Print_WPF_Preview(summarygrid);

        }

        public void Print_WPF_Preview(FrameworkElement wpf_Element)

        {
            if (File.Exists("print_previw.xps"))
            {
                File.Delete("print_previw.xps");
            }
            XpsDocument doc = new XpsDocument("print_previw.xps", FileAccess.ReadWrite);
            XpsDocumentWriter writer = XpsDocument.CreateXpsDocumentWriter(doc);
            SerializerWriterCollator preview_Document = writer.CreateVisualsCollator();
            preview_Document.BeginBatchWrite();
            preview_Document.Write(wpf_Element);  //*this or wpf xaml control
            preview_Document.EndBatchWrite();
            FixedDocumentSequence preview = doc.GetFixedDocumentSequence();

            var window = new Window();
            window.Content = new DocumentViewer { Document = preview };
            window.ShowDialog();
            doc.Close();

        }


    }
}
