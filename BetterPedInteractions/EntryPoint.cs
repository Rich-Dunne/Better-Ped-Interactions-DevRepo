using Rage;
using System;
using System.Linq;
using System.Reflection;
using BetterPedInteractions.Utils;
using System.IO;

[assembly: Rage.Attributes.Plugin("Better Ped Interactions", Author = "Rich", Description = "Custom dialogue menus for better ped interactions, among other features to enhance interaction experiences.", PrefersSingleInstance = true)]

namespace BetterPedInteractions
{
    [Obfuscation(Exclude = false, Feature = "-rename", ApplyToMembers = true)]
    internal class EntryPoint
    {
        private static readonly string DEFAULT_DIRECTORY = Directory.GetCurrentDirectory() + "\\plugins\\BetterPedInteractions\\Default";
        private static readonly string CUSTOM_DIRECTORY = Directory.GetCurrentDirectory() + "\\plugins\\BetterPedInteractions\\Custom";

        [Obfuscation(Exclude = false, Feature = "-rename")]
        public static void Main()
        {
            AppDomain.CurrentDomain.DomainUnload += MyTerminationHandler;
            Settings.LoadSettings();
            MenuManager.InitializeMenus();
            VocalInterface.Initialize();
            XMLReader.ReadFromDirectory(DEFAULT_DIRECTORY);
            XMLReader.ReadFromDirectory(CUSTOM_DIRECTORY);
            MenuManager.PopulateCategoryScrollers();
            MenuManager.InitialMenuPopulation();
            GetAssemblyVersion();
            GameFiber.StartNew(() => UserInput.HandleUserInput(), "Handle User Input");

            void GetAssemblyVersion()
            {
                string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                Game.LogTrivial($"V{version} is ready.");
            }
        }

        private static void MyTerminationHandler(object sender, EventArgs e)
        {
            VocalInterface.EndRecognition();
            PedHandler.ClearAllPeds();
        }
    }
}
