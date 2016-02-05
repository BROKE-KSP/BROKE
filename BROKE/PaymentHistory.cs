using System;
using System.Linq;
using System.Collections.Generic;
using System.Collections;

namespace BROKE
{
    internal class PaymentHistory
    {
        private Queue<PaymentEntry> entryHistory = new Queue<PaymentEntry>();
        public PaymentHistory()
        {
        }

        internal void Record(PaymentEntry paymentEntry)
        {
            if (paymentEntry.paymentOrRevenue != 0)
            {

                entryHistory.Enqueue(paymentEntry); 
            }
        }

        public IEnumerator<PaymentEntry> GetEnumerator()
        {
            return entryHistory.Reverse().Where(entry => entry.paymentOrRevenue != 0).GetEnumerator();
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
            entryHistory = new Queue<PaymentEntry>(BROKE_Data.ConfigNodeToList<PaymentEntry>(node));
        }
    }
}