using Rage;
using Rage.Attributes;
using Rage.ConsoleCommands.AutoCompleters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterPedInteractions.Utils
{
    class ConsoleCommands
    {
        [ConsoleCommand]
        internal static void Command_EnablePlayerLipAnimation([ConsoleCommandParameter(AutoCompleterType = typeof(ConsoleCommandAutoCompleterBoolean), Name = "EnablePlayerLipAnimation")] bool enabled)
        {
            GameFiber.StartNew(() =>
            {
                bool PlayerTalking = false;
                while (enabled)
                {
                    GameFiber.Sleep(new Random().Next(1000, 10000));
                    if (!PlayerTalking)
                    {
                        Game.LocalPlayer.Character.Tasks.PlayAnimation("mp_facial", "mic_chatter", 1.0f, AnimationFlags.SecondaryTask);
                        PlayerTalking = true;
                    }
                    GameFiber.Sleep(new Random().Next(1000,10000));
                    if (PlayerTalking)
                    {
                        Game.LocalPlayer.Character.Tasks.ClearSecondary();
                        PlayerTalking = false;
                    }
                    GameFiber.Yield();
                }
                Game.LocalPlayer.Character.Tasks.ClearSecondary();
            }, "Player Lip Animation Command");
        }
    }
}
