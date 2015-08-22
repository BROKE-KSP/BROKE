using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using KSP;
using KSPPluginFramework;

namespace BROKE
{
    
    //Utilizes the amazing KSP Plugin Framework by TriggerAU to make things much easier
    //http://forum.kerbalspaceprogram.com/threads/66503-KSP-Plugin-Framework-Plugin-Examples-and-Structure-v1-1-(Apr-6)

    [KSPAddon(KSPAddon.Startup.SpaceCentre | KSPAddon.Startup.TrackingStation | KSPAddon.Startup.Flight | KSPAddon.Startup.EditorAny, false)]
    public class BROKE : MonoBehaviourWindow
    {
        internal override void Start()
        {

        }

        internal override void FixedUpdate()
        {

        }

        internal override void DrawWindow(int id)
        {
            
        }

    }
}
