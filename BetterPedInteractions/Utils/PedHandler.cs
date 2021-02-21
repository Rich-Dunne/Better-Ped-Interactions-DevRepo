using Rage;
using System.Collections.Generic;
using System.Linq;
using BetterPedInteractions.Objects;

namespace BetterPedInteractions.Utils
{
    class PedHandler
    {
        internal static List<CollectedPed> CollectedPeds { get; private set; } = new List<CollectedPed>();
        internal static List<Blip> CollectedPedBlips { get; private set; } = new List<Blip>();
        internal static CollectedPed FocusedPed { get; set; } = null;

        internal static Ped NearbyPed { get => GetNearbyPed(); }

        private static Ped GetNearbyPed()
        {
            return Game.LocalPlayer.Character.GetNearbyPeds(16).Where(p => p && p != Game.LocalPlayer.Character && p.IsAlive && p.DistanceTo2D(Game.LocalPlayer.Character) <= Settings.InteractDistance).OrderBy(p => p.DistanceTo2D(Game.LocalPlayer.Character)).FirstOrDefault();
        }

        internal static void CollectOrFocusNearbyPed(Ped nearbyPed)
        {
            if (!nearbyPed)
            {
                Game.LogTrivial($"Nearby ped is null.");
                return;
            }
            Game.LogTrivial($"Nearby ped: {nearbyPed.Model.Name}");

            var collectedPed = CollectedPeds.FirstOrDefault(x => x == nearbyPed);
            if (collectedPed == null && (nearbyPed.RelationshipGroup == RelationshipGroup.Cop || nearbyPed.RelationshipGroup == "UBCOP" || nearbyPed.Model.Name == "MP_M_FREEMODE_01" || nearbyPed.Model.Name.Contains("COP")))
            {
                Game.LogTrivial($"CollectedPed is null, collecting nearby COP and assigning as focusedPed.");
                FocusedPed = CollectPed(nearbyPed, Settings.Group.Cop);
            }
            else if (collectedPed == null)
            {
                Game.LogTrivial($"CollectedPed is null, collecting nearby CIV and assigning as focusedPed.");
                FocusedPed = CollectPed(nearbyPed, Settings.Group.Civilian);
            }
            else
            {
                Game.LogTrivial($"CollectedPed was found in our collection.");
                FocusedPed = collectedPed;
            }
        }

        private static CollectedPed CollectPed(Ped ped, Settings.Group group) => new CollectedPed(ped, group);

        internal static void ClearAllPeds()
        {
            CollectedPedBlips.ForEach(x => { if (x) x.Delete(); });
            CollectedPeds.ForEach(x => x.Dismiss());
        }
    }
}
