using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;


namespace RCSLandAid
{
    [KSPAddon(KSPAddon.Startup.Flight, false)]
    public class RCSLandingAid :MonoBehaviour
    {
        Vector3 surVect; //our surface vector, includes vertical movement
        Transform vslRef; //our vessel reference
        Vector3 worldUp; //world up reference, SOI COM to vslRef origin
        Vector3 moveHoriz; //horizontal movement, world coords
        Vector3 moveHorizLocal; //horizontal movement, vessel local coords
        
        public static float engageHeight = 500;
        bool targetSelected = false;
        bool selectingTarget = false;
        Vector3 targetLocation;
        int controlState = 0; //0 = off, 1 = zero vel, 2= hover over point;
        private IButton RCSla1Btn;
        GameObject lineObj = new GameObject("Line");
        LineRenderer theLine = new LineRenderer();
        //public static bool forceSASup = true;
        //RCSLandingAidWindow RCSwin;
        private bool SASset = false;
        float vslHeight = 0f;
        Part lastRoot = new Part();
        Quaternion vslRefQuant;
        Vector3 vslUpRef;
        private ConfigNode RCSla;
        public float vslMass = 0;
        public float vslRCSpwr = 0;
        public float thisBodyAccel = 1F;
        private int frameCount = 0;
        ApplicationLauncherButton LAButton = null; //stock toolbar button instance
        bool checkBlizzyToolbar = false;
        Texture2D btnRed = new Texture2D(24, 24);
        Texture2D btnBlue = new Texture2D(24, 24);
        Texture2D btnGray = new Texture2D(24, 24);
        bool showLAMenu = false;
        Rect LASettingsWin = new Rect(Screen.width-200, 40, 100, 70);
        
                
        public void Start()
        {
            print("Landing Aid Ver. 2.1a start.");
            RenderingManager.AddToPostDrawQueue(0, LAOnDraw); //GUI window hook
            byte[] importTxtRed = File.ReadAllBytes(KSPUtil.ApplicationRootPath + "GameData/Diazo/RCSLandAid/iconRed.png"); //load our button textures
            byte[] importTxtBlue = File.ReadAllBytes(KSPUtil.ApplicationRootPath + "GameData/Diazo/RCSLandAid/iconBlue.png");
            byte[] importTxt = File.ReadAllBytes(KSPUtil.ApplicationRootPath + "GameData/Diazo/RCSLandAid/iconWhiteB.png");
            btnRed.LoadImage(importTxtRed);
            btnRed.Apply();
            btnBlue.LoadImage(importTxtBlue);
            btnBlue.Apply();
            btnGray.LoadImage(importTxt);
            btnGray.Apply();
            RCSla = ConfigNode.Load(KSPUtil.ApplicationRootPath + "GameData/Diazo/RCSLandAid/RCSla.cfg");
            engageHeight = (float)Convert.ToDouble(RCSla.GetValue("EngageHeight")); 
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

            theLine = lineObj.AddComponent<LineRenderer>();
            theLine.material = new Material(Shader.Find("Particles/Additive"));
            theLine.SetColors(Color.red, Color.red);
            theLine.SetWidth(0, 0);
            theLine.SetVertexCount(2);
            theLine.useWorldSpace = true;


        }

