using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BROKE
{
    public interface IFundingModifierBase
    {
        /// <summary>
        /// The name of the Funding Modifier (ie, Crew Payroll, Government Funding, etc)
        /// </summary>
        /// <returns>string Funding Modifier's Name</returns>
        string GetName();

        /// <summary>
        /// Gets the name to use for the ConfigNode (ie, BROKEPayroll, BROKEGovt, etc)
        /// </summary>
        /// <returns>string Funding Modifier's ConfigNode name</returns>
        string GetConfigName();

        /// <summary>
        /// Called when the FundingModifier is switched from Enabled to Disabled
        /// </summary>
        void OnDisabled();

        /// <summary>
        /// Called when the FundingModifier is switched from Disabled to Enabled
        /// </summary>
        void OnEnabled();

        /// <summary>
        /// This will be called once a day and should be used to update any time-based funding information (ie, # of running missions that day, which Kerbals are at KSC and for how long, etc)
        /// </summary>
        void DailyUpdate();

        /// <summary>
        /// Saves any important data to a ConfigNode to be saved into Persistence, or null if nothing to save.
        /// Name of ConfigNode will be set by GetConfigName automatically
        /// </summary>
        /// <returns>ConfigNode containing data to save, or null</returns>
        ConfigNode SaveData();

        /// <summary>
        /// Loads the saved data into the FundingModifier.
        /// </summary>
        /// <param name="node">Source ConfigNode containing any pertinent information</param>
        void LoadData(ConfigNode node);


        /// <summary>
        /// Is true when the FM has a GUI for changing settings
        /// </summary>
        /// <returns>True if has a Settings GUI</returns>
        bool hasSettingsGUI();

        /// <summary>
        /// Draws the Settings GUI elements
        /// </summary>
        void DrawSettingsGUI();

        /// <summary>
        /// Is true if the FM has a *main* GUI that expands on the info in an Expense Report
        /// </summary>
        /// <returns>True if has a GUI</returns>
        bool hasMainGUI();

        /// <summary>
        /// Draws the main GUI, an extension of the Expense Report
        /// </summary>
        void DrawMainGUI();

        /// <summary>
        /// Called when some payment is put toward expenses on an invoice item generated from this funding modifier.
        /// </summary>
        /// <param name="sender">The invoice item.</param>
        /// <param name="args">Data about the payment.</param>
        void OnInvoicePaid(object sender, InvoiceItem.InvoicePaidEventArgs args);

        /// <summary>
        /// Called when an invoice is unpaid during a period.
        /// </summary>
        /// <param name="sender">The invoice item.</param>
        /// <param name="args">Empty event arguments, can be ignored.</param>
        void OnInvoiceUnpaid(object sender, EventArgs args);
    }
}
