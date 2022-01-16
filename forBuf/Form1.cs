using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Windows.Forms;

namespace forBuf
{
    public partial class Form1 : Form
    {
        static String ip = "";
        static String key = "";
        String version = "5";


        private static TextBox tBox1;

        String Dropfile = "";

        ToolTip toolTipBtnUpdate;

        #region Библиотеки
        [DllImport("user32.dll")]
        static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc callback, IntPtr hInstance, uint threadId);

        [DllImport("user32.dll")]
        static extern bool UnhookWindowsHookEx(IntPtr hInstance);

        [DllImport("kernel32.dll")]
        static extern IntPtr LoadLibrary(string lpFileName);

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        const int WH_KEYBOARD_LL = 13; // Номер глобального LowLevel-хука на клавиатуру
        const int
            WM_KEYDOWN = 0x100,       //Key down
            WM_KEYUP = 0x101;         //Key up

        private LowLevelKeyboardProc _proc = hookProc;

        private static IntPtr hhook = IntPtr.Zero;

        public static int count = 0;

        public static bool press_ctrl_pre = false;
        public static bool press_c_pre = false;
        public static bool press_v_pre = false;
        public static bool press_x_pre = false;

        public static bool au_copy = false;
        public static bool au_paste = false;

        public static bool ctrl_c = false;
        public static bool ctrl_v = false;
        public static bool ctrl_x = false;

        #endregion

        static String lastDir = "";

        public Form1()
        {

            //Запуск хука
            /*
            IntPtr hInstance = LoadLibrary("User32");
            hhook = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, hInstance, 0);
            */


            InitializeComponent();

            tBox1 = textBox1;
            au_copy = false;
            au_paste = false;

            toolTipBtnUpdate = new ToolTip();
            toolTipBtnUpdate.SetToolTip(buttonUdateLastFile, "Обновить последний файл" + Dropfile);
        }

        public static IntPtr hookProc(int code, IntPtr wParam, IntPtr lParam)
        {
            int vkCode = Marshal.ReadInt32(lParam); //Получить код клавиши
            if (vkCode != 0)
            {

                if (ctrl_c && au_copy || ctrl_x && au_copy)
                {
                    set_buf();
                }


                bool press_ctrl = (wParam == (IntPtr)256 && vkCode == 162);
                bool press_c = (wParam == (IntPtr)256 && vkCode == 67);
                bool press_v = (wParam == (IntPtr)256 && vkCode == 86);
                bool press_x = (wParam == (IntPtr)256 && vkCode == 88);

                ctrl_c = (press_ctrl_pre && press_c || press_c_pre && press_ctrl);
                ctrl_v = (press_ctrl_pre && press_v || press_v_pre && press_ctrl);
                ctrl_x = (press_ctrl_pre && press_x || press_x_pre && press_ctrl);

                if (ctrl_v && au_paste)
                {
                    get(key);
                }

                press_ctrl_pre = press_ctrl;
                press_c_pre = press_c;
                press_v_pre = press_v;
                press_x_pre = press_x;

            }
            return IntPtr.Zero;
        }

        static void get(String _key)
        {
            try
            {
                WebClient client = new WebClient();
                var path = String.Format("http://{0}/?get=1&key={1}", ip, _key);
                var Text = client.DownloadString(path);
                byte[] newBytes = Convert.FromBase64String(Text);

                var strText = Encoding.UTF8.GetString(newBytes);

                if (strText.Contains("clipboard="))
                {
                    var Slug = Regex.Match(strText, "key=.*?clipboard=").ToString();
                    strText = strText.Replace(Slug, "");
                    if (strText != "")
                        Clipboard.SetText(strText);

                    add_text("get buff ok");
                }

                if (strText.Contains("fileInBase64="))
                {
                    var fileName = Regex.Match(strText, "(?<=fileInBase64Name=).*?(?=,)").ToString();
                    var Slug = Regex.Match(strText, "key=.*?fileInBase64=").ToString();
                    strText = strText.Replace(Slug, "");

                    var dir = "";

                    using (var fbd = new FolderBrowserDialog())
                    {
                        if (lastDir!="")
                            fbd.SelectedPath = lastDir;
                        fbd.Description = "Место сохранения файла";
                        DialogResult result = fbd.ShowDialog();

                        if (result == DialogResult.OK && !string.IsNullOrWhiteSpace(fbd.SelectedPath))
                        {
                            dir = fbd.SelectedPath;
                            lastDir = dir;
                        }
                    }

                    if (dir == "")
                    {
                        return;
                    }

                    var file = dir + "\\" + fileName;
                    if (File.Exists(file))
                    {
                        File.Delete(file);
                    }

                    File.WriteAllBytes(file, Convert.FromBase64String(strText));

                    add_text(String.Format("download file ok ({0})", fileName));
                }

            }
            catch (Exception ex)
            {
                tBox1.Text += ex.ToString();
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {

            get(key);

            button4.Visible = check_new_ver();

        }

        private void textBox1_TextChanged(object sender, EventArgs e)
        {

        }

        private void set_sett()
        {
            notifyIcon1.Text = String.Format("{0}:{1}", ip, key);
            Text = String.Format("Test ({0}) v{1}", key, version);
        }
        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                var sett = File.ReadAllText("sett.txt");
                ip = Regex.Match(sett, @"(?<=ip=)\S*").ToString();
                key = Regex.Match(sett, @"(?<=key=)\S*").ToString();
                String keylist = Regex.Match(sett, @"(?<=keylist=)\S*").ToString();
                foreach (var i in keylist.Split(','))
                {
                    var item1 = new System.Windows.Forms.ToolStripMenuItem();
                    item1.Image = global::forBuf.Properties.Resources.security_protection_protect_key_password_login_108554;
                    item1.Name = i;
                    item1.Text = i;
                    item1.Click += new System.EventHandler(this.item_click);
                    contextMenuStrip1.Items.Add(item1);
                }

                set_sett();

                sett_forn_for_curr_item();

            }
            catch (Exception ex)
            {

                textBox1.Text += ex.ToString();
            }
        }


