using Rage;
using System;
using System.Collections.Generic;
using System.Drawing;

namespace PedInterview
{
    internal class CollectedPed
    {
        internal Ped Ped { get; private set; }
        internal Blip Blip { get; private set; }
        internal string Gender { get; private set; }
        internal bool Following { get; private set; } = false;
        internal bool FleeingOrAttacking { get; private set; } = false;
        private bool Dismissed { get; set; } = false;
        private int _agitation = new Random().Next(0, 101); // Can adjust Agitation based on has weapons, if ped is pulled over/arrested, etc
        internal int Agitation
        {
            get => _agitation;
            set
            {
                var oldAgitation = _agitation;
                int difference;
                if(value - _agitation == Settings.IncreaseAgitationAmount || value - _agitation == Settings.RepeatedAgitationAmount)
                {
                    if(value >= 100)
                    {
                        _agitation = 100;
                    }
                    else
                    {
                        _agitation = value;
                    }

                    //Game.LogTrivial($"Agitation increased from {oldAgitation} to {_agitation}");
                    difference = Math.Abs(oldAgitation - value);
                    OnAgitationChanged(difference, AgitationChange.Increased);
                    return;
                }
                if(_agitation - value == Settings.DecreaseAgitationAmount)
                {
                    if (value <= 0)
                    {
                        _agitation = 0;
                    }
                    else
                    {
                        _agitation = value;
                    }

                    //Game.LogTrivial($"Agitation decreased from {oldAgitation} to {_agitation}");
                    difference = Math.Abs(oldAgitation - value);
                    OnAgitationChanged(difference, AgitationChange.Decreased);
                    return;
                }

                difference = Math.Abs(oldAgitation - value);
                _agitation = value;
                if (_agitation > oldAgitation)
                {
                    OnAgitationChanged(difference, AgitationChange.Increased);
                }
                else if (_agitation < oldAgitation)
                {
                    OnAgitationChanged(difference, AgitationChange.Decreased);
                }
                else if (_agitation == oldAgitation)
                {
                    OnAgitationChanged(difference, AgitationChange.None);
                }
                Game.LogTrivial($"Agitation changed from {oldAgitation} to {_agitation}");
            }
        }
        private enum AgitationChange
        {
            Decreased = 0,
            Increased = 1,
            None = 2
        }
        private bool PlayingNervousAnimation { get; set; } = false;
        internal bool StoppedTalking { get; private set; } = false;

