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

        public void OnInvoicePaid(object sender, InvoiceItem.InvoicePaidEventArgs args)
        {
            if (args.PaidInFull)
                Debug.Log("Invoice Paid in full");
            else
                Debug.Log("Invoice Paid : " + args.AmountPaid);
        }

        public void OnInvoiceUnpaid(object sender, EventArgs args)
        {
            Debug.Log("Invoice Unpaid!");
        }

        public InvoiceItem ProcessQuarterly()
        {
            Debug.Log("Example Quarterly update!");
            AllTimeRevenue += 31;
            AllTimeExpenses += 42;
            var invoice = new InvoiceItem(this, 31, 42);
            return invoice;
        }

        public InvoiceItem ProcessYearly()
        {
            Debug.Log("Example Yearly update!");
            AllTimeRevenue += 100;
            AllTimeExpenses += 25;
            var invoice = new InvoiceItem(this, 100, 25);
            return invoice;
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
