using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

namespace BROKE
{
    class FundingModTest : IFundingModifier
    {
        public FundingModTest()
        {
            Debug.Log("[BROKE] Instantiating test funding modifier!");
        }

        public string GetName()
        {
            return "Test Funding";
        }

        public string GetConfigName()
        {
            return "TestFundMod";
        }

        public void OnEnabled()
        {
            Debug.Log("[BROKE] Enabled!");
        }

        public void OnDisabled()
        {
            Debug.Log("[BROKE] Disabled!");
        }

        public double[] ProcessYearly() {
            Debug.Log("[BROKE] Yearly update!");
            return new double[] {100, 25}; 
        }

        public double[] ProcessQuarterly() {
            Debug.Log("[BROKE] Quarterly update!");
            return new double[] {31, 42}; 
        }

        public void DailyUpdate() { Debug.Log("[BROKE] Daily update!");  }

        public void LoadData(ConfigNode node) { }

        public ConfigNode SaveData() { return null; }

        public bool hasSettingsGUI()
        {
            return false;
        }

        public bool hasMainGUI()
        {
            return true;
        }

        public void DrawSettingsGUI() { }
        public void DrawMainGUI() { GUILayout.Label("Hello, World!"); }
    }
}
