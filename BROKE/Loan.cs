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

        public Loan(Agent offeringAgency, string name, double principal, double apr, double years, double latePenaltyRate = 0.05)
        {
            if (principal < 0)
                throw new ArgumentOutOfRangeException("principal");
            if (apr < 0)
                throw new ArgumentOutOfRangeException("apr");
            if (years < 0)
                throw new ArgumentOutOfRangeException("years");
            Agency = offeringAgency;
            var quarters = years * 4;
            if (apr != 0)
            {
                var i = apr / 12;
                quarterlyPayment = principal * (i + (i / (Math.Pow(1 + i, quarters) - 1)));
            }
            else
                quarterlyPayment = principal / quarters;
            remainingPayment = quarterlyPayment * quarters;
            penaltyRate = latePenaltyRate;
            this.name = name;
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