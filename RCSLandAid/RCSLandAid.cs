using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;


namespace RCSLandAid
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class RCSLandingAid : MonoBehaviour
    {


        bool selectingTarget = false;
        private IButton RCSla1Btn;

        //LineRenderer theLine = new LineRenderer();
        //public static bool forceSASup = true;
        //RCSLandingAidWindow RCSwin;
        Part lastRoot = new Part();
        //private ConfigNode RCSla;
        ApplicationLauncherButton LAButton = null; //stock toolbar button instance
        bool checkBlizzyToolbar = false;
        Texture2D btnRed = new Texture2D(24, 24);
        Texture2D btnBlue = new Texture2D(24, 24);
        Texture2D btnRedEnable = new Texture2D(24, 24);
        Texture2D btnBlueEnable = new Texture2D(24, 24);
        Texture2D btnGray = new Texture2D(24, 24);
        bool showLAMenu = false;
        Rect LASettingsWin = new Rect(Screen.width - 200, 40, 160, 90);
        public static RCSLandingAidModule curVsl;
        int lastBtnState = 0;
        public static int curBtnState = 0;
        public static bool overWindow;


        public void Start()
        {
            print("Landing Aid Ver. 2.6 start.");
            RenderingManager.AddToPostDrawQueue(0, LAOnDraw); //GUI window hook
            byte[] importTxtRed = File.ReadAllBytes(KSPUtil.ApplicationRootPath + "GameData/Diazo/RCSLandAid/iconRed.png"); //load our button textures
            byte[] importTxtBlue = File.ReadAllBytes(KSPUtil.ApplicationRootPath + "GameData/Diazo/RCSLandAid/iconBlue.png");
            byte[] importTxt = File.ReadAllBytes(KSPUtil.ApplicationRootPath + "GameData/Diazo/RCSLandAid/iconWhiteB.png");
            byte[] importTxtRedEnable = File.ReadAllBytes(KSPUtil.ApplicationRootPath + "GameData/Diazo/RCSLandAid/iconRedEnabled.png"); //load our button textures
            byte[] importTxtBlueEnable = File.ReadAllBytes(KSPUtil.ApplicationRootPath + "GameData/Diazo/RCSLandAid/iconBlueEnabled.png"); //load our button textures


            btnRed.LoadImage(importTxtRed);
            btnRed.Apply();
            btnBlue.LoadImage(importTxtBlue);
            btnBlue.Apply();
            btnGray.LoadImage(importTxt);
            btnGray.Apply();
            btnRedEnable.LoadImage(importTxtRedEnable);
            btnRedEnable.Apply();
            btnBlueEnable.LoadImage(importTxtBlueEnable);
            btnBlueEnable.Apply();
            //RCSla = ConfigNode.Load(KSPUtil.ApplicationRootPath + "GameData/Diazo/RCSLandAid/RCSla.cfg");

            //forceSASup = Convert.ToBoolean(RCSla.GetValue("ForceSAS"));    
            //FlightGlobals.ActiveVessel.OnFlyByWire += RCSLandAidControl;
            if (ToolbarManager.ToolbarAvailable) //check if toolbar available, load if it is
            {

                RCSla1Btn = ToolbarManager.Instance.add("RCSla", "RCSlaBtn");
                RCSla1Btn.TexturePath = "Diazo/RCSLandAid/iconWhiteB";
                //RCSla1Btn.Text = "RCS";
                RCSla1Btn.ToolTip = "RCS Land Aid";

                RCSla1Btn.OnClick += (e) =>
                {
                    if (e.MouseButton == 0)
                    {
                        LeftClick();
                    }
                    if (e.MouseButton == 1)
                    {
                        RightClick();
                    }
                };
                checkBlizzyToolbar = true;
            }
            else
            {
                //AGXShow = true; //toolbar not installed, show AGX regardless
                //now using stock toolbar as fallback
                LAButton = ApplicationLauncher.Instance.AddModApplication(onStockToolbarClick, onStockToolbarClick, DummyVoid, DummyVoid, DummyVoid, DummyVoid, ApplicationLauncher.AppScenes.FLIGHT, (Texture)GameDatabase.Instance.GetTexture("Diazo/RCSLandAid/iconWhiteB", false));
                checkBlizzyToolbar = false;
            }
            //RCSLandingAidWindow RCSwin = new RCSLandingAidWindow();


        }

        public void LAOnDraw()
        {
            if (showLAMenu)
            {
                LASettingsWin = GUI.Window(67347792, LASettingsWin, DrawWin, "Settings", HighLogic.Skin.window);
                if (Input.mousePosition.x > LASettingsWin.x && Input.mousePosition.x < LASettingsWin.x + LASettingsWin.width && (Screen.height - Input.mousePosition.y) > LASettingsWin.y && (Screen.height - Input.mousePosition.y) < LASettingsWin.y + LASettingsWin.height)
                {
                    overWindow = true;

                }
                else
                {
                    overWindow = false;
                }
            }
        }

        public void DummyVoid()
        {

        }
        public void onStockToolbarClick()
        {

            //print("mouse " + Input.GetMouseButtonUp(1) + Input.GetMouseButtonDown(1));
            if (Input.GetMouseButtonUp(1))
            {
                RightClick();
            }
            else
            {
                LeftClick();
            }

        }

        
        
        public void SetHoverOn()
        {
            curVsl.controlState = 1;
            curVsl.targetSelected = false;
            selectingTarget = false;
            curVsl.theLine.SetWidth(0, 0);
            if (checkBlizzyToolbar)
                    {
                        RCSla1Btn.TexturePath = "Diazo/RCSLandAid/iconBlue";
                        RCSla1Btn.Drawable = null;
                    }
                    else
                    {
                        LAButton.SetTexture(btnBlue);
                        showLAMenu = false;
                    }
            selectingTarget = false;
                    curVsl.targetSelected = false;
        }

        public void SetHoverOff()
        {
            curVsl.controlState = 0;
            curVsl.targetSelected = false;
            selectingTarget = false;
            curVsl.theLine.SetWidth(0, 0);
            if (checkBlizzyToolbar)
            {
                RCSla1Btn.TexturePath = "Diazo/RCSLandAid/iconWhiteB";
                RCSla1Btn.Drawable = null;
            }
            else
            {
                LAButton.SetTexture(btnGray);
                showLAMenu = false;
            }
        }

        public void SetHoldOn()
        {
            curVsl.controlState = 2;
            selectingTarget = true;
            curVsl.theLine.SetColors(Color.red, Color.red);
            if (checkBlizzyToolbar)
            {
                RCSla1Btn.TexturePath = "Diazo/RCSLandAid/iconRed";
                RCSLandingAidWindow RCSwin = new RCSLandingAidWindow();
                RCSla1Btn.Drawable = RCSwin;
            }
            else
            {
                LAButton.SetTexture(btnRed);
                showLAMenu = true;
            }
        }

        public void SetHoldOnLink()
        {
            curVsl.controlState = 2;
            selectingTarget = true;
            curVsl.theLine.SetColors(Color.red, Color.red);
            if (checkBlizzyToolbar)
            {
                RCSla1Btn.TexturePath = "Diazo/RCSLandAid/iconRed";
                //RCSLandingAidWindow RCSwin = new RCSLandingAidWindow();
                //RCSla1Btn.Drawable = RCSwin;
            }
            else
            {
                LAButton.SetTexture(btnRed);
                //showLAMenu = true;
            }
        }

        public void SetHoldOnHere()
        {
            curVsl.controlState = 2;
            selectingTarget = false;


            RaycastHit pHit;
            //FlightCamera FlightCam = FlightCamera.fetch;
            LayerMask pRayMask = 33792; //layermask does not ignore layer 0, why?
            Ray pRay = new Ray(FlightGlobals.ActiveVessel.transform.position, FlightGlobals.currentMainBody.position - FlightGlobals.ActiveVessel.transform.position); //FlightCam.mainCamera.ScreenPointToRay(Input.mousePosition);
            //Ray pRayDown = new Ray(FlightCamera. transform.position , FlightGlobals.currentMainBody.position);
            Vector3 hitLoc = new Vector3();
            if (Physics.Raycast(pRay, out pHit, 2000f, pRayMask)) //cast ray
            {
                hitLoc = pHit.point;
                // print(hitLoc);
                curVsl.theLine.SetWidth(0, 1);
                curVsl.theLine.SetPosition(0, hitLoc);
                curVsl.theLine.SetPosition(1, hitLoc + ((hitLoc - FlightGlobals.ActiveVessel.mainBody.position).normalized) * 7);
                //if (!overWindow)
                //{
                //    if (Input.GetKeyDown(KeyCode.Mouse0))
                //    {
                        //if (checkBlizzyToolbar)
                        //{
                        //    RCSla1Btn.Drawable = null;
                        //}
                        //else
                        //{
                        //    showLAMenu = false;
                        //}
                        selectingTarget = false;
                        curVsl.targetLocation = hitLoc;
                        curVsl.targetSelected = true;
                    //}
               //}
            }


            //curVsl.targetLocation = curVsl.vessel.transform.position;
            curVsl.theLine.SetColors(Color.red, Color.red);
            if (checkBlizzyToolbar)
            {
                RCSla1Btn.TexturePath = "Diazo/RCSLandAid/iconRed";
                //RCSLandingAidWindow RCSwin = new RCSLandingAidWindow();
                //RCSla1Btn.Drawable = RCSwin;
            }
            else
            {
                LAButton.SetTexture(btnRed);
                //showLAMenu = true;
            }
        }

        public void LeftClick()
        {
            if (curVsl != null)
            {
                if (curVsl.controlState == 0)
                {
                    SetHoverOn();
                }
                else
                {
                    SetHoverOff();
                }
            }

        }

        public void RightClick()
        {
            if (curVsl != null)
            {
                if (curVsl.controlState == 2)
                {
                    SetHoverOn();
                    //curVsl.controlState = 1;
                    //curVsl.targetSelected = false;
                    //selectingTarget = false;
                    //curVsl.theLine.SetWidth(0, 0);
                    //if (checkBlizzyToolbar)
                    //{
                    //    RCSla1Btn.TexturePath = "Diazo/RCSLandAid/iconBlue";
                    //    RCSla1Btn.Drawable = null;
                    //}
                    //else
                    //{
                    //    LAButton.SetTexture(btnBlue);
                    //    showLAMenu = false;
                    //}

                }
                else
                {
                    SetHoldOn();

                }
            }
        }

        public bool DataModulePresent(Vessel vsl)
        {
            try
            {
                foreach (Part p in vsl.Parts)
                {
                    if (p.Modules.Contains("RCSLandingAidModule"))
                    {
                        return true;
                    }
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        public void Update()
        {
            string errLine = "1";
            try
            {
                //Debug.Log("vsl nulla");
                if (!DataModulePresent(FlightGlobals.ActiveVessel) && curVsl != null)
                {
                    errLine = "1a";
                    //Debug.Log("vsl null");
                    curVsl.theLine.SetColors(Color.blue, Color.blue);
                    curVsl = null;
                    curBtnState = 0;
                }
                //Debug.Log("vsl nullb");
                errLine = "1b";
                if (curVsl == null && DataModulePresent(FlightGlobals.ActiveVessel) || curVsl != null && curVsl.vessel.rootPart != FlightGlobals.ActiveVessel.rootPart || curVsl != null && !curVsl.isMasterModule || curVsl != null && !FlightGlobals.ActiveVessel.parts.Contains(curVsl.part))
                {
                    //Debug.Log("vsl switch");
                    try
                    {
                        if (curVsl != null)
                        {
                            //Debug.Log("blue reset");
                            curVsl.theLine.SetColors(Color.blue, Color.blue);
                        }
                    }
                    catch
                    {
                        //Debug.Log("vsl nullb catch");
                    }
                    errLine = "3";
                    //bool mdlFound = false;
                    //foreach (Part p in FlightGlobals.ActiveVessel.parts)
                    //{
                    //    errLine = "4";
                    List<RCSLandingAidModule> dataModules = new List<RCSLandingAidModule>();
                    foreach (Part p in FlightGlobals.ActiveVessel.parts)
                    {
                        errLine = "4";
                        //foreach (TWR1Data td in p.Modules.OfType<TWR1Data>())
                        //{
                        dataModules.AddRange(p.Modules.OfType<RCSLandingAidModule>());
                        //}
                    }
                    errLine = "4a";
                    if (dataModules.Count == 0)
                    {
                        errLine = "4b";
                        curVsl = null;
                    }
                    else if (dataModules.Where(pm => pm.isMasterModule == true).Count() > 0)
                    {
                        errLine = "4c";
                        curVsl = dataModules.Where(pm => pm.isMasterModule == true).First();
                    }
                    else
                    {
                        errLine = "4d";
                        curVsl = dataModules.First();
                    }
                    errLine = "4e";
                    foreach (RCSLandingAidModule tdata in dataModules)
                    {
                        if (tdata == curVsl) //make sure our master is set
                        {
                            curVsl.isMasterModule = true;
                        }
                        else //all other modules are ignored
                        {
                            tdata.isMasterModule = false;
                            tdata.controlState = 0;
                        }
                    }
                    //foreach (RCSLandingAidModule la in p.Modules.OfType<RCSLandingAidModule>())
                    //{
                    //    if (!mdlFound)
                    //    {
                    //        curVsl = la;
                    //        mdlFound = true;
                    //        la.isMasterModule = true;
                    //        curVsl.theLine.SetColors(Color.red, Color.red);
                    //        //Debug.Log("td fnd");
                    //    }
                    //    else
                    //    {
                    //        la.isMasterModule = false;
                    //        //Debug.Log("td not found");
                    //    }
                    //}
                    //if (!mdlFound)
                    //{
                    //    //Debug.Log("vsl nulld not found");
                    //    curVsl = null;
                    //}
                    //if (p.Modules.Contains("TWR1Data"))
                    //{
                    //    errLine = "5";

                    //}
                    //errLine = "6";
                    //goto partFound;
                }
                //Debug.Log("vsl nulld");
                errLine = "7";
                //curVsl = null;
                errLine = "8";
                curBtnState = 0;
                //   }
            }
            catch
            {
                print("LandAid hit Update catch" + errLine); 
                curBtnState = 0;
                if (curVsl != null)
                {
                    curVsl.theLine.SetColors(Color.blue, Color.blue);
                }
                curVsl = null;
            }
            errLine = "9";
            try
            {
                //print("Height " + engageHeight);
                if (selectingTarget)
                {
                    RaycastHit pHit;
                    FlightCamera FlightCam = FlightCamera.fetch;
                    LayerMask pRayMask = 33792; //layermask does not ignore layer 0, why?
                    Ray pRay = FlightCam.mainCamera.ScreenPointToRay(Input.mousePosition);
                    //Ray pRayDown = new Ray(FlightCamera. transform.position , FlightGlobals.currentMainBody.position);
                    Vector3 hitLoc = new Vector3();
                    if (Physics.Raycast(pRay, out pHit, 2000f, pRayMask)) //cast ray
                    {
                        hitLoc = pHit.point;
                        // print(hitLoc);
                        curVsl.theLine.SetWidth(0, 1);
                        curVsl.theLine.SetPosition(0, hitLoc);
                        curVsl.theLine.SetPosition(1, hitLoc + ((hitLoc - FlightGlobals.ActiveVessel.mainBody.position).normalized) * 7);
                        if (!overWindow)
                        {
                            if (Input.GetKeyDown(KeyCode.Mouse0))
                            {
                                if (checkBlizzyToolbar)
                                {
                                    RCSla1Btn.Drawable = null;
                                }
                                else
                                {
                                    showLAMenu = false;
                                }
                                selectingTarget = false;
                                curVsl.targetLocation = hitLoc;
                                curVsl.targetSelected = true;
                            }
                        }
                    }
                }
                errLine = "10";
                if (lastBtnState != curBtnState)
                {
                    switch (curBtnState)
                    {
                        case 0:
                            if (checkBlizzyToolbar)
                            {
                                RCSla1Btn.TexturePath = "Diazo/RCSLandAid/iconWhiteB";
                            }
                            else
                            {
                                LAButton.SetTexture(btnGray);
                            }
                            break;
                        case 1:
                            if (checkBlizzyToolbar)
                            {
                                RCSla1Btn.TexturePath = "Diazo/RCSLandAid/iconBlue";
                            }
                            else
                            {
                                LAButton.SetTexture(btnBlue);
                            }
                            break;
                        case 2:
                            if (checkBlizzyToolbar)
                            {
                                RCSla1Btn.TexturePath = "Diazo/RCSLandAid/iconBlueEnabled";
                            }
                            else
                            {
                                LAButton.SetTexture(btnBlueEnable);
                            }
                            break;
                        case 3:
                            if (checkBlizzyToolbar)
                            {
                                RCSla1Btn.TexturePath = "Diazo/RCSLandAid/iconRed";
                            }
                            else
                            {
                                LAButton.SetTexture(btnRed);
                            }
                            break;
                        case 4:
                            if (checkBlizzyToolbar)
                            {
                                RCSla1Btn.TexturePath = "Diazo/RCSLandAid/iconRedEnabled";
                            }
                            else
                            {
                                LAButton.SetTexture(btnRedEnable);
                            }
                            break;
                    }
                    lastBtnState = curBtnState;
                }
            }
            catch (Exception e)
            {
                Debug.Log("Landing Aid Error " + e);
            }


        }

        public void OnDisable()
        {

            if (ToolbarManager.ToolbarAvailable) //if toolbar loaded, destroy button on leaving flight scene
            {
                RCSla1Btn.Destroy();


            }
            else
            {
                ApplicationLauncher.Instance.RemoveModApplication(LAButton);
            }
            //RCSla.SetValue("EngageHeight", curVsl.engageHeight.ToString());
            //RCSla.SetValue("ForceSAS", forceSASup.ToString());
            // RCSla.Save(KSPUtil.ApplicationRootPath + "GameData/Diazo/RCSLandAid/RCSla.cfg");
        }

        //public void UpdateButtons()
        //{
        //    if (checkBlizzyToolbar)
        //    {
        //        if (curVsl.controlState == 0)
        //        {
        //            RCSla1Btn.TexturePath = "Diazo/RCSLandAid/iconWhiteB";
        //        }
        //        else if (curVsl.controlState == 1 && curVsl.inControl)
        //        {
        //            RCSla1Btn.TexturePath = "Diazo/RCSLandAid/iconBlueEnabled";
        //        }
        //        else if (curVsl.controlState == 1 && !curVsl.inControl)
        //        {
        //            RCSla1Btn.TexturePath = "Diazo/RCSLandAid/iconBlue";
        //        }
        //        else if (curVsl.controlState == 2 && curVsl.inControl)
        //        {
        //            RCSla1Btn.TexturePath = "Diazo/RCSLandAid/iconRedEnabled";
        //        }
        //        else if (curVsl.controlState == 2 && !curVsl.inControl)
        //        {
        //            RCSla1Btn.TexturePath = "Diazo/RCSLandAid/iconRed";
        //        }

        //    }
        //    else
        //    {
        //        if(curVsl.controlState == 0)
        //        {
        //            LAButton.SetTexture(btnGray);
        //        }
        //        if (curVsl.controlState == 1 && curVsl.inControl)
        //        {
        //            LAButton.SetTexture(btnBlueEnable);
        //        }
        //        else if (curVsl.controlState == 1 && !curVsl.inControl)
        //        {
        //            LAButton.SetTexture(btnBlue);
        //        }
        //        else if (curVsl.controlState == 2 && curVsl.inControl)
        //        {
        //            LAButton.SetTexture(btnRedEnable);
        //        }
        //        else if (curVsl.controlState == 2 && !curVsl.inControl)
        //        {
        //            LAButton.SetTexture(btnRed);
        //        }

        //    }
        //}

        public void DrawWin(int WindowID)
        {

            GUI.Label(new Rect(10, 20, 100, 20), "Engage At:");
            string engageHeightStr = curVsl.engageHeight.ToString();//same^
            engageHeightStr = GUI.TextField(new Rect(100, 20, 50, 20), engageHeightStr, 5);//same^
            try//same^
            {
                curVsl.engageHeight = Convert.ToInt32(engageHeightStr); //convert string to number
            }
            catch//same^
            {
                engageHeightStr = curVsl.engageHeight.ToString(); //conversion failed, reset change
            }

            GUI.Label(new Rect(10, 40, 100, 20), "Max Tip:");
            string maxTipStr = curVsl.maxTip.ToString();//same^
            maxTipStr = GUI.TextField(new Rect(100, 40, 50, 20), maxTipStr, 5);//same^
            try//same^
            {
                curVsl.maxTip = Convert.ToInt32(maxTipStr); //convert string to number
            }
            catch//same^
            {
                maxTipStr = curVsl.maxTip.ToString(); //conversion failed, reset change
            }

            GUI.Label(new Rect(10, 60, 100, 20), "Speed%:");
            string speedStr = (curVsl.aggresiveness * 100f).ToString("####0");//same^
            speedStr = GUI.TextField(new Rect(100, 60, 50, 20), speedStr, 5);//same^
            try//same^
            {
                curVsl.aggresiveness = (float)(Convert.ToDouble(speedStr) / 100); //convert string to number
            }
            catch//same^
            {
                speedStr = (curVsl.aggresiveness * 100f).ToString("####0"); //conversion failed, reset change
            }
            //if(curVsl.useTip)
            //{
            //    if(GUI.Button(new Rect(10,80,70,20),"Tip: Yes"))
            //    {
            //        curVsl.useTip = false;
            //    }
            //}
            //else
            //{
            //    if (GUI.Button(new Rect(10, 80, 70, 20), "Tip: No"))
            //    {
            //        curVsl.useTip = true;
            //    }
            //}
            //if (curVsl.useRCS)
            //{
            //    if (GUI.Button(new Rect(80, 80, 70, 20), "RCS: Yes"))
            //    {
            //        curVsl.useRCS = false;
            //    }
            //}
            //else
            //{
            //    if (GUI.Button(new Rect(80, 80, 70, 20), "RCS: No"))
            //    {
            //        curVsl.useRCS = true;
            //    }
            //}

        }
    }

    public class RCSLandingAidWindow : MonoBehaviour, IDrawable
    {
        public Rect RCSlaWin = new Rect(0, 0, 180, 90);

        public Vector2 Draw(Vector2 position)
        {

            var oldSkin = GUI.skin;
            GUI.skin = HighLogic.Skin;

            RCSlaWin.x = position.x;
            RCSlaWin.y = position.y;

            GUI.Window(22334567, RCSlaWin, DrawWin, "", GUI.skin.window);
            //RCSlaWin = GUILayout.Window(42334567, RCSlaWin, DrawWin, (string)null, GUI.skin.box);
            GUI.skin = oldSkin;
            if (Input.mousePosition.x > RCSlaWin.x && Input.mousePosition.x < RCSlaWin.x + RCSlaWin.width && (Screen.height - Input.mousePosition.y) > RCSlaWin.y && (Screen.height - Input.mousePosition.y) < RCSlaWin.y + RCSlaWin.height)
            {
                RCSLandAid.RCSLandingAid.overWindow = true;

            }
            else
            {
                RCSLandAid.RCSLandingAid.overWindow = false;
            }
            return new Vector2(RCSlaWin.width, RCSlaWin.height);
        }

        public void DrawWin(int WindowID)
        {

            GUI.Label(new Rect(10, 20, 100, 20), "Engage At:");
            string engageHeightStr = RCSLandingAid.curVsl.engageHeight.ToString();//same^
            engageHeightStr = GUI.TextField(new Rect(100, 20, 50, 20), engageHeightStr, 5);//same^
            try//same^
            {
                RCSLandingAid.curVsl.engageHeight = Convert.ToInt32(engageHeightStr); //convert string to number
            }
            catch//same^
            {
                engageHeightStr = RCSLandingAid.curVsl.engageHeight.ToString(); //conversion failed, reset change
            }

            GUI.Label(new Rect(10, 40, 100, 20), "Max Tip:");
            string maxTipStr = RCSLandingAid.curVsl.maxTip.ToString();//same^
            maxTipStr = GUI.TextField(new Rect(100, 40, 50, 20), maxTipStr, 5);//same^
            try//same^
            {
                RCSLandingAid.curVsl.maxTip = Convert.ToInt32(maxTipStr); //convert string to number
            }
            catch//same^
            {
                maxTipStr = RCSLandingAid.curVsl.maxTip.ToString(); //conversion failed, reset change
            }

            GUI.Label(new Rect(10, 60, 100, 20), "Speed%:");
            string speedStr = (RCSLandingAid.curVsl.aggresiveness * 100f).ToString("####0");//same^
            speedStr = GUI.TextField(new Rect(100, 60, 50, 20), speedStr, 5);//same^
            try//same^
            {
                RCSLandingAid.curVsl.aggresiveness = (float)(Convert.ToDouble(speedStr) / 100); //convert string to number
            }
            catch//same^
            {
                speedStr = (RCSLandingAid.curVsl.aggresiveness * 100f).ToString("####0"); //conversion failed, reset change
            }


        }

        public void Update()
        {

        }
    }
}
