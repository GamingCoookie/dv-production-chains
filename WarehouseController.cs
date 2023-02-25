using DV.JObjectExtstensions;
using DV.Logic.Job;
using DV.ServicePenalty.UI;
using HarmonyLib;
using Newtonsoft.Json.Linq;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DVProductionChains
{
    internal class WarehouseController : MonoBehaviour
    {
        public static List<WarehouseController> allWarehouseControllers = new List<WarehouseController>();
        public static bool saveLoaded = false;
        public static readonly string ID_SAVE_KEY = "id";
        public static readonly string WAREHOUSE_INPUT_CARGO_TYPES_SAVE_KEY = "warehouseInputCargoTypes";
        public static readonly string WAREHOUSE_INPUT_CARGO_AMOUNTS_SAVE_KEY = "warehouseInputCargoAmounts";
        public static readonly string WAREHOUSE_OUTPUT_CARGO_TYPES_SAVE_KEY = "warehouseOutputCargoTypes";
        public static readonly string WAREHOUSE_OUTPUT_CARGO_AMOUNTS_SAVE_KEY = "warehouseOutputCargoAmounts";

        public string yardID => stationController.stationInfo.YardID;
        private StationController stationController;
        private Warehouse warehouse;
        private List<Job> endingHere = new List<Job>();
        public string[] productionRecipes;
        private Dictionary<CargoType, List<int>> cargoTypeToApplicableRecipe = new Dictionary<CargoType, List<int>>();
        public bool consumesTools = false;
        public bool consumesElectronics = false;
        //private DisplayScreenSwitcher careerManager;
        //private CareerManagerWarehouseScreen warehouseScreen;

        public void Awake()
        {
            if (allWarehouseControllers == null)
            {
                allWarehouseControllers = new List<WarehouseController>();
            }
            allWarehouseControllers.Add(this);
            stationController = GetComponent<StationController>();
            DVProductionChains.stationControllers.Add(stationController);
            warehouse = new Warehouse(stationController);
            warehouse.OutputChanged += OnOutputStorageChanged;
            FullParsePass();
            /*
            GetCareerManager();
            warehouseScreen = careerManager.gameObject.AddComponent<CareerManagerWarehouseScreen>();
            careerManager.idleScreen = warehouseScreen;
            StartCoroutine(SwitchToWarehouseScreen());
            */
        }

        public void Update()
        {
            if (PlayerJobs.Instance.currentJobs.Count(j => j.jobType == JobType.ShuntingUnload && j.chainData.chainDestinationYardId == stationController.stationInfo.YardID) > endingHere.Count)
            {
                foreach (Job job in PlayerJobs.Instance.currentJobs.FindAll(j => j.jobType == JobType.ShuntingUnload && j.chainData.chainDestinationYardId == stationController.stationInfo.YardID && !endingHere.Contains(j)))
                {
                    job.JobCompleted += (Job j) => OnShuntingUnloadCompleted(j);
                    endingHere.Add(job);
                    job.JobAbandoned += (Job j) => endingHere.Remove(j);
                }
            }
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
            cargoType = Utils.CheckCargoTypeForInternals(cargoType);
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

        public void OnShuntingUnloadCompleted(Job _)
        {
            foreach (CargoType cargoType in warehouse.inputStorages.Keys)
            {
                float amount = warehouse.inputStorages[cargoType];
                if (amount == 0)
                    continue;
                List<int> recipeIndices = cargoTypeToApplicableRecipe[cargoType];
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
            if (stationController.playerEnteredJobGenerationZone)
                StartCoroutine(TriggerTryGenerateJobsCoro());
        }

        public IEnumerator TriggerTryGenerateJobsCoro()
        {
            yield return new WaitUntil(() => stationController.ProceduralJobsController != null);
            yield return new WaitUntil(() => SingletonBehaviour<JobSaveManager>.Exists);
            yield return new WaitForSeconds(5);
            stationController.ExpireAllAvailableJobsInStation();
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
            yield return new WaitUntil(() => warehouse.initialized && saveLoaded);
            DVProductionChains.Log($"Working on '{recipe}'");
            string[] sides = recipe.Split(new string[] { " => " }, StringSplitOptions.None);
            if (sides[1] == "()")
            {
                if (sides[0] == "Electronics") { consumesElectronics = true; }
                else if (sides[0] == "Tools") { consumesTools = true; }
            }
            else if (sides[0] != "()")
            {
                string[] inputSide = sides[0].Split(new string[] { " + " }, StringSplitOptions.None);
                int numberOfInputsUnfulfilled = inputSide.Length - consumesElectronics.CompareTo(true) - consumesTools.CompareTo(true);
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
                    string[] outputSide = sides[1].Split(new string[] { " + " }, StringSplitOptions.None);
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
                    if (consumesElectronics) { quotients.Add(warehouse.inputStorages[CargoType.ElectronicsTraeg] / 0.2f); }
                    if (consumesTools) { quotients.Add(warehouse.inputStorages[CargoType.ToolsTraeg] / 0.2f); }
                    float ratio = quotients.Min() * 0.5f;
                    foreach (CargoType input in inputCargoNeeds.Keys)
                    {
                        warehouse.TakeFromInputStorage(input, inputCargoNeeds[input] * ratio);
                    }
                    if (consumesElectronics) { warehouse.TakeFromInputStorage(CargoType.ElectronicsTraeg, 0.2f * ratio); }
                    if (consumesTools) { warehouse.TakeFromInputStorage(CargoType.ToolsTraeg, 0.2f * ratio); }
                    foreach (CargoType output in outputCargoProduction.Keys)
                    {
                        warehouse.AddToOutputStorage(output, outputCargoProduction[output] * ratio);
                    }
                }
            }
            else
            {
                string[] outputSide = sides[1].Split(new string[] { " + " }, StringSplitOptions.None);
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
                //Comments are made with #
                if (line.StartsWith("#"))
                {
                    i++;
                    continue;
                }
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

        public static void LoadSaveData(JArray data)
        {
            Utils.CoroutineRunner.RunCoroutine(LoadSaveDataCoro(data));
        }

        public static IEnumerator LoadSaveDataCoro(JArray data)
        {
            foreach (var datum in data)
            {
                if (datum.Type == JTokenType.Object)
                {
                    JObject obj = (JObject)datum;
                    string yardID = obj.GetString(ID_SAVE_KEY);
                    DVProductionChains.Log("Loading data for station " + yardID);
                    yield return new WaitUntil(() => allWarehouseControllers.Count == 18);
                    WarehouseController controller = allWarehouseControllers.Find(c => c.yardID == yardID);
                    CargoType[] types = controller.warehouse.inputStorages.Keys.ToArray();
                    for (int i = 0; i < controller.warehouse.inputStorages.Count; i++) 
                    {
                        DVProductionChains.Log($"I{i}: " + Utils.GetCargoName(types[i]) + " - " + obj.GetFloat($"I{i}").ToString());
                        controller.warehouse.inputStorages[types[i]] = (float)obj.GetFloat($"I{i}");
                    }
                    types = controller.warehouse.outputStorages.Keys.ToArray();
                    for (int i = 0; i < controller.warehouse.outputStorages.Count; i++)
                    {
                        DVProductionChains.Log($"O{i}: " + Utils.GetCargoName(types[i]) + " - " + obj.GetFloat($"O{i}").ToString());
                        controller.warehouse.outputStorages[types[i]] = (float)obj.GetFloat($"O{i}");
                    }
                }
            }
            saveLoaded = true;
        }

        public static JArray GetSaveData()
        {
            List<JObject> source = new List<JObject>();
            foreach (WarehouseController controller in allWarehouseControllers)
            {
                JObject jObject = new JObject();
                jObject.SetString(ID_SAVE_KEY, controller.yardID);
                DVProductionChains.Log("saving warehouse of " + controller.yardID);
                /** I don't need this, do I?
                List<string> types = new List<string>();
                foreach (CargoType type in controller.warehouse.inputStorages.Keys)
                {
                    types.Add(Utils.GetCargoName(type));
                }
                jObject.SetStringArray(WAREHOUSE_INPUT_CARGO_TYPES_SAVE_KEY, types.ToArray());
                types.Clear();
                **/
                int i = 0;
                foreach (float amount in controller.warehouse.inputStorages.Values)
                {
                    DVProductionChains.Log("Saving I" + i + " with an amount of " + amount);
                    jObject.SetFloat($"I{i}", amount);
                    i++;
                }
                /**
                foreach (CargoType type in controller.warehouse.outputStorages.Keys)
                {
                    types.Add(Utils.GetCargoName(type));
                }
                jObject.SetStringArray(WAREHOUSE_OUTPUT_CARGO_TYPES_SAVE_KEY, types.ToArray());
                types.Clear();
                **/
                i = 0;
                foreach (float amount in controller.warehouse.outputStorages.Values)
                {
                    DVProductionChains.Log("Saving O" + i + " with an amount of " + amount);
                    jObject.SetFloat($"O{i}", amount);
                    i++;
                }
                source.Add(jObject);
            }
            return new JArray(source.ToArray());
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
