using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;

namespace HangulImgViewer
{
    public partial class MainFrm : Form
    {
        private Dictionary<string, HwpDocument> docs = new Dictionary<string, HwpDocument>();
        private HwpDocument selectedDoc = null;

        public MainFrm()
        {
            InitializeComponent();
        }

        private void MnuSelHWP_Click(object sender, EventArgs e)
        {
            cleanUp();

            using (var selDlg = new OpenFileDialog())
            {
                selDlg.Title = "Select HWP files...";
                selDlg.Multiselect = true;
                selDlg.Filter = "(HWP file)|*.hwp";
                if( selDlg.ShowDialog() == DialogResult.OK )
                {
                    foreach(var filename in selDlg.FileNames)
                    {
                        docs.Add(filename, new HwpDocument(filename));
                    }
                }
            }

            foreach(var doc in docs)
            {
                doc.Value.Open(hwp);
            }
            displayDocumentList();
        }

        private void MnuClearList_Click(object sender, EventArgs e)
        {
            cleanUp();
        }

        private void MnuQuit_Click(object sender, EventArgs e)
        {
            Close();
        }
        private void MnuAbout_Click(object sender, EventArgs e)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo fvi = FileVersionInfo.GetVersionInfo(assembly.Location);
            string version = fvi.FileVersion;

            MessageBox.Show(this, string.Format("Hangul Image Viewer {0}", version), "About...",
                MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void MnuAboutHWP_Click(object sender, EventArgs e)
        {
            hwp.AboutBox();
        }

        private void displayDocumentList()
        {
            foreach(var doc in docs.Values)
            {
                var di = new ListViewItem(new string[]{
                    Path.GetFileName(doc.HwpFilePath),
                    doc.State.ToString(),
                    doc.GetImageCount().ToString()
                });
                di.Tag = doc.HwpFilePath;

                fileList.Items.Add(di);
            }
        }

        private void fileList_SelectedItemChanged(object sender, EventArgs e)
        {
            cbImage.Items.Clear();
            pictureBox.Image = null;

            var item = fileList.SelectedItems.Cast<ListViewItem>().FirstOrDefault();
            if( item == null )
            {
                return;
            }

            var key = item.Tag.ToString();

            HwpDocument doc = null;
            if(docs.TryGetValue(key, out doc))
            {
                selectedDoc = doc;

                if( doc.State != HwpDocument.HwpState.HmlReady )
                {
                    return;
                }

                var imgList = doc.GetImageBinaryDataID();
                foreach(var img in imgList)
                {
                    cbImage.Items.Add(img);
                }

                if( imgList.Length > 0 )
                {
                    cbImage.SelectedIndex = 0;
                }
            }
        }

        private void cbImage_SelectedIndexChanged(object sender, EventArgs e)
        {
            pictureBox.Image = null;

            if( selectedDoc == null )
            {
                return;
            }

            var selBinId = cbImage.Text;
            var stream = selectedDoc.GetImageStream(selBinId);
            try
            {
                pictureBox.Image = Image.FromStream(stream);
            }
            catch
            {
                pictureBox.Image = pictureBox.ErrorImage;
            }
        }

        void cleanUp()
        {
            foreach (var doc in docs.Values)
            {
                doc.Dispose();
            }
            docs.Clear();

            fileList.Items.Clear();
            cbImage.Items.Clear();
            pictureBox.Image = null;
        }

        private void MainFrm_FormClosing(object sender, FormClosingEventArgs e)
        {
            cleanUp();
        }
    }
}