        public void LAOnDraw()
        {
            if(showLAMenu)
            {
                LASettingsWin = GUI.Window(67347792, LASettingsWin, DrawWin, "Auto Actions", HighLogic.Skin.window);
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

        public void LeftClick()
        {
            if (controlState == 0)
            {
                controlState = 1;
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
                targetSelected = false;
            }
            else 
            {
                controlState = 0;
                targetSelected = false;
                selectingTarget = false;
                theLine.SetWidth(0, 0);
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
        }

        public void RightClick()
        {
            if (controlState == 2)
            {
                controlState = 1;
                targetSelected = false;
                selectingTarget = false;
                theLine.SetWidth(0, 0);
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
                    
            }
            else
            {
                controlState = 2;
                selectingTarget = true;
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
        }

        public void Update()
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
                    theLine.SetWidth(0, 1);
                    theLine.SetPosition(0, hitLoc);
                    theLine.SetPosition(1, hitLoc + ((hitLoc - FlightGlobals.ActiveVessel.mainBody.position).normalized) * 7);
                    if(Input.GetKeyDown(KeyCode.Mouse0))
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
                        targetLocation = hitLoc;
                        targetSelected = true;
                    }
                }
            }
            //if (FlightGlobals.ActiveVessel.rootPart != lastRoot)
            //{
            //    try
            //    {
            //        FlightGlobals.ActiveVessel.OnFlyByWire -= RCSLandAidControl;
            //    }
            //    catch
            //    {

            //    }
            //    FlightGlobals.ActiveVessel.OnFlyByWire += RCSLandAidControl;
            //    lastRoot = FlightGlobals.ActiveVessel.rootPart;
            //}
            
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
            RCSla.SetValue("EngageHeight", engageHeight.ToString());
            //RCSla.SetValue("ForceSAS", forceSASup.ToString());
            RCSla.Save(KSPUtil.ApplicationRootPath + "GameData/Diazo/RCSLandAid/RCSla.cfg");
        }
        public void FixedUpdate()
        {
            if (controlState > 0 && vslHeight < engageHeight && FlightGlobals.ActiveVessel.ActionGroups[KSPActionGroup.SAS] && !SASset)// && forceSASup)
            {
                //Quaternion refRot = new Quaternion(0, 0, 0, 0);
               // Vector3 referUp = FindUpVector();
                //Quaternion refQuant = Quaternion.Euler(referUp);
                //refRot.SetFromToRotation(new Vector3(0,0,0), referUp);
                //FlightGlobals.ActiveVessel.VesselSAS.LockHeading(refRot);
                //print("angle2 " + Vector3.Angle(FlightGlobals.ActiveVessel.VesselSAS.referenceRotation, referUp));
               //print("Set sas ");

                //calculate our available sideways accel at 7 degrees, our max tip is 10 degrees (set later)
                float currGrav = (float)(FlightGlobals.ActiveVessel.mainBody.gravParameter / (Math.Pow((FlightGlobals.ActiveVessel.altitude + FlightGlobals.ActiveVessel.mainBody.Radius),2)));
                thisBodyAccel = (float)(Mathf.Tan(Mathf.Deg2Rad*1) * currGrav);
                 //print("body accel " + thisBodyAccel + " " + currGrav);
                
                vslRefQuant = FindUpVector(out vslUpRef);

                 Quaternion vslRefQuant2fasd = Quaternion.LookRotation(worldUp, vslUpRef) * vslRefQuant;
                //Quaternion refQuant = Quaternion.LookRotation(worldUp, vslRef.up - vslRef.forward) * vslRefQuant;
                 //FlightGlobals.ActiveVessel.VesselSAS.LockHeading(vslRefQuant2fasd, true);  //no longer locking directlyup
                SASset = true;
                frameCount = 0;
            

            }
            if(controlState == 0 || vslHeight > engageHeight || !FlightGlobals.ActiveVessel.ActionGroups[KSPActionGroup.SAS])
            {
                SASset = false;
            }
            //if (SASset)
            //{
                
            //    //print("Set sas " + refQuant);
            //}
            // print("ctrl " + vslRef.position);
            surVect = FlightGlobals.ActiveVessel.srf_velocity;
            vslRef = FlightGlobals.ActiveVessel.ReferenceTransform;
            worldUp = vslRef.position - FlightGlobals.ActiveVessel.mainBody.position;
            //moveHoriz = Vector3.Project(targetLocation,)

            moveHoriz = Vector3.Exclude(worldUp, surVect);
            moveHorizLocal = vslRef.InverseTransformDirection(moveHoriz);


            if (FlightGlobals.ActiveVessel.mainBody.ocean)
            {
                vslHeight = (float)Math.Min(FlightGlobals.ActiveVessel.altitude, FlightGlobals.ActiveVessel.heightFromTerrain);
            }
            else
            {
                vslHeight = FlightGlobals.ActiveVessel.heightFromTerrain;
            }
            //print("hgt " + engageHeight + " " + vslHeight);
            if (controlState == 1 && engageHeight > vslHeight) //just cancel velocity as fast as we can
            {
                //RCSlaCtrl.X = Mathf.Min(moveHorizLocal.x,0.95f);
                //RCSlaCtrl.Z = Mathf.Min(moveHorizLocal.y,0.95f);
                //RCSlaCtrl.Y = Mathf.Min(moveHorizLocal.z, 0.95f);
                TipOverControl(moveHorizLocal, new Vector3(0,0,0));
            }
            else if (controlState == 2 && engageHeight > vslHeight)
            {

                Vector3 targetVect = Vector3.Exclude(worldUp, targetLocation - vslRef.position); //vector from vessel to target
                Vector3 targetVectLocal = (vslRef.InverseTransformDirection(targetVect)); //our vector, as distance to target, in coords RCS uses

                float targetVel = Mathf.Sqrt(2f * Mathf.Abs(targetVectLocal.magnitude) * (thisBodyAccel*3)); //calc max speed we could be going for this distance to target. desired vel = sqaure root of (2*distToTarget*desiredAccel)
                
                Vector3 targetVectLocalModifiedSpeed = targetVectLocal.normalized * targetVel; //this is our desired vector for this distance from target
                Vector3 moveSpeedTorwardTarget = Vector3.Project(moveHorizLocal, targetVectLocal); //component of our motion to/from target
                Vector3 moveSpeedSidewaysFromTarget = moveSpeedTorwardTarget - moveHorizLocal;


                Vector3 currentVectorDiff = moveSpeedTorwardTarget - targetVectLocalModifiedSpeed; //find our difference to pass to tip over control
                TipOverControl(currentVectorDiff, moveSpeedSidewaysFromTarget); //pass sideways speed raw, we want to cancel it asap.
            }
        }

