using System;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace NitroDock
{
    public partial class NitroDockMain : Form
    {
        private bool isDragging = false;
        private Point lastCursor;
        private Point lastForm;
        private const int buttonSize = 60;
        private const int buttonSpacing = 13;

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
                        lastForm.Y + (Cursor.Position.Y - lastCursor.Y)
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

            NitroDockMain_OpacityPanel.DragDrop += NitroDockMain_OpacityPanel_DragDrop;

            // Set initial dock position
            SnapToEdge(currentDockPosition);
            PositionConfigButton();
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
                        20);
                    break;
                case DockPosition.Top:
                case DockPosition.Bottom:
                    NitroDockMain_OpacityPanel_Button_Configuration.Location = new Point(
                        20,
                        (NitroDockMain_OpacityPanel.Height / 2) - (buttonHeight / 2));
                    break;
            }
        }

        private void NitroDockMain_OpacityPanel_Button_Configuration_Click(object sender, EventArgs e)
        {
            NitroDockMain_Configuration configForm = new NitroDockMain_Configuration(this);
            configForm.Show();
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
            }
        }

        private void AddButtonForFileOrDirectory(string path)
        {
            Button button = new Button
            {
                Size = new Size(buttonSize, buttonSize),
                BackColor = Color.Transparent,
                FlatStyle = FlatStyle.Flat,
                FlatAppearance = { BorderSize = 0 }
            };

            try
            {
                Icon icon = GetIconForPath(path);
                if (icon != null)
                {
                    Bitmap bitmap = icon.ToBitmap();
                    button.Image = bitmap;
                    button.ImageAlign = ContentAlignment.MiddleCenter;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error loading icon: {ex.Message}");
            }

            button.Tag = path;
            button.Click += (sender, e) =>
            {
                string fullPath = ((Button)sender).Tag.ToString();
                if (File.Exists(fullPath))
                {
                    Process.Start(fullPath);
                }
                else if (Directory.Exists(fullPath))
                {
                    Process.Start("explorer.exe", fullPath);
                }
            };

            NitroDockMain_OpacityPanel.Controls.Add(button);
            PositionNewButton(button);
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
                // Index 3 corresponds to a closed folder icon in shell32.dll
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

        private void PositionNewButton(Button button)
        {
            int yPosition = buttonSpacing + NitroDockMain_OpacityPanel_Button_Configuration.Bottom;
            int xPosition = buttonSpacing;

            if (currentDockPosition == DockPosition.Left || currentDockPosition == DockPosition.Right)
            {
                foreach (Control control in NitroDockMain_OpacityPanel.Controls)
                {
                    if (control is Button && control != NitroDockMain_OpacityPanel_Button_Configuration)
                    {
                        yPosition = Math.Max(yPosition, control.Bottom + buttonSpacing);
                    }
                }
                button.Location = new Point((NitroDockMain_OpacityPanel.Width / 2) - (buttonSize / 2), yPosition);
            }
            else
            {
                foreach (Control control in NitroDockMain_OpacityPanel.Controls)
                {
                    if (control is Button && control != NitroDockMain_OpacityPanel_Button_Configuration)
                    {
                        xPosition = Math.Max(xPosition, control.Right + buttonSpacing);
                    }
                }
                button.Location = new Point(xPosition, (NitroDockMain_OpacityPanel.Height / 2) - (buttonSize / 2));
            }
        }

        public void SnapToEdge(DockPosition position)
        {
            Screen screen = Screen.FromControl(this);
            Rectangle workingArea = screen.WorkingArea;

            int dockWidth = 64;
            int dockHeight = 300;

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
                    Location = new Point(workingArea.Left + (workingArea.Width / 2) - (dockHeight / 2), workingArea.Top + DockOffset);
                    ClientSize = new Size(dockHeight, dockWidth);
                    break;
                case DockPosition.Bottom:
                    Location = new Point(workingArea.Left + (workingArea.Width / 2) - (dockHeight / 2), workingArea.Bottom - dockWidth - DockOffset);
                    ClientSize = new Size(dockHeight, dockWidth);
                    break;
            }
            PositionConfigButton();
        }
    }
}
