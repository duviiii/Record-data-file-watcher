using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Drawing.Imaging;
using System.Reflection;

namespace FileChangeNotifier
{
    public partial class frmNotifier : Form
    {
        private StringBuilder m_Sb;
        private bool m_bDirty;
        private System.IO.FileSystemWatcher m_Watcher;
        private bool m_bIsWatching;

        public frmNotifier()
        {
            InitializeComponent();
            m_Sb = new StringBuilder();
            m_bDirty = false;
            m_bIsWatching = false;
        }

        private void stopWatching()
        {
            m_bIsWatching = false;
            m_Watcher.EnableRaisingEvents = false;
            m_Watcher.Dispose();
            if (btnWatchFile.InvokeRequired)
            {
                m_stopWatchingText changeText = stopWatchingText;
                Invoke(changeText);
            }
            else
            {
                stopWatchingText();
            }
        }

        private void stopWatchingText()
        {
            btnWatchFile.BackColor = Color.LightSkyBlue;
            btnWatchFile.Text = "Start Watching";
        }

        delegate void m_stopWatchingText();

        private void btnWatchFile_Click(object sender, EventArgs e)
        {
            if (m_bIsWatching)
            {
                stopWatching();
            }
            else
            {
                m_bIsWatching = true;
                btnWatchFile.BackColor = Color.Red;
                btnWatchFile.Text = "Stop Watching";

                m_Watcher = new System.IO.FileSystemWatcher();
                if (rdbDir.Checked)
                {
                    m_Watcher.Filter = "*.*";
                    m_Watcher.Path = txtFile.Text + "\\";
                }
                else
                {
                    m_Watcher.Filter = txtFile.Text.Substring(txtFile.Text.LastIndexOf('\\') + 1);
                    m_Watcher.Path = txtFile.Text.Substring(0, txtFile.Text.Length - m_Watcher.Filter.Length);
                }

                if (chkSubFolder.Checked)
                {
                    m_Watcher.IncludeSubdirectories = true;
                }

                m_Watcher.NotifyFilter = NotifyFilters.LastAccess | NotifyFilters.LastWrite
                                     | NotifyFilters.FileName | NotifyFilters.DirectoryName;
                m_Watcher.Changed += new FileSystemEventHandler(OnChanged);
                m_Watcher.Created += new FileSystemEventHandler(OnChanged);
                m_Watcher.Deleted += new FileSystemEventHandler(OnChanged);
                m_Watcher.Renamed += new RenamedEventHandler(OnRenamed);
                m_Watcher.EnableRaisingEvents = true;
            }
        }

        private void OnChanged(object sender, FileSystemEventArgs e)
        {
            if (!m_bDirty)
            {
                m_Sb.Remove(0, m_Sb.Length);
                m_Sb.Append(e.FullPath);
                m_Sb.Append(" ");
                m_Sb.Append(e.ChangeType.ToString());
                m_Sb.Append("    ");
                m_Sb.Append(DateTime.Now.ToString());
                if (e.ChangeType.ToString().Equals("Deleted"))
                {
                    stopWatching();
                }
                else
                {
                    var bmpScreenShot = new Bitmap(Screen.PrimaryScreen.Bounds.Width,
                                                   Screen.PrimaryScreen.Bounds.Height,
                                                   PixelFormat.Format32bppArgb);

                    var gfxScreenShot = Graphics.FromImage(bmpScreenShot);

                    gfxScreenShot.CopyFromScreen(Screen.PrimaryScreen.Bounds.X,
                                                 Screen.PrimaryScreen.Bounds.Y,
                                                 0,
                                                 0,
                                                 Screen.PrimaryScreen.Bounds.Size,
                                                 CopyPixelOperation.SourceCopy);

                    drawAOI(gfxScreenShot);

                    var name = getFileName();

                    bmpScreenShot.Save(getFileName(), ImageFormat.Png);
                }
                m_bDirty = true;
            }

        }

        private void drawAOI(Graphics gfxScreenShot)
        {
            string line;
            System.IO.StreamReader file = new System.IO.StreamReader(txtFile.Text);
            while ((line = file.ReadLine()) != null)
            {
                Pen myPen = null;
                string[] data = line.Split('\t');

                if (data[2] == "click")
                {
                    continue;
                }

                var visible = data[4];
                if (visible == "false") continue;
                var width = Int32.Parse(data[5]);
                var height = Int32.Parse(data[6]);
                var x = Int32.Parse(data[11]);
                var y = Int32.Parse(data[12]);

                switch(data[2]){
                    case "button":
                        myPen = new Pen(Brushes.DarkRed);
                        break;
                    case "link":
                        myPen = new Pen(Brushes.Aqua);
                        break;
                    case "text":
                        myPen = new Pen(Brushes.Black);
                        break;
                    case "image":
                        myPen = new Pen(Brushes.DarkOrange);
                        break;
                }
                     
                var rect = new Rectangle(x, y, width, height);
                gfxScreenShot.DrawRectangle(myPen, rect);
            }
            file.Close();
        }

        private String getFileName()
        {
            var time = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            return "E:\\Record data\\Screenshot_" + time + ".png";
        }

        private void OnRenamed(object sender, RenamedEventArgs e)
        {
            stopWatching();      
        }

        private void tmrEditNotify_Tick(object sender, EventArgs e)
        {
            if (m_bDirty)
            {
                lstNotification.BeginUpdate();
                lstNotification.Items.Add(m_Sb.ToString());
                lstNotification.EndUpdate();
                m_bDirty = false;
            }
        }

        private void btnBrowseFile_Click(object sender, EventArgs e)
        {
            if (rdbDir.Checked)
            {
                DialogResult resDialog = dlgOpenDir.ShowDialog();
                if (resDialog.ToString() == "OK")
                {
                    txtFile.Text = dlgOpenDir.SelectedPath;
                }
            }
            else
            {
                DialogResult resDialog = dlgOpenFile.ShowDialog();
                if (resDialog.ToString() == "OK")
                {
                    txtFile.Text = dlgOpenFile.FileName;
                }
            }
        }

        private void btnLog_Click(object sender, EventArgs e)
        {
            DialogResult resDialog = dlgSaveFile.ShowDialog();
            if (resDialog.ToString() == "OK")
            {
                FileInfo fi = new FileInfo(dlgSaveFile.FileName);
                StreamWriter sw = fi.CreateText();
                foreach (string sItem in lstNotification.Items)
                {
                    sw.WriteLine(sItem);
                }
                sw.Close();
            }
        }

        private void rdbFile_CheckedChanged(object sender, EventArgs e)
        {
            if (rdbFile.Checked == true)
            {
                chkSubFolder.Enabled = false;
                chkSubFolder.Checked = false;
            }
        }

        private void rdbDir_CheckedChanged(object sender, EventArgs e)
        {
            if (rdbDir.Checked == true)
            {
                chkSubFolder.Enabled = true;
            }
        }
    }
}