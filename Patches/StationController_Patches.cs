using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVProductionChains.Patches
{
    public class StationController_Patches
    {
        public static void Setup(Harmony harmony)
        {
            DVProductionChains.Log("Setting up StationController patches.");

            var StationController_Start = AccessTools.Method(typeof(StationController), "Start");
            var StationController_Start_Postfix = AccessTools.Method(typeof(StationController_Patches), "Start_Postfix");
            harmony.Patch(StationController_Start, postfix: new HarmonyMethod(StationController_Start_Postfix));
        }

        public static void Start_Postfix(StationController __instance)
        {
            __instance.gameObject.AddComponent<WarehouseController>();
        }
    }
}
