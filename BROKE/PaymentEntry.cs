namespace BROKE
{
    internal class PaymentEntry
    {
        [Persistent]
        public double time;
        [Persistent]
        public string invoiceName;
        [Persistent]
        public string modifierName;
        [Persistent]
        public double paymentOrRevenue;

        public PaymentEntry()
        {
        }

        public PaymentEntry(string invoiceName, string modifierName, double paymentOrRevenue, double time)
            :this()
        {
            this.paymentOrRevenue = paymentOrRevenue;
            this.modifierName = modifierName;
            this.invoiceName = invoiceName;
            this.time = time;
        }

        public override string ToString()
        {
            return string.Format("{3}: {0} for {1}, √{2}", invoiceName, modifierName, paymentOrRevenue, KSPUtil.PrintDate((int)time, true));
        }
    }
}