        public void TipOverControl(Vector3 targetVect, Vector3 sideWaysVect)
        {
            //targetVect is our current "movement" relative to our target. In move to point mode, target is moving also as it is the desired velocity for our distance to target
            //worldUp is straight up
            float degTipDesiredForwards = Mathf.Min(20,(targetVect.magnitude/(thisBodyAccel*4))) *  -1f; //degrees to tip, make negative to tip away
            float degTipDesiredSideways = Mathf.Min(20, (sideWaysVect.magnitude / (thisBodyAccel * 4)));// * -1f; //degrees to tip, make negative to tip away
            Vector3 sasDirectionSidewaysOnly = Vector3.RotateTowards(worldUp, vslRef.TransformDirection(sideWaysVect), (Mathf.Deg2Rad * degTipDesiredSideways), 0f);
            Vector3 sasDirection = Vector3.RotateTowards(sasDirectionSidewaysOnly, vslRef.TransformDirection(targetVect), (Mathf.Deg2Rad * degTipDesiredForwards), 0f);
            if (frameCount == 0)
            {
                //print("seting sas");
                FlightGlobals.ActiveVessel.Autopilot.SAS.LockHeading(Quaternion.LookRotation(sasDirection, vslUpRef) * vslRefQuant, false);  //no longer locking directlyup

            }
                frameCount = frameCount + 1;
                if (frameCount == 5)
                {
                    frameCount = 0;
                }
                   
            //print("ang check" + Vector3.Angle(worldUp, sasDirection));
        }

        public void TipOverControlBackup(Vector3 targetVect, Vector3 sideWaysVect) //working code for speed cancel before adding sideways cancel
        {
            //targetVect is our current "movement" relative to our target. In move to point mode, target is moving also as it is the desired velocity for our distance to target
            //worldUp is straight up
            float degTipDesired = Mathf.Min(20, (targetVect.magnitude / (thisBodyAccel * 4))) * -1f; //degrees to tip, make negative to tip away
            Vector3 sasDirection = Vector3.RotateTowards(worldUp, vslRef.TransformDirection(targetVect), (Mathf.Deg2Rad * degTipDesired), 0f);
            if (frameCount == 0)
            {
                //print("seting sas");
                
                FlightGlobals.ActiveVessel.Autopilot.SAS.LockHeading(Quaternion.LookRotation(sasDirection, vslUpRef) * vslRefQuant, false);  //no longer locking directlyup

            }
            frameCount = frameCount + 1;
            if (frameCount == 5)
            {
                frameCount = 0;
            }

            //print("ang check" + Vector3.Angle(worldUp, sasDirection));
        }

            //print(FlightGlobals.ActiveVessel.VesselSAS.lockedHeading);
            //print("ref " + FlightGlobals.ActiveVessel.VesselSAS.referenceRotation+ " " + FlightGlobals.ActiveVessel.VesselSAS.lockedHeading.eulerAngles+ " " + FlightGlobals.ActiveVessel.VesselSAS.currentRotation.eulerAngles);
            //if (FlightGlobals.ActiveVessel.ActionGroups[KSPActionGroup.RCS] && controlState > 0 && vslHeight < engageHeight && FlightGlobals.ActiveVessel.ActionGroups[KSPActionGroup.SAS])
            //{
                // print("Set Rot!");
                
