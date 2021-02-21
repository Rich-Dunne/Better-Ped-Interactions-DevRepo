using BetterPedInteractions.Utils;
using Rage;
using RAGENativeUI.Elements;
using System;
using System.Drawing;
using System.Linq;
using System.Xml.Linq;

namespace BetterPedInteractions.Objects
{
    internal class CollectedPed : Ped
    {
        internal Blip Blip { get; private set; }
        internal new Settings.Group Group { get; set; }
        internal Settings.ResponseHonesty ResponseHonesty { get; set; } = Settings.ResponseHonesty.Unspecified;
        internal DialogueMenu Menu { get; private set; }
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

        internal CollectedPed(Ped ped, Settings.Group group)
        {
            Handle = ped.Handle;
            Group = group;
            BlockPermanentEvents = true;
            IsPersistent = true;
            AssignGender();

            CreateMenu();
            CreateBlip();

            if (Settings.EnableAgitation)
            {
                AdjustStartingAgitation();
            }

            if (!CurrentVehicle)
            {
                FacePlayer();
            }
            else if (CurrentVehicle && CurrentVehicle.Driver == this)
            {
                Tasks.PerformDrivingManeuver(VehicleManeuver.Wait);
            }
            
            PedHandler.FocusedPed = this;
            PedHandler.CollectedPeds.Add(this);
            Game.LogTrivial($"{Model.Name} collected.  CollectedPeds count: {PedHandler.CollectedPeds.Count()}");

            GameFiber.StartNew(() => LoopForValidity(), "CollectedPed Validity Loop Fiber");
        }

        private void CreateMenu() => Menu = MenuManager.InitializeMenu(Group);

        private void CreateBlip()
        {
            Blip = new Blip(this);
            Blip.Sprite = (BlipSprite)480;
            if (Group == Settings.Group.Civilian)
            {
                Blip.Color = Color.Gold;
            }
            else
            {
                Blip.Color = Color.CadetBlue;
            }

            Blip.Scale = 0.75f;
            PedHandler.CollectedPedBlips.Add(Blip);
        }

        private void AdjustStartingAgitation()
        {
            Game.LogTrivial($"Starting agitation: {Agitation}");
        }

        internal void AdjustAdgitationFromPrompt(MenuItem prompt)
        {
            if (prompt.IsMenuItemElementDefined("PromptType"))
            {
                if (prompt.Element.Element("PromptType").Value.ToLower() == "interview")
                {
                    DecreaseAgitation();
                }
                else if (prompt.Element.Element("PromptType").Value.ToLower() == "interrogation")
                {
                    IncreaseAgitation();
                }
            }
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
                    Game.DisplayNotification($"~o~[Better Ped Interactions]\n~w~The ped is refusing to speak to you");
                }
            }
            if (Agitation >= Settings.NervousThreshold && !PlayingNervousAnimation && !FleeingOrAttacking)
            {
                Game.DisplayNotification($"~o~[Better Ped Interactions]\n~w~The ped appears uncomfortable");
                PlayNervousAnimation();
            }

            void ChancePedAttacks()
            {
                Game.LogTrivial($"Ped should be attacking");
                Following = false;
                PlayingNervousAnimation = false;
                Tasks.Clear();
                Tasks.FightAgainst(Game.LocalPlayer.Character, -1);
            }

