using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BROKE
{
    public abstract class LoanBase : IFundingModifier
    {
        [Persistent]
        private double loanValue;

        [Persistent]
        private double monthlyPayment;

        private bool disabled;

        protected LoanBase()
        {
        }

        protected LoanBase(double principal, double apr, double years)
        {
            var months = years * 12;
            if (apr != 0)
            {
                var i = apr / 12;
                monthlyPayment = principal * (i + (i / (Math.Pow(1 + i, months) - 1)));
            }
            else
                monthlyPayment = principal / months;
            loanValue = monthlyPayment * months;
        }

        public void DailyUpdate()
        {
        }

        public void DrawMainGUI()
        {
            throw new NotImplementedException();
        }

        public void DrawSettingsGUI()
        {
            throw new NotImplementedException();
        }

        public abstract string GetConfigName();

        public abstract string GetName();

        public bool hasMainGUI()
        {
            throw new NotImplementedException();
        }

        public bool hasSettingsGUI()
        {
            throw new NotImplementedException();
        }

        public void LoadData(ConfigNode node)
        {
            ConfigNode.LoadObjectFromConfig(this, node);
        }

        public void OnDisabled()
        {
            disabled = true;
        }

        public void OnEnabled()
        {
            disabled = false;
        }

        public void OnInvoicePaid(object sender, InvoiceItem.InvoicePaidEventArgs args)
        {
            loanValue -= args.AmountPaid;
        }

        public void OnInvoiceUnpaid(object sender, EventArgs args)
        {
            var invoice = (InvoiceItem)sender;
            //Add 5% of missed payment to remaining loan value
            loanValue += invoice.Expenses * 0.05;
        }

        public InvoiceItem ProcessQuarterly()
        {
            return new InvoiceItem(this, 0, CalculatePayment());
        }

        private double CalculatePayment()
        {
            return Math.Min(monthlyPayment, loanValue);
        }

        public InvoiceItem ProcessYearly()
        {
            return InvoiceItem.EmptyInvoice;
        }

        public ConfigNode SaveData()
        {
            return ConfigNode.CreateConfigFromObject(this);
        }
    }
}
