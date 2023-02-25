using DV.CabControls;
using DV.Logic.Job;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace DVProductionChains
{
    public class Warehouse
    {
        public Dictionary<CargoType, float> inputStorages = new Dictionary<CargoType, float>();
        public Dictionary<CargoType, float> outputStorages = new Dictionary<CargoType, float>();

        public Action<StorageChange> InputChanged;
        public Action<StorageChange> OutputChanged;

        public bool initialized = false;

        public class StorageChange 
        {
            public CargoType cargoType;
            public float oldValue;
            public float newValue;
            public float delta;

            public StorageChange(float prev, float next, CargoType cargoType)
            {
                this.cargoType = cargoType;
                this.oldValue = prev;
                this.newValue = next;
                this.delta = next - prev;
            }
        }
        public Warehouse(StationController stationController)
        {
            foreach (CargoGroup cargoGroup in stationController.proceduralJobsRuleset.inputCargoGroups)
            {
                foreach (CargoType cargoType in cargoGroup.cargoTypes)
                {
                    var type = Utils.CheckCargoTypeForInternals(cargoType);
                    if (!inputStorages.ContainsKey(type)) 
                    {
                        inputStorages.Add(type, 0f);
                    }
                }
            }
            foreach (CargoGroup cargoGroup in stationController.proceduralJobsRuleset.outputCargoGroups)
            {
                foreach (CargoType cargoType in cargoGroup.cargoTypes)
                {
                    var type = Utils.CheckCargoTypeForInternals(cargoType);
                    if (!outputStorages.ContainsKey(type))
                    {
                        outputStorages.Add(type, 0f);
                    }
                }
            }
            initialized = true;
        }

        public void AddToInputStorage(WarehouseTask unloadTask)
        {
            Utils.CheckCargoTypeForInternals(unloadTask.cargoType);
            float prev = inputStorages[unloadTask.cargoType];
            inputStorages[unloadTask.cargoType] += unloadTask.cargoAmount/unloadTask.cars.Count;
            InputChanged?.Invoke(new StorageChange(prev, inputStorages[unloadTask.cargoType], unloadTask.cargoType));
        }

        public void TakeFromInputStorage(CargoType cargoType, float amount)
        {
            float prev = inputStorages[cargoType];
            inputStorages[cargoType] -= amount;
            InputChanged?.Invoke(new StorageChange(prev, inputStorages[cargoType], cargoType));
        }

        public void AddToOutputStorage(CargoType cargoType, float amount)
        {
            float prev = 0;
            if (outputStorages.ContainsKey(cargoType))
                prev = outputStorages[cargoType];
            outputStorages[cargoType] += amount;
            OutputChanged?.Invoke(new StorageChange(prev, outputStorages[cargoType], cargoType));
        }

        public void TakeFromOutputStorage(WarehouseTask loadTask)
        {
            Utils.CheckCargoTypeForInternals(loadTask.cargoType);
            float prev = outputStorages[loadTask.cargoType];
            outputStorages[loadTask.cargoType] -= loadTask.cargoAmount/loadTask.cars.Count;
            OutputChanged?.Invoke(new StorageChange(prev, outputStorages[loadTask.cargoType], loadTask.cargoType));
        }
    }
}