        internal CollectedPed(Ped p)
        {
            Ped = p;
            Ped.BlockPermanentEvents = true;
            Ped.IsPersistent = true;
            CreateBlip();
            if (Settings.EnableAgitation)
            {
                AdjustStartingAgitation();
            }
            AssignGender();

            if (Ped.IsOnFoot)
            {
                FacePlayer();
            }
            else if (Ped.CurrentVehicle && Ped.CurrentVehicle.Driver == Ped)
            {
                Ped.Tasks.PerformDrivingManeuver(VehicleManeuver.Wait);
            }

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

        private void AdjustStartingAgitation()
        {
            Game.LogTrivial($"Starting agitation: {Agitation}");

            //Game.LogTrivial($"Adjusted agitation: {Agitation}");

            //void SetLowerAgitation(int value)
            //{
            //    Game.LogTrivial($"Adjusting for lower agitation");
            //    if (Agitation - value <= 0)
            //    {
            //        Agitation = 0;
            //    }
            //    else
            //    {
            //        //Game.LogTrivial($"Agitation: {Agitation}");
            //        //Game.LogTrivial($"Value: {value}");
            //        Agitation -= value;
            //        //Game.LogTrivial($"New Agitation: {Agitation}");
            //    }
            //    return;
            //}

            //void SetHigherAgitation(int value)
            //{
            //    Game.LogTrivial($"Adjusting for higher agitation");
            //    if (Agitation + value >= 100)
            //    {
            //        Agitation = 100;
            //    }
            //    else
            //    {
            //        //Game.LogTrivial($"Agitation: {Agitation}");
            //        //Game.LogTrivial($"Value: {value}");
            //        Agitation += value;
            //        //Game.LogTrivial($"New Agitation: {Agitation}");
            //    }
            //    return;
            //}
        }

        private void OnAgitationChanged(int difference, AgitationChange agitationChange)
        {
            DisplayNotification();
            if (Agitation >= Settings.FleeAttackThreshold && !FleeingOrAttacking && MathHelper.GetRandomInteger(11) >= 8)
            {
                FleeingOrAttacking = true;
                if (MathHelper.GetChance(3))
                {
                    ChancePedAttacks();
                }
                else
                {
                    ChancePedFlees();
                }
                return;
            }
            if (Agitation >= Settings.StopRespondingThreshold)
            {
                if (!StoppedTalking && MathHelper.GetChance(3))
                {
                    ChancePedStopsResponding();
                }
                else if (StoppedTalking)
                {
                    Game.DisplayNotification($"~o~[Am I Being Detained?]\n~w~The ped is refusing to speak to you");
                }
            }
            if (Agitation >= Settings.NervousThreshold && !PlayingNervousAnimation && !FleeingOrAttacking)
            {
                Game.DisplayNotification($"~o~[Am I Being Detained?]\n~w~The ped appears uncomfortable");
                PlayNervousAnimation();
            }

            void ChancePedAttacks()
            {
                Game.LogTrivial($"Ped should be attacking");
                Following = false;
                PlayingNervousAnimation = false;
                Ped.Tasks.Clear();
                Ped.Tasks.FightAgainst(Game.LocalPlayer.Character, -1);
            }

            void ChancePedFlees()
            {
                Game.LogTrivial($"Ped should be fleeing");
                Following = false;
                PlayingNervousAnimation = false;
                Ped.Tasks.Clear();
                Ped.Tasks.Flee(Game.LocalPlayer.Character, 20, -1);
                GameFiber.StartNew(() =>
                {
                    while (Ped && Ped.IsAlive)
                    {
                        if (Ped.IsStill)
                        {
                            Ped.Tasks.Wander();
                        }
                        if (Game.LocalPlayer.Character.DistanceTo2D(Ped) < 20)
                        {
                            Ped.Tasks.Flee(Game.LocalPlayer.Character, 30, -1);
                        }
                        if (Ped.Health < Ped.MaxHealth/2)
                        {
                            Game.LogTrivial($"Setting fleeing/attacking to false because ped is too hurt");
                            FleeingOrAttacking = false;
                        }
                        GameFiber.Sleep(1000);
                    }
                    Game.LogTrivial($"Setting fleeing/attacking to false outside of loop");
                    FleeingOrAttacking = false;

                }, "Ped Fleeing Fiber");
            }

            void ChancePedStopsResponding()
            {
                StoppedTalking = true;
                Game.LogTrivial($"Ped is refusing to talk anymore");
                Game.DisplaySubtitle($"~y~Unidentified {Gender}: ~w~I'm not saying another word until my lawyer is present."); // Collection of responses
            }

            void PlayNervousAnimation()
            {
                PlayingNervousAnimation = true;
                var idleAnimations = new string[] { "idle_a", "idle_b", "idle_c" };

                void CoughAnimation()
                {
                    //Game.LogTrivial($"Playing cough animation");
                    Ped.Tasks.PlayAnimation("timetable@gardener@smoking_joint", "idle_cough", 1f, AnimationFlags.None).WaitForCompletion();
                }

                void SmokeAnimation()
                {
                    //Game.LogTrivial($"Playing smoke animation");
                    // Add cigarette to hand
                    Ped.Tasks.PlayAnimation("amb@world_human_aa_smoke@male@base", "base", 1f, AnimationFlags.None).WaitForCompletion();
                    Ped.Tasks.PlayAnimation("amb@world_human_aa_smoke@male@idle_a", idleAnimations[MathHelper.GetRandomInteger(3)], 1f, AnimationFlags.None).WaitForCompletion();
                }

                void ImpatientAnimation()
                {
                    //Game.LogTrivial($"Playing impatient animation");
                    Ped.Tasks.PlayAnimation("amb@world_human_stand_impatient@male@no_sign@enter", "enter", 1f, AnimationFlags.None).WaitForCompletion();
                    Ped.Tasks.PlayAnimation("amb@world_human_stand_impatient@male@no_sign@idle_a", idleAnimations[MathHelper.GetRandomInteger(3)], 1f, AnimationFlags.None).WaitForCompletion();
                    Ped.Tasks.PlayAnimation("amb@world_human_stand_impatient@male@no_sign@exit", "exit", 1f, AnimationFlags.None).WaitForCompletion();
                }

                void NervousAnimation()
                {
                    Game.LogTrivial($"Playing nervous animation");
                    Ped.Tasks.PlayAnimation("mp_missheist_countrybank@nervous", "nervous_idle", 1f, AnimationFlags.Loop);
                }

                Action[] functions = new Action[] {CoughAnimation, SmokeAnimation, ImpatientAnimation, NervousAnimation };

                GameFiber.StartNew(() =>
                { 
                    while (Agitation > Settings.NervousThreshold)
                    {
                        //Game.LogTrivial($"In animation loop");
                        if (FleeingOrAttacking || Dismissed)
                        {
                            return;
                        }
                        if(Ped.Tasks.CurrentTaskStatus != TaskStatus.InProgress || Ped.Tasks.CurrentTaskStatus != TaskStatus.Preparing)
                        {
                            functions[MathHelper.GetRandomInteger(4)]();
                        }
                        GameFiber.Sleep(10000);
                    }
                    Ped.Tasks.Clear();
                    PlayingNervousAnimation = false;
                }, "NervousAnimation Loop Fiber");
            }

            void DisplayNotification()
            {
                string change = agitationChange.ToString();
                string color;
                string symbol;
                if (agitationChange == AgitationChange.Increased)
                {
                    symbol = "+";
                    color = "~r~";
                }
                else if (agitationChange == AgitationChange.Decreased)
                {
                    symbol = "-";
                    color = "~g~";
                }
                else
                {
                    symbol = "";
                    color = "~y~";
                }
                Game.DisplayNotification($"~o~[Am I Being Detained?]\n~w~Ped agitation: {color}{Agitation} ({symbol}{difference})");
            }
        }

        internal void FacePlayer()
        {
            Rage.Native.NativeFunction.Natives.TASK_TURN_PED_TO_FACE_ENTITY(Ped, Game.LocalPlayer.Character, -1);
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

        internal void IncreaseAgitation(bool repeatedQuestion = false)
        {
            if (repeatedQuestion)
            {
                Game.LogTrivial($"Increasing agitation from repeated question");
                SetMaximumLimit(Settings.IncreaseAgitationAmount);
                Agitation += Settings.RepeatedAgitationAmount;
                return;
            }

            Game.LogTrivial($"Increasing agitation from interrogation question");
            SetMaximumLimit(Settings.IncreaseAgitationAmount);
            Agitation += Settings.IncreaseAgitationAmount;

            void SetMaximumLimit(int? value)
            {
                if(Agitation + value >= 100)
                {
                    Agitation = 100;
                    return;
                }
            }
        }

        internal void DecreaseAgitation()
        {
            if (Agitation - Settings.DecreaseAgitationAmount <= 0)
            {
                Agitation = 0;
            }
            else
            {
                Agitation -= Settings.DecreaseAgitationAmount;
            }
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
            DeleteBlip();
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
            while (Ped && Ped.IsAlive && Game.LocalPlayer.Character && Game.LocalPlayer.Character.IsAlive)
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
