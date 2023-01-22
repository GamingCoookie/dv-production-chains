using DV.Logic.Job;
using DV.ServicePenalty.UI;
using HarmonyLib;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
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
        public string[] productionRecipes;
        private Dictionary<CargoType, List<int>> cargoTypeToApplicableRecipe = new Dictionary<CargoType, List<int>>();
        //private DisplayScreenSwitcher careerManager;
        //private CareerManagerWarehouseScreen warehouseScreen;

        public void Awake()
        {
            stationController = GetComponent<StationController>();
            DVProductionChains.stationControllers.Add(stationController);
            warehouse = new Warehouse(stationController);
            warehouse.InputChanged += OnInputStorageChanged;
            warehouse.OutputChanged += OnOutputStorageChanged;
            FullParsePass();
            /*
            GetCareerManager();
            warehouseScreen = careerManager.gameObject.AddComponent<CareerManagerWarehouseScreen>();
            careerManager.idleScreen = warehouseScreen;
            StartCoroutine(SwitchToWarehouseScreen());
            */
        }

        public void AddToInputStorage(WarehouseTask unloadTask)
        {
            DVProductionChains.Log($"Adding {unloadTask.cargoAmount} units of {unloadTask.cargoType} to input storage of {stationController.stationInfo.Name}");
            warehouse.AddToInputStorage(unloadTask);
        }

        public void TakeFromOutputStorage(WarehouseTask loadTask)
        {
            DVProductionChains.Log($"Taking {loadTask.cargoAmount} units of {loadTask.cargoType} from output storage of {stationController.stationInfo.Name}");
            warehouse.TakeFromOutputStorage(loadTask);
        }

        public float GetAmountOfCargoType(CargoType cargoType)
        {
            if (!warehouse.inputStorages.TryGetValue(cargoType, out var amount))
            {
                warehouse.outputStorages.TryGetValue(cargoType, out amount);
            }
            return amount;
        }

        // Return array of both types
        public CargoType[][] GetArraysOfCargoTypes()
        {
            return new [] { warehouse.inputStorages.Keys.ToArray(), warehouse.outputStorages.Keys.ToArray() };
        }

        public void OnInputStorageChanged(Warehouse.StorageChange storageChange)
        {
            if (storageChange.delta > 0)
            {
                List<int> recipeIndices = cargoTypeToApplicableRecipe[storageChange.cargoType];
                System.Random rnd = new System.Random();
                for (int i = 0; i < recipeIndices.Count; i++)
                {
                    int index = rnd.Next(recipeIndices.Count);
                    string recipe = productionRecipes[recipeIndices[index]];
                    ProcessRecipe(recipe);
                    recipeIndices.RemoveAt(index);
                }
            }
        }

        public void OnOutputStorageChanged(Warehouse.StorageChange storageChange)
        {
            StartCoroutine(TriggerTryGenerateJobsCoro());
        }

        public IEnumerator TriggerTryGenerateJobsCoro()
        {
            yield return new WaitUntil(() => stationController.ProceduralJobsController != null);
            yield return new WaitUntil(() => SingletonBehaviour<JobSaveManager>.Exists);
            stationController.ProceduralJobsController.TryToGenerateJobs();;
            DVProductionChains.Log("Tried the job generation in " + stationController.stationInfo.YardID);
        }

        public void ProcessRecipe(string recipe, bool firstRun = false)
        {
            StartCoroutine(ProcessRecipeCoro(recipe, firstRun));
        }

        //recipes are of type "a*input_a (+ b*input_b + ...) => c*output_a (+ d*output_b + ...)"
        //for starts of production chains the syntax is following "() => a*output_a (+ ...)"
        public IEnumerator ProcessRecipeCoro(string recipe, bool firstRun)
        {
            yield return new WaitUntil(() => warehouse.initialized);
            DVProductionChains.Log($"Working on '{recipe}'");
            string[] sides = recipe.Split(new string[] { " => " }, StringSplitOptions.None);
            if (sides[0] != "()")
            {
                string[] inputSide = sides[0].Split('+');
                int numberOfInputsUnfulfilled = inputSide.Length;
                Dictionary<CargoType, float> inputCargoNeeds = new Dictionary<CargoType, float>();
                foreach (string inputCargo in inputSide)
                {
                    string cargoName = inputCargo.Split('*')[1];
                    bool needParsed = float.TryParse(inputCargo.Split('*')[0], out float cargoNeed);
                    CargoType cargoType = Utils.GetCargoTypeFromString(cargoName);
                    inputCargoNeeds.Add(cargoType, needParsed ? cargoNeed : 1f);
                    if (firstRun)
                    {
                        if (!cargoTypeToApplicableRecipe.ContainsKey(cargoType))
                            cargoTypeToApplicableRecipe.Add(cargoType, new List<int>() { productionRecipes.ToList().IndexOf(recipe) });
                        else
                            cargoTypeToApplicableRecipe[cargoType].Add(productionRecipes.ToList().IndexOf(recipe));
                    }
                    else
                    {
                        if (warehouse.inputStorages[cargoType] >= inputCargoNeeds[cargoType]) { numberOfInputsUnfulfilled--; }
                    }
                }
                //when the industry has enough materials, then produce thing
                if (numberOfInputsUnfulfilled == 0)
                {
                    string[] outputSide = sides[1].Split('+');
                    Dictionary<CargoType, float> outputCargoProduction = new Dictionary<CargoType, float>();
                    foreach (string outputCargo in outputSide)
                    {
                        string cargoName = outputCargo.Split('*')[1];
                        bool productionParsed = float.TryParse(outputCargo.Split('*')[0], out float cargoProduce);
                        CargoType cargoType = Utils.GetCargoTypeFromString(cargoName);
                        outputCargoProduction.Add(cargoType, productionParsed ? cargoProduce : 1f);
                    }
                    //Find the amount to produce by dividing warehouse content by need
                    List<float> quotients = new List<float>();
                    foreach (CargoType input in inputCargoNeeds.Keys)
                    {
                        quotients.Add(warehouse.inputStorages[input] / inputCargoNeeds[input]);
                    }
                    float ratio = quotients.Min();
                    foreach (CargoType input in inputCargoNeeds.Keys)
                    {
                        warehouse.TakeFromInputStorage(input, inputCargoNeeds[input] * ratio * 0.5f);
                    }
                    foreach (CargoType output in outputCargoProduction.Keys)
                    {
                        warehouse.AddToOutputStorage(output, outputCargoProduction[output] * ratio * 0.5f);
                    }
                }
            }
            else
            {
                string[] outputSide = sides[1].Split('+');
                Dictionary<CargoType, float> outputCargoProduction = new Dictionary<CargoType, float>();
                foreach (string outputCargo in outputSide)
                {
                    string cargoName = outputCargo.Split('*')[1];
                    bool productionParsed = float.TryParse(outputCargo.Split('*')[0], out float cargoProduce);
                    CargoType cargoType = Utils.GetCargoTypeFromString(cargoName);
                    outputCargoProduction.Add(cargoType, productionParsed ? cargoProduce : 1f);
                }
                foreach (CargoType output in outputCargoProduction.Keys)
                {
                    warehouse.AddToOutputStorage(output, outputCargoProduction[output]);
                }
            }
        }

        public void FullParsePass()
        {
            string[] allRecipes = File.ReadAllLines("./Mods/DVProductionChains/recipes.txt");
            int i = 0;
            bool blockStartReached = false;
            List<string> temp = new List<string>();
            foreach (string line in allRecipes)
            {
                //Each block of recipes starts with the yard ID
                if (line == stationController.stationInfo.YardID && !blockStartReached)
                {
                    DVProductionChains.Log($"Collecting recipes for {stationController.stationInfo.YardID}");
                    blockStartReached = true;
                }
                else if (line != stationController.stationInfo.YardID && line.Count() <= 3 && blockStartReached) { break; }
                else if (blockStartReached)
                {
                    temp.Add(line);
                }
                i++;
            }
            if (temp.Count == 0) { return; }
            productionRecipes = temp.ToArray();
            foreach (string recipe in productionRecipes)
            {
                ProcessRecipe(recipe, true);
            }
        }
        /*
        public void GetCareerManager()
        {
            DisplayScreenSwitcher[] dsss = FindObjectsOfType<DisplayScreenSwitcher>();
            List<float> distances = new List<float>();
            foreach (DisplayScreenSwitcher dss in dsss)
            {
                distances.Add(Vector3.Distance(dss.gameObject.transform.position, stationController.transform.position));
            }
            careerManager = dsss[distances.IndexOf(distances.Min())];
        }

        public IEnumerator SwitchToWarehouseScreen()
        {
            yield return WaitFor.Seconds(5f);
            careerManager.SetActiveDisplay(warehouseScreen);
        }
        */
    }
}
