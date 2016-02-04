using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BROKE
{
    public sealed class InvoiceItem : IPersistenceLoad, IPersistenceSave
    {
        /// <summary>
        /// Only for (de-)serialization purposes.  DO NOT USE.
        /// </summary>
        public InvoiceItem()
        { }
        /// <summary>
        /// Constructor for creating an InvoiceItem.
        /// </summary>
        /// <param name="modifier">The IFundingModifierBase that generated this InvoiceItem.</param>
        /// <param name="revenue">The revenue tied to this invoice.</param>
        /// <param name="expenses">The expenses tied to this invoice.</param>
        /// <param name="reason">(Optional parameter) The category for the invoice.</param>
        public InvoiceItem(IFundingModifierBase modifier, double revenue, double expenses, string itemName = "", TransactionReasons reason = TransactionReasons.None)
        {
            Modifier = modifier;
            Revenue = revenue;
            Expenses = expenses;
            InvoiceReason = reason;
            ItemName = itemName;
            RegisterEvents();
        }

        public IFundingModifierBase Modifier { get; private set; }
        
        [Persistent]
        private string invoiceName;

        [Persistent]
        private string itemName;

        public string ItemName
        {
            get { return itemName; }
            set { itemName = value; }
        }


        [Persistent]
        private double revenue;

        public double Revenue
        {
            get { return revenue; }
            private set { revenue = value; }
        }

        [Persistent]
        private double expenses;

        public double Expenses
        {
            get { return expenses; }
            private set { expenses = value; }
        }

        [Persistent]
        private TransactionReasons reason;

        public TransactionReasons InvoiceReason
        {
            get { return reason; }
            private set { reason = value; }
        }


        public class InvoicePaidEventArgs : EventArgs
        {
            public InvoicePaidEventArgs(double expenses, double amountPaid)
            {
                AmountPaid = amountPaid;
                PaidInFull = AmountPaid >= expenses;
            }
            public double AmountPaid { get; private set; }
            public bool PaidInFull { get; private set; }
        }

        public event EventHandler<InvoicePaidEventArgs> InvoicePaid;

        internal PaymentEntry WithdrawRevenue()
        {
            var paidRevenue = Revenue;
            Revenue = 0;
            return new PaymentEntry(ItemName, Modifier.GetName(), paidRevenue);
        }

        internal PaymentEntry PayInvoice(double amountPaid)
        {
            var handler = InvoicePaid;
            if (handler != null)
            {
                var args = new InvoicePaidEventArgs(Expenses, amountPaid);
                handler(this, args);
            }
            Expenses -= amountPaid;
            return new PaymentEntry(ItemName, Modifier.GetName(), -amountPaid);
        }

        public event EventHandler<EventArgs> InvoiceUnpaid;

        internal void NotifyMissedPayment()
        {
            var handler = InvoiceUnpaid;
            if (handler != null)
                handler(this, EventArgs.Empty);
        }

        public void PersistenceLoad()
        {
            Modifier = BROKE.Instance.fundingModifiers.FirstOrDefault(mod => mod.GetConfigName() == invoiceName);
            RegisterEvents();
        }

        private void RegisterEvents()
        {
            if (Modifier != null)
            {
                InvoicePaid += Modifier.OnInvoicePaid;
                InvoiceUnpaid += Modifier.OnInvoiceUnpaid;
            }
        }

        public void PersistenceSave()
        {
            invoiceName = Modifier.GetConfigName();
        }

        public static readonly InvoiceItem EmptyInvoice = new InvoiceItem(null, 0, 0);
    }
}