                //refRot.SetLookRotation(FindUpVector());//,FlightGlobals.ActiveVessel.VesselSAS.lockedHeading.eulerAngles);
                //refRot.SetLookRotation(-Vector3.up, Vector3.right);
                //FlightGlobals.ActiveVessel.VesselSAS.referenceRotation = worldUp;
                

            //}
            //print("angle " + Quaternion.Angle(FlightGlobals.ActiveVessel.VesselSAS.currentRotation, FlightGlobals.ActiveVessel.VesselSAS.referenceRotation));
           // print(SASset.ToString() + FindUpVector().normalized + "A" + worldUp.normalized + "B" + FlightGlobals.ActiveVessel.VesselSAS.referenceRotation + "C" + FlightGlobals.ActiveVessel.VesselSAS.lockedHeading.eulerAngles.normalized + "D");
        

        //public void RCSLandAidControl(FlightCtrlState RCSlaCtrl)
        //{
        //   // print("ctrl " + vslRef.position);
        //    surVect = FlightGlobals.ActiveVessel.srf_velocity;
        //    vslRef = FlightGlobals.ActiveVessel.ReferenceTransform;
        //    worldUp = vslRef.position - FlightGlobals.ActiveVessel.mainBody.position;
        //    //moveHoriz = Vector3.Project(targetLocation,)
            
        //    moveHoriz = Vector3.Exclude(worldUp, surVect);
        //    moveHorizLocal = vslRef.InverseTransformDirection(moveHoriz);
            
            
        //    if (FlightGlobals.ActiveVessel.mainBody.ocean)
        //    {
        //        vslHeight = (float)Math.Min(FlightGlobals.ActiveVessel.altitude, FlightGlobals.ActiveVessel.heightFromTerrain);
        //    }
        //    else
        //    {
        //        vslHeight = FlightGlobals.ActiveVessel.heightFromTerrain;
        //    }
        //    //print("hgt " + engageHeight + " " + vslHeight);
        //    if (controlState == 1 && engageHeight > vslHeight) //just cancel velocity as fast as we can
        //    {
        //        //RCSlaCtrl.X = Mathf.Min(moveHorizLocal.x,0.95f);
        //        //RCSlaCtrl.Z = Mathf.Min(moveHorizLocal.y,0.95f);
        //        //RCSlaCtrl.Y = Mathf.Min(moveHorizLocal.z, 0.95f);
        //        TipOverControl(moveHorizLocal, vslRefQuant);
        //    }
        //    else if (controlState == 2 && engageHeight > vslHeight)
        //    {

        //        Vector3 targetVect = Vector3.Exclude(worldUp, targetLocation-vslRef.position); //vector from vessel to target
        //        Vector3 targetVectLocal = (vslRef.InverseTransformDirection(targetVect)); //our vector, as distance to target, in coords RCS uses

        //        float targetVel = Mathf.Sqrt(0.2f * Mathf.Abs(targetVectLocal.magnitude)); //calc max speed we could be going for this distance to target
        //        Vector3 targetVectLocalModifiedSpeed = targetVectLocal.normalized * targetVel; //this is our desired vector for this distance from target
        //        Vector3 currentVectorDiff = moveHorizLocal - targetVectLocalModifiedSpeed;
        //    }
        //}
        
                //start new vector stuff
                //////Vector3 targetSpotDir = Vector3.Project(targetLocation - FlightGlobals.ActiveVessel.mainBody.position, worldUp); //find closest point on vessel up line
                //////Vector3 targetSpotLoc = FlightGlobals.ActiveVessel.mainBody.position + targetSpotDir; //convert from distance to location vector
                //////Vector3 targetSpotTravel = targetSpotLoc - targetLocation; //find travel from vessel up line to target spot
                //////Vector3 targetSpotDirLocal = vslRef.InverseTransformDirection(targetSpotTravel);
                //end new vector stuff

