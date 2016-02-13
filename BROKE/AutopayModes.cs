using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BROKE
{
    public abstract class AutopayMode
    {
        /// <summary>
        /// The name of the autopay mode.  Describes the mode to the player.
        /// </summary>
        public abstract string Name { get; }
        /// <summary>
        /// Executed each quarter after invoices are collected.
        /// </summary>
        /// <returns>Should the expense report window be suppressed from showing up?</returns>
        public abstract bool Execute();

        /// <summary>
        /// Loads settings from persistence.  Automatically accounts for <code>[Persistent]</code> attributes.
        /// </summary>
        /// <param name="node">The ConfigNode containing the persisted settings information.</param>
        public virtual void OnLoad(ConfigNode node)
        {
            ConfigNode.LoadObjectFromConfig(this, node);
        }

        /// <summary>
        /// Saves settings to persistence.  Automatically accounts for <code>[Persistent]</code> attributes.
        /// </summary>
        public virtual ConfigNode OnSave()
        {
            return ConfigNode.CreateConfigFromObject(this, new ConfigNode("AutopaySettings"));
        }

        /// <summary>
        /// Draws UI on the settings window when this mode is selected.
        /// </summary>
        public virtual void DrawSettingsWindow()
        { }
    }

    public class Manual : AutopayMode
    {
        public override string Name
        {
            get
            {
                return "Manual Payment";
            }
        }
        public override bool Execute()
        {
            return false;
        }
    }

    public class AutoCollect : AutopayMode
    {
        public override string Name
        {
            get
            {
                return "Automatically Collect Revenues";
            }
        }

        public override bool Execute()
        {
            BROKE.Instance.CashInRevenues(BROKE.Instance.InvoiceItems);
            return false;
        }
    }

    public class AutoBalance : AutopayMode
    {
        public override string Name
        {
            get
            {
                return "Pay Expenses with Revenues";
            }
        }

        public override bool Execute()
        {
            var pendingRevenue = BROKE.Instance.PendingRevenue();
            BROKE.Instance.CashInRevenues(BROKE.Instance.InvoiceItems);
            BROKE.Instance.PayExpenses(BROKE.Instance.InvoiceItems, pendingRevenue);
            return true;
        }
    }

    public class FullAutoPay : AutopayMode
    {
        [Persistent]
        private double safetyMinimum;

        public override string Name
        {
            get
            {
                return "Withdraw Revenue and Pay Expenses In Full";
            }
        }

        public override bool Execute()
        {
            BROKE.Instance.CashInRevenues(BROKE.Instance.InvoiceItems);
            if (Funding.Instance != null && Funding.Instance.Funds > safetyMinimum)
            {
                BROKE.Instance.PayExpenses(BROKE.Instance.InvoiceItems); 
            }
            return true;
        }

        public override void DrawSettingsWindow()
        {
            base.DrawSettingsWindow();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Do not pay expenses when funds below:");
            double.TryParse(GUILayout.TextField(safetyMinimum.ToString()), out safetyMinimum);
            GUILayout.EndHorizontal();
        }
    }
}
