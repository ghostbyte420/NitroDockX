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

            NitroDockMain_Configuration_OpacityPanel_GroupBox_ComboBox_DockPositioning.Items.AddRange(
                Enum.GetNames(typeof(DockPosition))
            );
            NitroDockMain_Configuration_OpacityPanel_GroupBox_ComboBox_DockPositioning.SelectedItem =
                _mainForm.currentDockPosition.ToString();

            NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_DockOpacity.Value =
                (int)(_mainForm.Opacity * 100);

            NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_DockOffset_Edge.Value =
                _mainForm.DockOffset;

            Screen screen = Screen.FromControl(_mainForm);
            Rectangle workingArea = screen.WorkingArea;
            int maxZOffset = _mainForm.currentDockPosition == DockPosition.Left || _mainForm.currentDockPosition == DockPosition.Right
                ? workingArea.Height - _mainForm.ClientSize.Height
                : workingArea.Width - _mainForm.ClientSize.Width;

            NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_DockOffset_Alignment.Maximum = maxZOffset;
            NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_DockOffset_Alignment.Value =
                _mainForm.DockOffsetZ;

            NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_ResizeIcons.Value =
                _mainForm.IconSize;

            NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_IconSpacing.Value =
                _mainForm.IconSpacing;

            // Populate Skin Selection ComboBox
            string skinsPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "NitroSkins");
            if (Directory.Exists(skinsPath))
            {
                foreach (string skinFolder in Directory.GetDirectories(skinsPath))
                {
                    string skinName = Path.GetFileName(skinFolder);
                    NitroDockMain_Configuration_OpacityPanel_GroupBox_ComboBox_DockSkin.Items.Add(skinName);
                }
            }

            // Populate Skin Mode ComboBox
            NitroDockMain_Configuration_OpacityPanel_GroupBox_ComboBox_SkinDisplayMode.Items.AddRange(new object[] { "None", "Tile", "Center", "Stretch", "Zoom" });

            // Load saved skin
            string iniPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "NitroDockX.ini");
            IniFile ini = new IniFile(iniPath);
            string savedSkin = ini.Read("DockSettings", "Skin", "Default");
            NitroDockMain_Configuration_OpacityPanel_GroupBox_ComboBox_DockSkin.SelectedItem = savedSkin;

            // Load saved skin mode
            string savedSkinMode = ini.Read("DockSettings", "SkinMode", "Stretch");
            NitroDockMain_Configuration_OpacityPanel_GroupBox_ComboBox_SkinDisplayMode.SelectedItem = savedSkinMode;

            NitroDockMain_Configuration_OpacityPanel_GroupBox_ComboBox_DockPositioning.SelectedIndexChanged +=
                (s, e) => ApplySelectedPosition();

            NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_DockOpacity.ValueChanged +=
                (s, e) => ApplyFormOpacity();

            NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_DockOffset_Edge.ValueChanged +=
                (s, e) => ApplyDockOffset();

            NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_DockOffset_Alignment.ValueChanged +=
                (s, e) => ApplyDockOffsetZ();

            NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_ResizeIcons.ValueChanged +=
                (s, e) => ApplyIconSize();

            NitroDockMain_Configuration_OpacityPanel_GroupBox_TrackBar_IconSpacing.ValueChanged +=
                (s, e) => ApplyIconSpacing();
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

        private void NitroDockMain_Configuration_OpacityPanel_Button_ApplyChanges_Click(object sender, EventArgs e)
        {
            string iniPath = Path.Combine(
                Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location),
                "NitroDockX.ini"
            );
            IniFile ini = new IniFile(iniPath);

            // Save dock settings
            ini.Write("DockSettings", "DockPosition", _mainForm.currentDockPosition.ToString());
            ini.Write("DockSettings", "DockOpacity", _mainForm.Opacity.ToString());
            ini.Write("DockSettings", "DockOffset", _mainForm.DockOffset.ToString());
            ini.Write("DockSettings", "DockOffsetZ", _mainForm.DockOffsetZ.ToString());
            ini.Write("DockSettings", "IconSize", _mainForm.IconSize.ToString());
            ini.Write("DockSettings", "IconSpacing", _mainForm.IconSpacing.ToString());

            // Save skin
            string selectedSkin = NitroDockMain_Configuration_OpacityPanel_GroupBox_ComboBox_DockSkin.SelectedItem?.ToString() ?? "Default";
            ini.Write("DockSettings", "Skin", selectedSkin);

            // Save skin mode
            string selectedSkinMode = NitroDockMain_Configuration_OpacityPanel_GroupBox_ComboBox_SkinDisplayMode.SelectedItem?.ToString() ?? "Stretch";
            ini.Write("DockSettings", "SkinMode", selectedSkinMode);

            // Save icons
            int index = 0;
            foreach (Button button in _mainForm.NitroDockMain_OpacityPanel.Controls.OfType<Button>()
                .Where(b => b.Tag?.ToString() != "NitroDockMain_Configuration"))
            {
                string iconKey = $"Icon{index}";
                ini.Write("Icons", $"{iconKey}_Path", button.Tag.ToString());
                if (button.Image?.Tag != null)
                {
                    string customIconPath = button.Image.Tag.ToString();
                    if (!string.IsNullOrEmpty(customIconPath) && File.Exists(customIconPath))
                    {
                        ini.Write("Icons", $"{iconKey}_CustomIcon", customIconPath);
                    }
                }
                index++;
            }

            // Apply skin
            _mainForm.ApplySkin(selectedSkin, selectedSkinMode);

            SystemSounds.Beep.Play();
            MessageBox.Show("Settings saved to NitroDockX.ini!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
    }
}
