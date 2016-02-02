using System;
using System.Collections.Generic;

namespace BROKE
{
    public class LoanGenerator : IPersistenceLoad
    {
        private const float ReputationMax = 1000;
        private Random rng = new Random();
        private const float aprYearScale = 1.05f;
        private const float aprPrincipalScale = 1.00005f;

        [Persistent]
        public string agencyName;

        [Persistent]
        public string namesCsv;

        public string[] possibleNames;
        [Persistent]
        public double minimumRequiredCapital;
        [Persistent]
        public float minimumReputation;
        [Persistent]
        public double minimumInterest;
        [Persistent]
        public double maximumInterest;
        [Persistent]
        public double minimumPrincipal;
        [Persistent]
        public double maximumPrincipal;
        [Persistent]
        public int minimumYears;
        [Persistent]
        public int maximumYears;

        public Loan GenerateLoan()
        {
            UnityEngine.Debug.Log("Generating a loan");
            var currentFunds = Funding.Instance.Funds;
            var currentReputation = Reputation.Instance.reputation;
            if (currentFunds < minimumRequiredCapital)
                return null;
            if (currentReputation < minimumReputation)
                return null;
            UnityEngine.Debug.Log("Minimum required funds and reputation satisfied");

            var repRatio = (ReputationMax - currentReputation) / (ReputationMax - minimumReputation);
            var aprRange = maximumInterest - minimumInterest;
            var years = rng.Next(minimumYears, maximumYears);
            var unshiftedYear = (years - minimumYears) / (float)(maximumYears - minimumYears);
            var principalRange = maximumPrincipal - minimumPrincipal;
            var unshiftedPrincipal = rng.NextDouble() * principalRange;
            var principal = unshiftedPrincipal + minimumPrincipal;
            var apr = aprRange * repRatio * (Math.Pow(aprYearScale, unshiftedYear)) * (Math.Pow(aprPrincipalScale, unshiftedPrincipal)) + minimumInterest;
            if (apr > maximumInterest) apr = maximumInterest;
            return new Loan(Contracts.Agents.AgentList.Instance.GetAgent(agencyName),
                string.Format("{0}, √{1:0} @{2:0.00}% Interest for {3} years", possibleNames[rng.Next(possibleNames.Length)], principal, apr, years)
                , principal, apr, years);
        }

        public void PersistenceLoad()
        {
            possibleNames = namesCsv.Split(',');
            for (int i = 0; i < possibleNames.Length; i++)
            {
                possibleNames[i] = possibleNames[i].Trim();
            }
        }
    }
}