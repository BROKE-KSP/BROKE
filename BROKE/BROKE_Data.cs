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
            BROKENode.AddValue("OutstandingDebt", BROKE.Instance.RemainingDebt);
            BROKENode.AddValue("Skin", BROKE.Instance.SelectedSkin);
            //BROKENode.AddValue("DisabledFMs", BROKE.Instance.disabledFundingModifiers);
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
                ConfigNode BROKENode = node.GetNode("BROKE_Data");
                if (BROKENode != null)
                {
                    double.TryParse(BROKENode.GetValue("OutstandingDebt"), out BROKE.Instance.RemainingDebt);
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

        public List<string> ConfigNodeToList(ConfigNode node)
        {
            List<string> retList = new List<string>();
            foreach (string s in node.GetValues("element"))
                retList.Add(s);
            return retList;
        }
    }
}
