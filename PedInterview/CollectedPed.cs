using Rage;
using System;
using System.Drawing;

namespace PedInterview
{
    internal class CollectedPed
    {
        internal Ped Ped { get; private set; }
        internal Blip Blip { get; private set; }
        internal string Gender { get; private set; }
        internal bool Following { get; private set; } = false;
        private bool Dismissed { get; set; } = false;
        internal int Agitation { get; private set; } = new Random().Next(0,101); // Can set Agitation based on ped relationship group to player, has weapons, if ped is pulled over/arrested

        internal CollectedPed(Ped p)
        {
            Ped = p;
            Ped.BlockPermanentEvents = true;
            Ped.IsPersistent = true;
            CreateBlip();
            AssignGender();

            if (Ped.IsOnFoot)
            {
                FacePlayer();
            }
            else if (Ped.CurrentVehicle && Ped.CurrentVehicle.Driver == Ped)
            {
                Ped.Tasks.PerformDrivingManeuver(VehicleManeuver.Wait);
            }
            Game.LogTrivial($"Starting agitation: {Agitation}");

            GameFiber.StartNew(() =>
            {
                LoopForValidity();
            }, "CollectedPed Validity Loop Fiber");

            void CreateBlip()
            {
                Blip = Ped.AttachBlip();
                Blip.Sprite = (BlipSprite)480;
                Blip.Color = Color.Gold;
                Blip.Scale = 0.75f;
            }
        }

        internal void FacePlayer()
        {
            if (Ped.Heading < 180)
            {
                Ped.Tasks.AchieveHeading(Game.LocalPlayer.Character.Heading + 180);
            }
            else
            {
                Ped.Tasks.AchieveHeading(Game.LocalPlayer.Character.Heading - 180);
            }
        }

        internal void RollDownWindow()
        {
            // Add condition to roll down passenger window if player is near it and there is no passenger
            Rage.Native.NativeFunction.Natives.ROLL_DOWN_WINDOW(Ped.CurrentVehicle, Ped.SeatIndex + 1);
        }

        internal void RollUpWindow()
        {
            Rage.Native.NativeFunction.Natives.ROLL_UP_WINDOW(Ped.CurrentVehicle, Ped.SeatIndex + 1);
        }

        internal void ExitVehicle()
        {
            Ped.Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion();
            if(Agitation > 50)
            {
                //do something
            }
            else
            {
                FacePlayer();
            }
        }

        internal void FollowMe()
        {
            Ped.Tasks.FollowToOffsetFromEntity(Game.LocalPlayer.Character, 1.5f * Vector3.WorldSouth);
            Following = true;
        }

        internal void StopFollowing()
        {
            Ped.Tasks.Clear();
            Following = false;
        }

        internal void TurnOffEngine()
        {
            Ped.CurrentVehicle.IsEngineOn = false;
        }

        internal void IncreaseAgitation()
        {
            if(Agitation <= 95)
            {
                Agitation += 5;
            }
            else if (Agitation > 95)
            {
                Agitation = 100;
            }
            Game.LogTrivial($"Agitation increased: {Agitation}");
        }

        internal void DecreaseAgitation()
        {
            if(Agitation > 2)
            {
                Agitation -= 2;
            }
            else if(Agitation == 1)
            {
                Agitation = 0;
            }
            Game.LogTrivial($"Agitation decreased: {Agitation}");
        }

        private void AssignGender()
        {
            if (Ped.IsMale)
            {
                Gender = "male";
            }
            else
            {
                Gender = "female";
            }
        }

        internal void Dismiss()
        {
            Dismissed = true;
            if (Ped)
            {
                if(Ped.CurrentVehicle && Ped.CurrentVehicle.IsCar)
                {
                    RollUpWindow();
                    if(Ped.CurrentVehicle.Driver == Ped)
                    {
                        Ped.Tasks.Clear();
                        Ped.Dismiss();
                    }
                }
                if (Ped.IsOnFoot)
                {
                    Ped.Tasks.Clear();
                    Game.LogTrivial($"{Ped.Model.Name} dismissed.");
                    Ped.IsPersistent = false;
                }
                Ped.BlockPermanentEvents = false;
                DeleteBlip();

            }
            EntryPoint.collectedPeds.Remove(this);
            EntryPoint.focusedPed = null;

            void DeleteBlip()
            {
                if (Blip)
                {
                    Blip.Delete();
                }
            }
        }

        private void LoopForValidity()
        {
            while (Ped && Ped.IsAlive)
            {
                GameFiber.Sleep(1000);
            }
            if (Ped && !Dismissed)
            {
                Dismiss();
                return;
            }
            else if (EntryPoint.collectedPeds.Contains(this))
            {
                EntryPoint.collectedPeds.Remove(this);
                Game.LogTrivial($"An invalid ped has been removed from the collection.");
                return;
            }
        }
    }
}
