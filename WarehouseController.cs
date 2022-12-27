using DV.Logic.Job;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DVProductionChains
{
    internal class WarehouseController : MonoBehaviour
    {
        private StationController stationController;
        private Warehouse warehouse;
        public void Awake()
        {
            stationController = GetComponent<StationController>();
            warehouse = new Warehouse(stationController);
            DVProductionChains.Log($"Input cargo groups for {stationController.name}");
            foreach (CargoGroup cargoGroup in stationController.proceduralJobsRuleset.inputCargoGroups)
            {
                foreach (CargoType type in cargoGroup.cargoTypes)
                {
                    DVProductionChains.Log(type);
                }
            }
            DVProductionChains.Log($"Output cargo groups for {stationController.name}");
            foreach (CargoGroup cargoGroup in stationController.proceduralJobsRuleset.outputCargoGroups)
            {
                foreach (CargoType type in cargoGroup.cargoTypes)
                {
                    DVProductionChains.Log(type);
                }
                foreach (StationController station in cargoGroup.stations)
                {
                    DVProductionChains.Log(station.name);
                }
            }
        }
    }
}
