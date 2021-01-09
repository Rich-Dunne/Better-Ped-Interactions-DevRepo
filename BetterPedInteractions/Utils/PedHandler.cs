using Rage;
using System.Collections.Generic;
using System.Linq;
using BetterPedInteractions.Objects;

namespace BetterPedInteractions.Utils
{
    class PedHandler
    {
        internal static List<CollectedPed> CollectedPeds = new List<CollectedPed>();
        internal static CollectedPed FocusedPed = null;
        internal static Ped NearbyPed { get => GetNearbyPed(); }

        internal static Ped GetNearbyPed()
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

            var collectedPed = CollectedPeds.FirstOrDefault(cp => cp.Ped == nearbyPed);
            if (collectedPed == null && (nearbyPed.RelationshipGroup == RelationshipGroup.Cop || nearbyPed.RelationshipGroup == "UBCOP" || nearbyPed.Model.Name == "MP_M_FREEMODE_01" || nearbyPed.Model.Name.Contains("COP")))
            {
                Game.LogTrivial($"CollectedPed is null, collecting nearby COP and assigning as focusedPed.");
                //nearbyPed = new CollectedPed(); Preparing to use inheritence for CollectedPed
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

            MakeCollectedPedFacePlayer();

            CollectedPed CollectPed(Ped p, Settings.Group group)
            {
                var newCollectedPed = new CollectedPed(p, group);
                CollectedPeds.Add(newCollectedPed);
                Game.LogTrivial($"{p.Model.Name} collected.");

                FocusedPed = newCollectedPed;
                return newCollectedPed;
            }

            void MakeCollectedPedFacePlayer()
            {
                if (FocusedPed.Ped.IsOnFoot && !FocusedPed.Following && !FocusedPed.FleeingOrAttacking)
                {
                    FocusedPed.FacePlayer();
                    Game.LogTrivial($"Ped should be facing player.");
                }
            }
        }

        internal static void ClearAllPeds()
        {
            CollectedPeds.ForEach(x => x.Dismiss());
        }
    }
}
