using Microsoft.Win32;
using System;
using System.IO;
using System.Media;
using System.Reflection;
using System.Windows.Forms;
using static NitroDock.NitroDockMain;

namespace NitroDock
{
    public partial class NitroDockMain_Configuration : Form
    {
        private NitroDockMain _mainForm;

        public NitroDockMain_Configuration(NitroDockMain mainForm)
        {
            InitializeComponent();
            _mainForm = mainForm;

            NitroDockMain_Configuration_OpacityPanel_GroupBox_ComboBox_DockPositioning.Items.AddRange(Enum.GetNames(typeof(DockPosition)));
            NitroDockMain_Configuration_OpacityPanel_GroupBox_ComboBox_DockPositioning.SelectedItem = _mainForm.currentDockPosition.ToString();

            int loadedDockOpacity = (int)(_mainForm.Opacity * 100);
            loadedDockOpacity = Math.Max(
                NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_DockOpacity.Minimum,
                Math.Min(
                    NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_DockOpacity.Maximum,
                    loadedDockOpacity
                )
            );
            NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_DockOpacity.Value = loadedDockOpacity;

            int loadedDockOffset = _mainForm.DockOffset;
            loadedDockOffset = Math.Max(
                NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_DockOffset_Edge.Minimum,
                Math.Min(
                    NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_DockOffset_Edge.Maximum,
                    loadedDockOffset
                )
            );
            NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_DockOffset_Edge.Value = loadedDockOffset;

            Screen screen = Screen.FromControl(_mainForm);
            Rectangle workingArea = screen.WorkingArea;
            int maxZOffset = _mainForm.currentDockPosition == DockPosition.Left || _mainForm.currentDockPosition == DockPosition.Right
                ? workingArea.Height - _mainForm.ClientSize.Height
                : workingArea.Width - _mainForm.ClientSize.Width;

            NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_DockOffset_Alignment.Maximum = maxZOffset;

            int loadedDockOffsetZ = _mainForm.DockOffsetZ;
            loadedDockOffsetZ = Math.Max(
                NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_DockOffset_Alignment.Minimum,
                Math.Min(
                    NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_DockOffset_Alignment.Maximum,
                    loadedDockOffsetZ
                )
            );
            NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_DockOffset_Alignment.Value = loadedDockOffsetZ;

            int loadedIconSize = _mainForm.IconSize;
            loadedIconSize = Math.Max(
                NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_ResizeIcons.Minimum,
                Math.Min(
                    NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_ResizeIcons.Maximum,
                    loadedIconSize
                )
            );
            NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_ResizeIcons.Value = loadedIconSize;

            int loadedIconSpacing = _mainForm.IconSpacing;
            loadedIconSpacing = Math.Max(
                NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_IconSpacing.Minimum,
                Math.Min(
                    NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_IconSpacing.Maximum,
                    loadedIconSpacing
                )
            );
            NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_IconSpacing.Value = loadedIconSpacing;

            NitroDockMain_Configuration_OpacityPanel_GroupBox_ComboBox_HighlightGlow.Items.AddRange(Enum.GetNames(typeof(GlowColor)));
            NitroDockMain_Configuration_OpacityPanel_GroupBox_ComboBox_HighlightGlow.SelectedItem = _mainForm.SelectedGlowColor.ToString();

            // Initialize ComboBox for Dock Corners
            NitroDockMain_Configuration_OpacityPanel_GroupBox_ComboBox_DockCorners.Items.Clear();
            NitroDockMain_Configuration_OpacityPanel_GroupBox_ComboBox_DockCorners.Items.AddRange(new object[] { "Round Dock Corners", "Square Dock Corners" });

            // Set SelectedIndexChanged event
            NitroDockMain_Configuration_OpacityPanel_GroupBox_ComboBox_DockCorners.SelectedIndexChanged += (s, e) =>
            {
                string cornerStyle = NitroDockMain_Configuration_OpacityPanel_GroupBox_ComboBox_DockCorners.SelectedItem.ToString();
                _mainForm.CurrentCornerStyle = (cornerStyle == "Round Dock Corners") ? CornerStyle.Round : CornerStyle.Square;
                _mainForm.UpdateRoundedRegion();
            };

            string skinsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "NitroSkins");
            if (Directory.Exists(skinsPath))
            {
                foreach (string skinFolder in Directory.GetDirectories(skinsPath))
                {
                    string skinName = Path.GetFileName(skinFolder);
                    NitroDockMain_Configuration_OpacityPanel_GroupBox_ComboBox_DockSkin.Items.Add(skinName);
                }
            }

