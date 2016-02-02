using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BROKE
{
    public class LoanManager : IMultiFundingModifier
    {
        private Dictionary<string, Loan> currentLoans = new Dictionary<string, Loan>();

        public void AddLoan(Loan newLoan)
        {
            if (CanAddLoan(newLoan)) currentLoans.Add(newLoan.GetName(), newLoan);
            else throw new InvalidOperationException("There is already a loan issued with this name.");
        }

        public bool CanAddLoan(Loan loan)
        {
            //Need unique because this is how the manager ids loans in sending the payments back.
            return !currentLoans.ContainsKey(loan.GetName());
        }

        public void DailyUpdate()
        {
        }

        public void DrawMainGUI()
        {
        }

        public void DrawSettingsGUI()
        {
        }

        public string GetConfigName()
        {
            return "LoanManager";
        }

        public string GetName()
        {
            return "Loan Payments";
        }

        public bool hasMainGUI()
        {
            return false;
        }

        public bool hasSettingsGUI()
        {
            return false;
        }

        public void LoadData(ConfigNode node)
        {
            var loanData = BROKE_Data.ConfigNodeToList<Loan>(node);
            currentLoans = loanData.ToDictionary(loan => loan.GetName(), loan => loan);
        }

        public void OnDisabled()
        {
        }

        public void OnEnabled()
        {
        }

        public void OnInvoicePaid(object sender, InvoiceItem.InvoicePaidEventArgs args)
        {
            InvoiceItem item = (InvoiceItem)sender;
            currentLoans.Single(loan => loan.Value.GetName() == item.ItemName).Value.Pay(args.AmountPaid);
        }

        public void OnInvoiceUnpaid(object sender, EventArgs args)
        {
            InvoiceItem item = (InvoiceItem)sender;
            currentLoans.Single(loan => loan.Value.GetName() == item.ItemName).Value.AddLatePenalty(item.Expenses);
        }

        public IEnumerable<InvoiceItem> ProcessQuarterly()
        {
            return currentLoans.Select(loan => new InvoiceItem(this, 0, loan.Value.GetNextPaymentAmount(), loan.Key, TransactionReasons.StrategyInput));
        }

        public IEnumerable<InvoiceItem> ProcessYearly()
        {
            return Enumerable.Empty<InvoiceItem>();
        }

        public ConfigNode SaveData()
        {
            return BROKE_Data.ListToConfigNode(currentLoans.Values.ToList());
        }
    }
}
