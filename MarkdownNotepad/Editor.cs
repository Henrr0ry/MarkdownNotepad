using MarkdownSharp;
using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.tool.xml;
using iTextSharp.tool.xml.parser;
using iTextSharp.tool.xml.pipeline.html;
using iTextSharp.tool.xml.pipeline.end;
using System.Diagnostics;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.ToolTip;

namespace MarkdownNotepad
{
    public partial class Editor : Form
    {
        public Editor()
        {
            InitializeComponent();
        }
        Markdown markdown = new Markdown();
        string[] history = new string[100];
        int backhis = 1;
        bool savehis = true;
        bool canDark = false;
        bool DarkMode = false;
        string cssText;
        string SettingsPath = Application.StartupPath + "\\settings.xml";
        string SavePathMD = "",SavePathHTML = "", SavePathPDF = "";
        string OpenPath = "";
        int StartMode = 3;
        int LastSave = 0;
        bool saved = true;
        string OpenFilePath;
        bool FirstLoad = true;
        string ConvertData;
        bool Search = false;
        string OldMark = " ";
        bool OnTop = false;
        bool AutoSave = false;
        string err1 = "File is not Saved";
        string err2 = "Are you want to close without saving?";
        string LanguagePath = "";

        [DllImport("dwmapi.dll", CharSet = CharSet.Unicode, PreserveSig = false)]
        public static extern void DwmSetWindowAttribute(IntPtr hwnd,DWMWINDOWATTRIBUTE attribute,ref int pvAttribute,uint cbAttribute);
        public enum DWMWINDOWATTRIBUTE : uint
        {
            DWMWA_NCRENDERING_ENABLED,
            DWMWA_NCRENDERING_POLICY,
            DWMWA_TRANSITIONS_FORCEDISABLED,
            DWMWA_ALLOW_NCPAINT,
            DWMWA_CAPTION_BUTTON_BOUNDS,
            DWMWA_NONCLIENT_RTL_LAYOUT,
            DWMWA_FORCE_ICONIC_REPRESENTATION,
            DWMWA_FLIP3D_POLICY,
            DWMWA_EXTENDED_FRAME_BOUNDS,
            DWMWA_HAS_ICONIC_BITMAP,
            DWMWA_DISALLOW_PEEK,
            DWMWA_EXCLUDED_FROM_PEEK,
            DWMWA_CLOAK,
            DWMWA_CLOAKED,
            DWMWA_FREEZE_REPRESENTATION,
            DWMWA_PASSIVE_UPDATE_MODE,
            DWMWA_USE_HOSTBACKDROPBRUSH,
            DWMWA_USE_IMMERSIVE_DARK_MODE = 20,
            DWMWA_WINDOW_CORNER_PREFERENCE = 33,
            DWMWA_BORDER_COLOR,
            DWMWA_CAPTION_COLOR,
            DWMWA_TEXT_COLOR,
            DWMWA_VISIBLE_FRAME_BORDER_THICKNESS,
            DWMWA_SYSTEMBACKDROP_TYPE,
            DWMWA_LAST
        }

        private void webBrowser_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            if (DarkMode)
                webBrowser.DocumentText = "<style>" + cssText + "</style>" + markdown.Transform(EditBox.Text);
            else
                webBrowser.DocumentText = markdown.Transform(EditBox.Text);
            webBrowser.DocumentCompleted -= webBrowser_DocumentCompleted;
        }

