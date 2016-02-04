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
    }
}