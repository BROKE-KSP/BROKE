using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BROKE
{
    /// <summary>
    /// This class acts as a shim for simple funding modifiers (IFundingModifier implementers), to make them act like IMultiFundingModifiers
    /// </summary>
    internal class SingleToMultiFundingModifier : IMultiFundingModifier
    {
        private readonly IFundingModifier modifier;

        public SingleToMultiFundingModifier(IFundingModifier modifier)
        {
            this.modifier = modifier;
        }

        public void DailyUpdate()
        {
            modifier.DailyUpdate();
        }

        public void DrawMainGUI()
        {
            modifier.DrawMainGUI();
        }

        public void DrawSettingsGUI()
        {
            modifier.DrawSettingsGUI();
        }

        public string GetConfigName()
        {
            return modifier.GetConfigName();
        }

        public string GetName()
        {
            return modifier.GetName();
        }

        public bool hasMainGUI()
        {
            return modifier.hasMainGUI();
        }

        public bool hasSettingsGUI()
        {
            return modifier.hasSettingsGUI();
        }

        public void LoadData(ConfigNode node)
        {
            modifier.LoadData(node);
        }

        public void OnDisabled()
        {
            modifier.OnDisabled();
        }

        public void OnEnabled()
        {
            modifier.OnEnabled();
        }

        public void OnInvoicePaid(object sender, InvoiceItem.InvoicePaidEventArgs args)
        {
            modifier.OnInvoicePaid(sender, args);
        }

        public void OnInvoiceUnpaid(object sender, EventArgs args)
        {
            modifier.OnInvoiceUnpaid(sender, args);
        }

        public IEnumerable<InvoiceItem> ProcessQuarterly()
        {
            yield return modifier.ProcessQuarterly();
        }

        public IEnumerable<InvoiceItem> ProcessYearly()
        {
            yield return modifier.ProcessYearly();
        }

        public ConfigNode SaveData()
        {
            return modifier.SaveData();
        }
    }
}
