using System;
using System.Windows.Forms;

namespace NitroDock
{
    public partial class NitroDockMain_Configuration : Form
    {
        private NitroDockMain _mainForm;

        public NitroDockMain_Configuration(NitroDockMain mainForm)
        {
            InitializeComponent();
            _mainForm = mainForm;

            // Populate the combo box with dock positions
            NitroDockMain_Configuration_OpacityPanel_ComboBox_DockPositioning.Items.AddRange(
                Enum.GetNames(typeof(NitroDockMain.DockPosition))
            );

            // Set the current dock position
            NitroDockMain_Configuration_OpacityPanel_ComboBox_DockPositioning.SelectedItem =
                _mainForm.currentDockPosition.ToString();

            // Set the initial values for the remaining trackbars
            NitroDockMain_Configuration_OpacityPanel_TrackBar_FormOpacity.Value =
                (int)(_mainForm.Opacity * 100);
            NitroDockMain_Configuration_OpacityPanel_TrackBar_OpacityPanel_DockOffset.Value =
                _mainForm.DockOffset;

            // Update the dock position when the combo box selection changes
            NitroDockMain_Configuration_OpacityPanel_ComboBox_DockPositioning.SelectedIndexChanged +=
                (s, e) => ApplySelectedPosition();

            // Update form opacity when the trackbar value changes
            NitroDockMain_Configuration_OpacityPanel_TrackBar_FormOpacity.ValueChanged +=
                (s, e) => ApplyFormOpacity();

            // Update dock offset when the trackbar value changes
            NitroDockMain_Configuration_OpacityPanel_TrackBar_OpacityPanel_DockOffset.ValueChanged +=
                (s, e) => ApplyDockOffset();
        }

        private void ApplySelectedPosition()
        {
            if (Enum.TryParse(
                NitroDockMain_Configuration_OpacityPanel_ComboBox_DockPositioning.SelectedItem.ToString(),
                out NitroDockMain.DockPosition position
            ))
            {
                _mainForm.currentDockPosition = position;
                _mainForm.SnapToEdge(position);
            }
        }

        private void ApplyFormOpacity()
        {
            float opacity = NitroDockMain_Configuration_OpacityPanel_TrackBar_FormOpacity.Value / 100f;
            _mainForm.Opacity = opacity;
        }

        private void ApplyDockOffset()
        {
            int offset = NitroDockMain_Configuration_OpacityPanel_TrackBar_OpacityPanel_DockOffset.Value;
            _mainForm.DockOffset = offset;
            _mainForm.SnapToEdge(_mainForm.currentDockPosition);
        }
    }
}
