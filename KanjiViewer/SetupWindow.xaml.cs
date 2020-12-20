using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
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
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace KanjiViewer
{
    /// <summary>
    /// Interaction logic for SetupWindow.xaml
    /// </summary>
    public partial class SetupWindow : Window
    {
        private Progress<ZipProgress> zip_pbar;
        private bool started_extraction = false;
        private bool success = false;
        private MainWindow parent;

        public SetupWindow(MainWindow p, bool s)
        {
            parent = p;
            success = s;
            Properties.Settings.Default.resource_directory = Directory.GetCurrentDirectory();
            zip_pbar = new Progress<ZipProgress>();
            zip_pbar.ProgressChanged += Report;

            InitializeComponent();

            dir_label.Content = Properties.Settings.Default.resource_directory;
        }

        private void Report(object sender, ZipProgress zProg)
        {
            load_bar.Value = 100*((double)zProg.Processed / (double)zProg.Total);

            if(started_extraction && zProg.Processed == zProg.Total)
            {
                MessageBox.Show("Complete");
                success = true;
                this.Close();
            }
        }

        private void MakeZip()
        {
            Stream zip_stream = new FileStream("frames.zip", FileMode.Open);
            ZipArchive frame_zip = new ZipArchive(zip_stream);
            started_extraction = true;
            frame_zip.ExtractToDirectory(Properties.Settings.Default.resource_directory, zip_pbar, true);
            Properties.Settings.Default.needs_init = false;
            Properties.Settings.Default.Save();
        }

        private void dir_button_Click(object sender, RoutedEventArgs e)
        {
            var fbd = new System.Windows.Forms.FolderBrowserDialog();
            fbd.Description = "Select new Directory for extraction";

            if(fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                Properties.Settings.Default.resource_directory = fbd.SelectedPath;
                dir_label.Content = fbd.SelectedPath;
            }
        }

        private void start_button_Click(object sender, RoutedEventArgs e)
        {
            load_bar.Visibility = Visibility.Visible;
            start_button.IsEnabled = false;
            dir_button.IsEnabled = false;
            existingdb_button.IsEnabled = false;
            Task.Run(MakeZip);
        }

        private void cancel_button_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (!success) Application.Current.Shutdown();
        }

        private void existingdb_button_Click(object sender, RoutedEventArgs e)
        {
            var fbd = new System.Windows.Forms.FolderBrowserDialog();
            fbd.Description = "Select the Directory with \"Frames\" folder.";

            if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
            {
                if (Directory.Exists(fbd.SelectedPath + "\\Frames") && File.Exists(fbd.SelectedPath + "\\Frames\\26085_frames.svg"))
                {
                    Properties.Settings.Default.resource_directory = fbd.SelectedPath;
                    dir_label.Content = fbd.SelectedPath;
                    success = true; 
                    Properties.Settings.Default.needs_init = false;
                    Properties.Settings.Default.Save();
                    this.Close();
                }
                else if(fbd.SelectedPath.EndsWith("\\Frames") && File.Exists(fbd.SelectedPath + "\\26085_frames.svg"))
                {
                    Properties.Settings.Default.resource_directory = fbd.SelectedPath.Substring(0, fbd.SelectedPath.Length - 7);
                    dir_label.Content = fbd.SelectedPath;
                    success = true;
                    Properties.Settings.Default.needs_init = false;
                    Properties.Settings.Default.Save();
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Invalid Directory selected; couldn't locate \"Frames\" folder");
                }
            }
        }
    }

    /* https://stackoverflow.com/a/43662519 Scott Chamberlain: Accessed Oct. 08 2019 */
    public class ZipProgress
    {
        public ZipProgress(int total, int processed, string currentItem)
        {
            Total = total;
            Processed = processed;
            CurrentItem = currentItem;
        }
        public int Total { get; }
        public int Processed { get; }
        public string CurrentItem { get; }
    }

    public static class MyZipFileExtensions
    {
        public static void ExtractToDirectory(this ZipArchive source, string destinationDirectoryName, IProgress<ZipProgress> progress)
        {
            ExtractToDirectory(source, destinationDirectoryName, progress, overwrite: false);
        }

        public static void ExtractToDirectory(this ZipArchive source, string destinationDirectoryName, IProgress<ZipProgress> progress, bool overwrite)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));

            if (destinationDirectoryName == null)
                throw new ArgumentNullException(nameof(destinationDirectoryName));


            // Rely on Directory.CreateDirectory for validation of destinationDirectoryName.

            // Note that this will give us a good DirectoryInfo even if destinationDirectoryName exists:
            DirectoryInfo di = Directory.CreateDirectory(destinationDirectoryName);
            string destinationDirectoryFullPath = di.FullName;

            int count = 0;
            foreach (ZipArchiveEntry entry in source.Entries)
            {
                count++;
                string fileDestinationPath = Path.GetFullPath(Path.Combine(destinationDirectoryFullPath, entry.FullName));

                if (!fileDestinationPath.StartsWith(destinationDirectoryFullPath, StringComparison.OrdinalIgnoreCase))
                    throw new IOException("File is extracting to outside of the folder specified.");

                var zipProgress = new ZipProgress(source.Entries.Count, count, entry.FullName);
                progress.Report(zipProgress);

                if (Path.GetFileName(fileDestinationPath).Length == 0)
                {
                    // If it is a directory:

                    if (entry.Length != 0)
                        throw new IOException("Directory entry with data.");

                    Directory.CreateDirectory(fileDestinationPath);
                }
                else
                {
                    // If it is a file:
                    // Create containing directory:
                    Directory.CreateDirectory(Path.GetDirectoryName(fileDestinationPath));
                    entry.ExtractToFile(fileDestinationPath, overwrite: overwrite);
                }
            }
        }
    }
}
