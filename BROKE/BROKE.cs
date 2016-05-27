using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSPPluginFramework;
using System.Reflection;
using KSP.UI.Screens;

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
        private enum BROKEView
        {
            ExpenseReport,
            Settings,
            History
        }

        public static BROKE Instance;

        public static long sPerDay = KSPUtil.dateTimeFormatter.Day, sPerYear = KSPUtil.dateTimeFormatter.Year, sPerQuarter = KSPUtil.dateTimeFormatter.Year / 4;
        private long LastUT = -1;

        public List<IMultiFundingModifier> fundingModifiers = new List<IMultiFundingModifier>();
        internal readonly List<InvoiceItem> InvoiceItems = new List<InvoiceItem>();
        public List<string> disabledFundingModifiers = new List<string>();
        internal PaymentHistory paymentHistory = new PaymentHistory();
        private BROKEView currentView;
        internal Dictionary<string, AutopayMode> autopayModes;
        internal AutopayMode currentAutopayMode = new Manual(); // Default to manual pay

        private Vector2 revenueScroll, expenseScroll, customScroll, historyScroll;

        public double RemainingDebt()
        {
            return InvoiceItems.Sum(item => item.Expenses);
        }

        public double PendingRevenue()
        {
            return InvoiceItems.Sum(item => item.Revenue);
        }

        private int WindowWidth = 360, WindowHeight = 540;

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
            LogFormatted_DebugOnly("Printing names of all classes implementing IMultiFundingModifier and IFundingMultiplier:");
            fundingModifiers = GetFundingModifiers();
            foreach (IMultiFundingModifier fundMod in fundingModifiers)
            {
                LogFormatted_DebugOnly(fundMod.GetName());
            }
            LogFormatted_DebugOnly("Printing names of all current autopay modes:");
            // Using a dictionary for faster lookup when setting modes
            autopayModes = GetInstanceOfAllImplementingClasses<AutopayMode>().ToDictionary(mode => mode.Name, mode => mode);
            foreach (var mode in autopayModes)
            {
                LogFormatted_DebugOnly(mode.Key);
            }
            if (ApplicationLauncher.Instance != null && ApplicationLauncher.Ready)
                OnAppLauncherReady();
            else
                GameEvents.onGUIApplicationLauncherReady.Add(OnAppLauncherReady);

        }

        internal override void OnDestroy()
        {
            GameEvents.onGUIApplicationLauncherReady.Remove(OnAppLauncherReady);
            ApplicationLauncher.Instance.RemoveModApplication(button);
        }


        internal override void FixedUpdate()
        {
            //Check the time and see if a new day, quarter, year has started and if so trigger the appropriate calls
            //We'll do this with the modulus function, rather than tracking the amount of time that has passed since the last update
            bool doExpenseReport = false;
            var UT = (long)Planetarium.GetUniversalTime();
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
                InvoiceItems.ForEach(item => item.NotifyMissedPayment());
                NewQuarter();
                doExpenseReport = true;
            }
            if (UT % sPerYear < LastUT % sPerYear)
            {
                //New year
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
            if (!currentAutopayMode.Execute())
            {
                button.SetTrue(); 
            }
        }

        internal override void DrawWindow(int id)
        {
            switch (currentView)
            {
                case BROKEView.ExpenseReport:
                    DrawExpenseReportWindow();
                    break;
                case BROKEView.Settings:
                    DrawSettingsWindow();
                    break;
                case BROKEView.History:
                    DrawPaymentHistoryWindow();
                    break;
                default:
                    break;
            }
        }

        private void DrawPaymentHistoryWindow()
        {
            historyScroll = GUILayout.BeginScrollView(historyScroll);
            GUILayout.BeginVertical();
            foreach (var paymentItem in paymentHistory)
            {
                GUILayout.Label(paymentItem.ToString());
            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
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
                HideWindow,
                null,
                null,
                null,
                null,
                ApplicationLauncher.AppScenes.SPACECENTER | ApplicationLauncher.AppScenes.FLIGHT | ApplicationLauncher.AppScenes.TRACKSTATION,
                (Texture)GameDatabase.Instance.GetTexture("BROKE/Textures/icon_button_stock", false));
        }

        public void HideWindow() //Deselect the active FM when the window is closed
        {
            this.Visible = false;
            selectedMainFM = null;
        }

        private string payAmountTxt = "Pay Maximum";
        private IMultiFundingModifier selectedMainFM;
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
            GUILayout.BeginHorizontal();
            GUILayout.Label("Revenue", yellowText);
            if (GUILayout.Button("Collect", GUILayout.ExpandWidth(false)))
            {
                CashInRevenues(InvoiceItems);
            }
            GUILayout.EndHorizontal();
            double totalRevenue = DisplayCategoryAndCalculateTotalForFMs(ref revenueScroll, greenText, greenText2, item => item.Revenue, "Revenue");
            GUILayout.Label("Expenses", yellowText);
            double totalExpenses = DisplayCategoryAndCalculateTotalForFMs(ref expenseScroll, redText, redText2, item => item.Expenses, "Expenses");
            GUILayout.BeginHorizontal();
            GUILayout.Label("Net Revenue: ", yellowText);
            GUILayout.Label("√" + Math.Abs(totalRevenue - totalExpenses).ToString("N"), (totalRevenue - totalExpenses) < 0 ? redText : greenText);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(GameDatabase.Instance.GetTexture("BROKE/Textures/gear", false), GUILayout.ExpandWidth(false)))
            {
                currentView = BROKEView.Settings;
            }
            if (GUILayout.Button("History", GUILayout.ExpandWidth(false)))
            {
                currentView = BROKEView.History;
            }
            ShowPayPromptAndPay(InvoiceItems);
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
            //draw main window for the selected FM
            if (selectedMainFM != null)
            {
                GUILayout.BeginVertical(GUILayout.Width(WindowWidth));
                customScroll = GUILayout.BeginScrollView(customScroll);
                //Put it in a scrollview as well
                if (selectedMainFM.hasMainGUI())
                    selectedMainFM.DrawMainGUI();
                DisplayInvoicesForFM(yellowText, redText2, greenText2, selectedMainFM);
                GUILayout.EndVertical();
            }
            GUILayout.EndHorizontal();
        }

        private void DisplayInvoicesForFM(GUIStyle headerStyle, GUIStyle negativeStyle, GUIStyle positiveStyle, IFundingModifierBase selectedFM)
        {
            var invoices = InvoiceItems.Where(item => item.Modifier.GetName() == selectedFM.GetName());
            GUILayout.Label("Current invoices", headerStyle);
            foreach (var groupedItems in invoices.GroupBy(item => item.ItemName))
            {
                GUILayout.BeginHorizontal();
                if (!string.IsNullOrEmpty(groupedItems.Key))
                {
                    GUILayout.Label(groupedItems.Key, SkinsLibrary.CurrentSkin.textArea, GUILayout.Width(WindowWidth / 3));
                }
                double revenue = groupedItems.Sum(item => item.Revenue);
                if (revenue != 0)
                {
                    GUILayout.Label("√" + revenue.ToString("N"), positiveStyle);
                }
                double expenses = groupedItems.Sum(item => item.Expenses);
                if (expenses != 0)
                {
                    GUILayout.Label("√" + groupedItems.Sum(item => item.Expenses).ToString("N"), negativeStyle);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndScrollView();
            GUILayout.BeginHorizontal();
            ShowPayPromptAndPay(invoices, "Pay Selected");
            if (!invoices.Any())
            {
                //hide side window
                selectedMainFM = null;
                this.WindowRect.width = WindowWidth;
                //maybe reset the width here then
            }
            GUILayout.EndHorizontal();
        }

        private void ShowPayPromptAndPay(IEnumerable<InvoiceItem> itemsToBalance, string payPrompt = "Pay")
        {
            payAmountTxt = GUILayout.TextField(payAmountTxt);
            if (GUILayout.Button(payPrompt, GUILayout.ExpandWidth(false)))
            {
                double toPay;
                if (!double.TryParse(payAmountTxt, out toPay))
                {
                    toPay = -1;
                    payAmountTxt = "Pay Maximum";
                }
                else if(toPay > 0)
                {
                    toPay += PendingRevenue();
                }
                else
                {
                    toPay = -1;
                }
                CashInRevenues(itemsToBalance);
                toPay = Math.Min(toPay, RemainingDebt());
                PayExpenses(itemsToBalance, toPay);
            }
        }

        private double DisplayCategoryAndCalculateTotalForFMs(ref Vector2 scrollData, GUIStyle summaryStyle, GUIStyle itemStyle, Func<InvoiceItem, double> memberSelector, string category)
        {
            //Wrap this in a scrollbar
            scrollData = GUILayout.BeginScrollView(scrollData, GUI.skin.textArea);//, SkinsLibrary.CurrentSkin.textArea);
            double total = 0;
            
            foreach (IMultiFundingModifier FM in fundingModifiers)
            {
                var invoices = InvoiceItems.Where(item => item.Modifier.GetName() == FM.GetName());
                var sumForFM = invoices.Sum(memberSelector);
                total += sumForFM;
                if (sumForFM != 0)
                {
                    GUILayout.BeginHorizontal();
                    GUILayout.Label(FM.GetName(), GUI.skin.textArea, GUILayout.Width(WindowWidth / 2));
                    GUILayout.Label("√" + sumForFM.ToString("N"), itemStyle);
                    if (FM.hasMainGUI() || invoices.Count() > 1)
                    {
                        if (selectedMainFM != FM && GUILayout.Button("→", GUILayout.ExpandWidth(false)))
                        {
                            //tell it to display Main stuff for this in a new vertical
                            selectedMainFM = FM;
                            this.WindowRect.width = WindowWidth * 2;
                            //widen the window probably
                        }
                        if (selectedMainFM != null && selectedMainFM == FM && GUILayout.Button("←", GUILayout.ExpandWidth(false)))
                        {
                            //hide side window
                            selectedMainFM = null;
                            this.WindowRect.width = WindowWidth;
                            //maybe reset the width here then
                        }
                    }
                    GUILayout.EndHorizontal();
                }
            }
            GUILayout.EndScrollView();
            GUILayout.BeginHorizontal();
            GUILayout.Label(String.Format("Total {0}: ", category));
            GUILayout.Label("√" + total.ToString("N"), summaryStyle);
            GUILayout.EndHorizontal();
            return total;
        }

        public void DrawSettingsWindow()
        {
            GUILayout.BeginHorizontal();
            GUILayout.BeginVertical();
            foreach (IMultiFundingModifier FM in fundingModifiers)
            {
                GUILayout.BeginHorizontal(GUI.skin.textArea);//SkinsLibrary.CurrentSkin.textArea);
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
                        this.WindowRect.width = WindowWidth * 2;
                        //widen the window probably
                    }
                    if (selectedMainFM != null && selectedMainFM == FM && GUILayout.Button("←", GUILayout.ExpandWidth(false)))
                    {
                        //hide side window
                        selectedMainFM = null;
                        this.WindowRect.width = WindowWidth;
                        //maybe reset the width here then
                    }
                }
                GUILayout.EndHorizontal();
            }


            GUILayout.BeginHorizontal();
            GUILayout.Label("Autopay Mode", GUILayout.Width(WindowWidth / 2));
            if(GUILayout.Button(currentAutopayMode.Name))
            {
                var settings = currentAutopayMode.OnSave();
                var modeList = autopayModes.Values.ToList();
                currentAutopayMode = modeList[(modeList.IndexOf(currentAutopayMode) + 1) % modeList.Count];
                currentAutopayMode.OnLoad(settings);
            }
            GUILayout.EndHorizontal();
            GUILayout.Label("Autopay settings:");
            currentAutopayMode.DrawSettingsWindow();

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
            /*if (skinID == 0)
                SkinsLibrary.SetCurrent(SkinsLibrary.DefSkinType.KSP);
            else if (skinID == 1)*/
                SkinsLibrary.SetCurrent(SkinsLibrary.DefSkinType.Unity);
            /*else if (skinID == 2)
            {
                if (!SkinsLibrary.SkinExists("Mixed"))
                {
                    GUISkin mixedSkin = SkinsLibrary.CopySkin(SkinsLibrary.DefSkinType.Unity);
                    mixedSkin.window = HighLogic.Skin.window;
                    SkinsLibrary.AddSkin("Mixed", mixedSkin, false);
                }
                SkinsLibrary.SetCurrent("Mixed");
            }
            */
            SelectedSkin = skinID;
        }

        public bool FMDisabled(IMultiFundingModifier toCheck)
        {
            return disabledFundingModifiers.Contains(toCheck.GetConfigName());
        }

        public bool FMDisabled(string toCheck)
        {
            return disabledFundingModifiers.Contains(toCheck);
        }

        public void DisableFundingModifier(IMultiFundingModifier toDisable)
        {
            disabledFundingModifiers.AddUnique(toDisable.GetConfigName());
            toDisable.OnDisabled();
        }

        public void EnableFundingModifier(IMultiFundingModifier toEnable)
        {
            disabledFundingModifiers.Remove(toEnable.GetConfigName());
            toEnable.OnEnabled();
        }

        public void NewDay()
        {
            paymentHistory.ClearOneYearAgo();
            //Update every FundingModifier
            LogFormatted_DebugOnly("New Day! " + KSPUtil.PrintDate((int)Planetarium.GetUniversalTime(), true, true));
            foreach(IMultiFundingModifier fundingMod in fundingModifiers)
            {
                if (!FMDisabled(fundingMod))
                {
                    fundingMod.DailyUpdate();
                }
            }
        }

        public void NewQuarter()
        {
            //Calculate quarterly expenses and display expense report
            LogFormatted_DebugOnly("New Quarter! " + KSPUtil.PrintDate((int)Planetarium.GetUniversalTime(), true, true));
            foreach (IMultiFundingModifier fundingMod in fundingModifiers)
            {
                if (!FMDisabled(fundingMod))
                {
                    InvoiceItems.AddRange(fundingMod.ProcessQuarterly());
                }
            }
        }

        public void NewYear()
        {
            //Calculate yearly expenses and display expense report
            LogFormatted_DebugOnly("New Year! " + KSPUtil.PrintDate((int)Planetarium.GetUniversalTime(), true, true));
            foreach (IMultiFundingModifier fundingMod in fundingModifiers)
            {
                if (!FMDisabled(fundingMod))
                {
                    InvoiceItems.AddRange(fundingMod.ProcessYearly());
                }
            }
        }

        private void DisplayExpenseReport()
        {
            currentView = BROKEView.ExpenseReport;
            Debug.Log("Revenue:");
            foreach (var invoiceItem in InvoiceItems)
                Debug.Log(invoiceItem.Modifier.GetName() + ": " + invoiceItem.ItemName + ": " + invoiceItem.Revenue);

            Debug.Log("Expenses:");
            foreach (var invoiceItem in InvoiceItems)
                Debug.Log(invoiceItem.Modifier.GetName() + ": " + invoiceItem.ItemName + ": " + invoiceItem.Expenses);

            this.Visible = true;
            this.DragEnabled = true;
            this.WindowRect.Set((Screen.width - WindowWidth) / 2, (Screen.height - WindowHeight) / 2, WindowWidth, WindowHeight);
            this.WindowCaption = "B.R.O.K.E.";
            //SkinsLibrary.SetCurrent(SkinsLibrary.DefSkinType.Unity);
        }

        public void CashInRevenues(IEnumerable<InvoiceItem> itemsToBalance)
        {
            foreach (var invoiceItem in itemsToBalance)
            {
                AdjustFunds(invoiceItem.Revenue);
                paymentHistory.Record(invoiceItem.WithdrawRevenue());
            }
        }

        public double PayExpenses(IEnumerable<InvoiceItem> invoicesToPay, double MaxToPay = -1)
        {
            LogFormatted_DebugOnly("Paying Expenses!");
            if (HighLogic.CurrentGame.Mode != Game.Modes.CAREER) //No funds outside of career
                return 0;
            double toPay = 0;
            if (MaxToPay < 0) //Pay anything we can
            {
                //pay the minimum of the remaining funds and the amount we owe
                toPay = Math.Min(RemainingDebt(), Funding.Instance.Funds);
            }
            else //Pay the minimum of the amount we have, the amount we're willing to pay, and the amount we owe
            {
                toPay = Math.Min(MaxToPay, Funding.Instance.Funds);
                toPay = Math.Min(toPay, RemainingDebt());
            }
            AdjustFunds(-toPay);
            foreach(var item in invoicesToPay)
            {
                if (toPay > 0)
                {
                    var amountToPay = Math.Min(item.Expenses, toPay);
                    paymentHistory.Record(item.PayInvoice(amountToPay));
                    toPay -= amountToPay;
                }
            }
            InvoiceItems.RemoveAll(item => item.Revenue == 0 && item.Expenses == 0);
            return RemainingDebt();
        }


        //Shamelessly modified from this StackOverflow question/answer: http://stackoverflow.com/questions/5411694/get-all-inherited-classes-of-an-abstract-class
        //http://stackoverflow.com/questions/26733/getting-all-types-that-implement-an-interface
        private IEnumerable<T> GetInstanceOfAllImplementingClasses<T>()
            where T : class
        {
            Type type = typeof(T);
            var instances = new List<T>();
            IEnumerable<Type> types = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(s =>
                {
                    try
                    {
                        return s.GetTypes();
                    }
                    catch (ReflectionTypeLoadException ex)
                    {
                        LogFormatted("Failed to load types from asssembly");
                        Debug.LogException(ex);
                        foreach (var subException in ex.LoaderExceptions)
                        {
                            Debug.LogException(subException);
                        }
                        return Enumerable.Empty<Type>();
                    }
                })
            .Where(p => type.IsAssignableFrom(p) && !p.IsInterface && !p.IsAbstract);
            List<T> fundingMods = new List<T>();

            foreach (Type t in types.Where(t => t.GetConstructor(Type.EmptyTypes) != null))
            {
                T instance;
                if (typeof(MonoBehaviour).IsAssignableFrom(t))
                {
                    instance = gameObject.AddComponent(t) as T;
                }
                else
                    instance = Activator.CreateInstance(t) as T;
                instances.Add(instance);
            }
            return instances;
        }

        public List<IMultiFundingModifier> GetFundingModifiers()
        {
            List<IMultiFundingModifier> modifiers = new List<IMultiFundingModifier>();
            // We need this cast here because .NET doesn't have co/contra-variance until 4.0 and we're stuck on 3.5
            modifiers.AddRange(GetInstanceOfAllImplementingClasses<IFundingModifier>()
                .Select(mod => (IMultiFundingModifier)new SingleToMultiFundingModifier(mod)));
            modifiers.AddRange(GetInstanceOfAllImplementingClasses<IMultiFundingModifier>());
            return modifiers;
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
