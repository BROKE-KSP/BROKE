using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSPPluginFramework;
using System.Reflection;

namespace BROKE
{
    
    //Utilizes the amazing KSP Plugin Framework by TriggerAU to make things much easier
    //http://forum.kerbalspaceprogram.com/threads/66503-KSP-Plugin-Framework-Plugin-Examples-and-Structure-v1-1-(Apr-6)

    [KSPAddon((KSPAddon.Startup.SpaceCentre), false)]
    public class BROKE_SpaceCenter : BROKE { }
    [KSPAddon((KSPAddon.Startup.TrackingStation), false)]
    public class BROKE_TrackingStation : BROKE { }
    [KSPAddon((KSPAddon.Startup.EditorAny), false)]
    public class BROKE_Editor : BROKE { }
    [KSPAddon((KSPAddon.Startup.Flight), false)]
    public class BROKE_Flight : BROKE { }

    public class BROKE : MonoBehaviourWindow
    {
        public int sPerDay = KSPUtil.Day, sPerYear = KSPUtil.Year, sPerQuarter = KSPUtil.Year / 4;
        private int LastUT = -1;

        public List<IFundingModifier> fundingModifiers;
        private Dictionary<string, double> Revenue, Expenses;

        internal override void Start()
        {
            LogFormatted_DebugOnly("Printing names of all classes implementing IFundingModifier:");
            fundingModifiers = GetFundingModifiers();
            foreach (IFundingModifier fundMod in fundingModifiers)
            {
                LogFormatted_DebugOnly(fundMod.GetName());
            }
        }

        internal override void FixedUpdate()
        {
            //Check the time and see if a new day, quarter, year has started and if so trigger the appropriate calls
            //We'll do this with the modulus function, rather than tracking the amount of time that has passed since the last update
            bool doExpenseReport = false;
            int UT = (int)Planetarium.GetUniversalTime();
            if (LastUT < 0)
                LastUT = UT;
            if (UT % sPerDay < LastUT % sPerDay) //if UT and LastUT are same day, then UT % sPerDay is > LastUT % sPerDay. If they're different days, then UT % sPerDay will be < LastUT % sPerDay
            {
                //new day
                NewDay();
            }
            if (UT % sPerQuarter < LastUT % sPerQuarter)
            {
                //New quarter
                NewQuarter();
                doExpenseReport = true;
            }
            if (UT % sPerYear < LastUT % sPerYear)
            {
                //New quarter
                NewYear();
            }

            LastUT = UT;

            if (doExpenseReport)
            {
                DisplayExpenseReport();
            }
        }

        internal override void DrawWindow(int id)
        {
            
        }



        public void NewDay()
        {
            //Update every FundingModifier
            LogFormatted_DebugOnly("New Day! " + KSPUtil.PrintDate((int)Planetarium.GetUniversalTime(), true, true));
            foreach(IFundingModifier fundingMod in fundingModifiers)
            {
                fundingMod.DailyUpdate();
            }
        }

        public void NewQuarter()
        {
            //Calculate quarterly expenses and display expense report (unless also a new year)
            LogFormatted_DebugOnly("New Quarter! " + KSPUtil.PrintDate((int)Planetarium.GetUniversalTime(), true, true));
            ResetFundingDictionaries();
            foreach (IFundingModifier fundingMod in fundingModifiers)
            {
                double[] results = fundingMod.ProcessQuarterly();
                AddOrCreateInDictionary(Revenue, fundingMod.GetName(), results[0]);
                AddOrCreateInDictionary(Expenses, fundingMod.GetName(), results[1]);
            }
        }

        public void NewYear()
        {
            //Calculate yearly expenses and display expense report
            LogFormatted_DebugOnly("New Year! " + KSPUtil.PrintDate((int)Planetarium.GetUniversalTime(), true, true));
            foreach (IFundingModifier fundingMod in fundingModifiers)
            {
                double[] results = fundingMod.ProcessYearly();
                AddOrCreateInDictionary(Revenue, fundingMod.GetName(), results[0]);
                AddOrCreateInDictionary(Expenses, fundingMod.GetName(), results[1]);
            }
        }

        private void ResetFundingDictionaries()
        {
            Revenue = new Dictionary<string, double>();
            Expenses = new Dictionary<string, double>();

            foreach (IFundingModifier fundMod in fundingModifiers)
            {
                Revenue.Add(fundMod.GetName(), 0);
                Expenses.Add(fundMod.GetName(), 0);
            }
        }

        private void DisplayExpenseReport()
        {
            Debug.Log("Revenue:");
            foreach (KeyValuePair<string, double> kvp in Revenue)
                Debug.Log(kvp.Key + ": " + kvp.Value);

            Debug.Log("Expenses:");
            foreach (KeyValuePair<string, double> kvp in Expenses)
                Debug.Log(kvp.Key + ": " + kvp.Value);
        }

        //Shamelessly modified from this StackOverflow question/answer: http://stackoverflow.com/questions/5411694/get-all-inherited-classes-of-an-abstract-class
        //http://stackoverflow.com/questions/26733/getting-all-types-that-implement-an-interface
        public List<IFundingModifier> GetFundingModifiers()
        {
            /*List<IFundingModifier> objects = new List<IFundingModifier>();
            foreach (Type type in
                Assembly.GetAssembly(typeof(IFundingModifier)).GetTypes()
                .Where(myType => myType.IsClass && !myType.IsInterface && (myType is IFundingModifier)))
            {
                objects.Add((IFundingModifier)Activator.CreateInstance(type));
            }
            objects.Sort();
            return objects;*/

            Type type = typeof(IFundingModifier);
            IEnumerable<Type> types = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(s => s.GetTypes())
                .Where(p => type.IsAssignableFrom(p) && !p.IsInterface);
            List<IFundingModifier> fundingMods = new List<IFundingModifier>();

            foreach (Type t in types)
            {
                fundingMods.Add(Activator.CreateInstance(t) as IFundingModifier);
            }

            return fundingMods;
        }

        public static void AddOrCreateInDictionary(Dictionary<string, double> dict, string key, double value)
        {
            if (!dict.ContainsKey(key))
                dict.Add(key, value);
            else
                dict[key] += value;
        }
    }
}
