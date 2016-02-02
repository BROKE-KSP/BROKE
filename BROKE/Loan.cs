using System;
using System.Linq;
using Contracts.Agents;

namespace BROKE
{
    public class Loan : IPersistenceSave, IPersistenceLoad
    {
        public Loan()
        {
        }

        public Loan(Agent offeringAgency, string name, double principal, double interest, double years, double latePenaltyRate = 0.05)
        {
            if (principal < 0)
                throw new ArgumentOutOfRangeException("principal");
            if (interest < 0)
                throw new ArgumentOutOfRangeException("interest");
            if (years < 0)
                throw new ArgumentOutOfRangeException("years");
            Agency = offeringAgency;
            var quarters = years * 4;
            if (interest != 0)
            {
                var quarterlyRate = interest / 4;
                quarterlyPayment = principal * (quarterlyRate / (1 - Math.Pow(1 + quarterlyRate, -quarters)));
            }
            else
                quarterlyPayment = principal / quarters;
            remainingPayment = quarterlyPayment * quarters;
            penaltyRate = latePenaltyRate;
            this.name = name;
            this.principal = principal;
        }

        public Agent Agency { get; private set; }
        [Persistent]
        private string agentName;
        [Persistent]
        private double remainingPayment;
        [Persistent]
        private double quarterlyPayment;
        [Persistent]
        private double penaltyRate;
        [Persistent]
        private string name;

        [Persistent]
        private double principal;

        public double GetPrincipal()
        {
            return principal;
        }

        public string GetName()
        {
            return name;
        }
        

        public double GetNextPaymentAmount()
        {
            return Math.Min(quarterlyPayment, remainingPayment);
        }

        internal void Pay(double amount)
        {
            remainingPayment -= amount;
        }

        internal void AddLatePenalty(double unpaidAmount)
        {
            remainingPayment += unpaidAmount * penaltyRate;
        }

        public void PersistenceLoad()
        {
            Agency = AgentList.Instance.Agencies.First(agent => agent.Name == agentName);
        }

        public void PersistenceSave()
        {
            agentName = Agency.Name;
        }
    }
}