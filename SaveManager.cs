using DVOwnership;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DVProductionChains
{
    internal class SaveManager
    {
        private static readonly string PRIMARY_SAVE_KEY = "DVProductionChains";
        private static readonly string VERSION_SAVE_KEY = "Version";
        private static readonly string WAREHOUSES_SAVE_KEY = "Warehouses";

        [HarmonyPatch(typeof(SaveGameManager), "Save")]
        class SaveGameManager_Save_Patch
        {
            static void Prefix(SaveGameManager __instance)
            {
                JObject saveData = new JObject(
                    new JProperty(VERSION_SAVE_KEY, DVProductionChains.Version.ToString()),
                    new JProperty(WAREHOUSES_SAVE_KEY, WarehouseController.GetSaveData())
                );

                SaveGameManager.data.SetJObject(PRIMARY_SAVE_KEY, saveData);
            }
        }

        [HarmonyPatch(typeof(SaveGameManager), "Load")]
        class SaveGameManager_Load_Patch
        {
            static void Postfix(SaveGameManager __instance)
            {
                DVProductionChains.Log("Loading production chain save data");
                JObject saveData = SaveGameManager.data.GetJObject(PRIMARY_SAVE_KEY);
                if (saveData == null)
                {
                    DVProductionChains.Log("Not loading save data: primary object is null.");

                    return;
                }

                var warehousesSaveData = saveData[WAREHOUSES_SAVE_KEY];
                if (warehousesSaveData == null) { DVProductionChains.Log("Not loading warehouses data; data is null"); }
                else
                {
                    WarehouseController.LoadSaveData(warehousesSaveData as JArray);
                }
            }
        }
    }
}
