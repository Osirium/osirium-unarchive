using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using Ionic.Zip;
using System.Diagnostics;
using System.Threading;
using System.Drawing.Imaging;

namespace Archive
{
    public partial class InputForm : Form
    {
        public InputForm()
        {
            InitializeComponent();

            this.BackgroundImageLayout = ImageLayout.Zoom;
        }

        private void Form1_DragDrop(object sender, DragEventArgs e)
        {
            ThreadPool.QueueUserWorkItem(new WaitCallback((state) =>
            {
                this.Invoke(new Action(() =>
                {
                    this.progressBar.Visible = true;
                }));

                string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop, false);

                foreach (string path in paths)
                {
                    DirectoryInfo temporyDirectory = Directory.CreateDirectory(Path.Combine(Path.GetTempPath(), String.Format("osirium-unarchive-{0}", DateTime.UtcNow.Ticks)));

                    try
                    {
                        int index = 0;
                        int indexWidth = 0;

                        using (ZipFile zipfile = new ZipFile(path))
                        {
                            List<ZipEntry> entries = zipfile.Where(entry => Path.GetExtension(entry.FileName).Equals(".png", StringComparison.InvariantCultureIgnoreCase)).ToList();

                            this.Invoke(new Action(() =>
                            {
                                this.progressBar.Maximum = entries.Count + 1;
                            }));

                            indexWidth = entries.Count.ToString().Length;

                            using (Bitmap resized = new Bitmap(1920, 1080))
                            {
                                foreach (ZipEntry entry in entries.ToArray())
                                {
                                    using (MemoryStream data = new MemoryStream())
                                    {
                                        entry.Extract(data);

                                        data.Seek(0, SeekOrigin.Begin);

                                        using (Image image = Bitmap.FromStream(data))
                                        {
                                            using (Graphics g = Graphics.FromImage(resized))
                                            {
                                                g.Clear(Color.Black);
                                                g.DrawImage(image, new Point((resized.Width - image.Width) / 2, (resized.Height - image.Height) / 2));
                                            }

                                            resized.Save(Path.Combine(temporyDirectory.FullName, String.Format("{0}.png", (index++).ToString(String.Format("D{0}", indexWidth)))), ImageFormat.Png);
                                        }
                                    }

                                    this.Invoke(new Action(() =>
                                    {
                                        this.progressBar.Value = index;
                                    }));
                                }

                                using (Graphics g = Graphics.FromImage(resized))
                                {
                                    g.Clear(Color.Black);
                                }

                                resized.Save(Path.Combine(temporyDirectory.FullName, String.Format("{0}.png", (index++).ToString(String.Format("D{0}", indexWidth)))), ImageFormat.Png);

                                this.Invoke(new Action(() =>
                                {
                                    this.progressBar.Value = index;
                                }));
                            }
                        }

                        string target = "output.wmv";
                        string output = Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), target);

                        string arguments = String.Format("-y -loglevel warning -r 1 -b:v 1800 -i %0{0}d.png {1}", indexWidth, target);

                        using (Process process = Process.Start(new ProcessStartInfo(Path.Combine(Path.GetDirectoryName(Application.ExecutablePath), "ffmpeg"), arguments) { WorkingDirectory = temporyDirectory.FullName, CreateNoWindow = true, UseShellExecute = false }))
                        {
                            process.WaitForExit();

                            if (process.ExitCode > 0)
                            {
                                throw new SystemException("Failed to generate video");
                            }
                        }

                        if (File.Exists(output))
                        {
                            File.Delete(output);
                        }

                        File.Copy(Path.Combine(temporyDirectory.FullName, target), output);
                    }
                    finally
                    {
                        temporyDirectory.Delete(true);

                        this.Invoke(new Action(() =>
                        {
                            this.progressBar.Visible = false;
                        }));
                    }
                }
            }), null);
        }
    
        private void Form1_Load(object sender, EventArgs e)
        {
            this.AllowDrop = true;
        }

        private void Form1_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] paths = (string[])e.Data.GetData(DataFormats.FileDrop, false);

                if (paths.Length == 1 && paths.All(path => Path.GetExtension(path).Equals(".zip", StringComparison.InvariantCultureIgnoreCase)) && paths.All(path => ZipFile.IsZipFile(path)))
                {
                    e.Effect = DragDropEffects.Copy;

                    return;
                }
            }
            e.Effect = DragDropEffects.None;
        }
    }
}