                //print("target " + targetSpotDirLocal + " " + targetVectLocal);
                //Quaternion findTarget = new Quaternion();
                //findTarget.SetLookRotation(worldUp,targetLocation);
                //findTarget.ToAngleAxis(90f,)
                //findTarget.position = FlightGlobals.ActiveVessel.transform.position;
                //findTarget.rotation.SetLookRotation(worldUp, targetLocation);
                //findTarget
                //RCSlaCtrl.X = SetRCSPowerVer2(targetVectLocal.x, moveHorizLocal.x); //no bounce control
                //////RCSlaCtrl.X = SetRCSPowerVer3(targetVectLocal.x, moveHorizLocal.x); //the 3 last working lines
                //////RCSlaCtrl.Y = SetRCSPowerVer3(targetVectLocal.z, moveHorizLocal.z);
                //////RCSlaCtrl.Z = SetRCSPowerVer3(targetVectLocal.y, moveHorizLocal.y);
                //print("Z stuff " + targetVectLocal.y + " " + moveHorizLocal.y); 
          //float targetXVel = Mathf.Sqrt(0.2f * Mathf.Abs(targetVectLocal.x));
                //if(targetVectLocal.x < 0)
                //{
                //    targetXVel = targetXVel * -1f;
                //}
                //float targetYVel = Mathf.Sqrt(0.2f * Mathf.Abs(targetVectLocal.y));
                //if (targetVectLocal.y < 0)
                //{
                //    targetYVel = targetYVel * -1f;
                //}
                //float targetZVel = Mathf.Sqrt(0.2f * Mathf.Abs(targetVectLocal.z));
                //if (targetVectLocal.z < 0)
                //{
                //    targetZVel = targetZVel * -1f;
                //}


                
                //float actualXVel = moveHorizLocal.x;
                //float actualYVel = moveHorizLocal.y;
                //float actualZVel = moveHorizLocal.z;
                //print("vects " + targetVectLocal + " " + moveHorizLocal);
                //print("vectssfd " + targetXVel + " " + actualXVel);
                
                
                //print("ctrl " + vslRef.position + targetVectLocal + moveHorizLocal);
                //float targetX = targetVectLocal.x + moveHorizLocal.x;
                //float targetZ = targetVectLocal.y + moveHorizLocal.y;
                //float targetY = targetVectLocal.z + moveHorizLocal.z;

                //RCSlaCtrl.X = targetVectLocal.x + moveHorizLocal.x;
                //RCSlaCtrl.Z = targetVectLocal.y + moveHorizLocal.y;
                //RCSlaCtrl.Y = targetVectLocal.z + moveHorizLocal.z;

                //RCSlaCtrl.X = Mathf.Min( SetRCSPower(targetVectLocal.x,moveHorizLocal.x),0.95f);
                //    RCSlaCtrl.Z = Mathf.Min(SetRCSPower(targetVectLocal.y,moveHorizLocal.y),0.95f);
                //    RCSlaCtrl.Y = Mathf.Min(SetRCSPower(targetVectLocal.z, moveHorizLocal.z), 0.95f);
            
            
                
            
        
        public float SetRCSPowerVer3(float targetDist, float actualVel) //need to limit return value by distance to stop bouncing
        {
            float rcsThrust = SetRCSPowerVer2(targetDist, actualVel);
            if(rcsThrust == 0f) //error check, this should never hit using floats but....
            {
                return rcsThrust;
            }
            if(rcsThrust * targetDist > 0) //make sure we are thrusting in direction of target, otherwise don't bounce compensate, this is true if both values positive, or both negative
            {
                if(rcsThrust > 0) 
                {
                    return Mathf.Min(rcsThrust, targetDist); //moving in positive direction, take smaller number
                }
                else if(rcsThrust< 0)
                {
                    return Mathf.Max(rcsThrust, targetDist);//moving in  negative direction, take larger number, -.1 is larger then -.2
                }
            }
            return rcsThrust;

        }

