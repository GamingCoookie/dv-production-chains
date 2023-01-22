using DV;
using DV.Logic.Job;
using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace DVProductionChains
{
    public class CommsRadioWarehouseCheck : MonoBehaviour, ICommsRadioMode
    {
        public static CommsRadioController controller;

        public ButtonBehaviourType ButtonBehaviour { get; private set; }

        private static Color laserColor = new Color(0f, 0f, 0f, 0f);
        public Color GetLaserBeamColor() { return laserColor; }

        [Header("Strings")]
        private const string MODE_NAME = "WAREHOUSE";
        private const string CONTENT_MAINMENU = "Check storage?";
        private const string CONTENT_SELECT_CARGO = "{0}\n{1}\n{2}";
        private const string ACTION_RETURN = "Return";
        private const string INPUT_CARGO = "Input";
        private const string OUTPUT_CARGO = "Output";

        public Transform signalOrigin;
        public void OverrideSignalOrigin(Transform signalOrigin) { this.signalOrigin = signalOrigin; }

        public CommsRadioDisplay display;

        [Header("Sounds")]
        public AudioClip confirmSound;
        public AudioClip cancelSound;

        private CargoType[] inputCargoTypes;
        private CargoType[] outputCargoTypes;
        private int selectedCargoTypeIndex = 0;
        private bool indexingOutput = false;
        private WarehouseController warehouseController;

        private State state;
        protected enum State
        {
            NotActive,
            MainMenu,
            Warehouse
        }

        protected enum Action
        {
            Trigger,
            Increase,
            Decrease,
        }

        public void Awake()
        {
            // Copy components from other radio modes
            var summoner = controller.crewVehicleControl;

            if (summoner == null) { throw new Exception("Crew vehicle radio mode could not be found!"); }

            signalOrigin = summoner.signalOrigin;
            display = summoner.display;

            confirmSound = summoner.confirmSound;
            cancelSound= summoner.cancelSound;

            if (!signalOrigin)
            {
                signalOrigin = transform;
            }
        }

        public void Enable() { TransitionToState(State.MainMenu); }

        public void Disable() { TransitionToState(State.NotActive); }

        public void SetStartingDisplay() { display.SetDisplay(MODE_NAME, CONTENT_MAINMENU); }

        public void OnUpdate()
        {

        }

        public void OnUse()
        {
            TransitionToState(DispatchAction(Action.Trigger));
        }

        public bool ButtonACustomAction()
        {
            TransitionToState(DispatchAction(Action.Decrease));
            return true;
        }

        public bool ButtonBCustomAction()
        {
            TransitionToState(DispatchAction(Action.Increase));
            return true;
        }

        private State DispatchAction(Action action)
        {
            switch (state)
            {
                case State.MainMenu:
                    switch (action)
                    {
                        case Action.Trigger:
                            return State.Warehouse;
                        default:
                            return State.MainMenu;
                    }

                case State.Warehouse:
                    switch (action)
                    {
                        case Action.Trigger:
                            return State.MainMenu;
                        case Action.Increase:
                            SelectNextCargoType();
                            SetCargoText();
                            break;
                        case Action.Decrease:
                            SelectPrevCargoType();
                            SetCargoText();
                            break;
                    }
                    return State.Warehouse;
            }

            return state;
        }

        private void TransitionToState(State newState)
        {
            var oldState = state;
            state = newState;

            switch (newState)
            {
                case State.NotActive:
                    ButtonBehaviour = ButtonBehaviourType.Regular;
                    StopAllCoroutines();
                    return;

                case State.MainMenu:
                    if (oldState != State.NotActive)
                    {
                        // Just went back
                        CommsRadioController.PlayAudioFromRadio(cancelSound, transform);
                    }
                    ButtonBehaviour = ButtonBehaviourType.Regular;
                    DisplayMainMenu();
                    return;

                case State.Warehouse:
                    if (oldState == State.MainMenu)
                    {
                        CommsRadioController.PlayAudioFromRadio(confirmSound, transform);
                    }
                    ButtonBehaviour = ButtonBehaviourType.Override;
                    DisplayCargoTypeAndAmount();
                    return;
            }
        }

        private void DisplayMainMenu()
        {
            SetStartingDisplay();
        }
        
        private void DisplayCargoTypeAndAmount()
        {
            Vector3 pos = PlayerManager.GetWorldAbsolutePlayerPosition();
            WarehouseController[] warehouseControllers = FindObjectsOfType<WarehouseController>();
            List<float> distances = new List<float>();
            foreach (WarehouseController warehouseController in warehouseControllers)
            {
                distances.Add(Vector3.Distance(warehouseController.gameObject.transform.position - WorldMover.currentMove, pos));
            }
            warehouseController = warehouseControllers[distances.IndexOf(distances.Min())];
            inputCargoTypes = warehouseController.GetArraysOfCargoTypes()[0];
            outputCargoTypes = warehouseController.GetArraysOfCargoTypes()[1];
            var action = ACTION_RETURN;
            display.SetAction(action);
            SetCargoText();
        }

        private void SelectNextCargoType()
        {
            selectedCargoTypeIndex++;
            if (selectedCargoTypeIndex >= inputCargoTypes.Length && !indexingOutput) 
            {
                selectedCargoTypeIndex = 0;
                if (outputCargoTypes.Length > 0)
                    indexingOutput = true;
            }
            else if (selectedCargoTypeIndex >= outputCargoTypes.Length && indexingOutput)
            {
                selectedCargoTypeIndex = 0;
                indexingOutput = false;
            }
        }

        private void SelectPrevCargoType()
        {
            selectedCargoTypeIndex--;
            if (selectedCargoTypeIndex < 0 && !indexingOutput) 
            { 
                selectedCargoTypeIndex = outputCargoTypes.Length - 1;
                if (outputCargoTypes.Length > 0)
                    indexingOutput = true;
            }
            else if (selectedCargoTypeIndex < 0 && indexingOutput)
            {
                selectedCargoTypeIndex = inputCargoTypes.Length - 1;
                indexingOutput = false;
            }
        }

        private void SetCargoText()
        {
            float storage = warehouseController.GetAmountOfCargoType(SelectedCargoType);
            var content = string.Format(CONTENT_SELECT_CARGO, indexingOutput ? OUTPUT_CARGO : INPUT_CARGO, Utils.GetCargoName(SelectedCargoType), storage.ToString("F0"));
            display.SetContent(content);
        }

        private CargoType SelectedCargoType { get 
            { 
                if (indexingOutput)
                {
                    return outputCargoTypes[selectedCargoTypeIndex];
                }
                else
                {
                    return inputCargoTypes[selectedCargoTypeIndex];
                }
                 
            } }

        [HarmonyPatch(typeof(CommsRadioController), "Awake")]
        class CommsRadioController_Awake_Patch
        {
            public static CommsRadioWarehouseCheck warehouseCheck = null;

            static void Postfix(CommsRadioController __instance, List<ICommsRadioMode> ___allModes)
            {
                controller = __instance;

                if (warehouseCheck == null) { warehouseCheck = controller.gameObject.AddComponent<CommsRadioWarehouseCheck>(); }

                if (!___allModes.Contains(warehouseCheck))
                {
                    int spawnerIndex = ___allModes.FindIndex(mode => mode is CommsRadioCarSpawner);
                    if (spawnerIndex != -1) { ___allModes.Insert(spawnerIndex, warehouseCheck); }
                    else { ___allModes.Add(warehouseCheck); }
                    controller.ReactivateModes();
                }
            }
        }
    }
}