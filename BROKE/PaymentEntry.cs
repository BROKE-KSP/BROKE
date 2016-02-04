namespace BROKE
{
    internal struct PaymentEntry
    {
        [Persistent]
        public string invoiceName;
        [Persistent]
        public string modifierName;
        [Persistent]
        public double paymentOrRevenue;

        public PaymentEntry(string invoiceName, string modifierName, double paymentOrRevenue)
            :this()
        {
            this.paymentOrRevenue = paymentOrRevenue;
            this.modifierName = modifierName;
            this.invoiceName = invoiceName;
        }

        public override string ToString()
        {
            return string.Format("{0} for {1}, √{2}", invoiceName, modifierName, paymentOrRevenue);
        }
    }
}