        //LOAD SETTINGS
        private void Editor_Load(object sender, EventArgs e)
        {
            string[] jsonFiles = Directory.GetFiles(Application.StartupPath + "\\language", "*.xml");

            foreach (string filePath in jsonFiles)
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(filePath);

                XmlNode packnameNode = xmlDocument.SelectSingleNode("//pack-name");
                string packname = packnameNode.InnerText;

                ToolStripMenuItem ToolStripMenuItem = new ToolStripMenuItem("packname");
                ToolStripMenuItem.Text = packname;
                ToolStripMenuItem.Tag = filePath;
                ToolStripMenuItem.Click += language_click;
                languageToolStripMenuItem.DropDownItems.Add(ToolStripMenuItem);
            }
            try
            {
                cssText = File.ReadAllText(Application.StartupPath + "\\DarkMode.css");
                canDark = true;
            }
            catch
            {
                MessageBox.Show("File \"DarkMode.css\" not found!\nSpare resources loaded!","Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                cssText = "body {\r\n    color: #F5F5F5;\r\n    background-color: #1E1E1E;\r\n}\r\n\r\na {\r\n    color: #4FC3F7;\r\n}\r\n\r\nh1, h2, h3, h4, h5, h6 {\r\n    color: #F5F5F5;\r\n}";
            }
            try
            {
                XmlDocument xmlDocument = new XmlDocument();
                xmlDocument.Load(SettingsPath);

                XmlNode darkModeNode = xmlDocument.SelectSingleNode("//darkmode");
                DarkMode = bool.Parse(darkModeNode.InnerText);

                XmlNode savePathMDNode = xmlDocument.SelectSingleNode("//savepathmd");
                SavePathMD = savePathMDNode.InnerText;

                XmlNode savePathHTMLNode = xmlDocument.SelectSingleNode("//savepathhtml");
                SavePathHTML = savePathHTMLNode.InnerText;

                XmlNode savePathPDFNode = xmlDocument.SelectSingleNode("//savepathpdf");
                SavePathPDF = savePathPDFNode.InnerText;

                XmlNode OpenPathNode = xmlDocument.SelectSingleNode("//openpath");
                OpenPath = OpenPathNode.InnerText;

                XmlNode startModeNode = xmlDocument.SelectSingleNode("//startmode");
                StartMode = int.Parse(startModeNode.InnerText);

                XmlNode LanguagePathNode = xmlDocument.SelectSingleNode("//languagepath");
                LanguagePath = LanguagePathNode.InnerText;

                if (LanguagePath != "")
                {
                    Load_Language(LanguagePath);
                }

                OpenFilePath = Program.ofp;
                if (OpenFilePath != null)
                {
                    string temp = File.ReadAllText(OpenFilePath);
                    EditBox.Text = temp;
                }

                if (DarkMode == true)
                {
                    var preference = Convert.ToInt32(true);
                    DwmSetWindowAttribute(this.Handle, DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, ref preference, sizeof(uint));
                    DarkMode = true;
                    menuStrip.BackColor = Color.Black;
                    menuStrip.ForeColor = Color.White;
                    toolStrip.BackColor = Color.Black;
                    statusStrip.BackColor = Color.Black;
                    splitContainer.BackColor = Color.Black;
                    this.BackColor = Color.Black;
                    foreach (ToolStripMenuItem item in menuStrip.Items)
                    {
                        item.BackColor = Color.Black;
                        item.ForeColor = Color.White;
                        if (item is ToolStripMenuItem menuItem)
                        {
                            foreach (ToolStripItem childItem in menuItem.DropDownItems)
                            {
                                if (childItem is ToolStripMenuItem childMenuItem)
                                {
                                    SetToolStripMenuItemBackColor(menuItem, Color.Black, Color.White);
                                }
                            }
                        }
                    }
                    EditBox.BackColor = Color.FromArgb(30, 30, 30);
                    EditBox.ForeColor = Color.White;
                    webBrowser.DocumentText = "<style>" + cssText + "</style>" + markdown.Transform(EditBox.Text);
                }
                if (StartMode == 1)
                {
                    btnEdit.Checked = true;
                    btnView.Checked = false;
                    btnSplit.Checked = false;

                    btnEdit.BackgroundImage = Properties.Resources.selected;
                    btnView.BackgroundImage = null;
                    btnSplit.BackgroundImage = null;

                    splitContainer.Panel1Collapsed = false;
                    splitContainer.Panel2Collapsed = true;
                    this.Width = nX;
                    this.Height = nY;
                }
                if (StartMode == 2)
                {
                    btnEdit.Checked = false;
                    btnView.Checked = true;
                    btnSplit.Checked = false;

                    btnEdit.BackgroundImage = null;
                    btnView.BackgroundImage = Properties.Resources.selected;
                    btnSplit.BackgroundImage = null;

                    splitContainer.Panel1Collapsed = true;
                    splitContainer.Panel2Collapsed = false;
                    this.Width = nX;
                    this.Height = nY;
                }
                if (StartMode == 3)
                {
                    btnEdit.Checked = false;
                    btnView.Checked = false;
                    btnSplit.Checked = true;

                    btnEdit.BackgroundImage = null;
                    btnView.BackgroundImage = null;
                    btnSplit.BackgroundImage = Properties.Resources.selected;

                    splitContainer.Panel1Collapsed = false;
                    splitContainer.Panel2Collapsed = false;
                    this.Width = sX;
                    this.Height = sY;
                }
            }
            catch { MessageBox.Show("File \"settings.xml\" not found!\nYour settings have not been loaded!", "Error",MessageBoxButtons.OK,MessageBoxIcon.Error); }
        }
        private void language_click(object sender, EventArgs e)
        {
            if (sender is ToolStripMenuItem menuItem)
            {
                string tagValue = menuItem.Tag?.ToString();
                LanguagePath = tagValue;
                Load_Language(tagValue);
            }
        }

        private void Load_Language(string path)
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(path);

            XmlNode backNode = xmlDocument.SelectSingleNode("//back");
            btnBack.Text = backNode.InnerText;

            XmlNode returnNode = xmlDocument.SelectSingleNode("//undo");
            btnReturn.Text = returnNode.InnerText;

            XmlNode quickNode = xmlDocument.SelectSingleNode("//quicksave");
            QuickSaveStripButton.Text = quickNode.InnerText;

            XmlNode searchNode = xmlDocument.SelectSingleNode("//search");
            SearchStripButton.Text = searchNode.InnerText;

            XmlNode err1Node = xmlDocument.SelectSingleNode("//err1");
            err1 = err1Node.InnerText;

            XmlNode err2Node = xmlDocument.SelectSingleNode("//err2");
            err2 = err2Node.InnerText;

            XmlNode fileNode = xmlDocument.SelectSingleNode("//file//title");
            fileToolStripMenuItem.Text = fileNode.InnerText;

            XmlNode newNode = xmlDocument.SelectSingleNode("//file//new");
            newToolStripMenuItem.Text = newNode.InnerText;

            XmlNode openNode = xmlDocument.SelectSingleNode("//file//open");
            openToolStripMenuItem.Text = openNode.InnerText;

            XmlNode saveNode = xmlDocument.SelectSingleNode("//file//save");
            saveToolStripMenuItem.Text = saveNode.InnerText;

            XmlNode saveasNode = xmlDocument.SelectSingleNode("//file//saveas");
            saveAsToolStripMenuItem.Text = saveasNode.InnerText;
            SaveAsStripButton.Text = saveasNode.InnerText + " Markdown";

            XmlNode editNode = xmlDocument.SelectSingleNode("//edit//title");
            editToolStripMenuItem.Text = editNode.InnerText;

            XmlNode m1Node = xmlDocument.SelectSingleNode("//edit//m1");
            editModeToolStripMenuItem.Text = m1Node.InnerText;
            btnEdit.Text = m1Node.InnerText;

            XmlNode m2Node = xmlDocument.SelectSingleNode("//edit//m2");
            viewModeToolStripMenuItem.Text = m2Node.InnerText;
            btnView.Text = m2Node.InnerText;

            XmlNode m3Node = xmlDocument.SelectSingleNode("//edit//m3");
            splitEditAndViewToolStripMenuItem.Text = m3Node.InnerText;
            btnSplit.Text = m3Node.InnerText;

            XmlNode settingsNode = xmlDocument.SelectSingleNode("//settings//title");
            settingsToolStripMenuItem.Text = settingsNode.InnerText;

            XmlNode colorNode = xmlDocument.SelectSingleNode("//settings//color");
            colorThemeToolStripMenuItem.Text = colorNode.InnerText;

            XmlNode lightNode = xmlDocument.SelectSingleNode("//settings//light");
            lightToolStripMenuItem.Text = lightNode.InnerText;

            XmlNode darkNode = xmlDocument.SelectSingleNode("//settings//dark");
            darkToolStripMenuItem.Text = darkNode.InnerText;

            XmlNode fontNode = xmlDocument.SelectSingleNode("//settings//font");
            fontToolStripMenuItem.Text = fontNode.InnerText;

            XmlNode autosaveNode = xmlDocument.SelectSingleNode("//settings//autosave");
            autoSaveToolStripMenuItem.Text = autosaveNode.InnerText;

            XmlNode aotNode = xmlDocument.SelectSingleNode("//settings//alwaysontop");
            alwaysOnTopToolStripMenuItem.Text = aotNode.InnerText;

            XmlNode aboutNode = xmlDocument.SelectSingleNode("//settings//about");
            aboutToolStripMenuItem.Text = aboutNode.InnerText;

            XmlNode languageNode = xmlDocument.SelectSingleNode("//settings//language");
            languageToolStripMenuItem.Text = languageNode.InnerText;
        }

