using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSPPluginFramework;

namespace BROKE
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, new GameScenes[] { GameScenes.TRACKSTATION, GameScenes.SPACECENTER, GameScenes.FLIGHT })]
    class BROKE_Data : ScenarioModule
    {
        public override void OnSave(ConfigNode node)
        {
            base.OnSave(node);

            ConfigNode BROKENode = new ConfigNode();
            //Save BROKE data
            ConfigNode outstandingInvoices = ListToConfigNode(BROKE.Instance.InvoiceItems);
            BROKENode.AddNode("Invoices", outstandingInvoices);
            BROKENode.AddValue("Skin", BROKE.Instance.SelectedSkin);
            ConfigNode disabled = ListToConfigNode(BROKE.Instance.disabledFundingModifiers);
            if (disabled != null)
                BROKENode.AddNode("DisabledFMs", disabled);
            
            //Save each IMultiFundingModifier and IFundingModifier
            foreach (IMultiFundingModifier fundingMod in BROKE.Instance.fundingModifiers)
            {
                ConfigNode fmNode = fundingMod.SaveData();
                if (fmNode != null)
                    BROKENode.AddNode(fundingMod.GetConfigName(), fmNode);
            }

            BROKENode.AddNode(BROKE.Instance.paymentHistory.OnSave());


            node.AddNode("BROKE_Data", BROKENode);
        }
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            try
            {
                ConfigNode BROKENode = node.GetNode("BROKE_Data");
                if (BROKENode != null)
                {
                    int skinID = 2;
                    int.TryParse(BROKENode.GetValue("Skin"), out skinID);
                    BROKE.Instance.SelectSkin(skinID);
                    ConfigNode disabled = BROKENode.GetNode("DisabledFMs");
                    if (disabled != null)
                        BROKE.Instance.disabledFundingModifiers = ConfigNodeToList(disabled);
                    print("loaded disabled list");
                    //Save each IMultiFundingModifier and IFundingModifier
                    foreach (IMultiFundingModifier fundingMod in BROKE.Instance.fundingModifiers)
                    {
                        ConfigNode fmNode = BROKENode.GetNode(fundingMod.GetConfigName());
                        if (fmNode != null)
                            fundingMod.LoadData(fmNode);
                    }
                    print("loaded funding modifiers");
                    ConfigNode invoices = BROKENode.GetNode("Invoices");
                    if (invoices != null)
                    {
                        BROKE.Instance.InvoiceItems.Clear();
                        BROKE.Instance.InvoiceItems.AddRange(ConfigNodeToList<InvoiceItem>(invoices));
                    }
                    ConfigNode history = BROKENode.GetNode("PaymentHistory");
                    if (history != null)
                    {
                        BROKE.Instance.paymentHistory.OnLoad(history);
                    }
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
        }

        public static ConfigNode ListToConfigNode(List<string> list)
        {
            ConfigNode retNode = new ConfigNode();
            foreach (string s in list)
                retNode.AddValue("element", s);
            return retNode;
        }

        public static ConfigNode ListToConfigNode<T>(List<T> list)
        {
            ConfigNode retNode = new ConfigNode();
            foreach (var item in list)
                retNode.AddNode(ConfigNode.CreateConfigFromObject(item));
            return retNode;
        }

        public static List<string> ConfigNodeToList(ConfigNode node)
        {
            List<string> retList = new List<string>();
            foreach (string s in node.GetValues("element"))
                retList.Add(s);
            return retList;
        }
        

        //We need the class requirement here because the loading stuff won't load into structs.
        public static List<T> ConfigNodeToList<T>(ConfigNode node)
            where T :class, new()
        {
            List<T> retList = new List<T>();
            foreach (var element in node.GetNodes())
            {
                T value = new T();
                ConfigNode.LoadObjectFromConfig(value, element);
                retList.Add(value);
            }
            return retList;
        }
    }
}