        private static void set_buf()
        {
            try
            {
                WebClient client = new WebClient();
                var path = String.Format("http://{0}:80", ip);
                client.UploadData(path, "POST", Encoding.UTF8.GetBytes(String.Format("key={0},clipboard={1}", key, Clipboard.GetText())));
                add_text("set buf ok");

            }
            catch (Exception ex)
            {
                tBox1.Text += ex.ToString();
            }
        }

        public static void add_text(String text)
        {
            if (tBox1.Lines.Length > 20)
                tBox1.Text = "";

            tBox1.Text = String.Format("[{0}:{1}:{2}] {3}",
                    DateTime.Now.Hour.ToString(),
                    DateTime.Now.Minute.ToString(),
                    DateTime.Now.Second.ToString(),
                    text + Environment.NewLine)
                ;
        }

        // SET
        private void button2_Click(object sender, EventArgs e)
        {
            set_buf();

            button4.Visible = check_new_ver();

        }

        void sett_forn_for_curr_item()
        {
            for (int i = 0; i < contextMenuStrip1.Items.Count; i++)
            {
                var item = contextMenuStrip1.Items[i];
                if (item.ToString() == key)
                    item.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Bold);
                else
                    item.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular);
            }
        }

        private void item_click(object sender, EventArgs e)
        {
            key = sender.ToString();
            set_sett();


            sett_forn_for_curr_item();



        }

        private void button3_Click(object sender, EventArgs e)
        {

            WindowState = FormWindowState.Minimized;
            // прячем наше окно из панели
            this.ShowInTaskbar = false;
            // делаем нашу иконку в трее активной
            notifyIcon1.Visible = true;
        }

        private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            this.ShowInTaskbar = !this.ShowInTaskbar;
            if (this.ShowInTaskbar)
                WindowState = FormWindowState.Normal;
            else
                WindowState = FormWindowState.Minimized;
        }

        private void setToolStripMenuItem_Click(object sender, EventArgs e)
        {
            button2_Click(sender, e);
        }

        private void getToolStripMenuItem_Click(object sender, EventArgs e)
        {
            button1_Click(sender, e);
        }

        private void closeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void showToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.ShowInTaskbar = true;
            WindowState = FormWindowState.Normal;
        }

        private void button2_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;

            }
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            UnhookWindowsHookEx(hhook); //Остановить хук
        }

        private void check_huk()
        {
            //if (autoCopy.Checked || autoPaste.Checked)
            if (autoCopy.Checked)
            {
                IntPtr hInstance = LoadLibrary("User32");
                hhook = SetWindowsHookEx(WH_KEYBOARD_LL, _proc, hInstance, 0);
            }
            else
            {
                UnhookWindowsHookEx(hhook); //Остановить хук
            }

            au_copy = autoCopy.Checked;
            //au_paste = autoPaste.Checked;
            au_paste = autoCopy.Checked;

        }

        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            check_huk();
        }

        private void autoPaste_CheckedChanged(object sender, EventArgs e)
        {
            check_huk();
        }

        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {

        }

        void set_file(String file)
        {
            try
            {
                //var file = (string[])e.Data.GetData(DataFormats.FileDrop);
                var mf = file.Split('\\');
                var fileName = mf[mf.Length - 1];
                var str_bin = Convert.ToBase64String(File.ReadAllBytes(file));

                WebClient client = new WebClient();
                var path = String.Format("http://{0}:80", ip);
                client.UploadData(path, "POST", Encoding.UTF8.GetBytes(String.Format("key={0},fileInBase64Name={1},fileInBase64={2}", key, fileName, str_bin)));
                add_text(String.Format("upload file ok ({0})", fileName));

            }
            catch (Exception ex)
            {
                textBox1.Text += ex.ToString();
            }
        }

        private void buttonUdateLastFile_Click(object sender, EventArgs e)
        {
            if (Dropfile != "")
                set_file(Dropfile);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            try
            {
                WebClient client = new WebClient();
                var path = String.Format("http://{0}/?curVer=get", ip);
                var Text = client.DownloadString(path);
                byte[] newBytes = Convert.FromBase64String(Text);

                var newVerStr = Encoding.UTF8.GetString(newBytes);
                add_text(newVerStr);

                int oldVer = int.Parse(version);
                int newVer = int.Parse(newVerStr);
                if (newVer > oldVer)
                {
                    add_text("Доступна новая версия");
                    get("newVer");
                }
            }

            catch (Exception ex)
            {

            }
        }

        private bool check_new_ver()
        {
            try
            {
                WebClient client = new WebClient();
                var path = String.Format("http://{0}/?curVer=get", ip);
                var Text = client.DownloadString(path);
                byte[] newBytes = Convert.FromBase64String(Text);

                var newVerStr = Encoding.UTF8.GetString(newBytes);
                int oldVer = int.Parse(version);
                int newVer = int.Parse(newVerStr);
                if (newVer > oldVer)
                {
                    return true;
                }

            }

            catch (Exception ex)
            {

            }

            return false;
        }

        private void button2_DragDrop(object sender, DragEventArgs e)
        {

            var file = (string[])e.Data.GetData(DataFormats.FileDrop);
            if (file.Length > 0)
            {
                set_file(file[0]);
                Dropfile = file[0];

                toolTipBtnUpdate.SetToolTip(buttonUdateLastFile, "Обновить " + Dropfile);

            }
        }
    }
}
