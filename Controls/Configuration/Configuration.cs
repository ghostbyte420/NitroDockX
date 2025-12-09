using System;
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

            NitroDockMain_Configuration_OpacityPanel_ComboBox_DockPositioning.Items.AddRange(
                Enum.GetNames(typeof(NitroDockMain.DockPosition))
            );

            NitroDockMain_Configuration_OpacityPanel_ComboBox_DockPositioning.SelectedItem =
                _mainForm.currentDockPosition.ToString();

            NitroDockMain_Configuration_OpacityPanel_TrackBar_DockOpacity.Value =
                (int)(_mainForm.Opacity * 100);
            NitroDockMain_Configuration_OpacityPanel_TrackBar_DockOffset_Edge.Value =
                _mainForm.DockOffset;

            Screen screen = Screen.FromControl(_mainForm);
            Rectangle workingArea = screen.WorkingArea;
            int maxZOffset = _mainForm.currentDockPosition == DockPosition.Left || _mainForm.currentDockPosition == DockPosition.Right
                ? workingArea.Height - _mainForm.ClientSize.Height
                : workingArea.Width - _mainForm.ClientSize.Width;

            NitroDockMain_Configuration_OpacityPanel_TrackBar_DockOffset_Alignment.Maximum = maxZOffset;
            NitroDockMain_Configuration_OpacityPanel_TrackBar_DockOffset_Alignment.Value =
                _mainForm.DockOffsetZ;

            NitroDockMain_Configuration_OpacityPanel_TrackBar_ResizeIcons.Value =
                _mainForm.IconSize;
            NitroDockMain_Configuration_OpacityPanel_TrackBar_IconSpacing.Value =
                _mainForm.IconSpacing;

            NitroDockMain_Configuration_OpacityPanel_ComboBox_DockPositioning.SelectedIndexChanged +=
                (s, e) => ApplySelectedPosition();
            NitroDockMain_Configuration_OpacityPanel_TrackBar_DockOpacity.ValueChanged +=
                (s, e) => ApplyFormOpacity();
            NitroDockMain_Configuration_OpacityPanel_TrackBar_DockOffset_Edge.ValueChanged +=
                (s, e) => ApplyDockOffset();
            NitroDockMain_Configuration_OpacityPanel_TrackBar_DockOffset_Alignment.ValueChanged +=
                (s, e) => ApplyDockOffsetZ();
            NitroDockMain_Configuration_OpacityPanel_TrackBar_ResizeIcons.ValueChanged +=
                (s, e) => ApplyIconSize();
            NitroDockMain_Configuration_OpacityPanel_TrackBar_IconSpacing.ValueChanged +=
                (s, e) => ApplyIconSpacing();
        }

        private void ApplySelectedPosition()
        {
            if (Enum.TryParse(
                NitroDockMain_Configuration_OpacityPanel_ComboBox_DockPositioning.SelectedItem.ToString(),
                out NitroDockMain.DockPosition position
            ))
            {
                _mainForm.currentDockPosition = position;

                Screen screen = Screen.FromControl(_mainForm);
                Rectangle workingArea = screen.WorkingArea;
                int maxZOffset = position == DockPosition.Left || position == DockPosition.Right
                    ? workingArea.Height - _mainForm.ClientSize.Height
                    : workingArea.Width - _mainForm.ClientSize.Width;

                NitroDockMain_Configuration_OpacityPanel_TrackBar_DockOffset_Alignment.Maximum = maxZOffset;
                _mainForm.SnapToEdge(position);
            }
        }

        private void ApplyFormOpacity()
        {
            float opacity = NitroDockMain_Configuration_OpacityPanel_TrackBar_DockOpacity.Value / 100f;
            _mainForm.Opacity = opacity;
        }

        private void ApplyDockOffset()
        {
            int offset = NitroDockMain_Configuration_OpacityPanel_TrackBar_DockOffset_Edge.Value;
            _mainForm.DockOffset = offset;
            _mainForm.SnapToEdge(_mainForm.currentDockPosition);
        }

        private void ApplyDockOffsetZ()
        {
            int offsetZ = NitroDockMain_Configuration_OpacityPanel_TrackBar_DockOffset_Alignment.Value;
            _mainForm.DockOffsetZ = offsetZ;
            _mainForm.SnapToEdge(_mainForm.currentDockPosition);
        }

        private void ApplyIconSize()
        {
            int newIconSize = NitroDockMain_Configuration_OpacityPanel_TrackBar_ResizeIcons.Value;
            _mainForm.UpdateAllIconSizes(newIconSize);
        }

        private void ApplyIconSpacing()
        {
            int newIconSpacing = NitroDockMain_Configuration_OpacityPanel_TrackBar_IconSpacing.Value;
            _mainForm.UpdateAllIconSpacings(newIconSpacing);
        }
    }
}
