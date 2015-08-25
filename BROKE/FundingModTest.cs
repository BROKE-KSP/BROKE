using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;

namespace BROKE
{
    class FundingModTest : IFundingModifier
    {
        private bool enabled = true;
        public FundingModTest()
        {
            Debug.Log("[BROKE] Instantiating test funding modifier!");
        }

        public string GetName()
        {
            return "Test Funding Modifier";
        }

        public string GetConfigName()
        {
            return "TestFundMod";
        }

        public bool isEnabled()
        {
            return enabled;
        }

        public void SetEnabled(bool en)
        {
            enabled = en;
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
    }
}
