using KSP.UI.Screens;
using KSPPluginFramework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace BROKE
{
    [KSPAddon(KSPAddon.Startup.SpaceCentre, false)]
    class LoanIssuerUI : MonoBehaviourWindow
    {
        private ApplicationLauncherButton button;
        private int WindowWidth = 360, WindowHeight = 540;
        private Vector2 scrollPos;
        private List<LoanGenerator> loanGenerators = new List<LoanGenerator>();
        private List<Loan> currentOfferings = new List<Loan>();
        private LoanManager manager;

        internal override void Start()
        {
            WindowCaption = "B.R.O.K.E Loan Directory";
            DragEnabled = true;
            WindowRect.Set((Screen.width - WindowWidth) / 2, (Screen.height - WindowHeight) / 2, WindowWidth, WindowHeight);
            if (ApplicationLauncher.Instance != null && ApplicationLauncher.Ready)
                OnAppLauncherReady();
            else
                GameEvents.onGUIApplicationLauncherReady.Add(OnAppLauncherReady);
            LoadLoanGenerators();
            if (BROKE.Instance != null)
                LoadLoanManager();
        }

        private void LoadLoanGenerators()
        {
            foreach (var node in GameDatabase.Instance.GetConfigNodes("LoanTemplate"))
            {
                LogFormatted_DebugOnly("Loading LoanTemplate[{0}]", node.GetValue("name"));
                var generator = new LoanGenerator();
                ConfigNode.LoadObjectFromConfig(generator, node);
                loanGenerators.Add(generator);
                LogFormatted_DebugOnly("Writing loaded config back out.");
                LogFormatted_DebugOnly("{0}", ConfigNode.CreateConfigFromObject(generator).ToString());
            }
        }

        private void LoadLoanManager()
        {
            manager = BROKE.Instance.fundingModifiers.OfType<LoanManager>().FirstOrDefault();
        }

        void OnAppLauncherReady()
        {
            if (button != null)
            {
                ApplicationLauncher.Instance.RemoveModApplication(button);
                button = null;
            }
            button = ApplicationLauncher.Instance.AddModApplication(
                ShowWindow,
                () => Visible = false,
                null,
                null,
                null,
                null,
                ApplicationLauncher.AppScenes.SPACECENTER,
                GameDatabase.Instance.GetTexture("BROKE/Textures/icon_button_stock", false));
        }

        private void ShowWindow()
        {
            GenerateLoans();
            Visible = true;
        }

        internal override void OnDestroy()
        {
            GameEvents.onGUIApplicationLauncherReady.Remove(OnAppLauncherReady);
            ApplicationLauncher.Instance.RemoveModApplication(button);
        }

        private void GenerateLoans()
        {
            currentOfferings.Clear();
            foreach (var item in loanGenerators)
            {
                var loan = item.GenerateLoan();
                if (loan != null)
                {
                    currentOfferings.Add(loan);
                    LogFormatted_DebugOnly("Generated loan: {0}", loan.GetName());
                }
            }
        }

        internal override void DrawWindow(int id)
        {
            if (manager == null)
                LoadLoanManager();
            GUILayout.BeginVertical(GUILayout.Width(WindowWidth));
            scrollPos = GUILayout.BeginScrollView(scrollPos);
            GUILayout.BeginVertical();
            foreach (var loan in currentOfferings.Where(loan => manager.CanAddLoan(loan)))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Box(loan.Agency.LogoScaled);
                GUILayout.Label(loan.GetName());
                if(GUILayout.Button("Purchase"))
                {
                    LogFormatted_DebugOnly("Purchasing loan {0}", loan.GetName());
                    manager.AddLoan(loan);
                    Funding.Instance.AddFunds(loan.GetPrincipal(), TransactionReasons.StrategyOutput);
                }
                GUILayout.EndHorizontal();
            }
            GUILayout.EndVertical();
            GUILayout.EndScrollView();
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Refresh"))
                GenerateLoans();
            GUILayout.EndHorizontal();
            GUILayout.EndVertical();
        }
    }
}
