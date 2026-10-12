// ============================================================
//  红果免费短剧 / 国班桌面伴侣 (cn.guoban.desktop-companion)
//  通用缓存清理工具 —— 图形界面版 (Windows Forms)
//  编译: csc /target:winexe /reference:System.Windows.Forms.dll
//        /reference:System.Drawing.dll /out:clean-guo-cache-gui.exe
// ============================================================
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace HongguoCacheCleanerGui
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    class CacheItem
    {
        public string Name;
        public string Path;
        public string Desc;
        public bool Risky;
        public long FileCount;
        public double SizeMB;
    }

    class MainForm : Form
    {
        private Label titleLabel;
        private Label statusLabel;
        private CheckBox cbVideo;
        private CheckBox cbPoster;
        private CheckBox cbInstaller;
        private CheckBox cbWeb;
        private Label totalLabel;
        private Button refreshBtn;
        private Button cleanBtn;
        private TextBox logBox;
        private List<CacheItem> items = new List<CacheItem>();
        private bool scanning = false;

        private string AppDataDir
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "cn.guoban.desktop-companion"); }
        }
        private string LocalDataDir
        {
            get { return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "cn.guoban.desktop-companion"); }
        }

        public MainForm()
        {
            this.Text = "红果免费短剧缓存清理工具";
            this.Width = 640;
            this.Height = 590;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.Font = new Font("Microsoft YaHei", 9F);

            titleLabel = new Label();
            titleLabel.Text = "红果免费短剧 / 国班桌面伴侣 缓存清理工具";
            titleLabel.Font = new Font("Microsoft YaHei", 12F, FontStyle.Bold);
            titleLabel.AutoSize = true;
            titleLabel.Location = new Point(16, 12);

            statusLabel = new Label();
            statusLabel.Text = "正在检测缓存目录...";
            statusLabel.ForeColor = Color.Gray;
            statusLabel.AutoSize = true;
            statusLabel.Location = new Point(18, 46);

            cbVideo = MakeCheckBox(new Point(16, 80));
            cbPoster = MakeCheckBox(new Point(16, 122));
            cbInstaller = MakeCheckBox(new Point(16, 164));
            cbWeb = MakeCheckBox(new Point(16, 206));

            totalLabel = new Label();
            totalLabel.Text = "合计可释放: -";
            totalLabel.Font = new Font("Microsoft YaHei", 10F, FontStyle.Bold);
            totalLabel.ForeColor = Color.DarkGreen;
            totalLabel.AutoSize = true;
            totalLabel.Location = new Point(16, 250);

            refreshBtn = new Button();
            refreshBtn.Text = "重新检测";
            refreshBtn.Size = new Size(100, 34);
            refreshBtn.Location = new Point(16, 288);
            refreshBtn.Click += (s, e) => Scan();

            cleanBtn = new Button();
            cleanBtn.Text = "开始清理";
            cleanBtn.Size = new Size(140, 40);
            cleanBtn.BackColor = Color.Firebrick;
            cleanBtn.ForeColor = Color.White;
            cleanBtn.Font = new Font("Microsoft YaHei", 11F, FontStyle.Bold);
            cleanBtn.Location = new Point(450, 286);
            cleanBtn.Click += (s, e) => CleanAsync();

            logBox = new TextBox();
            logBox.Multiline = true;
            logBox.ReadOnly = true;
            logBox.ScrollBars = ScrollBars.Vertical;
            logBox.BackColor = Color.FromArgb(28, 28, 28);
            logBox.ForeColor = Color.LightGreen;
            logBox.Font = new Font("Consolas", 9F);
            logBox.Location = new Point(16, 340);
            logBox.Size = new Size(590, 200);

            this.Controls.Add(titleLabel);
            this.Controls.Add(statusLabel);
            this.Controls.Add(cbVideo);
            this.Controls.Add(cbPoster);
            this.Controls.Add(cbInstaller);
            this.Controls.Add(cbWeb);
            this.Controls.Add(totalLabel);
            this.Controls.Add(refreshBtn);
            this.Controls.Add(cleanBtn);
            this.Controls.Add(logBox);

            Scan();
        }

        private CheckBox MakeCheckBox(Point p)
        {
            CheckBox cb = new CheckBox();
            cb.AutoSize = true;
            cb.Location = p;
            cb.Checked = true;
            cb.Enabled = false;
            cb.CheckedChanged += (s, e) => UpdateTotal();
            return cb;
        }

        private void Log(string msg)
        {
            if (logBox.InvokeRequired) { logBox.BeginInvoke(new Action<string>(Log), msg); return; }
            logBox.AppendText(msg + Environment.NewLine);
        }

        private void Scan()
        {
            if (scanning) return;
            scanning = true;
            statusLabel.Text = "正在检测缓存目录...";
            statusLabel.ForeColor = Color.Gray;
            cbVideo.Enabled = cbPoster.Enabled = cbInstaller.Enabled = cbWeb.Enabled = false;
            Task.Run(() =>
            {
                var list = new List<CacheItem>();
                AddItem(list, "视频缓存 stream-cache", Path.Combine(AppDataDir, "stream-cache"), "离线/播放过的短剧视频文件（主要占用）", false);
                AddItem(list, "封面缓存 poster-cache", Path.Combine(AppDataDir, "poster-cache-v1"), "短剧海报封面缩略图", false);
                AddItem(list, "安装缓存 installer", Path.Combine(LocalDataDir, "installer-cache-v1"), "安装包残留（重装时需重新下载）", false);
                AddItem(list, "网页缓存 EBWebView", Path.Combine(LocalDataDir, "EBWebView"), "内嵌浏览器缓存，清理后可能需重新登录", true);
                BeginInvoke(new Action<List<CacheItem>>(ScanDone), list);
            });
        }

        private void ScanDone(List<CacheItem> list)
        {
            scanning = false;
            items = list;
            if (items.Count == 0)
            {
                statusLabel.Text = "未检测到红果免费短剧桌面版的数据目录（请确认已安装应用）";
                statusLabel.ForeColor = Color.OrangeRed;
                totalLabel.Text = "合计可释放: -";
                return;
            }
            statusLabel.Text = "检测完成，勾选要清理的缓存（网页缓存默认不勾选）：";
            statusLabel.ForeColor = Color.Gray;

            cbVideo.Text = FormatItem(items[0]);
            cbPoster.Text = items.Count > 1 ? FormatItem(items[1]) : "-";
            cbInstaller.Text = items.Count > 2 ? FormatItem(items[2]) : "-";
            cbWeb.Text = items.Count > 3 ? FormatItem(items[3]) : "-";

            cbVideo.Checked = true;
            cbPoster.Checked = true;
            cbInstaller.Checked = true;
            cbWeb.Checked = false;

            cbVideo.Enabled = true;
            cbPoster.Enabled = items.Count > 1;
            cbInstaller.Enabled = items.Count > 2;
            cbWeb.Enabled = items.Count > 3;

            UpdateTotal();
        }

        private string FormatItem(CacheItem t)
        {
            string risk = t.Risky ? "  [清理后可能需重新登录]" : "";
            return string.Format("{0}（{1} 个文件, {2:0.0} MB）{3}", t.Name, t.FileCount, t.SizeMB, risk);
        }

        private void UpdateTotal()
        {
            double total = 0;
            if (cbVideo.Checked && items.Count > 0) total += items[0].SizeMB;
            if (cbPoster.Checked && items.Count > 1) total += items[1].SizeMB;
            if (cbInstaller.Checked && items.Count > 2) total += items[2].SizeMB;
            if (cbWeb.Checked && items.Count > 3) total += items[3].SizeMB;
            totalLabel.Text = string.Format("合计可释放: 约 {0:0.0} MB", total);
        }

        private List<CacheItem> GetSelected()
        {
            var sel = new List<CacheItem>();
            if (cbVideo.Checked && items.Count > 0) sel.Add(items[0]);
            if (cbPoster.Checked && items.Count > 1) sel.Add(items[1]);
            if (cbInstaller.Checked && items.Count > 2) sel.Add(items[2]);
            if (cbWeb.Checked && items.Count > 3) sel.Add(items[3]);
            return sel;
        }

        private void CleanAsync()
        {
            var sel = GetSelected();
            if (sel.Count == 0)
            {
                MessageBox.Show("请至少勾选一项要清理的缓存。", "提示", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }
            double est = sel.Sum(x => x.SizeMB);
            var names = string.Join(Environment.NewLine, sel.Select(x => "  - " + x.Name));
            if (MessageBox.Show(string.Format("确认清理以下缓存？\n\n{0}\n\n预计释放约 {1:0.0} MB", names, est),
                "确认清理", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes)
                return;

            cleanBtn.Enabled = false;
            Log("========== 开始清理 ==========");
            Task.Run(() =>
            {
                long allDeleted = 0;
                long allLocked = 0;
                foreach (var t in sel)
                {
                    Log(string.Format("正在清理: {0} ...", t.Name));
                    long deleted = 0;
                    long locked = 0;
                    IEnumerable<string> files;
                    try { files = Directory.EnumerateFiles(t.Path, "*", SearchOption.AllDirectories); }
                    catch { files = new string[0]; }
                    foreach (var f in files)
                    {
                        try
                        {
                            File.SetAttributes(f, FileAttributes.Normal);
                            File.Delete(f);
                            deleted++;
                        }
                        catch { locked++; }
                    }
                    Log(string.Format("  {0}: 删除 {1} 个, 跳过 {2} 个", t.Name, deleted, locked));
                    allDeleted += deleted;
                    allLocked += locked;
                }
                Log(string.Format("清理完成！共删除 {0} 个文件，释放约 {1:0.0} MB", allDeleted, est));
                if (allLocked > 0) Log(string.Format("跳过 {0} 个占用文件（退出应用后重试可清除）", allLocked));
                BeginInvoke(new Action(() =>
                {
                    cleanBtn.Enabled = true;
                    MessageBox.Show(string.Format("清理完成！\n\n共删除 {0} 个文件\n释放约 {1:0.0} MB", allDeleted, est),
                        "完成", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Scan();
                }));
            });
        }

        private void AddItem(List<CacheItem> list, string name, string path, string desc, bool risky)
        {
            if (!Directory.Exists(path)) return;
            long size = 0;
            long count = 0;
            try
            {
                foreach (var f in Directory.EnumerateFiles(path, "*", SearchOption.AllDirectories))
                {
                    try { size += new FileInfo(f).Length; count++; }
                    catch { }
                }
            }
            catch { }
            list.Add(new CacheItem { Name = name, Path = path, Desc = desc, Risky = risky, FileCount = count, SizeMB = size / 1024.0 / 1024.0 });
        }
    }
}
