using DV.Logic.Job;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVProductionChains.Patches
{
    public class WarehouseMachine_Patches
    {
        public static void Setup(Harmony harmony)
        {
            DVProductionChains.Log("Setting up WarehouseMachine patches.");
            var WarehouseMachine_UnloadOneCarOfTask = AccessTools.Method(typeof(WarehouseMachine), "UnloadOneCarOfTask");
            var WarehouseMachine_UnloadOneCarOfTask_Postfix = AccessTools.Method(typeof(WarehouseMachine_Patches), "UnloadOneCarOfTask_Postfix");
            harmony.Patch(WarehouseMachine_UnloadOneCarOfTask, postfix: new HarmonyMethod(WarehouseMachine_UnloadOneCarOfTask_Postfix));
            var WarehouseMachine_LoadOneCarOfTask = AccessTools.Method(typeof(WarehouseMachine), "LoadOneCarOfTask");
            var WarehouseMachine_LoadOneCarOfTask_Postfix = AccessTools.Method(typeof(WarehouseMachine_Patches), "LoadOneCarOfTask_Postfix");
            harmony.Patch(WarehouseMachine_LoadOneCarOfTask, postfix: new HarmonyMethod(WarehouseMachine_LoadOneCarOfTask_Postfix));
        }

        public static void UnloadOneCarOfTask_Postfix(WarehouseTask task, Car __result)
        {
            DVProductionChains.Log("Unloading Task");
            if (__result == null)
            {
                return;
            }

            string yardID = task.Job.chainData.chainDestinationYardId;
            DVProductionChains.Log($"Unloading in {yardID}");
            StationController stationController = DVProductionChains.stationControllers.First(s => s.stationInfo.YardID == yardID);
            if (stationController != null)
            {
                WarehouseController warehouseController = stationController.GetComponent<WarehouseController>();
                warehouseController.AddToInputStorage(task);
            }
        }

        public static void LoadOneCarOfTask_Postfix(WarehouseTask task, Car __result)
        {
            DVProductionChains.Log("Loading task");
            if (__result == null)
            {
                DVProductionChains.Log("No car got loaded? This shouldn't happen");
                return;
            }

            string yardID = task.Job.chainData.chainOriginYardId;
            StationController stationController = DVProductionChains.stationControllers.First(s => s.stationInfo.YardID == yardID);
            if (stationController != null)
            {
                WarehouseController warehouseController = stationController.GetComponent<WarehouseController>();
                warehouseController.TakeFromOutputStorage(task);
            }
            else
            {
                DVProductionChains.Log("No station controller found");
            }
        }
    }
}
