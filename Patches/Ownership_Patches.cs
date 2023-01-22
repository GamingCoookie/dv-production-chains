using DV.Logic.Job;
using DVOwnership;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Schema;

namespace DVProductionChains.Patches
{
    internal class Ownership_Patches
    {
        public static void Setup(Harmony harmony)
        {
            DVProductionChains.Log("Setting up DVOwnership patches.");
            var ProceduralJobsGenerators_GLCJFC = AccessTools.Method(typeof(ProceduralJobGenerators), nameof(ProceduralJobGenerators.GenerateLoadChainJobForCars));
            var ProceduralJobsGenerators_GLCJFC_Prefix = AccessTools.Method(typeof(Ownership_Patches), nameof(Ownership_Patches.GenerateLoadChainJobForCars_Patch));
            harmony.Patch(ProceduralJobsGenerators_GLCJFC, prefix: new HarmonyMethod(ProceduralJobsGenerators_GLCJFC_Prefix));
        }

        public static bool GenerateLoadChainJobForCars_Patch(ref List<List<Car>> carSetsForJob, CargoGroup cargoGroup, StationController originController)
        {
            //Make exception for source industries

            DVProductionChains.Log("GenerateLoadChainJobForCars Prefix");
            WarehouseController warehouseController = originController.GetComponent<WarehouseController>();
            List<CargoType> cargoTypes = cargoGroup.cargoTypes;
            foreach (var cargoType in cargoTypes)
            {
                DVProductionChains.Log($"CargoType = {cargoType}");
                var amount = warehouseController.GetAmountOfCargoType(cargoType);
                DVProductionChains.Log($"Amount: {amount}");
                var carSetsForCargoType = carSetsForJob.Where(set => set.Any(car => CargoTypes.CanCarContainCargoType(car.carType, cargoType))).ToList();
                carSetsForJob = carSetsForJob.Except(carSetsForCargoType).ToList();
                int totalCars = carSetsForCargoType.Sum(carSet => carSet.Count);
                DVProductionChains.Log(totalCars.ToString());
                while (totalCars > amount)
                {
                    var smallest = 1000;
                    List<Car> smallestSet = null;
                    foreach (var set in carSetsForCargoType)
                    {
                        if (set.Count <= smallest) { smallest = set.Count; smallestSet = set; }
                    }
                    carSetsForCargoType.Remove(smallestSet);
                    totalCars -= smallest;
                }
                carSetsForJob.AddRange(carSetsForCargoType);

            }
            return true;
        }
    }
}