            void ChancePedFlees()
            {
                Game.LogTrivial($"Ped should be fleeing");
                Following = false;
                PlayingNervousAnimation = false;
                Tasks.Clear();
                Tasks.Flee(Game.LocalPlayer.Character, 20, -1);
                GameFiber.StartNew(() =>
                {
                    while (this && IsAlive)
                    {
                        if (IsStill)
                        {
                            Tasks.Wander();
                        }
                        if (Game.LocalPlayer.Character.DistanceTo2D(this) < 20)
                        {
                            Tasks.Flee(Game.LocalPlayer.Character, 30, -1);
                        }
                        if (Health < MaxHealth/2)
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
                    Tasks.PlayAnimation("timetable@gardener@smoking_joint", "idle_cough", 1f, AnimationFlags.None).WaitForCompletion();
                }

                void SmokeAnimation()
                {
                    //Game.LogTrivial($"Playing smoke animation");
                    // Add cigarette to hand
                    Tasks.PlayAnimation("amb@world_human_aa_smoke@male@base", "base", 1f, AnimationFlags.None).WaitForCompletion();
                    Tasks.PlayAnimation("amb@world_human_aa_smoke@male@idle_a", idleAnimations[MathHelper.GetRandomInteger(3)], 1f, AnimationFlags.None).WaitForCompletion();
                }

                void ImpatientAnimation()
                {
                    //Game.LogTrivial($"Playing impatient animation");
                    Tasks.PlayAnimation("amb@world_human_stand_impatient@male@no_sign@enter", "enter", 1f, AnimationFlags.None).WaitForCompletion();
                    Tasks.PlayAnimation("amb@world_human_stand_impatient@male@no_sign@idle_a", idleAnimations[MathHelper.GetRandomInteger(3)], 1f, AnimationFlags.None).WaitForCompletion();
                    Tasks.PlayAnimation("amb@world_human_stand_impatient@male@no_sign@exit", "exit", 1f, AnimationFlags.None).WaitForCompletion();
                }

                void NervousAnimation()
                {
                    Game.LogTrivial($"Playing nervous animation");
                    Tasks.PlayAnimation("mp_missheist_countrybank@nervous", "nervous_idle", 1f, AnimationFlags.Loop);
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
                        if(Tasks.CurrentTaskStatus != TaskStatus.InProgress || Tasks.CurrentTaskStatus != TaskStatus.Preparing)
                        {
                            if (Dismissed)
                            {
                                return;
                            }
                            functions[MathHelper.GetRandomInteger(4)]();
                        }
                        GameFiber.Sleep(10000);
                    }
                    Tasks.Clear();
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
                Game.DisplayNotification($"~o~[Better Ped Interactions]\n~w~Ped agitation: {color}{Agitation} ({symbol}{difference})");
            }
        }

        internal void PerformAction(MenuItem prompt)
        {
            Game.LogTrivial($"Action: {prompt.Action}");
            switch (prompt.Action)
            {
                case Settings.Actions.Follow:
                    var menuItem = prompt.UIMenuItem as UIMenuCheckboxItem;
                    menuItem.Checked = !menuItem.Checked;
                    if (menuItem.Checked)
                    {
                        FollowMe();
                    }
                    else
                    {
                        StopFollowing();
                    }
                    break;

                case Settings.Actions.Dismiss:
                    Dismiss();
                    break;

                case Settings.Actions.RollWindowDown:
                    RollDownWindow();
                    break;

                case Settings.Actions.TurnOffEngine:
                    TurnOffEngine();
                    break;

                case Settings.Actions.ExitVehicle:
                    ExitVehicle();
                    break;
            }
            Game.LogTrivial($"Prompt is a ped action.  We don't need a response.");
        }

        internal XElement ChooseResponse(MenuItem prompt)
        {
            XElement response = null;
            if (ResponseHonesty == Settings.ResponseHonesty.Unspecified || (ResponseHonesty != Settings.ResponseHonesty.Unspecified && GetResponseChance() == 3))
            {
                Game.LogTrivial($"First, deviated, or unspecified honesty response");
                response = prompt.Responses[GetRandomResponseValue()];
            }
            // If this is not the ped's first response, choose a response that matches their initial response's type
            else if (ResponseHonesty != Settings.ResponseHonesty.Unspecified && GetResponseChance() < 3)
            {
                Game.LogTrivial($"Follow-up response");
                // Response is null when the ped's ResponseHonesty is defined, but there are no responses without a honesty attribute
                response = prompt.Responses.FirstOrDefault(x => x.Attribute("honesty")?.Value.ToLower() == ResponseHonesty.ToString().ToLower());
                if (response == null)
                {
                    response = prompt.Responses[GetRandomResponseValue()];
                }
            }
            if(response == null)
            {
                Game.LogTrivial($"Response is null.");
                return response;
            }

            return response;

            int GetResponseChance() => new Random().Next(0, 4);

            int GetRandomResponseValue() => new Random().Next(prompt.Responses.Count);
        }

        internal void SetHonesty(XElement response)
        {
            var honestyAttribute = response.Attribute("honesty")?.Value;
            if(honestyAttribute == null)
            {
                return;
            }
            Game.LogTrivial($"Setting ped honesty.");
            //Game.LogTrivial($"Response type: {honestyAttribute}");
            honestyAttribute = char.ToUpper(honestyAttribute[0]) + honestyAttribute.Substring(1);
            //Game.LogTrivial($"Response type after char.ToUpper: {honestyAttribute}");
            var parsed = Enum.TryParse(honestyAttribute, out Settings.ResponseHonesty responseHonesty);
            //Game.LogTrivial($"Response type parsed: {parsed}, Response type: {responseHonesty}");
            if (parsed)
            {
                ResponseHonesty = responseHonesty;
            }
        }

        internal void PlayLipAnimation(XElement response)
        {
            Tasks.PlayAnimation("mp_facial", "mic_chatter", 1.0f, AnimationFlags.None);
            GameFiber.StartNew(() =>
            {
                var numberOfWords = response.Value.Split();
                //Game.LogTrivial($"Response: {response.Value}, number of words: {numberOfWords.Length}");
                var timer = 0;
                GameFiber.Sleep(500);

                while (timer < (numberOfWords.Length * 10))
                {
                    timer += 1;
                    GameFiber.Yield();
                }

                //Game.DisplayHelp("Stop animation");
                if (this != null && IsValid() && IsAlive)
                {
                    Tasks.Clear();
                }
            }, "Lip Animation Fiber");
        }

        internal void FacePlayer()
        {
            if(IsOnFoot && !Following && !FleeingOrAttacking) // && ped is standing
            {
                Rage.Native.NativeFunction.Natives.TASK_TURN_PED_TO_FACE_ENTITY(this, Game.LocalPlayer.Character, -1);
                Game.LogTrivial($"Ped should be facing player.");
            }
        }

        internal void RollDownWindow()
        {
            // Add condition to roll down passenger window if player is near it and there is no passenger
            Rage.Native.NativeFunction.Natives.ROLL_DOWN_WINDOW(CurrentVehicle, SeatIndex + 1);
        }

        internal void RollUpWindow()
        {
            Rage.Native.NativeFunction.Natives.ROLL_UP_WINDOW(CurrentVehicle, SeatIndex + 1);
        }

        internal void ExitVehicle()
        {
            Tasks.LeaveVehicle(LeaveVehicleFlags.None).WaitForCompletion();
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
            Tasks.FollowToOffsetFromEntity(Game.LocalPlayer.Character, 1.5f * Vector3.WorldSouth);
            Following = true;
            Game.LogTrivial($"{Model.Name} following player.");
        }

        internal void StopFollowing()
        {
            Tasks.Clear();
            Following = false;
            Game.LogTrivial($"{Model.Name} following player.");
        }

        internal void TurnOffEngine()
        {
            CurrentVehicle.IsEngineOn = false;
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
            if (IsMale)
            {
                Gender = "male";
            }
            else
            {
                Gender = "female";
            }
        }

        public new void Dismiss()
        {
            Dismissed = true;
            DeleteBlip();
            if (this)
            {
                if(CurrentVehicle && CurrentVehicle.IsCar)
                {
                    RollUpWindow();
                    if(CurrentVehicle.Driver == this)
                    {
                        Tasks.Clear();
                        base.Dismiss();
                    }
                }
                if (IsOnFoot)
                {
                    Tasks.Clear();
                    Game.LogTrivial($"{Model.Name} dismissed.");
                    IsPersistent = false;
                }
                BlockPermanentEvents = false;
            }
            Menu.Close();
            PedHandler.CollectedPeds.Remove(this);
            PedHandler.FocusedPed = null;

            void DeleteBlip()
            {
                if (Blip)
                {
                    Game.LogTrivial($"Deleting ped's blip.");
                    Blip.Delete();
                }
            }
        }

        private void LoopForValidity()
        {
            while (IsValid() && IsAlive && Game.LocalPlayer.Character && Game.LocalPlayer.Character.IsAlive)
            {
                GameFiber.Sleep(1000);
            }

            if (IsValid() && !Dismissed)
            {
                Dismiss();
                return;
            }
            else if (PedHandler.CollectedPeds.Contains(this))
            {
                PedHandler.CollectedPeds.Remove(this);
                Game.LogTrivial($"An invalid ped has been removed from the collection.");
                return;
            }
        }
    }
}
