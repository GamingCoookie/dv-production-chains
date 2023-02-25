using DV.ServicePenalty.UI;
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
        public static Version Version => mod.Version;
        public static List<StationController> stationControllers = new List<StationController>();

        static bool Load(ModEntry modEntry)
        {
            mod = modEntry;
            harmony = new Harmony(mod.Info.Id);
            harmony.PatchAll();
            StationController_Patches.Setup(harmony);
            WarehouseMachine_Patches.Setup(harmony);
            ModEntry ownershipModEntry = FindMod("DVOwnership");
            if (ownershipModEntry != null && ownershipModEntry.Enabled)
                Ownership_Patches.Setup(harmony);

            return true;
        }

        public static void Log(object message)
        {
            mod.Logger.NativeLog(message as string);
        }
    }
}
