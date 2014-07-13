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
        
        static float engageHeight = 500;
        bool targetSelected = false;
        bool selectingTarget = false;
        Vector3 targetLocation;
        int controlState = 0; //0 = off, 1 = zero vel, 2= hover over point;
        private IButton RCSla1Btn;
        GameObject lineObj = new GameObject("Line");
        LineRenderer theLine = new LineRenderer();
        //RCSLandingAidWindow RCSwin;

        public void Start()
        {
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
            }
            else 
            {
                controlState = 0;
                targetSelected = false;
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
                theLine.SetWidth(0, 0);
                RCSla1Btn.TexturePath = "Diazo/RCSLandAid/iconBlue";
                RCSLandingAidWindow RCSwin = new RCSLandingAidWindow();
                RCSla1Btn.Drawable = RCSwin;
                    
            }
            else
            {
                controlState = 2;
                RCSla1Btn.TexturePath = "Diazo/RCSLandAid/iconRed";
                selectingTarget = true;
                RCSla1Btn.Drawable = null;

            }
        }

        public void Update()
        {


            if (selectingTarget)
            {
                RaycastHit pHit;
                FlightCamera FlightCam = FlightCamera.fetch;
                LayerMask pRayMask = 33792; //layermask does not ignore layer 0, why?
                Ray pRay = FlightCam.mainCamera.ScreenPointToRay(Input.mousePosition);
                //Ray pRayDown = new Ray(FlightCamera. transform.position , FlightGlobals.currentMainBody.position);
                Vector3 hitLoc = new Vector3();    
                if (Physics.Raycast(pRay, out pHit, 5000f, pRayMask)) //cast ray
                {
                    hitLoc = pHit.point;
                   // print(hitLoc);
                    theLine.SetWidth(0, 1);
                    theLine.SetPosition(0, hitLoc);
                    theLine.SetPosition(1, hitLoc + ((hitLoc - FlightGlobals.ActiveVessel.mainBody.position).normalized) * 5);
                    if(Input.GetKeyDown(KeyCode.Mouse0))
                    {
                        selectingTarget = false;
                        targetLocation = hitLoc;
                        targetSelected = true;
                    }
                }
            }
            
        }

        public void OnDisable()
        {

            if (ToolbarManager.ToolbarAvailable) //if toolbar loaded, destroy button on leaving flight scene
            {
                RCSla1Btn.Destroy();
            }
        }
        public void FixedUpdate()
        {
            
            
        }

        public void RCSLandAidControl(FlightCtrlState RCSlaCtrl)
        {
           // print("ctrl " + vslRef.position);
            surVect = FlightGlobals.ActiveVessel.srf_velocity;
            vslRef = FlightGlobals.ActiveVessel.ReferenceTransform;
            worldUp = vslRef.position - FlightGlobals.ActiveVessel.mainBody.position;
            moveHoriz = Vector3.Exclude(worldUp, surVect);
            moveHorizLocal = vslRef.InverseTransformDirection(moveHoriz);
            float vslHeight = 0f;
            if (FlightGlobals.ActiveVessel.mainBody.ocean)
            {
                vslHeight = (float)Math.Min(FlightGlobals.ActiveVessel.altitude, FlightGlobals.ActiveVessel.heightFromTerrain);
            }
            else
            {
                vslHeight = FlightGlobals.ActiveVessel.heightFromTerrain;
            }
            print("hgt " + engageHeight + " " + vslHeight);
            if (controlState == 1 && engageHeight > vslHeight)
            {
                RCSlaCtrl.X = moveHorizLocal.x;
                RCSlaCtrl.Z = moveHorizLocal.y;
                RCSlaCtrl.Y = moveHorizLocal.z;
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

                RCSlaCtrl.X = SetRCSPower(targetVectLocal.x,moveHorizLocal.x);
                    RCSlaCtrl.Z = SetRCSPower(targetVectLocal.y,moveHorizLocal.y);
                    RCSlaCtrl.Y = SetRCSPower(targetVectLocal.z, moveHorizLocal.z);
            }
                
                
            
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
       public Rect RCSlaWin = new Rect(0, 0, 200, 100);

        public Vector2 Draw(Vector2 position)
        {
            print("drawing win2");
            var oldSkin = GUI.skin;
            GUI.skin = HighLogic.Skin;

            RCSlaWin.x = position.x;
            RCSlaWin.y = position.y;

            //GUI.Window(22334567, RCSlaWin, DrawWin, "Test",GUI.skin.window);
            RCSlaWin = GUILayout.Window(42334567, RCSlaWin, DrawWin, (string)null, GUI.skin.box);
            GUI.skin = oldSkin;

            return new Vector2(RCSlaWin.width, RCSlaWin.height);
        }

        public void DrawWin(int WindowID)
        {
            print("drawing win");
            GUI.Label(new Rect(10, 10, 60, 20), "testing");
        }

        public void Update()
        {
            print("Updated");
        }
    }
}
