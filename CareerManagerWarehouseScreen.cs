using DV.ServicePenalty.UI;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DVProductionChains
{
    public class CareerManagerWarehouseScreen : MonoBehaviour, IDisplayScreen
    {
        private void Awake()
        {
            TextMeshPro textField = GetComponent<DisplayScreenSwitcher>().allTextFields[0];

            GameObject canvas = new GameObject("Canvas");
            canvas.transform.parent = transform;
            canvas.transform.localScale = new Vector3(0.1f, 0.1f, 0.1f);
            canvas.transform.Rotate(new Vector3(0f, -45f, 0f));
            canvas.AddComponent<Canvas>();

            RectTransform rectTransform = canvas.GetComponent<RectTransform>();
            rectTransform.sizeDelta = new Vector2(2000, 1000);
            rectTransform.localScale = new Vector3(.002f, 0.0005f);

            GameObject textObject = new GameObject("Text");
            textObject.transform.parent = canvas.transform;
            textObject.transform.position = textField.transform.position;

            text = textObject.AddComponent<Text>();
            text.font = Font.CreateDynamicFontFromOSFont("Arial", 16);
            text.fontSize = 100;
            text.alignment = TextAnchor.MiddleCenter;
            text.resizeTextForBestFit = true;   
        }

        public void Activate (IDisplayScreen _)
        {
            DVProductionChains.Log("Activating WarehouseScreen");
            text.text = "Omg this works!!!";
        }

        public void Disable()
        {
            text.text = string.Empty;
        }

        public void HandleInputAction(InputAction input)
        {

        }

        public Text text;
    }
}