            NitroDockMain_Configuration_OpacityPanel_GroupBox_ComboBox_SkinDisplayMode.Items.AddRange(
                new object[] { "None", "Tile", "Center", "Stretch", "Zoom" }
            );

            LoadSettings();

            NitroDockMain_Configuration_OpacityPanel_GroupBox_ComboBox_DockPositioning.SelectedIndexChanged += (s, e) => ApplySelectedPosition();
            NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_DockOpacity.ValueChanged += (s, e) => ApplyFormOpacity();
            NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_DockOffset_Edge.ValueChanged += (s, e) => ApplyDockOffset();
            NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_DockOffset_Alignment.ValueChanged += (s, e) => ApplyDockOffsetZ();
            NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_ResizeIcons.ValueChanged += (s, e) => ApplyIconSize();
            NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_IconSpacing.ValueChanged += (s, e) => ApplyIconSpacing();
            NitroDockMain_Configuration_OpacityPanel_GroupBox_ComboBox_HighlightGlow.SelectedIndexChanged += (s, e) => ApplyGlowEffect();
        }

        private void LoadSettings()
        {
            string iniPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "NitroDockX.ini");
            IniFile ini = new IniFile(iniPath);

            if (Enum.TryParse(ini.Read("DockSettings", "DockPosition"), out DockPosition position))
                NitroDockMain_Configuration_OpacityPanel_GroupBox_ComboBox_DockPositioning.SelectedItem = position.ToString();

            if (int.TryParse(ini.Read("DockSettings", "DockOpacityValue"), out int opacityValue))
                NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_DockOpacity.Value = opacityValue;

            if (int.TryParse(ini.Read("DockSettings", "DockOffset"), out int dockOffset))
                NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_DockOffset_Edge.Value = dockOffset;

            if (int.TryParse(ini.Read("DockSettings", "DockOffsetZ"), out int dockOffsetZ))
                NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_DockOffset_Alignment.Value = dockOffsetZ;

            if (int.TryParse(ini.Read("DockSettings", "IconSize"), out int iconSize))
                NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_ResizeIcons.Value = iconSize;

            if (int.TryParse(ini.Read("DockSettings", "IconSpacing"), out int iconSpacing))
                NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_IconSpacing.Value = iconSpacing;

            if (Enum.TryParse(ini.Read("DockSettings", "GlowColor"), out GlowColor glowColor))
                NitroDockMain_Configuration_OpacityPanel_GroupBox_ComboBox_HighlightGlow.SelectedItem = glowColor.ToString();

            // Load Dock Corner Style
            string cornerStyle = ini.Read("DockSettings", "DockCornerStyle", "Round Dock Corners");
            if (NitroDockMain_Configuration_OpacityPanel_GroupBox_ComboBox_DockCorners.Items.Contains(cornerStyle))
            {
                NitroDockMain_Configuration_OpacityPanel_GroupBox_ComboBox_DockCorners.SelectedItem = cornerStyle;
            }
            else
            {
                NitroDockMain_Configuration_OpacityPanel_GroupBox_ComboBox_DockCorners.SelectedItem = "Round Dock Corners";
            }
            _mainForm.CurrentCornerStyle = (cornerStyle == "Round Dock Corners") ? CornerStyle.Round : CornerStyle.Square;
            _mainForm.UpdateRoundedRegion();

            string savedSkin = ini.Read("DockSettings", "Skin", "Default");
            NitroDockMain_Configuration_OpacityPanel_GroupBox_ComboBox_DockSkin.SelectedItem = savedSkin;

            string savedSkinMode = ini.Read("DockSettings", "SkinMode", "Stretch");
            NitroDockMain_Configuration_OpacityPanel_GroupBox_ComboBox_SkinDisplayMode.SelectedItem = savedSkinMode;

