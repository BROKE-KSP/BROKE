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
            
            //Save each IFundingModifier
            foreach (IFundingModifier fundingMod in BROKE.Instance.fundingModifiers)
            {
                ConfigNode fmNode = fundingMod.SaveData();
                if (fmNode != null)
                    BROKENode.AddNode(fundingMod.GetConfigName(), fmNode);
            }


            node.AddNode("BROKE_Data", BROKENode);
        }
        public override void OnLoad(ConfigNode node)
        {
            base.OnLoad(node);

            try
            {
                print("Loading BROKE data");
                ConfigNode BROKENode = node.GetNode("BROKE_Data");
                if (BROKENode != null)
                {
                    print("Loading invoices");
                    ConfigNode invoices = BROKENode.GetNode("Invoices");
                    if (invoices != null)
                    {
                        print("invoices is not null");
                        BROKE.Instance.InvoiceItems.Clear();
                        BROKE.Instance.InvoiceItems.AddRange(ConfigNodeToList<InvoiceItem>(invoices)); 
                    }
                    int skinID = 2;
                    int.TryParse(BROKENode.GetValue("Skin"), out skinID);
                    BROKE.Instance.SelectSkin(skinID);
                    ConfigNode disabled = BROKENode.GetNode("DisabledFMs");
                    if (disabled != null)
                        BROKE.Instance.disabledFundingModifiers = ConfigNodeToList(disabled);

                    //load each IFundingModifier
                    foreach (IFundingModifier fundingMod in BROKE.Instance.fundingModifiers)
                    {
                        ConfigNode fmNode = BROKENode.GetNode(fundingMod.GetConfigName());
                        if (fmNode != null)
                            fundingMod.LoadData(fmNode);
                    }
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.Log("Exception while loading BROKE data! "+ e.Message);
            }
        }

        public ConfigNode ListToConfigNode(List<string> list)
        {
            ConfigNode retNode = new ConfigNode();
            foreach (string s in list)
                retNode.AddValue("element", s);
            return retNode;
        }

        public ConfigNode ListToConfigNode<T>(List<T> list)
        {
            ConfigNode retNode = new ConfigNode();
            foreach (var item in list)
                retNode.AddNode(ConfigNode.CreateConfigFromObject(item));
            return retNode;
        }

        public List<string> ConfigNodeToList(ConfigNode node)
        {
            List<string> retList = new List<string>();
            foreach (string s in node.GetValues("element"))
                retList.Add(s);
            return retList;
        }

        public List<T> ConfigNodeToList<T>(ConfigNode node)
            where T :new()
        {
            List<T> retList = new List<T>();
            foreach (var element in node.GetNodes())
            {
                T value = new T();
                ConfigNode.LoadObjectFromConfig(value, element);
                print("Loading element :" + element.name);
                //retList.Add(ConfigNode.CreateObjectFromConfig<T>(element));
                retList.Add(value);
            }
            return retList;
        }
    }
}
