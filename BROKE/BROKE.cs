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
        public static BROKE Instance;

        public int sPerDay = KSPUtil.Day, sPerYear = KSPUtil.Year, sPerQuarter = KSPUtil.Year / 4;
        private int LastUT = -1;

        public List<IFundingModifier> fundingModifiers;
        private Dictionary<string, double> Revenue = new Dictionary<string, double>(), Expenses = new Dictionary<string, double>();
        public List<string> disabledFundingModifiers = new List<string>();
        public double RemainingDebt = 0;

        private int WindowWidth = 360, WindowHeight = 540;
        private bool DrawSettings = false;

        private ApplicationLauncherButton button;

        private List<string> skins = new List<string> { "KSP", "Unity", "Mixed" };
        public int SelectedSkin = 0;

        internal override void Start()
        {
            if (Instance != null)
            {
                Destroy(Instance);
            }
            Instance = this;
            LogFormatted_DebugOnly("Printing names of all classes implementing IFundingModifier:");
            fundingModifiers = GetFundingModifiers();
            foreach (IFundingModifier fundMod in fundingModifiers)
            {
                LogFormatted_DebugOnly(fundMod.GetName());
            }

            if (ApplicationLauncher.Instance != null && ApplicationLauncher.Ready)
                OnAppLauncherReady();
            else
                GameEvents.onGUIApplicationLauncherReady.Add(OnAppLauncherReady);
        }

        internal override void OnDestroy()
        {
           // GameEvents.onGUIApplicationLauncherReady.Remove(OnAppLauncherReady);
            ApplicationLauncher.Instance.RemoveModApplication(button);
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
                ProcessExpenseReport();
            }
        }

        public void ProcessExpenseReport()
        {
            CashInRevenues();
            UpdateRemainingDebt();
            double totalRevenue = 0;
            Revenue.Values.ToList().ForEach(delegate(double val) { totalRevenue += val; });
            PayExpenses(totalRevenue);
           // DisplayExpenseReport();
            button.SetTrue();
        }

        internal override void DrawWindow(int id)
        {
            if (DrawSettings)
                DrawSettingsWindow();
            else
                DrawExpenseReportWindow();
        }


        void OnAppLauncherReady()
        {
            if (button != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(button);
                button = null;
            }
            this.button = ApplicationLauncher.Instance.AddModApplication(
                DisplayExpenseReport,
                () => { this.Visible = false; },
                null,
                null,
                null,
                null,
                ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.TRACKSTATION,
                (Texture)GameDatabase.Instance.GetTexture("BROKE/Textures/icon_button_stock", false));
        }


        private string payAmountTxt = "-1";
        private IFundingModifier selectedMainFM;
        private Vector2 revenueScroll, expenseScroll, customScroll;
        public void DrawExpenseReportWindow()
        {
            GUIStyle redText = new GUIStyle(GUI.skin.label);
            redText.normal.textColor = Color.red;
            GUIStyle yellowText = new GUIStyle(GUI.skin.label);
            yellowText.normal.textColor = Color.yellow;
            GUIStyle greenText = new GUIStyle(GUI.skin.label);
            greenText.normal.textColor = Color.green;

            GUIStyle redText2 = new GUIStyle(GUI.skin.textArea);
            redText2.normal.textColor = Color.red;
            GUIStyle yellowText2 = new GUIStyle(GUI.skin.textArea);
            yellowText2.normal.textColor = Color.yellow;
            GUIStyle greenText2 = new GUIStyle(GUI.skin.textArea);
            greenText2.normal.textColor = Color.green;

            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical(GUILayout.Width(WindowWidth));
            GUILayout.Label("Revenue", yellowText);
            //Wrap this in a scrollbar
            revenueScroll = GUILayout.BeginScrollView(revenueScroll, SkinsLibrary.CurrentSkin.textArea);
            double totalRevenue = 0;
            foreach (IFundingModifier FM in fundingModifiers)
            {
                double revenueForFM = 0;
                if (Revenue.ContainsKey(FM.GetName()))
                    revenueForFM = Revenue[FM.GetName()];
                
                totalRevenue += revenueForFM;
                if (revenueForFM != 0)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(FM.GetName(), SkinsLibrary.CurrentSkin.textArea, GUILayout.Width(WindowWidth/2));
                    GUILayout.Label("√" + revenueForFM.ToString("N"), greenText2); //GREEN
                    if (FM.hasMainGUI())
                    {
                        if (selectedMainFM != FM && GUILayout.Button("→", GUILayout.ExpandWidth(false)))
                        {
                            //tell it to display Main stuff for this in a new vertical
                            selectedMainFM = FM;
                            this.WindowRect.width *= 2;
                            //widen the window probably
                        }
                        if (selectedMainFM != null && selectedMainFM == FM && GUILayout.Button("←", GUILayout.ExpandWidth(false)))
                        {
                            //hide side window
                            selectedMainFM = null;
                            this.WindowRect.width /= 2;
                            //maybe reset the width here then
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Total Revenue: ");
            GUILayout.Label("√" + totalRevenue.ToString("N"), greenText);
            GUILayout.EndHorizontal();

            GUILayout.Label("Expenses", yellowText);
            //Wrap this in a scrollbar
            expenseScroll = GUILayout.BeginScrollView(expenseScroll, SkinsLibrary.CurrentSkin.textArea);
            double totalExpenses = 0;
            foreach (IFundingModifier FM in fundingModifiers)
            {
                double expenseForFM = 0;
                if (Expenses.ContainsKey(FM.GetName()))
                    expenseForFM = Expenses[FM.GetName()];
                totalExpenses += expenseForFM;
                if (expenseForFM != 0)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(FM.GetName(), SkinsLibrary.CurrentSkin.textArea, GUILayout.Width(WindowWidth / 2));
                    GUILayout.Label("√" + expenseForFM.ToString("N"), redText2); //RED
                    if (FM.hasMainGUI())
                    {
                        if (selectedMainFM != FM && GUILayout.Button("→", GUILayout.ExpandWidth(false)))
                        {
                            //tell it to display Main stuff for this in a new vertical
                            selectedMainFM = FM;
                            this.WindowRect.width *= 2;
                            //widen the window probably
                        }
                        if (selectedMainFM != null && selectedMainFM == FM && GUILayout.Button("←", GUILayout.ExpandWidth(false)))
                        {
                            //hide side window
                            selectedMainFM = null;
                            this.WindowRect.width /= 2;
                            //maybe reset the width here then
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();
            GUILayout.BeginHorizontal();
            GUILayout.Label("Total Expenses: ");
            GUILayout.Label("√" + totalExpenses.ToString("N"), redText);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Net Revenue: ", yellowText);
            GUILayout.Label("√" + Math.Abs(totalRevenue - totalExpenses).ToString("N"), (totalRevenue - totalExpenses) < 0 ? redText : greenText);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            GUILayout.Label("Remaining Debt: ", yellowText);
            GUILayout.Label("√"+RemainingDebt.ToString("N"), RemainingDebt != 0 ? redText : greenText);
            GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(((Texture)GameDatabase.Instance.GetTexture("BROKE/Textures/gear", false)), GUILayout.ExpandWidth(false)))
            {
                DrawSettings = true;
            }
            payAmountTxt = GUILayout.TextField(payAmountTxt);
            if (GUILayout.Button("Pay", GUILayout.ExpandWidth(false)))
            {
                double toPay;
                if (!double.TryParse(payAmountTxt, out toPay))
                {
                    toPay = -1;
                    payAmountTxt = "-1";
                }
                toPay = Math.Min(toPay, RemainingDebt);
                RemainingDebt = PayExpenses(toPay);
            }
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            //draw main window for the selected FM
            if (selectedMainFM != null)
            {
                GUILayout.BeginVertical(GUILayout.Width(WindowWidth));
                customScroll = GUILayout.BeginScrollView(customScroll);
                //Put it in a scrollview as well
                selectedMainFM.DrawMainGUI();
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }

        public void DrawSettingsWindow()
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            foreach (IFundingModifier FM in fundingModifiers)
            {
                GUILayout.BeginHorizontal(SkinsLibrary.CurrentSkin.textArea);
                GUILayout.Label(FM.GetName(), GUILayout.Width(WindowWidth / 2));
                bool disabled = FMDisabled(FM);
                if (GUILayout.Button(disabled ? "Enable" : "Disable"))
                {
                    if (disabled)
                        EnableFundingModifier(FM);
                    else
                        DisableFundingModifier(FM);
                }
                if (FM.hasSettingsGUI())
                {
                    if (selectedMainFM != FM && GUILayout.Button("→", GUILayout.ExpandWidth(false)))
                    {
                        //tell it to display Main stuff for this in a new vertical
                        selectedMainFM = FM;
                        this.WindowRect.width *= 2;
                        //widen the window probably
                    }
                    if (selectedMainFM != null && selectedMainFM == FM && GUILayout.Button("←", GUILayout.ExpandWidth(false)))
                    {
                        //hide side window
                        selectedMainFM = null;
                        this.WindowRect.width /= 2;
                        //maybe reset the width here then
                    }
                }
                GUILayout.EndHorizontal();
            }

            if (GUILayout.Button("Change Skin"))
            {
                //SkinsLibrary.SetCurrent(SkinsLibrary.List.Keys.ElementAt((SkinsLibrary.List.Values.ToList().IndexOf(SkinsLibrary.CurrentSkin) + 1)%SkinsLibrary.List.Count));
                SelectedSkin++;
                SelectedSkin %= skins.Count;
                SelectSkin(SelectedSkin);
            }

            GUILayout.EndVertical();


            //draw main window for the selected FM
            if (selectedMainFM != null)
            {
                GUILayout.BeginVertical(GUILayout.Width(WindowWidth));
                customScroll = GUILayout.BeginScrollView(customScroll);
                //Put it in a scrollview as well
                selectedMainFM.DrawSettingsGUI();
                GUILayout.EndScrollView();
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }

        public void SelectSkin(int skinID)
        {
            if (skinID == 0)
                SkinsLibrary.SetCurrent(SkinsLibrary.DefSkinType.KSP);
            else if (skinID == 1)
                SkinsLibrary.SetCurrent(SkinsLibrary.DefSkinType.Unity);
            else if (skinID == 2)
            {
                if (!SkinsLibrary.SkinExists("Mixed"))
                {
                    GUISkin mixedSkin = SkinsLibrary.CopySkin(SkinsLibrary.DefSkinType.Unity);
                    mixedSkin.window = HighLogic.Skin.window;
                    SkinsLibrary.AddSkin("Mixed", mixedSkin, false);
                }
                SkinsLibrary.SetCurrent("Mixed");
            }

            SelectedSkin = skinID;
        }

        public bool FMDisabled(IFundingModifier toCheck)
        {
            return disabledFundingModifiers.Contains(toCheck.GetConfigName());
        }

        public bool FMDisabled(string toCheck)
        {
            return disabledFundingModifiers.Contains(toCheck);
        }

        public void DisableFundingModifier(IFundingModifier toDisable)
        {
            disabledFundingModifiers.AddUnique(toDisable.GetConfigName());
            toDisable.OnDisabled();
        }

        public void EnableFundingModifier(IFundingModifier toEnable)
        {
            disabledFundingModifiers.Remove(toEnable.GetConfigName());
            toEnable.OnEnabled();
        }

        public void NewDay()
        {
            //Update every FundingModifier
            LogFormatted_DebugOnly("New Day! " + KSPUtil.PrintDate((int)Planetarium.GetUniversalTime(), true, true));
            foreach(IFundingModifier fundingMod in fundingModifiers)
            {
                if (!FMDisabled(fundingMod))
                {
                    fundingMod.DailyUpdate();
                }
            }
        }

        public void NewQuarter()
        {
            //Calculate quarterly expenses and display expense report (unless also a new year)
            LogFormatted_DebugOnly("New Quarter! " + KSPUtil.PrintDate((int)Planetarium.GetUniversalTime(), true, true));
            ResetFundingDictionaries();
            foreach (IFundingModifier fundingMod in fundingModifiers)
            {
                if (!FMDisabled(fundingMod))
                {
                    double[] results = fundingMod.ProcessQuarterly();
                    AddOrCreateInDictionary(Revenue, fundingMod.GetName(), results[0]);
                    AddOrCreateInDictionary(Expenses, fundingMod.GetName(), results[1]);
                }
            }
        }

        public void NewYear()
        {
            //Calculate yearly expenses and display expense report
            LogFormatted_DebugOnly("New Year! " + KSPUtil.PrintDate((int)Planetarium.GetUniversalTime(), true, true));
            foreach (IFundingModifier fundingMod in fundingModifiers)
            {
                if (!FMDisabled(fundingMod))
                {
                    double[] results = fundingMod.ProcessYearly();
                    AddOrCreateInDictionary(Revenue, fundingMod.GetName(), results[0]);
                    AddOrCreateInDictionary(Expenses, fundingMod.GetName(), results[1]);
                }
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
            DrawSettings = false;
            Debug.Log("Revenue:");
            foreach (KeyValuePair<string, double> kvp in Revenue)
                Debug.Log(kvp.Key + ": " + kvp.Value);

            Debug.Log("Expenses:");
            foreach (KeyValuePair<string, double> kvp in Expenses)
                Debug.Log(kvp.Key + ": " + kvp.Value);

            this.Visible = true;
            this.DragEnabled = true;
            this.WindowRect.Set((Screen.width - WindowWidth) / 2, (Screen.height - WindowHeight) / 2, WindowWidth, WindowHeight);
            this.WindowCaption = "B.R.O.K.E.";
            //SkinsLibrary.SetCurrent(SkinsLibrary.DefSkinType.Unity);
        }

        public void CashInRevenues()
        {
            foreach (KeyValuePair<string, double> kvp in Revenue)
            {
                AdjustFunds(kvp.Value);
            }
        }

        public double UpdateRemainingDebt()
        {
            foreach (KeyValuePair<string, double> kvp in Expenses)
            {
                RemainingDebt += kvp.Value;
            }

            return RemainingDebt;
        }

        public double PayExpenses(double MaxToPay = -1)
        {
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER) //No funds outside of career
                return 0;

            double toPay = 0;
            if (MaxToPay < 0) //Pay anything we can
            {
                //pay the minimum of the remaining funds and the amount we owe
                toPay = Math.Min(RemainingDebt, Funding.Instance.Funds);
            }
            else //Pay the minimum of the amount we have, the amount we're willing to pay, and the amount we owe
            {
                toPay = Math.Min(MaxToPay, Funding.Instance.Funds);
                toPay = Math.Min(toPay, RemainingDebt);
            }
            AdjustFunds(-toPay);
            RemainingDebt -= toPay;
            return RemainingDebt;
        }

        //Shamelessly modified from this StackOverflow question/answer: http://stackoverflow.com/questions/5411694/get-all-inherited-classes-of-an-abstract-class
        //http://stackoverflow.com/questions/26733/getting-all-types-that-implement-an-interface
        public List<IFundingModifier> GetFundingModifiers()
        {
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

        public static double AdjustFunds(double fundsToAdd, TransactionReasons reason = TransactionReasons.None)
        {
            if (HighLogic.CurrentGame.Mode == Game.Modes.CAREER)
            {
                Funding.Instance.AddFunds(fundsToAdd, reason);
                return Funding.Instance.Funds;
            }
            return 0;
        }
    }
}