            bool launchOnRestart = bool.TryParse(ini.Read("DockSettings", "LaunchOnRestart"), out bool result) && result;
            NitroDockMain_Configuration_OpacityPanel_GroupBox_CheckBox_LaunchOnRestart.Checked = launchOnRestart;
        }

        private void ApplySelectedPosition()
        {
            if (Enum.TryParse(
                NitroDockMain_Configuration_OpacityPanel_GroupBox_ComboBox_DockPositioning.SelectedItem.ToString(),
                out DockPosition position
            ))
            {
                _mainForm.currentDockPosition = position;
                Screen screen = Screen.FromControl(_mainForm);
                Rectangle workingArea = screen.WorkingArea;
                int maxZOffset = position == DockPosition.Left || position == DockPosition.Right
                    ? workingArea.Height - _mainForm.ClientSize.Height
                    : workingArea.Width - _mainForm.ClientSize.Width;

                NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_DockOffset_Alignment.Maximum = maxZOffset;
                _mainForm.SnapToEdge(position);
            }
        }

        private void ApplyFormOpacity()
        {
            float opacity = NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_DockOpacity.Value / 100f;
            _mainForm.Opacity = opacity;
        }

        private void ApplyDockOffset()
        {
            int offset = NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_DockOffset_Edge.Value;
            _mainForm.DockOffset = offset;
            _mainForm.SnapToEdge(_mainForm.currentDockPosition);
        }

        private void ApplyDockOffsetZ()
        {
            int offsetZ = NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_DockOffset_Alignment.Value;
            _mainForm.DockOffsetZ = offsetZ;
            _mainForm.SnapToEdge(_mainForm.currentDockPosition);
        }

        private void ApplyIconSize()
        {
            int newIconSize = NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_ResizeIcons.Value;
            _mainForm.UpdateAllIconSizes(newIconSize);
        }

        private void ApplyIconSpacing()
        {
            int newIconSpacing = NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_IconSpacing.Value;
            _mainForm.UpdateAllIconSpacings(newIconSpacing);
        }

        private void ApplyGlowEffect()
        {
            if (Enum.TryParse(NitroDockMain_Configuration_OpacityPanel_GroupBox_ComboBox_HighlightGlow.SelectedItem.ToString(), out GlowColor selectedGlowColor))
            {
                _mainForm.SelectedGlowColor = selectedGlowColor;
                _mainForm.ApplyGlowEffect();
            }
        }

        private void NitroDockMain_Configuration_OpacityPanel_Button_ApplyChanges_Click(object sender, EventArgs e)
        {
            string iniPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "NitroDockX.ini");
            IniFile ini = new IniFile(iniPath);

            // Store old values for comparison
            int oldIconSize = _mainForm.IconSize;
            int oldIconSpacing = _mainForm.IconSpacing;

            if (Enum.TryParse(
                NitroDockMain_Configuration_OpacityPanel_GroupBox_ComboBox_DockPositioning.SelectedItem.ToString(),
                out DockPosition position
            ))
            {
                _mainForm.currentDockPosition = position;
                ini.Write("DockSettings", "DockPosition", position.ToString());
            }

            int dockOpacityValue = NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_DockOpacity.Value;
            float dockOpacity = dockOpacityValue / 100f;
            _mainForm.Opacity = dockOpacity;
            ini.Write("DockSettings", "DockOpacity", dockOpacity.ToString());
            ini.Write("DockSettings", "DockOpacityValue", dockOpacityValue.ToString());

            int dockOffset = NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_DockOffset_Edge.Value;
            _mainForm.DockOffset = dockOffset;
            ini.Write("DockSettings", "DockOffset", dockOffset.ToString());

            int dockOffsetZ = NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_DockOffset_Alignment.Value;
            _mainForm.DockOffsetZ = dockOffsetZ;
            ini.Write("DockSettings", "DockOffsetZ", dockOffsetZ.ToString());

            ini.Write("DockSettings", "DockLocationX", _mainForm.Location.X.ToString());
            ini.Write("DockSettings", "DockLocationY", _mainForm.Location.Y.ToString());

            int iconSize = NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_ResizeIcons.Value;
            _mainForm.UpdateAllIconSizes(iconSize);
            ini.Write("DockSettings", "IconSize", iconSize.ToString());

            int iconSpacing = NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_IconSpacing.Value;
            _mainForm.UpdateAllIconSpacings(iconSpacing);
            ini.Write("DockSettings", "IconSpacing", iconSpacing.ToString());

            if (Enum.TryParse(NitroDockMain_Configuration_OpacityPanel_GroupBox_ComboBox_HighlightGlow.SelectedItem.ToString(), out GlowColor selectedGlowColor))
            {
                _mainForm.SelectedGlowColor = selectedGlowColor;
                ini.Write("DockSettings", "GlowColor", selectedGlowColor.ToString());
            }

            // Save Dock Corner Style
            string cornerStyle = NitroDockMain_Configuration_OpacityPanel_GroupBox_ComboBox_DockCorners.SelectedItem.ToString();
            _mainForm.CurrentCornerStyle = (cornerStyle == "Round Dock Corners") ? CornerStyle.Round : CornerStyle.Square;
            ini.Write("DockSettings", "DockCornerStyle", cornerStyle);
            _mainForm.UpdateRoundedRegion();

            string selectedSkin = NitroDockMain_Configuration_OpacityPanel_GroupBox_ComboBox_DockSkin.SelectedItem?.ToString() ?? "Default";
            ini.Write("DockSettings", "Skin", selectedSkin);

            string selectedSkinMode = NitroDockMain_Configuration_OpacityPanel_GroupBox_ComboBox_SkinDisplayMode.SelectedItem?.ToString() ?? "Stretch";
            ini.Write("DockSettings", "SkinMode", selectedSkinMode);

            bool launchOnRestart = NitroDockMain_Configuration_OpacityPanel_GroupBox_CheckBox_LaunchOnRestart.Checked;
            ini.Write("DockSettings", "LaunchOnRestart", launchOnRestart.ToString());
            SetStartup(launchOnRestart);

            // Clear existing icon entries
            int index = 0;
            while (true)
            {
                string path = ini.Read("Icons", $"Icon{index}_Path");
                if (string.IsNullOrEmpty(path)) break;
                ini.Write("Icons", $"Icon{index}_Path", "");
                ini.Write("Icons", $"Icon{index}_CustomIcon", "");
                ini.Write("Icons", $"Icon{index}_ContainerBackgroundColor", "");
                ini.Write("Icons", $"Icon{index}_ContainerBackgroundTexture", "");
                index++;
            }

            // Save current icons and containers
            index = 0;
            foreach (IconContainer container in _mainForm.NitroDockMain_OpacityPanel.Controls.OfType<IconContainer>())
            {
                if ((container.Controls[0] as Button).Tag.ToString() == "NitroDockMain_Configuration")
                {
                    // Save the Configuration button container's background color separately
                    ini.Write("Icons", "ConfigContainer_ContainerBackgroundColor", container.BackColor.ToArgb().ToString());
                }
                else
                {
                    string iconKey = $"Icon{index}";
                    ini.Write("Icons", $"{iconKey}_Path", (container.Controls[0] as Button).Tag.ToString());

                    if ((container.Controls[0] as Button).Image?.Tag != null)
                    {
                        string customIconPath = (container.Controls[0] as Button).Image.Tag.ToString();
                        if (!string.IsNullOrEmpty(customIconPath) && File.Exists(customIconPath))
                        {
                            ini.Write("Icons", $"{iconKey}_CustomIcon", customIconPath);
                        }
                    }

                    // Save container background color
                    ini.Write("Icons", $"{iconKey}_ContainerBackgroundColor", container.BackColor.ToArgb().ToString());

                    // Save container background texture
                    if (container.BackgroundImage?.Tag is string texturePath)
                        ini.Write("Icons", $"{iconKey}_ContainerBackgroundTexture", texturePath);

                    index++;
                }
            }

            _mainForm.ApplySkin(selectedSkin, selectedSkinMode);
            SystemSounds.Beep.Play();
            MessageBox.Show("Settings saved to NitroDockX.ini!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);

            // Only redistribute if spacing or size changed
            if (oldIconSpacing != _mainForm.IconSpacing || oldIconSize != _mainForm.IconSize)
                _mainForm.RedistributeContainers();
        }

        private void SetStartup(bool enabled)
        {
            string appPath = Application.ExecutablePath;
            RegistryKey rk = Registry.CurrentUser.OpenSubKey("SOFTWARE\\Microsoft\\Windows\\CurrentVersion\\Run", true);

            if (enabled)
            {
                rk.SetValue("NitroDockX", appPath);
            }
            else
            {
                rk.DeleteValue("NitroDockX", false);
            }
        }
    }
}
