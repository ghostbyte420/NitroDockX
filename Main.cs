using System;
using System.ComponentModel;
using System.Diagnostics;
using System.DirectoryServices;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace NitroDock
{
    public partial class NitroDockMain : Form
    {
        private bool isDragging = false;
        private Point lastCursor;
        private Point lastForm;
        private const int minDockWidth = 64 + 10;
        private const int cornerRadius = 24;
        private const int maxIconSize = 70;
        private const int maxIconSpacing = 50;
        private Button selectedButton = null;
        private bool isIconSelected = false;
        private MouseHook mouseHook;

        [DefaultValue(45)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int IconSize { get; set; } = 45;

        [DefaultValue(13)]
        [DesignerSerializationVisibility(DesignerSerializationVisibility.Visible)]
        public int IconSpacing { get; set; } = 13;

        public enum DockPosition { Left, Right, Top, Bottom }
        public DockPosition currentDockPosition = DockPosition.Right;

        [DefaultValue(0)]
        public int DockOffset { get; set; } = 0;

        [DefaultValue(0)]
        public int DockOffsetZ { get; set; } = 0;

        public enum GlowColor { Blue, Cyan, Pink, Red, SlateGray, White, Yellow }

        [DefaultValue(GlowColor.Blue)]
        public GlowColor SelectedGlowColor { get; set; } = GlowColor.Cyan;

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int ExtractIconEx(string lpszFile, int nIconIndex, IntPtr[] phiconLarge, IntPtr[] phiconSmall, int nIcons);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern uint SHGetFileInfo(string pszPath, uint dwFileAttributes, out SHFILEINFO psfi, uint cbFileInfo, uint uFlags);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern uint GetShortPathName(string lpszLongPath, StringBuilder lpszShortPath, uint cchBuffer);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern uint GetLongPathName(string lpszShortPath, StringBuilder lpszLongPath, uint cchBuffer);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private struct SHFILEINFO
        {
            public IntPtr hIcon;
            public int iIcon;
            public uint dwAttributes;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string szDisplayName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 80)]
            public string szTypeName;
        }

        private const uint SHGFI_ICON = 0x000000100;
        private const uint SHGFI_LARGEICON = 0x00000000;
        private const uint SHGFI_USEFILEATTRIBUTES = 0x000000010;

        public NitroDockMain()
        {
            InitializeComponent();
            StartPosition = FormStartPosition.Manual;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.Black;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.UserPaint, true);
            mouseHook = MouseHook.Instance;
            mouseHook.MouseMiddleButtonDown += OnMouseMiddleButtonDown;
            mouseHook.MouseWheel += OnMouseWheel;

            NitroDockMain_OpacityPanel.MouseDown += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    isDragging = true;
                    lastCursor = Cursor.Position;
                    lastForm = Location;
                }
            };

            NitroDockMain_OpacityPanel.MouseMove += (s, e) =>
            {
                if (isDragging)
                {
                    Location = new Point(
                        lastForm.X + (Cursor.Position.X - lastCursor.X),
                        lastForm.Y + (Cursor.Position.Y - lastForm.Y)
                    );
                }
            };

            NitroDockMain_OpacityPanel.MouseUp += (s, e) =>
            {
                if (e.Button == MouseButtons.Left)
                {
                    isDragging = false;
                    SnapToEdge(currentDockPosition);
                    SaveDockLocation();
                }
            };

            NitroDockMain_OpacityPanel.AllowDrop = true;
            NitroDockMain_OpacityPanel.DragEnter += (s, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    e.Effect = DragDropEffects.Copy;
            };

            NitroDockMain_OpacityPanel.DragOver += NitroDockMain_OpacityPanel_DragOver;
            NitroDockMain_OpacityPanel.DragDrop += NitroDockMain_OpacityPanel_DragDrop;

            AddConfigButton();
            LoadSettings();
            SnapToEdge(currentDockPosition);
            ApplyGlowEffect();
        }

        private void SaveDockLocation()
        {
            string iniPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "NitroDockX.ini");
            IniFile ini = new IniFile(iniPath);
            ini.Write("DockSettings", "DockLocationX", Location.X.ToString());
            ini.Write("DockSettings", "DockLocationY", Location.Y.ToString());
        }

        private void LoadSettings()
        {
            string iniPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "NitroDockX.ini");
            if (!File.Exists(iniPath)) return;

            IniFile ini = new IniFile(iniPath);

            if (Enum.TryParse(ini.Read("DockSettings", "DockPosition"), out DockPosition position))
                currentDockPosition = position;

            if (float.TryParse(ini.Read("DockSettings", "DockOpacity"), out float opacity))
                Opacity = opacity;

            if (int.TryParse(ini.Read("DockSettings", "DockOffset"), out int offset))
                DockOffset = offset;

            if (int.TryParse(ini.Read("DockSettings", "DockOffsetZ"), out int offsetZ))
                DockOffsetZ = offsetZ;

            if (int.TryParse(ini.Read("DockSettings", "DockLocationX"), out int locationX) &&
                int.TryParse(ini.Read("DockSettings", "DockLocationY"), out int locationY))
            {
                Location = new Point(locationX, locationY);
            }

            if (int.TryParse(ini.Read("DockSettings", "IconSize"), out int iconSize))
                IconSize = iconSize;

            if (int.TryParse(ini.Read("DockSettings", "IconSpacing"), out int iconSpacing))
                IconSpacing = iconSpacing;

            if (Enum.TryParse(ini.Read("DockSettings", "GlowColor"), out GlowColor glowColor))
                SelectedGlowColor = glowColor;

            string skinName = ini.Read("DockSettings", "Skin", "Default");
            string skinMode = ini.Read("DockSettings", "SkinMode", "Stretch");
            ApplySkin(skinName, skinMode);

            ClearIcons();
            LoadIcons(ini);
        }

        private void ClearIcons()
        {
            var buttons = NitroDockMain_OpacityPanel.Controls.OfType<Button>()
                .Where(b => b.Tag.ToString() != "NitroDockMain_Configuration")
                .ToList();

            foreach (Button button in buttons)
            {
                NitroDockMain_OpacityPanel.Controls.Remove(button);
                button.Dispose();
            }
        }

        private void LoadIcons(IniFile ini)
        {
            int index = 0;
            while (true)
            {
                string path = ini.Read("Icons", $"Icon{index}_Path");
                if (string.IsNullOrEmpty(path)) break;

                string customIcon = ini.Read("Icons", $"Icon{index}_CustomIcon");
                Button button = CreateButtonForFileOrDirectory(path);

                if (!string.IsNullOrEmpty(customIcon) && File.Exists(customIcon))
                {
                    try
                    {
                        button.Image = ResizeImage(Image.FromFile(customIcon), IconSize, IconSize);
                        button.Image.Tag = customIcon;
                    }
                    catch { }
                }

                NitroDockMain_OpacityPanel.Controls.Add(button);
                index++;
            }
            RedistributeButtons();
        }

        public void ApplySkin(string skinName, string skinMode)
        {
            string skinPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "NitroSkins", skinName, "01.png");
            if (File.Exists(skinPath))
            {
                Image skinImage = Image.FromFile(skinPath);
                NitroDockMain_OpacityPanel.BackgroundImage = skinImage;

                switch (skinMode)
                {
                    case "None":
                        NitroDockMain_OpacityPanel.BackgroundImageLayout = ImageLayout.None;
                        break;
                    case "Tile":
                        NitroDockMain_OpacityPanel.BackgroundImageLayout = ImageLayout.Tile;
                        break;
                    case "Center":
                        NitroDockMain_OpacityPanel.BackgroundImageLayout = ImageLayout.Center;
                        break;
                    case "Stretch":
                        NitroDockMain_OpacityPanel.BackgroundImageLayout = ImageLayout.Stretch;
                        break;
                    case "Zoom":
                        NitroDockMain_OpacityPanel.BackgroundImageLayout = ImageLayout.Zoom;
                        break;
                }
            }
            else
            {
                NitroDockMain_OpacityPanel.BackgroundImage = null;
            }
        }

        private void OnMouseMiddleButtonDown(MouseEventArgs e)
        {
            Point clientPoint = NitroDockMain_OpacityPanel.PointToClient(new Point(e.X, e.Y));
            var buttons = NitroDockMain_OpacityPanel.Controls.OfType<Button>()
                .Where(b => b.Bounds.Contains(clientPoint) && b.Tag.ToString() != "NitroDockMain_Configuration")
                .ToList();

            selectedButton = buttons.FirstOrDefault();
            isIconSelected = (selectedButton != null);
            NitroDockMain_OpacityPanel.Focus();
        }

        private void OnMouseWheel(MouseEventArgs e)
        {
            Point clientPoint = NitroDockMain_OpacityPanel.PointToClient(new Point(e.X, e.Y));
            if (NitroDockMain_OpacityPanel.ClientRectangle.Contains(clientPoint) && isIconSelected && selectedButton != null)
            {
                bool isVerticalDock = (currentDockPosition == DockPosition.Left || currentDockPosition == DockPosition.Right);
                ReorderSelectedButton(isVerticalDock ? e.Delta > 0 : e.Delta < 0);
            }
        }

        private void ReorderSelectedButton(bool moveUp)
        {
            var buttons = NitroDockMain_OpacityPanel.Controls.OfType<Button>()
                .Where(b => b.Tag.ToString() != "NitroDockMain_Configuration")
                .ToList();

            int currentIndex = buttons.IndexOf(selectedButton);
            if (currentIndex == -1) return;

            int nextIndex = moveUp ? currentIndex - 1 : currentIndex + 1;
            if (nextIndex < 0 || nextIndex >= buttons.Count) return;

            var nextButton = buttons[nextIndex];
            Point currentLocation = selectedButton.Location;
            selectedButton.Location = nextButton.Location;
            nextButton.Location = currentLocation;

            NitroDockMain_OpacityPanel.Controls.SetChildIndex(selectedButton, nextIndex);
            NitroDockMain_OpacityPanel.Controls.SetChildIndex(nextButton, currentIndex);
        }

        private string GetNitroIconsPath()
        {
            string appDirectory = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            string nitroIconsPath = Path.Combine(appDirectory, "NitroIcons");
            if (!Directory.Exists(nitroIconsPath))
            {
                Directory.CreateDirectory(nitroIconsPath);
            }
            return nitroIconsPath;
        }

        private void AddConfigButton()
        {
            Button configButton = new Button
            {
                Size = new Size(IconSize, IconSize),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Tag = "NitroDockMain_Configuration"
            };

            string configIconPath = Path.Combine(GetNitroIconsPath(), "Config.png");
            if (File.Exists(configIconPath))
            {
                try
                {
                    using (Bitmap bitmap = new Bitmap(configIconPath))
                    {
                        configButton.Image = ResizeImage(bitmap, IconSize, IconSize);
                    }
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error loading Config.png: {ex.Message}");
                    configButton.Image = ResizeImage(SystemIcons.Shield.ToBitmap(), IconSize, IconSize);
                }
            }
            else
            {
                configButton.Image = ResizeImage(SystemIcons.Shield.ToBitmap(), IconSize, IconSize);
            }

            configButton.ImageAlign = ContentAlignment.MiddleCenter;

            ContextMenuStrip configContextMenu = new ContextMenuStrip();
            ToolStripMenuItem propertiesItem = new ToolStripMenuItem("Properties");
            propertiesItem.Click += (s, e) => ShowIconProperties(configButton);
            configContextMenu.Items.Add(propertiesItem);

            ToolStripMenuItem removeItem = new ToolStripMenuItem("Remove Item");
            removeItem.Click += (s, e) => RemoveButton(configButton);
            configContextMenu.Items.Add(removeItem);

            configButton.ContextMenuStrip = configContextMenu;

            configButton.MouseEnter += (s, e) =>
            {
                foreach (ToolStripItem item in configContextMenu.Items)
                {
                    if (item.Text == "Remove Item")
                    {
                        item.ForeColor = Color.Gray;
                        break;
                    }
                }
            };

            configButton.MouseLeave += (s, e) =>
            {
                foreach (ToolStripItem item in configContextMenu.Items)
                {
                    if (item.Text == "Remove Item")
                    {
                        item.ForeColor = SystemColors.ControlText;
                        break;
                    }
                }
            };

            configButton.MouseDown += (s, ev) =>
            {
                if (ev.Button == MouseButtons.Left)
                {
                    NitroDockMain_Configuration configForm = new NitroDockMain_Configuration(this);
                    configForm.Show();
                }
            };

            NitroDockMain_OpacityPanel.Controls.Add(configButton);
            PositionConfigButton(configButton);
        }

        private void ShowIconProperties(Button button)
        {
            NitroDockMain_IconProperties propertiesForm = new NitroDockMain_IconProperties(button);
            if (button.Tag?.ToString() == "NitroDockMain_Configuration")
            {
                propertiesForm.HideRemoveOption();
            }
            propertiesForm.ShowDialog();
        }

        private void PositionConfigButton(Button configButton)
        {
            int buttonWidth = configButton.Width;
            int buttonHeight = configButton.Height;

            switch (currentDockPosition)
            {
                case DockPosition.Left:
                case DockPosition.Right:
                    configButton.Location = new Point(
                        (NitroDockMain_OpacityPanel.Width / 2) - (buttonWidth / 2),
                        IconSpacing
                    );
                    break;
                case DockPosition.Top:
                case DockPosition.Bottom:
                    configButton.Location = new Point(
                        IconSpacing,
                        (NitroDockMain_OpacityPanel.Height / 2) - (buttonHeight / 2)
                    );
                    break;
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            UpdateRoundedRegion();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            UpdateRoundedRegion();
        }

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            base.OnPaintBackground(e);
            using (GraphicsPath path = GetRoundedRect(this.ClientRectangle, cornerRadius))
            using (SolidBrush brush = new SolidBrush(Color.Black))
            {
                e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
                e.Graphics.FillPath(brush, path);
            }
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            e.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
        }

        private GraphicsPath GetRoundedRect(Rectangle rect, int radius)
        {
            GraphicsPath path = new GraphicsPath();
            float diameter = radius * 2f;
            path.StartFigure();
            path.AddArc(rect.X, rect.Y, diameter, diameter, 180, 90);
            path.AddArc(rect.Right - diameter, rect.Y, diameter, diameter, 270, 90);
            path.AddArc(rect.Right - diameter, rect.Bottom - diameter, diameter, diameter, 0, 90);
            path.AddArc(rect.X, rect.Bottom - diameter, diameter, diameter, 90, 90);
            path.CloseFigure();
            return path;
        }

        private void UpdateRoundedRegion()
        {
            using (GraphicsPath path = GetRoundedRect(this.ClientRectangle, cornerRadius))
            {
                this.Region = new Region(path);
            }
        }

        private void NitroDockMain_OpacityPanel_DragOver(object sender, DragEventArgs e)
        {
            Point clientPoint = NitroDockMain_OpacityPanel.PointToClient(new Point(e.X, e.Y));
            foreach (Control control in NitroDockMain_OpacityPanel.Controls)
            {
                if (control is Button && control.Bounds.Contains(clientPoint))
                {
                    e.Effect = DragDropEffects.None;
                    return;
                }
            }
            e.Effect = DragDropEffects.Copy;
        }

        private void NitroDockMain_OpacityPanel_DragDrop(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                Point dropPoint = NitroDockMain_OpacityPanel.PointToClient(new Point(e.X, e.Y));
                foreach (string filePath in files)
                {
                    string resolvedPath = ResolveShortcut(filePath);
                    Button newButton = CreateButtonForFileOrDirectory(resolvedPath);
                    InsertButtonAtDropPosition(newButton, dropPoint);
                }
            }
        }

        private Button CreateButtonForFileOrDirectory(string path)
        {
            Button button = new Button
            {
                Size = new Size(IconSize, IconSize),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 },
                Tag = path
            };

            try
            {
                Icon icon = GetIconForPath(path);
                if (icon != null)
                {
                    Bitmap bitmap = icon.ToBitmap();
                    button.Image = ResizeImage(bitmap, IconSize, IconSize);
                    button.ImageAlign = ContentAlignment.MiddleCenter;
                }
                else
                {
                    button.Image = ResizeImage(GetDefaultIcon().ToBitmap(), IconSize, IconSize);
                    button.ImageAlign = ContentAlignment.MiddleCenter;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading icon: {ex.Message}");
                button.Image = ResizeImage(GetDefaultIcon().ToBitmap(), IconSize, IconSize);
                button.ImageAlign = ContentAlignment.MiddleCenter;
            }

            ContextMenuStrip contextMenu = new ContextMenuStrip();
            ToolStripMenuItem propertiesItem = new ToolStripMenuItem("Properties");
            propertiesItem.Click += (s, ev) => ShowIconProperties(button);
            contextMenu.Items.Add(propertiesItem);

            ToolStripMenuItem removeItem = new ToolStripMenuItem("Remove Item");
            removeItem.Click += (s, ev) => RemoveButton(button);
            contextMenu.Items.Add(removeItem);

            ToolStripMenuItem clearIniItem = new ToolStripMenuItem("Clear .ini File");
            clearIniItem.Click += (s, ev) => ClearIniFile();
            contextMenu.Items.Add(clearIniItem);

            contextMenu.Items.Add(new ToolStripSeparator());

            ToolStripMenuItem exitItem = new ToolStripMenuItem("Exit NitroDockX");
            exitItem.Click += (s, ev) => Application.Exit();
            contextMenu.Items.Add(exitItem);

            button.ContextMenuStrip = contextMenu;

            button.MouseDown += (s, ev) =>
            {
                if (ev.Button == MouseButtons.Left)
                {
                    string fullPath = button.Tag.ToString();
                    if (File.Exists(fullPath))
                    {
                        Process.Start(fullPath);
                    }
                    else if (Directory.Exists(fullPath))
                    {
                        Process.Start("explorer.exe", fullPath);
                    }
                }
            };

            button.MouseEnter += (sender, e) =>
            {
                Color glowColor = GetGlowColor(SelectedGlowColor);
                button.BackColor = Color.FromArgb(50, glowColor);
            };

            button.MouseLeave += (sender, e) =>
            {
                button.BackColor = Color.Transparent;
            };

            return button;
        }

        private void ClearIniFile()
        {
            string iniPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "NitroDockX.ini");
            if (File.Exists(iniPath))
            {
                File.WriteAllText(iniPath, string.Empty);
                MessageBox.Show("The .ini file has been cleared. Restart NitroDockX to apply changes.", "Info", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }

        private void InsertButtonAtDropPosition(Button newButton, Point dropPoint)
        {
            var buttons = NitroDockMain_OpacityPanel.Controls.OfType<Button>()
                .Where(b => b.Tag.ToString() != "NitroDockMain_Configuration")
                .ToList();

            if (buttons.Count == 0)
            {
                NitroDockMain_OpacityPanel.Controls.Add(newButton);
                RedistributeButtons();
                return;
            }

            Button closestButton = null;
            int minDistance = int.MaxValue;

            foreach (Button button in buttons)
            {
                int distance = Math.Abs(button.Top - dropPoint.Y);
                if (currentDockPosition == DockPosition.Top || currentDockPosition == DockPosition.Bottom)
                {
                    distance = Math.Abs(button.Left - dropPoint.X);
                }

                if (distance < minDistance)
                {
                    minDistance = distance;
                    closestButton = button;
                }
            }

            if (closestButton != null)
            {
                int insertIndex = NitroDockMain_OpacityPanel.Controls.GetChildIndex(closestButton);
                NitroDockMain_OpacityPanel.Controls.Add(newButton);
                NitroDockMain_OpacityPanel.Controls.SetChildIndex(newButton, insertIndex);
            }
            else
            {
                NitroDockMain_OpacityPanel.Controls.Add(newButton);
            }

            RedistributeButtons();
        }

        private string ResolveShortcut(string shortcutPath)
        {
            if (!shortcutPath.EndsWith(".lnk", StringComparison.OrdinalIgnoreCase))
            {
                return shortcutPath;
            }

            try
            {
                IShellLinkW shellLink = (IShellLinkW)new ShellLink();
                IPersistFile persistFile = (IPersistFile)shellLink;
                persistFile.Load(shortcutPath, 0);
                StringBuilder targetPath = new StringBuilder(260);
                shellLink.GetPath(targetPath, targetPath.Capacity, out _, 0);
                return targetPath.ToString();
            }
            catch
            {
                return shortcutPath;
            }
        }

        [ComImport]
        [Guid("00021401-0000-0000-C000-000000000046")]
        private class ShellLink
        {
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("000214F9-0000-0000-C000-000000000046")]
        private interface IShellLinkW
        {
            void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, out WIN32_FIND_DATAW pfd, uint fFlags);
            void GetIDList(out IntPtr ppidl);
            void SetIDList(IntPtr pidl);
            void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
            void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
            void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
            void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
            void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
            void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
            void GetHotkey(out ushort pwHotkey);
            void SetHotkey(ushort wHotkey);
            void GetShowCmd(out int piShowCmd);
            void SetShowCmd(int iShowCmd);
            void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
            void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
            void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
            void Resolve(IntPtr hwnd, uint fFlags);
            void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
        }

        [ComImport]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        [Guid("0000010B-0000-0000-C000-000000000046")]
        private interface IPersistFile
        {
            void GetClassID(out Guid pClassID);
            void IsDirty();
            void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
            void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);
            void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
            void GetCurFile([Out, MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct WIN32_FIND_DATAW
        {
            public uint dwFileAttributes;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftCreationTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastAccessTime;
            public System.Runtime.InteropServices.ComTypes.FILETIME ftLastWriteTime;
            public uint nFileSizeHigh;
            public uint nFileSizeLow;
            public uint dwReserved0;
            public uint dwReserved1;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public string cFileName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 14)]
            public string cAlternateFileName;
        }

        private void AddButtonForFileOrDirectory(string path)
        {
            string resolvedPath = ResolveShortcut(path);
            Button newButton = CreateButtonForFileOrDirectory(resolvedPath);
            NitroDockMain_OpacityPanel.Controls.Add(newButton);
            RedistributeButtons();
        }

        private Bitmap ResizeImage(Image image, int width, int height)
        {
            Rectangle destRect = new Rectangle(0, 0, width, height);
            Bitmap destImage = new Bitmap(width, height);
            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (Graphics graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (ImageAttributes wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }

        private Icon GetIconForPath(string path)
        {
            if (File.Exists(path))
            {
                return Icon.ExtractAssociatedIcon(path);
            }
            else if (Directory.Exists(path))
            {
                return GetFolderIcon(path);
            }
            else if (path.Length == 2 && path.EndsWith(":"))
            {
                return GetDriveIcon(path);
            }

            return SystemIcons.Application;
        }

        private Icon GetFolderIcon(string folderPath)
        {
            try
            {
                SHFILEINFO shinfo = new SHFILEINFO();
                uint attributes = (uint)FileAttributes.Directory;
                uint result = SHGetFileInfo(
                    folderPath,
                    attributes,
                    out shinfo,
                    (uint)Marshal.SizeOf(shinfo),
                    SHGFI_ICON | SHGFI_LARGEICON | SHGFI_USEFILEATTRIBUTES);

                if (shinfo.hIcon != IntPtr.Zero)
                {
                    return Icon.FromHandle(shinfo.hIcon);
                }
            }
            catch { }

            try
            {
                IntPtr[] largeIconPtr = new IntPtr[1];
                int result = ExtractIconEx("shell32.dll", 3, largeIconPtr, null, 1);
                if (result > 0 && largeIconPtr[0] != IntPtr.Zero)
                {
                    return Icon.FromHandle(largeIconPtr[0]);
                }
            }
            catch { }

            return CreateFolderIcon();
        }

        private Icon GetDriveIcon(string drivePath)
        {
            try
            {
                DriveInfo drive = new DriveInfo(drivePath);
                if (drive.DriveType == DriveType.Removable || drive.DriveType == DriveType.Fixed)
                {
                    IntPtr[] largeIconPtr = new IntPtr[1];
                    int result = ExtractIconEx("shell32.dll", 8, largeIconPtr, null, 1);
                    if (result > 0 && largeIconPtr[0] != IntPtr.Zero)
                    {
                        return Icon.FromHandle(largeIconPtr[0]);
                    }
                }
            }
            catch { }

            return SystemIcons.WinLogo;
        }

        private Icon CreateFolderIcon()
        {
            Bitmap folderBitmap = new Bitmap(32, 32);
            using (Graphics g = Graphics.FromImage(folderBitmap))
            {
                g.Clear(Color.Transparent);
                g.FillRectangle(Brushes.Goldenrod, 4, 8, 24, 16);
                g.FillPolygon(Brushes.Goldenrod, new Point[] {
                    new Point(4, 8),
                    new Point(12, 4),
                    new Point(28, 4),
                    new Point(28, 8)
                });
                g.DrawRectangle(Pens.Black, 4, 8, 23, 16);
                g.DrawLine(Pens.White, 4, 8, 28, 8);
                g.DrawLine(Pens.White, 4, 8, 12, 4);
                g.DrawLine(Pens.DarkGray, 28, 8, 28, 24);
                g.DrawLine(Pens.DarkGray, 4, 24, 28, 24);
            }

            return Icon.FromHandle(folderBitmap.GetHicon());
        }

        private Icon GetDefaultIcon()
        {
            return SystemIcons.Application;
        }

        private void RedistributeButtons()
        {
            var buttons = NitroDockMain_OpacityPanel.Controls.OfType<Button>()
                .Where(b => b.Tag.ToString() != "NitroDockMain_Configuration")
                .ToList();

            if (buttons.Count == 0)
            {
                Button cfgButton = NitroDockMain_OpacityPanel.Controls.OfType<Button>()
                    .FirstOrDefault(b => b.Tag.ToString() == "NitroDockMain_Configuration");

                if (cfgButton != null)
                {
                    PositionConfigButton(cfgButton);
                }

                return;
            }

            Button cfgButtonLocal = NitroDockMain_OpacityPanel.Controls.OfType<Button>()
                .FirstOrDefault(b => b.Tag.ToString() == "NitroDockMain_Configuration");

            int startY = IconSpacing;
            int startX = IconSpacing;

            if (cfgButtonLocal != null)
            {
                PositionConfigButton(cfgButtonLocal);
                startY = cfgButtonLocal.Bottom + IconSpacing;
                startX = cfgButtonLocal.Right + IconSpacing;
            }

            switch (currentDockPosition)
            {
                case DockPosition.Left:
                case DockPosition.Right:
                    {
                        int totalButtonsHeight = buttons.Count * IconSize + (buttons.Count - 1) * IconSpacing;
                        int dockHeight = startY + totalButtonsHeight + IconSpacing;
                        ClientSize = new Size(ClientSize.Width, dockHeight);
                        int centerX = ClientSize.Width / 2;

                        for (int i = 0; i < buttons.Count; i++)
                        {
                            buttons[i].Location = new Point(
                                centerX - (IconSize / 2),
                                startY + i * (IconSize + IconSpacing)
                            );
                        }

                        break;
                    }
                case DockPosition.Top:
                case DockPosition.Bottom:
                    {
                        int totalButtonsWidth = buttons.Count * IconSize + (buttons.Count - 1) * IconSpacing;
                        int dockWidth = startX + totalButtonsWidth + IconSpacing;
                        ClientSize = new Size(dockWidth, ClientSize.Height);
                        int centerY = ClientSize.Height / 2;

                        for (int i = 0; i < buttons.Count; i++)
                        {
                            buttons[i].Location = new Point(
                                startX + i * (IconSize + IconSpacing),
                                centerY - (IconSize / 2)
                            );
                        }

                        break;
                    }
            }

            UpdateRoundedRegion();
        }

        public void SnapToEdge(DockPosition position)
        {
            Screen screen = Screen.FromControl(this);
            Rectangle workingArea = screen.WorkingArea;
            int dockWidth, dockHeight;

            var buttons = NitroDockMain_OpacityPanel.Controls.OfType<Button>()
                .Where(b => b.Tag.ToString() != "NitroDockMain_Configuration")
                .ToList();

            if (position == DockPosition.Left || position == DockPosition.Right)
            {
                dockWidth = minDockWidth;
                int totalButtonsHeight = buttons.Count * IconSize + (buttons.Count > 0 ? (buttons.Count - 1) * IconSpacing : 0);
                dockHeight = totalButtonsHeight + 2 * IconSpacing + IconSize;
            }
            else // Top/Bottom
            {
                dockHeight = minDockWidth;
                int totalButtonsWidth = buttons.Count * IconSize + (buttons.Count > 0 ? (buttons.Count - 1) * IconSpacing : 0);
                dockWidth = totalButtonsWidth + 2 * IconSpacing + IconSize;
            }

            switch (position)
            {
                case DockPosition.Left:
                case DockPosition.Right:
                    int maxZOffsetVertical = workingArea.Height - dockHeight;
                    DockOffsetZ = Math.Max(0, Math.Min(DockOffsetZ, maxZOffsetVertical));
                    Location = new Point(
                        position == DockPosition.Left ? workingArea.Left + DockOffset : workingArea.Right - dockWidth - DockOffset,
                        workingArea.Top + DockOffsetZ
                    );
                    break;
                case DockPosition.Top:
                case DockPosition.Bottom:
                    int maxZOffsetHorizontal = workingArea.Width - dockWidth;
                    DockOffsetZ = Math.Max(0, Math.Min(DockOffsetZ, maxZOffsetHorizontal));
                    Location = new Point(
                        workingArea.Left + DockOffsetZ,
                        position == DockPosition.Top ? workingArea.Top + DockOffset : workingArea.Bottom - dockHeight - DockOffset
                    );
                    break;
            }

            ClientSize = new Size(dockWidth, dockHeight);

            Button cfgButton = NitroDockMain_OpacityPanel.Controls.OfType<Button>()
                .FirstOrDefault(b => b.Tag.ToString() == "NitroDockMain_Configuration");

            if (cfgButton != null)
            {
                PositionConfigButton(cfgButton);
            }

            RedistributeButtons();
            UpdateRoundedRegion();
        }

        private void RemoveButton(Button button)
        {
            if (button.Tag?.ToString() == "NitroDockMain_Configuration")
            {
                MessageBox.Show("The Configuration button cannot be removed.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            NitroDockMain_OpacityPanel.Controls.Remove(button);
            button.Dispose();
            RedistributeButtons();
        }

        public void UpdateAllIconSizes(int newSize)
        {
            IconSize = Math.Min(newSize, maxIconSize);

            foreach (Button button in NitroDockMain_OpacityPanel.Controls.OfType<Button>())
            {
                string path = button.Tag?.ToString();
                if (path != null)
                {
                    try
                    {
                        if (button.Tag.ToString() != "NitroDockMain_Configuration")
                        {
                            if (button.Image?.Tag is string customIconPath && File.Exists(customIconPath))
                            {
                                using (Image customImage = Image.FromFile(customIconPath))
                                {
                                    button.Image = ResizeImage(customImage, IconSize, IconSize);
                                    button.Image.Tag = customIconPath;
                                }
                            }
                            else
                            {
                                Icon icon = GetIconForPath(path);
                                if (icon != null)
                                {
                                    Bitmap bitmap = icon.ToBitmap();
                                    button.Image = ResizeImage(bitmap, IconSize, IconSize);
                                }
                                else
                                {
                                    button.Image = ResizeImage(GetDefaultIcon().ToBitmap(), IconSize, IconSize);
                                }
                            }
                        }
                        else
                        {
                            string configIconPath = Path.Combine(GetNitroIconsPath(), "Config.png");
                            if (File.Exists(configIconPath))
                            {
                                using (Bitmap bitmap = new Bitmap(configIconPath))
                                {
                                    button.Image = ResizeImage(bitmap, IconSize, IconSize);
                                }
                            }
                            else
                            {
                                button.Image = ResizeImage(SystemIcons.Shield.ToBitmap(), IconSize, IconSize);
                            }
                        }

                        button.Size = new Size(IconSize, IconSize);
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"Error resizing icon: {ex.Message}");
                        button.Image = ResizeImage(GetDefaultIcon().ToBitmap(), IconSize, IconSize);
                    }
                }
            }

            SnapToEdge(currentDockPosition);
            ApplyGlowEffect();
        }

        public void UpdateAllIconSpacings(int newSpacing)
        {
            IconSpacing = Math.Min(newSpacing, maxIconSpacing);
            RedistributeButtons();
        }

        public void ApplyGlowEffect()
        {
            foreach (Button button in NitroDockMain_OpacityPanel.Controls.OfType<Button>())
            {
                button.FlatAppearance.BorderSize = 0;
                button.BackColor = Color.Transparent;

                button.MouseEnter += (sender, e) =>
                {
                    Color glowColor = GetGlowColor(SelectedGlowColor);
                    button.BackColor = Color.FromArgb(50, glowColor);
                };

                button.MouseLeave += (sender, e) =>
                {
                    button.BackColor = Color.Transparent;
                };
            }
        }

        private Color GetGlowColor(GlowColor glowColor)
        {
            return glowColor switch
            {
                GlowColor.Blue => Color.Blue,
                GlowColor.Cyan => Color.Cyan,
                GlowColor.Pink => Color.HotPink,
                GlowColor.Red => Color.Red,
                GlowColor.SlateGray => Color.SlateGray,
                GlowColor.White => Color.WhiteSmoke,
                GlowColor.Yellow => Color.LightYellow,
                _ => Color.Blue,
            };
        }
    }
}
