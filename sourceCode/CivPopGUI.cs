using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using KSP;

namespace CivilianManagment
{
    
    //TODO:  Setup GUI
    [KSPAddon(KSPAddon.Startup.EveryScene, false)]
    public class CivPopGUI : MonoBehaviour
    {

        internal static string assetFolder = "NetherdyneAerospace/CivilianManagement/Assets/";
        private static ApplicationLauncherButton CivPopButton = null;
        internal bool CivPopGUIOn = false;
        internal bool CivPopTooltip = false;
        private GUIStyle _windowstyle, _labelstyel;
        private bool hasInitStyles = false;
    
        
        
               
        void Awake()
        {
            if (HighLogic.LoadedScene == GameScenes.SPACECENTER)
            {
                DontDestroyOnLoad(this);
                GameEvents.onGUIApplicationLauncherReady.Add(OnGUIAppLauncherReady);
                GameEvents.onGUIApplicationLauncherDestroyed.Add(OnGUIAppLauncherDestroyed);
            }
        }

        void OnGUIAppLauncherReady()
        {

            if (HighLogic.LoadedScene == GameScenes.SPACECENTER && CivPopButton == null)
            {
                InitStyle();
                string IconFile = "CivPopIcon";
                CivPopButton = ApplicationLauncher.Instance.AddModApplication(
                    BTOn,
                    BTOff,
                    BTHoverOn,
                    BTHoverOff,
                    null, null,
                    ApplicationLauncher.AppScenes.SPACECENTER,
                    (Texture)GameDatabase.Instance.GetTexture(assetFolder + IconFile, false));
            }
            CivPopGUIOn = false;
        }
        void OnGUIAppLauncherDestroyed()
        {
            if (CivPopButton != null)
            {
                BTOff();
                ApplicationLauncher.Instance.RemoveModApplication(CivPopButton);
                CivPopButton = null;
            }
        }

       private void InitStyle()
        {
            _windowstyle = new GUIStyle(HighLogic.Skin.window);
            _labelstyel = new GUIStyle(HighLogic.Skin.label);
           hasInitStyles = true;
        }
        
        
        void BTOn()
        {
            if (CivPopButton == null)
            {
                Debug.LogError("CivPOP :: BTOn called without a button.");
                return;
            }
            CivPopGUIOn = true;
        }

        void BTOff()
        {
            if (CivPopButton == null)
            {
                Debug.LogError("CivPOP :: BTOff called without a button.");
                return;
            }
            CivPopGUIOn = false;
        }
        void BTHoverOn()
        {
            if (CivPopTooltip == false)
            {
                CivPopTooltip = true;
            }
        }
        
        void BTHoverOff()
        {
            if (CivPopTooltip == true)
            {
                CivPopTooltip = false;
            }
        }

        void OnGUI()
        {
            if ((CivPopGUIOn) && (HighLogic.LoadedScene == GameScenes.SPACECENTER))
            {
                CivilianManagmentGUI();
            }
        }

        void CivilianManagmentGUI()
        {

            GUI.BeginGroup(new Rect(Screen.width / 2 - 250, Screen.height / 2 - 250, 500, 500));
            GUILayout.BeginVertical("box");
            GUILayout.Label("CIVPOP PlaceHolder GUI");
            if (GUILayout.Button("Close this Window", GUILayout.Width(200f)))
                BTOff();
            GUILayout.EndVertical();
            GUI.EndGroup();
                    
        }
    
    
    
    }
}
