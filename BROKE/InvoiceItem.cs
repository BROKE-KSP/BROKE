using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BROKE
{
    public sealed class InvoiceItem : IPersistenceLoad
    {
        /// <summary>
        /// Only for (de-)serialization purposes.  DO NOT USE.
        /// </summary>
        public InvoiceItem()
        { }
        /// <summary>
        /// Constructor for creating an InvoiceItem.
        /// </summary>
        /// <param name="modifier">The IFundingModifier that generated this InvoiceItem.</param>
        /// <param name="revenue">The revenue tied to this invoice.</param>
        /// <param name="expenses">The expenses tied to this invoice.</param>
        /// <param name="reason">(Optional parameter) The category for the invoice.</param>
        public InvoiceItem(IFundingModifier modifier, double revenue, double expenses, TransactionReasons reason = TransactionReasons.None)
        {
            this.modifier = modifier;
            InvoiceName = modifier != null? modifier.GetName() : string.Empty;
            Revenue = revenue;
            Expenses = expenses;
            InvoiceReason = reason;
            RegisterEvents();
        }

        private IFundingModifier modifier;
        
        [Persistent]
        private string invoiceName;

        public string InvoiceName
        {
            get { return invoiceName; }
            private set { invoiceName = value; }
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

        internal void WithdrawRevenue()
        {
            Revenue = 0;
        }

        internal void PayInvoice(double amountPaid)
        {
            var handler = InvoicePaid;
            if (handler != null)
            {
                var args = new InvoicePaidEventArgs(Expenses, amountPaid);
                handler(this, args);
            }
            Expenses -= amountPaid;
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
            modifier = BROKE.Instance.fundingModifiers.FirstOrDefault(mod => mod.GetName() == InvoiceName);
            RegisterEvents();
        }

        private void RegisterEvents()
        {
            if (modifier != null)
            {
                InvoicePaid += modifier.OnInvoicePaid;
                InvoiceUnpaid += modifier.OnInvoiceUnpaid;
            }
        }

        public static readonly InvoiceItem EmptyInvoice = new InvoiceItem(null, 0, 0);
    }
}
