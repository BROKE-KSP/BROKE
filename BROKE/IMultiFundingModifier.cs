using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace BROKE
{
    public interface IMultiFundingModifier : IFundingModifierBase
    {
        /// <summary>
        /// Return the funds gained and lost for this Quarter. The returned values will be added/removed by B.R.O.K.E. so there is no need to do that yourself.
        /// </summary>
        /// <returns>Multiple InvoiceItems representing the quarterly invoice.</returns>
        IEnumerable<InvoiceItem> ProcessQuarterly();

        /// <summary>
        /// Return the funds gained and lost Yearly. The returned values will be added/removed by B.R.O.K.E. so there is no need to do that yourself.
        /// Note that this should NOT be the sum of the Quarterly income/expenses for the year. That's calculated by B.R.O.K.E. This is just for
        /// income/expenses that happen ONLY once a year
        /// </summary>
        /// <returns>Multiple InvoiceItems representing the yearly invoice.</returns>
        IEnumerable<InvoiceItem> ProcessYearly();
    }
}
