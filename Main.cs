using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace NitroDock
{
    public partial class NitroDockMain : Form
    {
        private bool isDragging = false;
        private Point lastCursor;
        private Point lastForm;
        private const int buttonSize = 45;
        private const int buttonSpacing = 13;
        private const int minDockWidth = 64 + 10;
        private const int minDockHeight = 2 * buttonSize + 3 * buttonSpacing + 10;
        private const int cornerRadius = 24;

        public enum DockPosition { Left, Right, Top, Bottom }
        public DockPosition currentDockPosition = DockPosition.Right;

        [DefaultValue(0)]
        public int DockOffset { get; set; } = 0;

        [DllImport("shell32.dll", CharSet = CharSet.Auto)]
        private static extern int ExtractIconEx(string lpszFile, int nIconIndex, IntPtr[] phiconLarge, IntPtr[] phiconSmall, int nIcons);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool DestroyIcon(IntPtr hIcon);

        public NitroDockMain()
        {
            InitializeComponent();
            StartPosition = FormStartPosition.Manual;
            this.FormBorderStyle = FormBorderStyle.None;
            this.BackColor = Color.Black;
            this.SetStyle(ControlStyles.OptimizedDoubleBuffer, true);
            this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
            this.SetStyle(ControlStyles.UserPaint, true);

            // Replace the default configuration button with a custom icon
            SetConfigButtonIcon();

            // Make the form draggable
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
                isDragging = false;
                SnapToEdge(currentDockPosition);
            };

            // Enable drag-and-drop functionality
            NitroDockMain_OpacityPanel.AllowDrop = true;
            NitroDockMain_OpacityPanel.DragEnter += (s, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    e.Effect = DragDropEffects.Copy;
            };

            NitroDockMain_OpacityPanel.DragOver += NitroDockMain_OpacityPanel_DragOver;
            NitroDockMain_OpacityPanel.DragDrop += NitroDockMain_OpacityPanel_DragDrop;

            // Set initial dock position
            SnapToEdge(currentDockPosition);
            PositionConfigButton();
        }

        private void SetConfigButtonIcon()
        {
            // Use a system gear icon (or any other system icon)
            Icon gearIcon = SystemIcons.Shield;
            NitroDockMain_OpacityPanel_Button_Configuration.Image = ResizeImage(gearIcon.ToBitmap(), buttonSize, buttonSize);
            NitroDockMain_OpacityPanel_Button_Configuration.ImageAlign = ContentAlignment.MiddleCenter;
            NitroDockMain_OpacityPanel_Button_Configuration.Text = "";
            NitroDockMain_OpacityPanel_Button_Configuration.FlatStyle = FlatStyle.Flat;
            NitroDockMain_OpacityPanel_Button_Configuration.FlatAppearance.BorderSize = 0;
            NitroDockMain_OpacityPanel_Button_Configuration.BackColor = Color.Transparent;

            // Add context menu to change the config button icon
            ContextMenuStrip configContextMenu = new ContextMenuStrip();
            ToolStripMenuItem changeConfigIconItem = new ToolStripMenuItem("Change Configuration Icon");
            changeConfigIconItem.Click += (s, e) => ChangeConfigButtonIcon();
            configContextMenu.Items.Add(changeConfigIconItem);
            NitroDockMain_OpacityPanel_Button_Configuration.ContextMenuStrip = configContextMenu;
        }

        private void ChangeConfigButtonIcon()
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Icon Files (*.ico)|*.ico|PNG Files (*.png)|*.png|All Files (*.*)|*.*";
                openFileDialog.Title = "Select a new icon for the Configuration button";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string selectedFile = openFileDialog.FileName;
                        Image newIcon;

                        if (selectedFile.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
                        {
                            Icon ico = new Icon(selectedFile);
                            newIcon = ico.ToBitmap();
                        }
                        else // PNG or other image
                        {
                            newIcon = Image.FromFile(selectedFile);
                        }

                        NitroDockMain_OpacityPanel_Button_Configuration.Image = ResizeImage(newIcon, buttonSize, buttonSize);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
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

        private void PositionConfigButton()
        {
            int buttonWidth = NitroDockMain_OpacityPanel_Button_Configuration.Width;
            int buttonHeight = NitroDockMain_OpacityPanel_Button_Configuration.Height;

            switch (currentDockPosition)
            {
                case DockPosition.Left:
                case DockPosition.Right:
                    NitroDockMain_OpacityPanel_Button_Configuration.Location = new Point(
                        (NitroDockMain_OpacityPanel.Width / 2) - (buttonWidth / 2),
                        buttonSpacing);
                    break;
                case DockPosition.Top:
                case DockPosition.Bottom:
                    NitroDockMain_OpacityPanel_Button_Configuration.Location = new Point(
                        buttonSpacing,
                        (NitroDockMain_OpacityPanel.Height / 2) - (buttonHeight / 2));
                    break;
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
                foreach (string filePath in files)
                {
                    AddButtonForFileOrDirectory(filePath);
                }
                RedistributeButtons();
            }
        }

        private void AddButtonForFileOrDirectory(string path)
        {
            Button button = new Button
            {
                Size = new Size(buttonSize, buttonSize),
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
                    button.Image = ResizeImage(bitmap, buttonSize, buttonSize);
                    button.ImageAlign = ContentAlignment.MiddleCenter;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading icon: {ex.Message}");
            }

            // Add context menu for changing icon
            ContextMenuStrip contextMenu = new ContextMenuStrip();
            ToolStripMenuItem changeIconItem = new ToolStripMenuItem("Change Icon");
            changeIconItem.Click += (s, ev) => ChangeIcon(button);
            contextMenu.Items.Add(changeIconItem);
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

            NitroDockMain_OpacityPanel.Controls.Add(button);
        }

        private void ChangeIcon(Button button)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Icon Files (*.ico)|*.ico|PNG Files (*.png)|*.png|All Files (*.*)|*.*";
                openFileDialog.Title = "Select a new icon";

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    try
                    {
                        string selectedFile = openFileDialog.FileName;
                        Image newIcon;

                        if (selectedFile.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
                        {
                            Icon ico = new Icon(selectedFile);
                            newIcon = ico.ToBitmap();
                        }
                        else // PNG or other image
                        {
                            newIcon = Image.FromFile(selectedFile);
                        }

                        button.Image = ResizeImage(newIcon, buttonSize, buttonSize);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error loading image: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
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
                return GetFolderIcon();
            }
            return null;
        }

        private Icon GetFolderIcon()
        {
            IntPtr[] largeIconPtr = new IntPtr[1];
            try
            {
                ExtractIconEx("shell32.dll", 3, largeIconPtr, null, 1);
                Icon icon = Icon.FromHandle(largeIconPtr[0]);
                return icon;
            }
            finally
            {
                if (largeIconPtr[0] != IntPtr.Zero)
                {
                    DestroyIcon(largeIconPtr[0]);
                }
            }
        }

        private void RedistributeButtons()
        {
            var buttons = NitroDockMain_OpacityPanel.Controls.OfType<Button>()
                .Where(b => b != NitroDockMain_OpacityPanel_Button_Configuration)
                .ToList();

            if (buttons.Count == 0)
                return;

            int startY = NitroDockMain_OpacityPanel_Button_Configuration.Bottom + buttonSpacing;
            int startX = NitroDockMain_OpacityPanel_Button_Configuration.Right + buttonSpacing;

            switch (currentDockPosition)
            {
                case DockPosition.Left:
                case DockPosition.Right:
                    int newHeight = Math.Max(minDockHeight, startY + buttons.Count * (buttonSize + buttonSpacing));
                    ClientSize = new Size(ClientSize.Width, newHeight);
                    for (int i = 0; i < buttons.Count; i++)
                    {
                        buttons[i].Location = new Point(
                            (ClientSize.Width / 2) - (buttonSize / 2),
                            startY + i * (buttonSize + buttonSpacing)
                        );
                    }
                    break;
                case DockPosition.Top:
                case DockPosition.Bottom:
                    int newWidth = Math.Max(minDockWidth, startX + buttons.Count * (buttonSize + buttonSpacing));
                    ClientSize = new Size(newWidth, ClientSize.Height);
                    for (int i = 0; i < buttons.Count; i++)
                    {
                        buttons[i].Location = new Point(
                            startX + i * (buttonSize + buttonSpacing),
                            (ClientSize.Height / 2) - (buttonSize / 2)
                        );
                    }
                    break;
            }
        }

        public void SnapToEdge(DockPosition position)
        {
            Screen screen = Screen.FromControl(this);
            Rectangle workingArea = screen.WorkingArea;
            int dockWidth = (position == DockPosition.Left || position == DockPosition.Right) ? minDockWidth : (2 * buttonSize + 3 * buttonSpacing + 10);
            int dockHeight = (position == DockPosition.Left || position == DockPosition.Right) ? minDockHeight : minDockWidth;

            switch (position)
            {
                case DockPosition.Left:
                    Location = new Point(workingArea.Left + DockOffset, workingArea.Top + (workingArea.Height / 2) - (dockHeight / 2));
                    ClientSize = new Size(dockWidth, dockHeight);
                    break;
                case DockPosition.Right:
                    Location = new Point(workingArea.Right - dockWidth - DockOffset, workingArea.Top + (workingArea.Height / 2) - (dockHeight / 2));
                    ClientSize = new Size(dockWidth, dockHeight);
                    break;
                case DockPosition.Top:
                    Location = new Point(workingArea.Left + (workingArea.Width / 2) - (dockWidth / 2), workingArea.Top + DockOffset);
                    ClientSize = new Size(dockWidth, minDockWidth);
                    break;
                case DockPosition.Bottom:
                    Location = new Point(workingArea.Left + (workingArea.Width / 2) - (dockWidth / 2), workingArea.Bottom - minDockWidth - DockOffset);
                    ClientSize = new Size(dockWidth, minDockWidth);
                    break;
            }
            PositionConfigButton();
            RedistributeButtons();
            UpdateRoundedRegion();
        }

        private void NitroDockMain_OpacityPanel_Button_Configuration_Click(object sender, EventArgs e)
        {
            NitroDockMain_Configuration configForm = new NitroDockMain_Configuration(this);
            configForm.Show();
        }
    }
}