        public float SetRCSPowerVer2(float targetDist, float actualVel)
        {
            try
            {
                float targetVel = Mathf.Sqrt(0.2f * Mathf.Abs(targetDist)); //calc max speed we could be going for this distance to target
                actualVel = actualVel * -1f; //this vector is backwards for some reason

                if (targetDist < 0) //had to use an ABS above, bring the negative sign back if needed
                {
                    targetVel = targetVel * -1f;
                }
                
               // print("vel check " + targetVel + " " + targetDist + " " + actualVel);
                if (targetVel < 0)//target is negative, relatively
                {
                    if (actualVel > -5f && actualVel > (targetVel * .90)) //if we are under 5 m/s and 95% of target vel, accellerate towards target
                    {
                        return -0.95f;
                    }
                    else if (actualVel < targetVel * 1.01 || actualVel < -5.5) //we are faster then target vel, or we are over speed limit, slow down
                    {
                        return 0.95f;
                    }
                    else
                    {
                        return 0f; //we are good, do nothing.
                    }
                }
                else if(targetDist > 0)//target must be positive, relatively
                {
                    if (actualVel < 5f && actualVel < (targetVel * .90)) //if we are under 5 m/s and 95% of target vel, accellerate towards target
                    {
                        return 0.95f;
                    }
                    else if (actualVel > targetVel * 1.01 || actualVel > 5.5) //we are faster then target vel, or we are over speed limit, slow down
                    {
                        return -0.95f;
                    }
                    else
                    {
                        return 0f; //we are good, do nothing.
                    }
                }
                
                
                    return 0f; //should never hit this, but just in case
                
            }
            catch
            {
                return 0f; //if the square root function above goes wrong, reutrn 0
            }
        }

        public enum vslDirection {UP,DOWN,LEFT,RIGHT,FORWARD,BACK}
        

        public class SaveAngle
        {
            public float angleOff;
            public vslDirection refDir;
        }

        public Quaternion FindUpVector(out Vector3 vslUp)
        {
        List<SaveAngle> anglesList = new List<SaveAngle>();
        anglesList.Add(new SaveAngle() { angleOff = Vector3.Angle(worldUp, vslRef.up), refDir = vslDirection.UP });
        anglesList.Add(new SaveAngle() { angleOff = Vector3.Angle(worldUp, -vslRef.up), refDir = vslDirection.DOWN });
        anglesList.Add(new SaveAngle() { angleOff = Vector3.Angle(worldUp, vslRef.forward), refDir = vslDirection.FORWARD });
        anglesList.Add(new SaveAngle() { angleOff = Vector3.Angle(worldUp, -vslRef.forward), refDir = vslDirection.BACK });
        anglesList.Add(new SaveAngle() { angleOff = Vector3.Angle(worldUp, vslRef.right), refDir = vslDirection.RIGHT });
        anglesList.Add(new SaveAngle() { angleOff = Vector3.Angle(worldUp, -vslRef.right), refDir = vslDirection.LEFT });
        float toReturnAngle = anglesList.Min(p => p.angleOff);
        SaveAngle toReturn = anglesList.First(q => q.angleOff == toReturnAngle);
        vslRefQuant = Quaternion.Euler(90, 0, 0);
        vslUp = -vslRef.forward;
        //print("ref ang " +toReturn.refDir);
        if (toReturn.refDir == vslDirection.UP)
            {
            vslRefQuant = Quaternion.Euler(90, 0, 0);
            vslUp = -vslRef.forward;
            //print("up");
            }
        else if (toReturn.refDir == vslDirection.DOWN)
            {
                vslRefQuant = Quaternion.Euler(-90, 0, 0);
                vslUp = vslRef.forward;
                //print("down");
            }
        else if (toReturn.refDir == vslDirection.FORWARD)
            {
                vslRefQuant = Quaternion.Euler(0, 0, 0);
                vslUp = vslRef.up;
                //print("3");
            }
        else if (toReturn.refDir == vslDirection.BACK)
            {
                vslRefQuant = Quaternion.Euler(0, 180, 0);
                vslUp = vslRef.up;
                //print("4");
            }
        else if (toReturn.refDir == vslDirection.RIGHT)
            {
                vslRefQuant = Quaternion.Euler(0, -90, 0);
                vslUp = vslRef.up;
                //print("5");
            }
        else if (toReturn.refDir == vslDirection.LEFT)
            {
                vslRefQuant = Quaternion.Euler(0, 90, 0);
                vslUp = vslRef.up;
                //print("6");
            }

        //print("angle of ref " + toReturn.refAngle+ " " + worldUp.normalized + " " + Vector3.Angle(toReturn.refAngle,worldUp));
        return vslRefQuant;
        }

