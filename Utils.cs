using DV.Logic.Job;
using DV.Utils.String;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DVProductionChains
{
    public static class Utils
    {
        static Utils()
        {
            foreach (CargoType type in Enum.GetValues(typeof(CargoType))) 
            {
                nameToCargo[GetCargoName(type)] = type;
            }
        }

        public static CargoType CheckCargoTypeForInternals(CargoType cargoType)
        {
            if (cargoType.GetShortCargoName().EndsWith("Electronics")) { cargoType = CargoType.ElectronicsTraeg; }
            else if (cargoType.GetShortCargoName().EndsWith("Apparel")) { cargoType = CargoType.ClothingTraeg; }
            else if (cargoType.GetShortCargoName().EndsWith("Tooling")) { cargoType = CargoType.ToolsTraeg; }
            else if (cargoType.GetShortCargoName().EndsWith("Container")) { cargoType = CargoType.EmptyNeoGamma; }
            else if (cargoType.GetShortCargoName().EndsWith("Chemicals")) { cargoType = CargoType.ChemicalsSperex; }
            else if (cargoType.GetShortCargoName().StartsWith("Steel")) { cargoType = CargoType.SteelRolls; }
            else if (80 <= ((int)cargoType) && ((int)cargoType) <= 104) { cargoType = CargoType.Wheat; }
            else if (120 <= ((int)cargoType) && ((int)cargoType) <= 124) { cargoType = CargoType.Bread; }
            return cargoType;
        }

        public static string GetCargoName(CargoType cargoType)
        {
            string name;
            if (cargoType.GetShortCargoName().EndsWith("Electronics")) { name = "Electronics"; }
            else if (cargoType.GetShortCargoName().EndsWith("Apparel")) { name = "Apparel"; }
            else if (cargoType.GetShortCargoName().EndsWith("Tooling")) { name = "Tooling"; }
            else if (cargoType.GetShortCargoName().EndsWith("Container")) { name = "Empty Containers"; ; }
            else if (cargoType.GetShortCargoName().EndsWith("Chemicals")) { name = "Chemicals"; }
            else if (cargoType.GetShortCargoName().StartsWith("Steel")) { name = "Steel Parts"; }
            else if (80 <= ((int)cargoType) && ((int)cargoType) <= 104) { name = "Farm Produce"; }
            else if (120 <= ((int)cargoType) && ((int)cargoType) <= 124) { name = "Processed Food"; }
            else { name = StringUtils.BreakCamelCaseToSeparateWords(cargoType.ToString()); }
            //DVProductionChains.Log(name);
            return name;
        }

        public static CargoType GetCargoTypeFromString(string name)
        {
            CargoType cargoType = CargoType.None;
            if (name == "Electronics") { cargoType = CargoType.ElectronicsTraeg; }
            else if (name == "Apparel") { cargoType = CargoType.ClothingTraeg; }
            else if (name == "Tooling") { cargoType = CargoType.ToolsTraeg; }
            else if (name == "Empty Containers") { cargoType = CargoType.EmptyNeoGamma; }
            else if (name == "Chemicals") { cargoType = CargoType.ChemicalsSperex; }
            else if (name == "Steel Parts") { cargoType = CargoType.SteelRolls; }
            else if (name == "Farm Produce") { cargoType = CargoType.Wheat; }
            else if (name == "Processed Food") { cargoType = CargoType.Bread; }
            else if (!nameToCargo.TryGetValue(name, out cargoType))
            {
                DVProductionChains.Log($"No CargoType found that corresponds to {name}");
            }
            return cargoType;
        }

        public static CargoType[] GetCargoTypesFromString(string name)
        {
            List<CargoType> cargoTypes = new List<CargoType>();
            if (name == "Electronics") { cargoTypes.AddRange(new CargoType[] { (CargoType)160, (CargoType)161, (CargoType)162, (CargoType)163, (CargoType)164 }); }
            else if (name == "Apparel") { cargoTypes.AddRange(new CargoType[] { (CargoType)220, (CargoType)221, (CargoType)222, (CargoType)223 }); }
            else if (name == "Tooling") { cargoTypes.AddRange(new CargoType[] { (CargoType)180, (CargoType)181, (CargoType)182, (CargoType)183, (CargoType)184 }); }
            else if (name == "Empty Containers") { cargoTypes.AddRange(new CargoType[] { (CargoType)900, (CargoType)901, (CargoType)902, (CargoType)903, (CargoType)904, (CargoType)905, (CargoType)906, (CargoType)907, (CargoType)908, (CargoType)909, (CargoType)910, (CargoType)911 }); }
            else if (name == "Chemicals") { cargoTypes.AddRange(new CargoType[] { (CargoType)241, (CargoType)242 }); }
            else if (name == "Steel Parts") { cargoTypes.AddRange(new CargoType[] { (CargoType)140, (CargoType)141, (CargoType)142, (CargoType)143, (CargoType)144 }); }
            else if (name == "Farm Produce") { cargoTypes.AddRange(new CargoType[] { (CargoType)80, (CargoType)81, (CargoType)100, (CargoType)101, (CargoType)102, (CargoType)103, (CargoType)104 }); }
            else if (name == "Processed Food") { cargoTypes.AddRange(new CargoType[] { (CargoType)120, (CargoType)121, (CargoType)122, (CargoType)123, (CargoType)124 }); }
            else if(!nameToCargo.TryGetValue(name, out var cargoType))
            {
                DVProductionChains.Log($"No CargoType found that corresponds to {name}");
                cargoTypes.Add(CargoType.None);
            }
            else
            {
                cargoTypes.Add(cargoType);
            }
            return cargoTypes.ToArray();
        }

        public class CoroutineRunner : MonoBehaviour
        {
            public static void RunCoroutine(IEnumerator coroutine)
            {
                var go = new GameObject("runner");
                DontDestroyOnLoad(go);

                var runner = go.AddComponent<CoroutineRunner>();

                runner.StartCoroutine(runner.MonitorRunning(coroutine));
            }

            IEnumerator MonitorRunning(IEnumerator coroutine)
            {
                while (coroutine.MoveNext())
                {
                    yield return coroutine.Current;
                }

                Destroy(gameObject);
            }
        }

        private static Dictionary<string, CargoType> nameToCargo = new Dictionary<string, CargoType>();
    }
}
