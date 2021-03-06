﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using KSPPluginFramework;

namespace BROKE
{
    [KSPScenario(ScenarioCreationOptions.AddToAllGames, new GameScenes[] { GameScenes.TRACKSTATION, GameScenes.SPACECENTER, GameScenes.FLIGHT })]
    class BROKE_Data : ScenarioModule
    {
        public override void OnSave (ConfigNode node)
        {
            base.OnSave(node);

            ConfigNode BROKENode = new ConfigNode();
            //Save BROKE data
            ConfigNode outstandingInvoices = ListToConfigNode(BROKE.State.InvoiceItems);
            BROKENode.AddNode("Invoices", outstandingInvoices);
            BROKENode.AddValue("Skin", BROKE.State.SelectedSkin);
            ConfigNode disabled = ListToConfigNode(BROKE.State.disabledFundingModifiers);
            if (disabled != null)
                BROKENode.AddNode("DisabledFMs", disabled);

            //Save each IMultiFundingModifier and IFundingModifier
            foreach (IMultiFundingModifier fundingMod in BROKE.State.fundingModifiers)
            {
                ConfigNode fmNode = fundingMod.SaveData();
                if (fmNode != null)
                    BROKENode.AddNode(fundingMod.GetConfigName(), fmNode);
            }

            BROKENode.AddNode(BROKE.State.paymentHistory.OnSave());
            BROKENode.AddValue("AutopayMode", BROKE.State.currentAutopayMode.Name);
            BROKENode.AddNode(BROKE.State.currentAutopayMode.OnSave());

            node.AddNode("BROKE_Data", BROKENode);
        }
        public override void OnLoad (ConfigNode node)
        {
            base.OnLoad(node);

            if (BROKE.State.sane) throw new InvalidOperationException("Insanity check failed: state not yet reset");

            try
            {
                ConfigNode BROKENode = node.GetNode("BROKE_Data");
                if (BROKENode != null)
                {
                    int skinID = 1;
                    int.TryParse(BROKENode.GetValue("Skin"), out skinID);
                    //BROKE.State.SelectSkin(skinID); //TODO: current;y broken
                    ConfigNode disabled = BROKENode.GetNode("DisabledFMs");
                    if (disabled != null)
                        BROKE.State.disabledFundingModifiers = ConfigNodeToList(disabled);
                    print("loaded disabled list");
                    //Save each IMultiFundingModifier and IFundingModifier
                    foreach (IMultiFundingModifier fundingMod in BROKE.State.fundingModifiers)
                    {
                        ConfigNode fmNode = BROKENode.GetNode(fundingMod.GetConfigName());
                        if (fmNode != null)
                            fundingMod.LoadData(fmNode);
                    }
                    print("loaded funding modifiers");
                    ConfigNode invoices = BROKENode.GetNode("Invoices");
                    if (invoices != null)
                    {
                        BROKE.State.InvoiceItems.Clear();
                        BROKE.State.InvoiceItems.AddRange(ConfigNodeToList<InvoiceItem>(invoices));
                    }
                    print("loaded current invoices");
                    ConfigNode history = BROKENode.GetNode("PaymentHistory");
                    if (history != null)
                    {
                        BROKE.State.paymentHistory.OnLoad(history);
                    }
                    print("loaded payment history");
                    string autopayName = BROKENode.GetValue("AutopayMode");
                    ConfigNode autopaySettings = BROKENode.GetNode("AutopaySettings");
                    if (BROKE.State.autopayModes.ContainsKey(autopayName))
                    {
                        BROKE.State.currentAutopayMode = BROKE.State.autopayModes[autopayName];
                        BROKE.State.currentAutopayMode.OnLoad(autopaySettings);
                    }
                    print("loaded autopay mode");
                }
            }
            catch (Exception e)
            {
                UnityEngine.Debug.LogException(e);
            }
            BROKE.State.sane = true;
        }

        public static ConfigNode ListToConfigNode (List<string> list)
        {
            ConfigNode retNode = new ConfigNode();
            foreach (string s in list)
                retNode.AddValue("element", s);
            return retNode;
        }

        public static ConfigNode ListToConfigNode<T> (List<T> list)
        {
            ConfigNode retNode = new ConfigNode();
            foreach (var item in list)
                retNode.AddNode(ConfigNode.CreateConfigFromObject(item));
            return retNode;
        }

        public static List<string> ConfigNodeToList (ConfigNode node)
        {
            List<string> retList = new List<string>();
            foreach (string s in node.GetValues("element"))
                retList.Add(s);
            return retList;
        }


        //We need the class requirement here because the loading stuff won't load into structs.
        public static List<T> ConfigNodeToList<T> (ConfigNode node)
            where T : class, new()
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
