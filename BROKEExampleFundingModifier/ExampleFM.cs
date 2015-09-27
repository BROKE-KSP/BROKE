using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using BROKE;

namespace BROKEExampleFundingModifier
{
    public class ExampleFM : IFundingModifier
    {
        double AllTimeRevenue = 0, AllTimeExpenses = 0;

        public string GetName()
        {
            return "Example Funding";
        }

        public string GetConfigName()
        {
            return "ExampleFundMod";
        }

        public void OnEnabled()
        {
            Debug.Log("Example Enabled!");
        }

        public void OnDisabled()
        {
            Debug.Log("Example Disabled!");
        }

        public double[] ProcessYearly()
        {
            Debug.Log("Example Yearly update!");
            AllTimeRevenue += 100;
            AllTimeExpenses += 25;
            return new double[] { 100, 25 };
        }

        public double[] ProcessQuarterly()
        {
            Debug.Log("Example Quarterly update!");
            AllTimeRevenue += 31;
            AllTimeExpenses += 42;
            return new double[] { 31, 42 };
        }

        public void DailyUpdate() { Debug.Log("Example Daily update!"); }

        public void LoadData(ConfigNode node) 
        {
            double.TryParse(node.GetValue("AllTimeRevenue"), out AllTimeRevenue);
            double.TryParse(node.GetValue("AllTimeExpenses"), out AllTimeExpenses);
        }

        public ConfigNode SaveData() 
        {
            ConfigNode data = new ConfigNode();
            data.AddValue("AllTimeRevenue", AllTimeRevenue);
            data.AddValue("AllTimeExpenses", AllTimeExpenses);
            return data;
        }

        public bool hasSettingsGUI()
        {
            return true;
        }

        public bool hasMainGUI()
        {
            return true;
        }

        public void DrawSettingsGUI() { GUILayout.Label("Example settings!"); }
        public void DrawMainGUI() { GUILayout.Label("Hello, World!"); }
    }
}
