using Rage;
using System.Windows.Forms;


namespace BetterPedInteractions
{
    internal static class Settings
    {
        internal enum Group
        {
            Civilian = 0,
            Cop = 1
        }

        internal enum ResponseType
        {
            Unspecified = 0,
            Interview = 1,
            Interrogation = 2
        }

        internal static Keys ToggleKey = Keys.E;
        internal static Keys ModifierKey = Keys.LShiftKey;
        internal static ControllerButtons ToggleButton = ControllerButtons.Y;
        internal static ControllerButtons ModifierButton = ControllerButtons.A;
        internal static float InteractDistance = 1.5f;
        internal static bool EnableAgitation = true;
        internal static int IncreaseAgitationAmount = 5, DecreaseAgitationAmount = 2, RepeatedAgitationAmount = 1, NervousThreshold = 40, StopRespondingThreshold = 60,
             FleeAttackThreshold = 80;

        internal static void LoadSettings()
        {
            Game.LogTrivial("Loading BetterPedInteractions.ini settings");
            InitializationFile ini = new InitializationFile("Plugins/BetterPedInteractions.ini");
            ini.Create();
            ToggleKey = ini.ReadEnum("Keybindings", "ToggleKey", Keys.E);
            ModifierKey = ini.ReadEnum("Keybindings", "ModifierKey", Keys.LShiftKey);
            ToggleButton = ini.ReadEnum("Keybindings", "ToggleButton", ControllerButtons.LeftShoulder);
            ModifierButton = ini.ReadEnum("Keybindings", "ModifierButton", ControllerButtons.DPadDown);
            InteractDistance = (float)ini.ReadDouble("Other Settings", "InteractDistance", 2f);

            EnableAgitation = ini.ReadBoolean("Agitation Settings", "EnableAgitation", true);
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
