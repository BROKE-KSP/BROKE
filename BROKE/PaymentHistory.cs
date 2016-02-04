using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;

namespace BROKE
{
    internal class PaymentHistory : IEnumerable
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

            public override string ToString()
            {
                return string.Format("{0}: {1}",
                    KSPUtil.PrintDate((int)Planetarium.GetUniversalTime(), true),
                    entry);
            }
        }

        private Queue<TimedPaymentHistory> entryHistory = new Queue<TimedPaymentHistory>();
        public PaymentHistory()
        {
        }

        internal void Record(PaymentEntry paymentEntry)
        {
            entryHistory.Enqueue(new TimedPaymentHistory(Planetarium.GetUniversalTime(), paymentEntry));
        }

        public IEnumerator GetEnumerator()
        {
            return entryHistory.Reverse().Where(entry => entry.entry.paymentOrRevenue != 0).GetEnumerator();
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