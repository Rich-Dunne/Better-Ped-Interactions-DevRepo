using Rage;
using System.Windows.Forms;


namespace PedInterview
{
    internal static class Settings
    {
        internal static Keys ToggleKey = Keys.E;
        internal static Keys ModifierKey = Keys.LShiftKey;
        internal static ControllerButtons ToggleButton = ControllerButtons.Y;
        internal static ControllerButtons ModifierButton = ControllerButtons.A;
        internal static float InteractDistance = 1.5f;

        internal static void LoadSettings()
        {
            Game.LogTrivial("Loading PedInterview.ini settings");
            InitializationFile ini = new InitializationFile("Plugins/PedInterview.ini");
            ini.Create();
            ToggleKey = ini.ReadEnum("Keybindings", "ToggleKey", Keys.E);
            ModifierKey = ini.ReadEnum("Keybindings", "ModifierKey", Keys.LShiftKey);
            ToggleButton = ini.ReadEnum("Keybindings", "ToggleButton", ControllerButtons.LeftShoulder);
            ModifierButton = ini.ReadEnum("Keybindings", "ModifierButton", ControllerButtons.DPadDown);
            InteractDistance = (float)ini.ReadDouble("Other Settings", "InteractDistance", 2f);
        }
    }
}
