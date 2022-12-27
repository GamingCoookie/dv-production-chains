using DVProductionChains.Patches;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityModManagerNet;
using static RootMotion.FinalIK.VRIKCalibrator;
using static UnityModManagerNet.UnityModManager;

namespace DVProductionChains
{
    public static class DVProductionChains
    {
        public static ModEntry mod;
        private static Harmony harmony;

        static bool Load(ModEntry modEntry)
        {
            mod = modEntry;
            harmony = new Harmony(mod.Info.Id);

            StationController_Patches.Setup(harmony);

            return true;
        }

        public static void Log(object message)
        {
            mod.Logger.Log(message as string);
        }
    }
}
