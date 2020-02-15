using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using System.Management;
using System.IO;
using System.Threading;

namespace PictureImporter
{
    public partial class MainWindow : Form
    {
        private string _copyToPath;
        private string _copyabelText = "Importing picture {0} of {1}...";
        private delegate void SafeCallDelegate();

        public MainWindow()
        {
            InitializeComponent();
            backgroundWorker1.RunWorkerAsync();
        }

        private void DeviceInsertedEvent(object sender, EventArrivedEventArgs e)
        {
            ManagementBaseObject instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
            //foreach (var property in instance.Properties)
            //{
            //    Console.WriteLine(property.Name + " = " + property.Value);
            //}
            string driveLetter = instance.Properties["Name"].Value.ToString() + "\\";

            List<string> pictures = GetImages(driveLetter);

            if (pictures.Count > 0)
            {
                DialogResult dialogResult = MessageBox.Show("Pictures detected!  Would you like to import them?", "Import Photos?", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (dialogResult == DialogResult.Yes)
                {
                    RestoreWindow();
                    _copyToPath = Directory.CreateDirectory(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "\\Pictures\\" + DateTime.Now.ToString("MM-dd-yyyy")).FullName;

                    CopyFiles(pictures, _copyToPath);
                    DeleteFiles(pictures);
                }
                else
                    MessageBox.Show("OK, pictures were not imported.", "Import Canceled", MessageBoxButtons.OK, MessageBoxIcon.Error);

                HideWindow();
            }
        }

        private void DeviceRemovedEvent(object sender, EventArrivedEventArgs e)
        {
            ManagementBaseObject instance = (ManagementBaseObject)e.NewEvent["TargetInstance"];
            foreach (var property in instance.Properties)
            {
                Console.WriteLine(property.Name + " - " + property.Value);
            }
        }

        private void backgroundWorker1_DoWork(object sender, DoWorkEventArgs e)
        {
            WqlEventQuery insertQuery = new WqlEventQuery("SELECT * FROM __InstanceCreationEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_LogicalDisk'");

            ManagementEventWatcher insertWatcher = new ManagementEventWatcher(insertQuery);
            insertWatcher.EventArrived += new EventArrivedEventHandler(DeviceInsertedEvent);
            insertWatcher.Start();

            WqlEventQuery removeQuery = new WqlEventQuery("SELECT * FROM __InstanceDeletionEvent WITHIN 2 WHERE TargetInstance ISA 'Win32_LogicalDisk'");
            ManagementEventWatcher removeWatcher = new ManagementEventWatcher(removeQuery);
            removeWatcher.EventArrived += new EventArrivedEventHandler(DeviceRemovedEvent);
            removeWatcher.Start();
        }

        private List<string> GetImages(string directory)
        {
            List<string> extensions = new List<string> { ".jpg", ".jpeg", ".png", ".gif", ".tif", ".bmp" };

            return Directory.GetFiles(directory, "*.*", SearchOption.AllDirectories).Where(x => extensions.IndexOf(Path.GetExtension(x)) >= 0).ToList();
        }

        private void CopyFiles(List<string> files, string destinationDirectory)
        {
            if (files.Count > 0)
            {
                DisplayProgress(files.Count, 1);
                foreach (var file in files)
                {
                    UpdateProgressBar();
                    UpdateProgressBarText(files.IndexOf(file) + 1, files.Count);
                    File.Copy(file, $"{destinationDirectory}{file.Remove(0, file.LastIndexOf('\\'))}");
                    // For Testing
                    Thread.Sleep(1000);
                }
                HideWindow();
                HideProgress();
                MessageBox.Show("All photos imported.  You can find them in your pictures folder under today's date.", "Photos Imported!", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void DeleteFiles(List<string> files)
        {
            foreach (var filePath in files)
            {
                File.Delete(filePath);
            }
        }

        private void DisplayProgress(int max, int step)
        {
            if (progressBar1.InvokeRequired)
                Invoke(new SafeCallDelegate(() => { progressBar1.Visible = true; progressBar1.Maximum = max; progressBar1.Step = step; }));
            else
            {
                progressBar1.Maximum = max;
                progressBar1.Step = step;
                progressBar1.Visible = true;
            }
            if (label1.InvokeRequired)
                Invoke(new SafeCallDelegate(() => { UpdateProgressBarText(1, max); label1.Visible = true; }));
            else
            {
                UpdateProgressBarText(1, max);
                label1.Visible = true;
            }
        }

        private void UpdateProgressBar()
        {
            if (progressBar1.InvokeRequired)
                Invoke(new SafeCallDelegate(() => progressBar1.PerformStep()));
            else
                progressBar1.PerformStep();
        }

        private void UpdateProgressBarText(int current, int max)
        {
            Invoke(new SafeCallDelegate(() => label1.Text = string.Format(_copyabelText, current, max)));
        }

        private void HideProgress()
        {
            Invoke(new SafeCallDelegate(() => { progressBar1.Visible = false; label1.Visible = false; }));
        }

        private void RestoreWindow()
        {
            Invoke(new SafeCallDelegate(() => { CenterToScreen(); Show(); BringToFront(); TopMost = true; Focus(); WindowState = FormWindowState.Normal; notifyIcon.Visible = false; }));
        }

        private void HideWindow()
        {
            Invoke(new SafeCallDelegate(() => { Hide(); notifyIcon.Visible = true; }));
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if(this.WindowState == FormWindowState.Minimized)
            {
                HideWindow();
            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            HideWindow();
        }
    }
}
