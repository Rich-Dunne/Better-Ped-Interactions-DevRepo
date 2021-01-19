using Rage;
using System.Windows.Forms;


namespace BetterPedInteractions
{
    internal static class Settings
    {
        internal enum Actions
        {
            None = 0,
            Follow = 1,
            Dismiss = 2,
            RollWindowDown = 3,
            TurnOffEngine = 4,
            ExitVehicle = 5
        }

        internal enum Group
        {
            Civilian = 0,
            Cop = 1
        }

        internal enum PromptType
        {
            Unspecified = 0,
            Interview = 1,
            Interrogation = 2
        }

        internal enum ResponseHonesty
        {
            Unspecified = 0,
            Truth = 1,
            Lie = 2
        }

        internal static Keys MenuKey = Keys.E;
        internal static Keys MenuModifierKey = Keys.LShiftKey;
        internal static ControllerButtons MenuButton = ControllerButtons.Y;
        internal static ControllerButtons MenuModifierButton = ControllerButtons.A;
        internal static Keys SpeechKey = Keys.LMenu;
        internal static Keys SpeechKeyModifier = Keys.None;
        internal static ControllerButtons SpeechButton = ControllerButtons.DPadUp;
        internal static ControllerButtons SpeechButtonModifier = ControllerButtons.None;
        internal static float InteractDistance = 1.5f;
        internal static string SpeechLanguage = "en-US";
        internal static bool EnableAgitation = false;
        internal static int IncreaseAgitationAmount = 5, DecreaseAgitationAmount = 2, RepeatedAgitationAmount = 1, NervousThreshold = 40, StopRespondingThreshold = 60,
             FleeAttackThreshold = 80;

        internal static void LoadSettings()
        {
            Game.LogTrivial("Loading BetterPedInteractions.ini settings");
            InitializationFile ini = new InitializationFile("Plugins/BetterPedInteractions.ini");
            ini.Create();
            MenuKey = ini.ReadEnum("Keybindings", "MenuKey", Keys.E);
            MenuModifierKey = ini.ReadEnum("Keybindings", "MenuModifierKey", Keys.LShiftKey);
            MenuButton = ini.ReadEnum("Keybindings", "MenuButton", ControllerButtons.LeftShoulder);
            MenuModifierButton = ini.ReadEnum("Keybindings", "MenuModifierButton", ControllerButtons.DPadDown);
            SpeechKey = ini.ReadEnum("Keybindings", "SpeechKey", Keys.LMenu);
            SpeechKeyModifier = ini.ReadEnum("KeyBindings", "SpeechKeyModifier", Keys.None);
            SpeechButton = ini.ReadEnum("Keybindings", "SpeechButton", ControllerButtons.DPadUp);
            SpeechButtonModifier = ini.ReadEnum("Keybindings", "SpeechButtonModifier", ControllerButtons.None);
            InteractDistance = (float)ini.ReadDouble("Other Settings", "InteractDistance", 2f);
            SpeechLanguage = ini.ReadString("Other Settings", "SpeechLanguage", "en-US");
            EnableAgitation = ini.ReadBoolean("Agitation Settings", "EnableAgitation", false);
            IncreaseAgitationAmount = ini.ReadInt32("Agitation Settings", "IncreaseAgitationAmount", 5);
            DecreaseAgitationAmount = ini.ReadInt32("Agitation Settings", "DecreaseAgitationAmount", 2);
            RepeatedAgitationAmount = ini.ReadInt32("Agitation Settings", "RepeatedAgitationAmount", 1);
            NervousThreshold = ini.ReadInt32("Agitation Settings", "NervousThreshold", 40);
            StopRespondingThreshold = ini.ReadInt32("Agitation Settings", "StopRespondingThreshold", 60);
            FleeAttackThreshold = ini.ReadInt32("Agitation Settings", "FleeAttackThreshold", 80);

            if (EnableAgitation)
            {
                CheckForValidThresholds();
                CheckForRandomThresholds();
            }

            int AssignRandomThreshold()
            {
                return MathHelper.GetRandomInteger(101);
            }

            void CheckForRandomThresholds()
            {
                if (NervousThreshold == -1)
                {
                    Game.LogTrivial($"NervousThreshold is random.");
                    NervousThreshold = AssignRandomThreshold();
                    NervousThreshold = MathHelper.GetRandomInteger(101);
                    Game.LogTrivial($"NervousThreshold is {NervousThreshold}.");
                }
                if (StopRespondingThreshold == -1)
                {
                    Game.LogTrivial($"StopRespondingThreshold is random.");
                    StopRespondingThreshold = AssignRandomThreshold();
                    Game.LogTrivial($"StopRespondingThreshold is {StopRespondingThreshold}.");
                }
                if (FleeAttackThreshold == -1)
                {
                    Game.LogTrivial($"FleeAttackThreshold is random.");
                    FleeAttackThreshold = AssignRandomThreshold();
                    Game.LogTrivial($"FleeAttackThreshold is {FleeAttackThreshold}.");
                }
            }

            void CheckForValidThresholds()
            {
                if(NervousThreshold > 100 || NervousThreshold < -1)
                {
                    Game.LogTrivial($"NervousThreshold is an invalid value, resetting to default value.");
                    NervousThreshold = 40;
                }
                if(StopRespondingThreshold > 100 || StopRespondingThreshold < -1)
                {
                    Game.LogTrivial($"StopRespondingThreshold is an invalid value, resetting to default value.");
                    StopRespondingThreshold = 60;
                }
                if(FleeAttackThreshold > 100 || FleeAttackThreshold < -1)
                {
                    Game.LogTrivial($"FleeAttackThreshold is an invalid value, resetting to default value.");
                    FleeAttackThreshold = 80;
                }
            }
        }
    }
}
