using System;
using System.Linq;
using System.Collections.Generic;

namespace BROKE
{
    internal class PaymentHistory
    {
        struct TimedPaymentHistory
        {
            [Persistent]
            public double time;
            [Persistent]
            public PaymentEntry entry;
            public TimedPaymentHistory(double time, PaymentEntry entry)
            {
                this.time = time;
                this.entry = entry;
            }
        }

        private Queue<TimedPaymentHistory> entryHistory = new Queue<TimedPaymentHistory>();
        public PaymentHistory()
        {
        }

        internal void Record(PaymentEntry paymentEntry)
        {
            UnityEngine.Debug.Log(string.Format("Recording transaction: {0} for {1}, √{2}", paymentEntry.invoiceName, paymentEntry.modifierName, paymentEntry.paymentOrRevenue));
            entryHistory.Enqueue(new TimedPaymentHistory(Planetarium.GetUniversalTime(), paymentEntry));
        }

        internal void ClearOneYearAgo()
        {
            while (Planetarium.GetUniversalTime() - entryHistory.Peek().time > KSPUtil.Year)
            {
                entryHistory.Dequeue();
            }
        }

        internal ConfigNode OnSave()
        {
            var node = BROKE_Data.ListToConfigNode(entryHistory.ToList());
            node.name = "PaymentHistory";
            return node;
        }

        internal void OnLoad(ConfigNode node)
        {
            entryHistory = new Queue<TimedPaymentHistory>(BROKE_Data.ConfigNodeToList<TimedPaymentHistory>(node));
        }
    }
}