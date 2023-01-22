using DV.Logic.Job;
using DV.Utils.String;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
            return cargoType;
        }

        public static string GetCargoName(CargoType cargoType)
        {
            string name;
            if (cargoType.GetShortCargoName().EndsWith("Electronics")) { name = "Electronics"; }
            else if (cargoType.GetShortCargoName().EndsWith("Apparel")) { name = "Apparel"; }
            else if (cargoType.GetShortCargoName().EndsWith("Tooling")) { name = "Tooling"; }
            else if (cargoType.GetShortCargoName().EndsWith("Container")) { name = "Empty Container"; ; }
            else if (cargoType.GetShortCargoName().EndsWith("Chemicals")) { name = "Chemicals"; }
            else { name = StringUtils.BreakCamelCaseToSeparateWords(cargoType.ToString()); }
            //DVProductionChains.Log(name);
            return name;
        }

        public static CargoType GetCargoTypeFromString(string name)
        {
            if(!nameToCargo.TryGetValue(name, out var cargoType))
            {
                DVProductionChains.Log($"No CargoType found that corresponds to {name}");
                return CargoType.None;
            }
            return cargoType;
        }

        private static Dictionary<string, CargoType> nameToCargo = new Dictionary<string, CargoType>();
    }
}