        public float SetRCSPower(float targetDist, float moveSpd)
        {
            if (Mathf.Abs(moveSpd) < 8)
            {
                return targetDist + moveSpd;
            }
            else if ((targetDist + moveSpd) * moveSpd > 0)
            {
                return targetDist + moveSpd;
            }
            else
            {
                return 0f;
            }
        }
        public void DrawWin(int WindowID)
        {

            GUI.Label(new Rect(10, 20, 100, 20), "LandAid Height:");
            string engageHeightStr = RCSLandingAid.engageHeight.ToString();//same^
            //GUI.skin.label.alignment = TextAnchor.MiddleRight;
            //GUI.skin.textField.alignment = TextAnchor.MiddleRight;
            engageHeightStr = GUI.TextField(new Rect(10, 40, 55, 20), engageHeightStr, 5);//same^
            try//same^
            {
                RCSLandingAid.engageHeight = Convert.ToInt32(engageHeightStr); //convert string to number
            }
            catch//same^
            {
                engageHeightStr = RCSLandingAid.engageHeight.ToString(); //conversion failed, reset change
                //GUI.FocusControl(""); //non-number key pressed, return control focus to vessel
            }
            //if (RCSLandingAid.forceSASup)
            //{
            //    //GUI.DrawTexture(new Rect(11, 36, 158, 23), BtnTexGrn);
            //    if (GUI.Button(new Rect(10, 35, 160, 25), "Force SAS Up: True"))
            //    {
            //        RCSLandingAid.forceSASup = false;
            //    }
            //}
            //else
            //{
            //    //GUI.DrawTexture(new Rect(11, 36, 158, 23), BtnTexRed);
            //    if (GUI.Button(new Rect(10, 35, 160, 25), "Force SAS Up: False"))
            //    {
            //        RCSLandingAid.forceSASup = true;
            //    }
            //}

        }
    }

    public class RCSLandingAidWindow : MonoBehaviour, IDrawable
    {
       public Rect RCSlaWin = new Rect(0, 0, 180, 70);

        //private bool txtMade = false;
        //Texture2D BtnTexRed = new Texture2D(1, 1);
        //Texture2D BtnTexGrn = new Texture2D(1, 1);
        public Vector2 Draw(Vector2 position)
        {
            //print("SAS "+RCSLandingAid.forceSASup);
            //if (!txtMade)
            //{
            //    BtnTexRed.SetPixel(0, 0, new Color(1, 0, 0, .5f));
            //    BtnTexRed.Apply();
            //    BtnTexGrn.SetPixel(0, 0, new Color(0, 1, 0, .5f));
            //    BtnTexGrn.Apply();
            //    txtMade = true;
            //}
            var oldSkin = GUI.skin;
            GUI.skin = HighLogic.Skin;

            RCSlaWin.x = position.x;
            RCSlaWin.y = position.y;

            GUI.Window(22334567, RCSlaWin, DrawWin, "",GUI.skin.window);
            //RCSlaWin = GUILayout.Window(42334567, RCSlaWin, DrawWin, (string)null, GUI.skin.box);
            GUI.skin = oldSkin;

            return new Vector2(RCSlaWin.width, RCSlaWin.height);
        }

        public void DrawWin(int WindowID)
        {
            
            GUI.Label(new Rect(10, 10, 100, 20), "LandAid Height:");
            string engageHeightStr = RCSLandingAid.engageHeight.ToString();//same^
            //GUI.skin.label.alignment = TextAnchor.MiddleRight;
            //GUI.skin.textField.alignment = TextAnchor.MiddleRight;
            engageHeightStr = GUI.TextField(new Rect(115, 10, 55, 20), engageHeightStr, 5);//same^
            try//same^
            {
                RCSLandingAid.engageHeight = Convert.ToInt32(engageHeightStr); //convert string to number
            }
            catch//same^
            {
                engageHeightStr = RCSLandingAid.engageHeight.ToString(); //conversion failed, reset change
                //GUI.FocusControl(""); //non-number key pressed, return control focus to vessel
            }
            //if (RCSLandingAid.forceSASup)
            //{
            //    //GUI.DrawTexture(new Rect(11, 36, 158, 23), BtnTexGrn);
            //    if (GUI.Button(new Rect(10, 35, 160, 25), "Force SAS Up: True"))
            //    {
            //        RCSLandingAid.forceSASup = false;
            //    }
            //}
            //else
            //{
            //    //GUI.DrawTexture(new Rect(11, 36, 158, 23), BtnTexRed);
            //    if (GUI.Button(new Rect(10, 35, 160, 25), "Force SAS Up: False"))
            //    {
            //        RCSLandingAid.forceSASup = true;
            //    }
            //}
            
        }

        public void Update()
        {
            
        }
    }
}