        //MENUSTRIP
        private void lightToolStripMenuItem_Click(object sender, EventArgs e)
        {
            DarkMode = false;
            menuStrip.BackColor = SystemColors.Control;
            toolStrip.BackColor = SystemColors.Control;
            statusStrip.BackColor = SystemColors.Control;
            splitContainer.BackColor = SystemColors.Control;
            this.BackColor = SystemColors.Control;
            foreach (ToolStripMenuItem item in menuStrip.Items)
            {
                item.BackColor = SystemColors.Control; ;
                item.ForeColor = Color.Black;
                if (item is ToolStripMenuItem menuItem)
                {
                    foreach (ToolStripItem childItem in menuItem.DropDownItems)
                    {
                        if (childItem is ToolStripMenuItem childMenuItem)
                        {
                            SetToolStripMenuItemBackColor(menuItem, SystemColors.Control, Color.Black);
                        }
                    }
                }
            }
            EditBox.BackColor = Color.White;
            EditBox.ForeColor = Color.Black;
            webBrowser.DocumentText = markdown.Transform(EditBox.Text);
            marked();
        }
        private void darkToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var preference = Convert.ToInt32(true);
            DwmSetWindowAttribute(this.Handle,DWMWINDOWATTRIBUTE.DWMWA_USE_IMMERSIVE_DARK_MODE, ref preference, sizeof(uint));
            DarkMode = true;
            menuStrip.BackColor = Color.Black;
            menuStrip.ForeColor = Color.White;
            toolStrip.BackColor = Color.Black;
            statusStrip.BackColor = Color.Black;
            splitContainer.BackColor = Color.Black;
            this.BackColor = Color.Black;
            foreach (ToolStripMenuItem item in menuStrip.Items)
            {
                item.BackColor = Color.Black;
                item.ForeColor = Color.White;
                if (item is ToolStripMenuItem menuItem)
                {
                    foreach (ToolStripItem childItem in menuItem.DropDownItems)
                    {
                        if (childItem is ToolStripMenuItem childMenuItem)
                        {
                            SetToolStripMenuItemBackColor(menuItem, Color.Black,Color.White);
                        }
                    }
                }
            }
            EditBox.BackColor = Color.FromArgb(30, 30, 30);
            EditBox.ForeColor = Color.White;
            webBrowser.DocumentText = "<style>" + cssText + "</style>" + markdown.Transform(EditBox.Text);
            marked();
        }

        void SetToolStripMenuItemBackColor(ToolStripMenuItem menuItem, Color bcolor, Color fcolor)
        {
            menuItem.BackColor = bcolor;
            menuItem.ForeColor = fcolor;

            foreach (ToolStripItem childItem in menuItem.DropDownItems)
            {
                if (childItem is ToolStripMenuItem childMenuItem)
                {
                    SetToolStripMenuItemBackColor(childMenuItem, bcolor, fcolor);
                }
            }
        }

        void SetBackColorRecursive(Control control, Color color)
        {
            control.BackColor = color;

            foreach (Control c in control.Controls)
                SetBackColorRecursive(c, color);
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            var ofd = new OpenFileDialog();

            ofd.Filter = "Markdown Files (*.md)|*.md|All Files (*.*)|*.*";
            ofd.FileName = OpenPath;

            var result = ofd.ShowDialog();

            if (result == DialogResult.OK)
            {
                OpenPath = ofd.FileName;
                EditBox.Text = File.ReadAllText(OpenPath);
                saved = true;
                SaveIcon.Image = Properties.Resources.saved;
                backhis = 0;
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (LastSave == 1)
            {
                saved = true;
                SaveIcon.Image = Properties.Resources.saved;
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Title = "Save as markdown";
                sfd.Filter = "Markdown Files (*.md)| *.md|Text Files (*.txt)| *.txt";
                sfd.FileName = SavePathMD;

                using (StreamWriter sw = new StreamWriter(sfd.FileName))
                {
                    sw.Write(EditBox.Text);
                    SavePathMD = sfd.FileName;
                    LastSave = 1;
                }
            }
            else if (LastSave == 2)
            {
                saved = true;
                SaveIcon.Image = Properties.Resources.saved;
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Title = "Save as HTML";
                sfd.Filter = "HTML Files (*.html)| *.html|Text Files (*.txt)| *.txt";
                sfd.FileName = SavePathHTML;

                using (StreamWriter sw = new StreamWriter(sfd.FileName))
                {
                    sw.Write(webBrowser.DocumentText);
                    SavePathHTML = sfd.FileName;
                    LastSave = 2;
                }
            }
            else if (LastSave == 3)
            {
                saved = true;
                SaveIcon.Image = Properties.Resources.saved;
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Title = "Save as PDF";
                sfd.Filter = "PDF Files (*.pdf)| *.pdf|Text Files (*.txt)| *.txt";
                sfd.FileName = SavePathPDF;

                using (var stream = new FileStream(sfd.FileName, FileMode.Create))
                {
                    using (var document = new Document())
                    {
                        PdfWriter writer = PdfWriter.GetInstance(document, stream);
                        document.Open();

                        // převod HTML na PDF
                        using (var stringReader = new StringReader(webBrowser.DocumentText))
                        {
                            XMLWorkerHelper.GetInstance().ParseXHtml(writer, document, stringReader);
                        }
                    }
                }

                SavePathPDF = sfd.FileName;
                    LastSave = 3;
                
            }
            else
            {
                MessageBox.Show("First you need to \"Save As\" something!","Error",MessageBoxButtons.OK,MessageBoxIcon.Warning);
            }
        }

        private void markdownToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "Save as markdown";
            sfd.Filter = "Markdown Files (*.md)| *.md|Text Files (*.txt)| *.txt";
            sfd.FileName = SavePathMD;

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                using (StreamWriter sw = new StreamWriter(sfd.FileName))
                {
                    saved = true;
                    SaveIcon.Image = Properties.Resources.saved;
                    sw.Write(EditBox.Text);
                    SavePathMD = sfd.FileName;
                    LastSave = 1;
                }
            }
        }
        private void hTMLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "Save as HTML";
            sfd.Filter = "HTML Files (*.html)| *.html|Text Files (*.txt)| *.txt";
            sfd.FileName = SavePathHTML;

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                using (StreamWriter sw = new StreamWriter(sfd.FileName))
                {
                    saved = true;
                    SaveIcon.Image = Properties.Resources.saved;
                    sw.Write(webBrowser.DocumentText);
                    SavePathHTML = sfd.FileName;
                    LastSave = 2;
                }
            }
        }

        private void pDFToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "Save as PDF";
            sfd.Filter = "PDF Files (*.pdf)| *.pdf|Text Files (*.txt)| *.txt";
            sfd.FileName = SavePathPDF;

            if (sfd.ShowDialog() == DialogResult.OK)
            {
                var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(webBrowser.DocumentText));
                Document document = new Document();
                document = new Document(PageSize.A4, 50, 50, 50, 50);
                var writer = PdfWriter.GetInstance(document, new FileStream(sfd.FileName, FileMode.Create));
                document.Open();

                XMLWorkerHelper.GetInstance().ParseXHtml(writer, document, ms, null, System.Text.Encoding.UTF8);
                document.Close();
                writer.Close();

                saved = true;
                SaveIcon.Image = Properties.Resources.saved;
                SavePathPDF = sfd.FileName;
                LastSave = 3;
            }
        }


        private void newToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Process.Start(Application.ExecutablePath);
        }

        private void fontToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (FontDialog fontDialog = new FontDialog())
            {
                DialogResult result = fontDialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    System.Drawing.Font selectedFont = fontDialog.Font;

                    // Použít vybraný font
                    EditBox.Font = selectedFont;
                    webBrowser.Document.ExecCommand("FontName", false, selectedFont.Name);
                    webBrowser.Document.ExecCommand("FontSize", false, selectedFont.SizeInPoints.ToString());
                }
            }
        }

        private void autoSaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!AutoSave)
            {
                AutoSave = true;
                autoSaveToolStripMenuItem.Image = Properties.Resources._true;
                AutoSaveTimer.Start();
            }
            else
            {
                AutoSave = false;
                autoSaveToolStripMenuItem.Image = Properties.Resources._false;
                AutoSaveTimer.Stop();
            }
        }
        private void alwaysOnTopToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!OnTop)
            {
                this.TopMost = true;
                alwaysOnTopToolStripMenuItem.Image = Properties.Resources._true;
                OnTop = true;
            }
            else
            {
                this.TopMost = false;
                alwaysOnTopToolStripMenuItem.Image = Properties.Resources._false;
                OnTop = false;
            }
        }

        private void AutoSaveTimer_Tick(object sender, EventArgs e)
        {
            if (LastSave == 1)
            {
                saved = true;
                SaveIcon.Image = Properties.Resources.saved;
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Title = "Save as markdown";
                sfd.Filter = "Markdown Files (*.md)| *.md|Text Files (*.txt)| *.txt";
                sfd.FileName = SavePathMD;

                using (StreamWriter sw = new StreamWriter(sfd.FileName))
                {
                    sw.Write(EditBox.Text);
                    SavePathMD = sfd.FileName;
                    LastSave = 1;
                }
            }
            else if (LastSave == 2)
            {
                saved = true;
                SaveIcon.Image = Properties.Resources.saved;
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Title = "Save as HTML";
                sfd.Filter = "HTML Files (*.html)| *.html|Text Files (*.txt)| *.txt";
                sfd.FileName = SavePathHTML;

                using (StreamWriter sw = new StreamWriter(sfd.FileName))
                {
                    sw.Write(webBrowser.DocumentText);
                    SavePathHTML = sfd.FileName;
                    LastSave = 2;
                }
            }
            else if (LastSave == 3)
            {
                saved = true;
                SaveIcon.Image = Properties.Resources.saved;
                SaveFileDialog sfd = new SaveFileDialog();
                sfd.Title = "Save as PDF";
                sfd.Filter = "PDF Files (*.pdf)| *.pdf|Text Files (*.txt)| *.txt";
                sfd.FileName = SavePathPDF;

                using (var stream = new FileStream(sfd.FileName, FileMode.Create))
                {
                    using (var document = new Document())
                    {
                        PdfWriter writer = PdfWriter.GetInstance(document, stream);
                        document.Open();

                        // převod HTML na PDF
                        using (var stringReader = new StringReader(webBrowser.DocumentText))
                        {
                            XMLWorkerHelper.GetInstance().ParseXHtml(writer, document, stringReader);
                        }
                    }
                }

                SavePathPDF = sfd.FileName;
                LastSave = 3;

            }
            else
            {
                MessageBox.Show("First you need to \"Save As\" something!", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AboutBox aboutBox = new AboutBox();
            aboutBox.ShowDialog();
        }

        //TOOLSTRIP BUTTONS
        private void btnBack_Click(object sender, EventArgs e)
        {
            savehis = false;
            EditBox.Text = history[backhis];
            if (backhis != 99)
                backhis++;
            savehis = true;
        }

        private void btnReturn_Click(object sender, EventArgs e)
        {
            savehis = false;
            EditBox.Text = history[backhis];
            if (backhis != 0)
                backhis--;
            savehis = true;
        }
        int nX = 500, nY = 700;
        int sX = 1000, sY = 700;

        private void btnEdit_Click(object sender, EventArgs e)
        {
            StartMode = 1;
            btnEdit.Checked = true;
            btnView.Checked = false;
            btnSplit.Checked = false;

            btnEdit.BackgroundImage = Properties.Resources.selected;
            btnView.BackgroundImage = null;
            btnSplit.BackgroundImage = null;

            splitContainer.Panel1Collapsed = false;
            splitContainer.Panel2Collapsed = true;
            this.Width = nX;
            this.Height = nY;
        }

        private void btnView_Click(object sender, EventArgs e)
        {
            StartMode = 2;
            btnEdit.Checked = false;
            btnView.Checked = true;
            btnSplit.Checked = false;

            btnEdit.BackgroundImage = null;
            btnView.BackgroundImage = Properties.Resources.selected;
            btnSplit.BackgroundImage = null;

            splitContainer.Panel1Collapsed = true;
            splitContainer.Panel2Collapsed = false;
            this.Width = nX;
            this.Height = nY;
        }

        private void btnSplit_Click(object sender, EventArgs e)
        {
            StartMode = 3;
            btnEdit.Checked = false;
            btnView.Checked = false;
            btnSplit.Checked = true;

            btnEdit.BackgroundImage = null;
            btnView.BackgroundImage = null;
            btnSplit.BackgroundImage = Properties.Resources.selected;

            splitContainer.Panel1Collapsed = false;
            splitContainer.Panel2Collapsed = false;
            this.Width = sX;
            this.Height = sY;
        }

        private void marked()
        {
            if (SearchBox.Text != "")
            {
                EditBox.Select(0, EditBox.Text.Length);
                if (DarkMode)
                    EditBox.SelectionBackColor = Color.FromArgb(30, 30, 30);
                else
                    EditBox.SelectionBackColor = Color.White;
                int index = EditBox.Text.IndexOf(SearchBox.Text, StringComparison.OrdinalIgnoreCase);
                while (index >= 0)
                {
                    Search = true;
                    EditBox.Select(index, SearchBox.Text.Length);
                    if (DarkMode)
                        EditBox.SelectionBackColor = Color.Orange;
                    else
                        EditBox.SelectionBackColor = Color.Yellow;

                    index = EditBox.Text.IndexOf(SearchBox.Text, index + 1, StringComparison.OrdinalIgnoreCase);
                }
            }
        }

        private void languageToolStripMenuItem_Click(object sender, EventArgs e)
        {

        }

        private void SearchBox_KeyDown(object sender, KeyEventArgs e)
        {
            switch (e.KeyCode)
            {
                case Keys.Enter:
                    marked();
                    break;
            }
        }

        private void SearchStripButton_Click(object sender, EventArgs e)
        {
            marked();
        }

        //CONVERT TO VIEW
        private void EditTextbox_TextChanged(object sender, EventArgs e)
        {
            saved = false;
            SaveIcon.Image = Properties.Resources.notsaved;
            ConvertData = markdown.Transform(EditBox.Text);
            if (SearchBox.Text != "" & Search == true)
            {
                string highlightedText = "<span class=\"mark\">" + SearchBox.Text + "</span>";
                ConvertData = ConvertData.Replace(SearchBox.Text, highlightedText);
                Search = false;
                OldMark = SearchBox.Text;
            }
            if (SearchBox.Text != "" & Search == false)
            {
                string highlightedText = "<span class=\"mark\">" + OldMark + "</span>";
                ConvertData = ConvertData.Replace(OldMark, highlightedText);
            }
            if (DarkMode)
                ConvertData = "<style>.mark {background-color: orange;}</style>" + "<style>" + cssText + "</style>" + ConvertData;
            else
                ConvertData = "<style>.mark {background-color: yellow;}</style>" + ConvertData;
            if (savehis)
            {
                for(int i = 98; i > 0; i--) 
                {
                    history[i] = history[i - 1];
                }
                history[0] = EditBox.Text;
                backhis = 1;
            }
            webBrowser.DocumentText = ConvertData;
        }

        //FORM CLOSING
        private void Editor_FormClosing(object sender, FormClosingEventArgs e)
        {
            XmlDocument xmlDoc = new XmlDocument();
            XmlDeclaration xmlDeclaration = xmlDoc.CreateXmlDeclaration("1.0", "UTF-8", null);
            xmlDoc.AppendChild(xmlDeclaration);

            XmlElement root = xmlDoc.CreateElement("settings");
            xmlDoc.AppendChild(root);

            XmlElement darkModeElement = xmlDoc.CreateElement("darkmode");
            darkModeElement.InnerText = DarkMode.ToString();
            root.AppendChild(darkModeElement);

            XmlElement savePathMDElement = xmlDoc.CreateElement("savepathmd");
            savePathMDElement.InnerText = SavePathMD;
            root.AppendChild(savePathMDElement);

            XmlElement savePathHTMLElement = xmlDoc.CreateElement("savepathhtml");
            savePathHTMLElement.InnerText = SavePathHTML;
            root.AppendChild(savePathHTMLElement);

            XmlElement savePathPDFElement = xmlDoc.CreateElement("savepathpdf");
            savePathPDFElement.InnerText = SavePathPDF;
            root.AppendChild(savePathPDFElement);

            XmlElement openPathElement = xmlDoc.CreateElement("openpath");
            openPathElement.InnerText = OpenPath;
            root.AppendChild(openPathElement);

            XmlElement startModeElement = xmlDoc.CreateElement("startmode");
            startModeElement.InnerText = StartMode.ToString();
            root.AppendChild(startModeElement);

            XmlElement LanguagePathElement = xmlDoc.CreateElement("languagepath");
            LanguagePathElement.InnerText = LanguagePath;
            root.AppendChild(LanguagePathElement);

            xmlDoc.Save(SettingsPath);

            if (saved == false)
            {
                if (MessageBox.Show(err2,err1, MessageBoxButtons.YesNo, MessageBoxIcon.Asterisk) == DialogResult.No)
                    e.Cancel = true;
            }
        }
    }
}
