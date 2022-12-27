using DV.Logic.Job;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DVProductionChains
{
    public class Warehouse
    {
        public Dictionary<CargoType, float> inputStorages;
        public Dictionary<CargoType, float> outputStorages;

        public Warehouse(StationController stationController)
        {
            foreach (CargoGroup cargoGroup in stationController.proceduralJobsRuleset.inputCargoGroups)
            {
                foreach (CargoType cargoType in cargoGroup.cargoTypes)
                {
                    if (!inputStorages.ContainsKey(cargoType)) 
                    {
                        inputStorages.Add(cargoType, 0f);
                    }
                }
            }
            foreach (CargoGroup cargoGroup in stationController.proceduralJobsRuleset.outputCargoGroups)
            {
                foreach (CargoType cargoType in cargoGroup.cargoTypes)
                {
                    if (!outputStorages.ContainsKey(cargoType))
                    {
                        inputStorages.Add(cargoType, 0f);
                    }
                }
            }
        }
    }
}
