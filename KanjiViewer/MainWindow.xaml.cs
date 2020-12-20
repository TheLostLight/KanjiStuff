using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

using SharpVectors;

namespace KanjiViewer
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private static bool text_in_use = false;
        
        public MainWindow()
        {
            //Properties.Settings.Default.Reset(); //For debugging purposes
            
            if(KanjiViewer.Properties.Settings.Default.needs_init)
            {
                run_Setup(false);
            }
            InitializeComponent();
        }

        public void run_Setup(bool acon)
        {
            SetupWindow sw = new SetupWindow(this, acon);
            sw.ShowDialog();
        }

        private void InputBox_GotFocus(object sender, RoutedEventArgs e)
        {
            InputBox.Text = "";
            InputBox.Opacity = 100;
            text_in_use = true;
        }

        private void InputBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            SharpVectors.Converters.SvgViewbox sv;
            String path = "";
            int num;
            int flag = 0;

            myPanel.Children.Clear();

            if (InputBox.Text.Length < 1 || !text_in_use) return;

            for (int i=0; i<InputBox.Text.Length; i++)
            {
                num = InputBox.Text[i];
                try
                {
                    path = "\\Frames\\" + num + "_frames.svg";
                    sv = new SharpVectors.Converters.SvgViewbox();
                    path = Properties.Settings.Default.resource_directory + path;
                    sv.Source = new Uri("file://" + path);
                }
                catch (Exception ex) when (ex is UriFormatException || ex is FileNotFoundException || ex is DirectoryNotFoundException) 
                {
                    if (i == InputBox.Text.Length - 1 && flag < 1)
                    {
                        sv = new SharpVectors.Converters.SvgViewbox();
                        path = System.IO.Path.GetFullPath("frown.svg");
                        sv.Source = new Uri("file://" + path);
                        myPanel.Children.Add(sv);
                        return;
                    }
                    continue;
                }

             myPanel.Children.Add(sv);
             flag++;
            }
        }

        private void InputBox_LostFocus(object sender, RoutedEventArgs e)
        {
            text_in_use = false;
            InputBox.Opacity = 50;
        }

        private void scale_button_Checked(object sender, RoutedEventArgs e)
        {
            Scrollbox.HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled;
            //Scrollbox.VerticalScrollBarVisibility = ScrollBarVisibility.Disabled;
        }

        private void scale_button_Unchecked(object sender, RoutedEventArgs e)
        {
            Scrollbox.HorizontalScrollBarVisibility = ScrollBarVisibility.Auto;
            //Scrollbox.VerticalScrollBarVisibility = ScrollBarVisibility.Auto;
        }

        private void reinit_item_Click(object sender, RoutedEventArgs e)
        {
            run_Setup(true);
        }
    }
}
