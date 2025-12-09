using System;
using System.Drawing;
using System.IO;
using System.Reflection;
using System.Windows.Forms;

namespace NitroDock
{
    public partial class NitroDockMain_IconProperties : Form
    {
        private Button targetButton;
        private string nitroIconsPath;
        private string originalPath;

        public NitroDockMain_IconProperties(Button button)
        {
            InitializeComponent();
            targetButton = button;
            nitroIconsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "NitroIcons");
            originalPath = targetButton.Tag?.ToString() ?? string.Empty;

            NitroDockMain_SplitContainer_Panel2_Properties_OpacityPanel_TextBox_SourceDirectory.Text = nitroIconsPath;
            NitroDockMain_SplitContainer_Panel2_Properties_OpacityPanel_TextBox_SelectedIcon.Text =
                Path.GetFileName(targetButton.Tag?.ToString() ?? "None");

            PopulateIconPreview();
            NitroDockMain_SplitContainer_Panel2_Properties_OpacityPanel_Button_OpenNitroIconsDirectory.Click +=
                (s, e) => System.Diagnostics.Process.Start("explorer.exe", nitroIconsPath);

            if (targetButton.Tag?.ToString() == "NitroDockMain_Configuration")
            {
                HideRemoveOption();
            }
        }

        public void HideRemoveOption()
        {
            if (targetButton?.ContextMenuStrip != null)
            {
                foreach (ToolStripItem item in targetButton.ContextMenuStrip.Items)
                {
                    if (item.Text == "Remove Item")
                    {
                        item.ForeColor = Color.Gray;
                        item.Enabled = false;
                        break;
                    }
                }
            }
        }

        private void PopulateIconPreview()
        {
            NitroDockMain_SplitContainer_Panel1_IconProperties_OpacityPanel_IconPreview.Controls.Clear();
            NitroDockMain_SplitContainer_Panel1_IconProperties_OpacityPanel_IconPreview.AutoScroll = true;

            if (Directory.Exists(nitroIconsPath))
            {
                int x = 10, y = 10;
                int maxWidth = NitroDockMain_SplitContainer_Panel1_IconProperties_OpacityPanel_IconPreview.ClientSize.Width - 20;

                foreach (string iconFile in Directory.GetFiles(nitroIconsPath, "*.png")
                    .Concat(Directory.GetFiles(nitroIconsPath, "*.ico")))
                {
                    PictureBox pic = new PictureBox
                    {
                        Size = new Size(64, 64),
                        SizeMode = PictureBoxSizeMode.Zoom,
                        Tag = iconFile,
                        Cursor = Cursors.Hand,
                        Location = new Point(x, y)
                    };

                    pic.DoubleClick += (s, e) => SelectIcon(iconFile);

                    try
                    {
                        if (iconFile.EndsWith(".ico"))
                            pic.Image = Icon.ExtractAssociatedIcon(iconFile).ToBitmap();
                        else
                            pic.Image = Image.FromFile(iconFile);
                    }
                    catch
                    {
                        pic.Image = new Bitmap(64, 64, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
                        using (Graphics g = Graphics.FromImage(pic.Image))
                        {
                            g.Clear(Color.LightGray);
                            g.DrawString("Invalid", Font, Brushes.Red, new PointF(5, 5));
                        }
                    }

                    NitroDockMain_SplitContainer_Panel1_IconProperties_OpacityPanel_IconPreview.Controls.Add(pic);
                    x += 74;
                    if (x + 74 > maxWidth)
                    {
                        x = 10;
                        y += 74;
                    }
                }
            }
        }

        private void SelectIcon(string iconPath)
        {
            try
            {
                NitroDockMain_SplitContainer_Panel2_Properties_OpacityPanel_TextBox_SelectedIcon.Text = Path.GetFileName(iconPath);
                targetButton.Image = ResizeImage(Image.FromFile(iconPath), targetButton.Width, targetButton.Height);
                targetButton.Image.Tag = iconPath;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading icon: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private Bitmap ResizeImage(Image image, int width, int height)
        {
            Rectangle destRect = new Rectangle(0, 0, width, height);
            Bitmap destImage = new Bitmap(width, height);
            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (Graphics graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = System.Drawing.Drawing2D.CompositingMode.SourceCopy;
                graphics.CompositingQuality = System.Drawing.Drawing2D.CompositingQuality.HighQuality;
                graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;

                using (System.Drawing.Imaging.ImageAttributes wrapMode = new System.Drawing.Imaging.ImageAttributes())
                {
                    wrapMode.SetWrapMode(System.Drawing.Drawing2D.WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }
    }
}
