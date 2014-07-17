using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

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
        public static bool forceSASup = true;
        //RCSLandingAidWindow RCSwin;
        private bool SASset = false;
        float vslHeight = 0f;
        Part lastRoot = new Part();
        Quaternion vslRefQuant;
        Vector3 vslUpRef;
        private ConfigNode RCSla;

        
        
        public void Start()
        {
            RCSla = ConfigNode.Load(KSPUtil.ApplicationRootPath + "GameData/Diazo/RCSLandAid/RCSla.cfg");
            engageHeight = (float)Convert.ToDouble(RCSla.GetValue("EngageHeight")); 
            forceSASup = Convert.ToBoolean(RCSla.GetValue("ForceSAS"));    
            FlightGlobals.ActiveVessel.OnFlyByWire += RCSLandAidControl;
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
                
            }
           //RCSLandingAidWindow RCSwin = new RCSLandingAidWindow();

            theLine = lineObj.AddComponent<LineRenderer>();
            theLine.material = new Material(Shader.Find("Particles/Additive"));
            theLine.SetColors(Color.red, Color.red);
            theLine.SetWidth(0, 0);
            theLine.SetVertexCount(2);
            theLine.useWorldSpace = true;


        }

        public void LeftClick()
        {
            if (controlState == 0)
            {
                controlState = 1;
                RCSla1Btn.TexturePath = "Diazo/RCSLandAid/iconBlue";
                RCSla1Btn.Drawable = null;
                selectingTarget = false;
                targetSelected = false;
            }
            else 
            {
                controlState = 0;
                targetSelected = false;
                selectingTarget = false;
                theLine.SetWidth(0, 0);
                RCSla1Btn.TexturePath = "Diazo/RCSLandAid/iconWhiteB";
                RCSla1Btn.Drawable = null;
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
                RCSla1Btn.TexturePath = "Diazo/RCSLandAid/iconBlue";
                
                
                RCSla1Btn.Drawable = null;
                    
            }
            else
            {
                controlState = 2;
                RCSla1Btn.TexturePath = "Diazo/RCSLandAid/iconRed";
                selectingTarget = true;
                RCSLandingAidWindow RCSwin = new RCSLandingAidWindow();
                RCSla1Btn.Drawable = RCSwin; 

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
                        RCSla1Btn.Drawable = null;
                        selectingTarget = false;
                        targetLocation = hitLoc;
                        targetSelected = true;
                    }
                }
            }
            if (FlightGlobals.ActiveVessel.rootPart != lastRoot)
            {
                try
                {
                    FlightGlobals.ActiveVessel.OnFlyByWire -= RCSLandAidControl;
                }
                catch
                {

                }
                FlightGlobals.ActiveVessel.OnFlyByWire += RCSLandAidControl;
                lastRoot = FlightGlobals.ActiveVessel.rootPart;
            }
            
        }

        public void OnDisable()
        {

            if (ToolbarManager.ToolbarAvailable) //if toolbar loaded, destroy button on leaving flight scene
            {
                RCSla1Btn.Destroy();
                RCSla.SetValue("EngageHeight", engageHeight.ToString()); 
                RCSla.SetValue("ForceSAS", forceSASup.ToString());
                RCSla.Save(KSPUtil.ApplicationRootPath + "GameData/Diazo/RCSLandAid/RCSla.cfg");
            }
        }
        public void FixedUpdate()
        {
            if (FlightGlobals.ActiveVessel.ActionGroups[KSPActionGroup.RCS] && controlState > 0 && vslHeight < engageHeight && FlightGlobals.ActiveVessel.ActionGroups[KSPActionGroup.SAS] && !SASset && forceSASup)
            {
                //Quaternion refRot = new Quaternion(0, 0, 0, 0);
               // Vector3 referUp = FindUpVector();
                //Quaternion refQuant = Quaternion.Euler(referUp);
                //refRot.SetFromToRotation(new Vector3(0,0,0), referUp);
                //FlightGlobals.ActiveVessel.VesselSAS.LockHeading(refRot);
                //print("angle2 " + Vector3.Angle(FlightGlobals.ActiveVessel.VesselSAS.referenceRotation, referUp));
               //print("Set sas ");
                
                vslRefQuant = FindUpVector(out vslUpRef);
                
                Quaternion refQuant = Quaternion.LookRotation(worldUp, vslUpRef) * vslRefQuant;
                //Quaternion refQuant = Quaternion.LookRotation(worldUp, vslRef.up - vslRef.forward) * vslRefQuant;
                FlightGlobals.ActiveVessel.VesselSAS.LockHeading(refQuant, true);
                SASset = true;
            

            }
            if(!FlightGlobals.ActiveVessel.ActionGroups[KSPActionGroup.RCS] || controlState == 0 || vslHeight > engageHeight || !FlightGlobals.ActiveVessel.ActionGroups[KSPActionGroup.SAS])
            {
                SASset = false;
            }
            if (SASset)
            {
                
                //print("Set sas " + refQuant);
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
        }

        public void RCSLandAidControl(FlightCtrlState RCSlaCtrl)
        {
           // print("ctrl " + vslRef.position);
            surVect = FlightGlobals.ActiveVessel.srf_velocity;
            vslRef = FlightGlobals.ActiveVessel.ReferenceTransform;
            worldUp = vslRef.position - FlightGlobals.ActiveVessel.mainBody.position;
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
            if (controlState == 1 && engageHeight > vslHeight)
            {
                RCSlaCtrl.X = Mathf.Min(moveHorizLocal.x,0.95f);
                RCSlaCtrl.Z = Mathf.Min(moveHorizLocal.y,0.95f);
                RCSlaCtrl.Y = Mathf.Min(moveHorizLocal.z, 0.95f);
            }
            else if (controlState == 2 && engageHeight > vslHeight)
            {
               
                Vector3 targetVect = Vector3.Exclude(worldUp, vslRef.position - targetLocation);
                Vector3 targetVectLocal = (vslRef.InverseTransformDirection(targetVect))/8;
                //print("ctrl " + vslRef.position + targetVectLocal + moveHorizLocal);
                //float targetX = targetVectLocal.x + moveHorizLocal.x;
                //float targetZ = targetVectLocal.y + moveHorizLocal.y;
                //float targetY = targetVectLocal.z + moveHorizLocal.z;

                //RCSlaCtrl.X = targetVectLocal.x + moveHorizLocal.x;
                //RCSlaCtrl.Z = targetVectLocal.y + moveHorizLocal.y;
                //RCSlaCtrl.Y = targetVectLocal.z + moveHorizLocal.z;

                RCSlaCtrl.X = Mathf.Min( SetRCSPower(targetVectLocal.x,moveHorizLocal.x),0.95f);
                    RCSlaCtrl.Z = Mathf.Min(SetRCSPower(targetVectLocal.y,moveHorizLocal.y),0.95f);
                    RCSlaCtrl.Y = Mathf.Min(SetRCSPower(targetVectLocal.z, moveHorizLocal.z), 0.95f);
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
            if (RCSLandingAid.forceSASup)
            {
                //GUI.DrawTexture(new Rect(11, 36, 158, 23), BtnTexGrn);
                if (GUI.Button(new Rect(10, 35, 160, 25), "Force SAS Up: True"))
                {
                    RCSLandingAid.forceSASup = false;
                }
            }
            else
            {
                //GUI.DrawTexture(new Rect(11, 36, 158, 23), BtnTexRed);
                if (GUI.Button(new Rect(10, 35, 160, 25), "Force SAS Up: False"))
                {
                    RCSLandingAid.forceSASup = true;
                }
            }
            
        }

        public void Update()
        {
            
        }
    }
}
