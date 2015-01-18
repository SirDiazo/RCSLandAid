using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using System.IO;


namespace RCSLandAid
{
    
    public class RCSLandingAidModule : PartModule
    {
        Vector3 surVect; //our surface vector, includes vertical movement
        Transform vslRef; //our vessel reference
        Vector3 worldUp; //world up reference, SOI COM to vslRef origin
        Vector3 moveHoriz; //horizontal movement, world coords
        Vector3 moveHorizLocal; //horizontal movement, vessel local coords

        public float engageHeight = 500;
        public bool targetSelected = false;
        public Vector3 targetLocation;
        public int controlState = 0; //0 = off, 1 = zero vel, 2= hover over point;
       public bool isMasterModule = false;
     
        public bool SASset = false;
        public float vslHeight = 0f;
        
        Quaternion vslRefQuant;
        Vector3 vslUpRef;
        public float thisBodyAccel = 1f;
        private int frameCount = 0;
        public LineRenderer theLine = new LineRenderer();
         GameObject lineObj = new GameObject("Line");
         
        
        public void Start()
        {

            theLine = lineObj.AddComponent<LineRenderer>();
            theLine.material = new Material(Shader.Find("Particles/Additive"));
            //theLine.SetColors(Color.red, Color.red);
            theLine.SetColors(Color.blue, Color.blue);
            theLine.SetWidth(0, 0);
            theLine.SetVertexCount(2);
            theLine.useWorldSpace = true;


        }

        

       
        

       

        public void Update()
        {

            

        }

        public void OnDisable()
        {

           
        
        }
        public void FixedUpdate()
        {
            if (isMasterModule)
            {
                if (controlState > 0 && vslHeight < engageHeight && this.vessel.ActionGroups[KSPActionGroup.SAS] && !SASset) //we just enable the mod, set SAS up direction
                {

                    this.vessel.Autopilot.SetMode(VesselAutopilot.AutopilotMode.StabilityAssist);
                    //calculate our available sideways accel at 7 degrees, our max tip is 10 degrees (set later)
                    float currGrav = (float)(this.vessel.mainBody.gravParameter / (Math.Pow((this.vessel.altitude + this.vessel.mainBody.Radius), 2)));
                    thisBodyAccel = (float)(Mathf.Tan(Mathf.Deg2Rad * 1) * currGrav);

                    vslRefQuant = FindUpVector(out vslUpRef);

                    SASset = true;
                    frameCount = 0;


                }
                if (controlState == 0 || vslHeight > engageHeight || !this.vessel.ActionGroups[KSPActionGroup.SAS]) //one of our conditions is not met, disable mod and unset SAS up dir
                {
                    SASset = false;
                }
                surVect = this.vessel.srf_velocity;
                vslRef = this.vessel.ReferenceTransform;
                worldUp = vslRef.position - this.vessel.mainBody.position;

                moveHoriz = Vector3.Exclude(worldUp, surVect);
                moveHorizLocal = vslRef.InverseTransformDirection(moveHoriz);


                if (this.vessel.mainBody.ocean)
                {
                    vslHeight = (float)Math.Min(this.vessel.altitude, this.vessel.heightFromTerrain);
                }
                else
                {
                    vslHeight = this.vessel.heightFromTerrain;
                }

                if (controlState == 1 && engageHeight > vslHeight) //just cancel velocity as fast as we can
                {


                    if (this.vessel.ActionGroups[KSPActionGroup.SAS])
                    {
                        RCSLandingAid.curBtnState = 2;
                    }
                    else
                    {
                        RCSLandingAid.curBtnState = 1;
                    }
                    TipOverControl(moveHorizLocal, new Vector3(0, 0, 0));
                    
                }

                else if (controlState == 2 && engageHeight > vslHeight)
                {
                    if(this.vessel.ActionGroups[KSPActionGroup.SAS])
                    {
                        RCSLandingAid.curBtnState = 4;
                    }
                    else
                    {
                        RCSLandingAid.curBtnState = 3;
                    }
                    
                    Vector3 targetVect = Vector3.Exclude(worldUp, targetLocation - vslRef.position); //vector from vessel to target
                    Vector3 targetVectLocal = (vslRef.InverseTransformDirection(targetVect)); //our vector, as distance to target, in local coords uses

                    float targetVel = Mathf.Sqrt(2f * Mathf.Abs(targetVectLocal.magnitude) * (thisBodyAccel * 3)); //calc max speed we could be going for this distance to target. desired vel = sqaure root of (2*distToTarget*desiredAccel)

                    Vector3 targetVectLocalModifiedSpeed = targetVectLocal.normalized * targetVel; //this is our desired vector for this distance from target
                    Vector3 moveSpeedTorwardTarget = Vector3.Project(moveHorizLocal, targetVectLocal); //component of our motion to/from target
                    Vector3 moveSpeedSidewaysFromTarget = moveSpeedTorwardTarget - moveHorizLocal;


                    Vector3 currentVectorDiff = moveSpeedTorwardTarget - targetVectLocalModifiedSpeed; //find our difference to pass to tip over control
                    TipOverControl(currentVectorDiff, moveSpeedSidewaysFromTarget); //pass sideways speed raw, we want to cancel it asap.
                }
                else
                {
                    RCSLandingAid.curBtnState = 0;
                }
                
            }
        }

        public void TipOverControl(Vector3 targetVect, Vector3 sideWaysVect)
        {
            //targetVect is our current "movement" relative to our target. In move to point mode, target is moving also as it is the desired velocity for our distance to target
            //worldUp is straight up
            float degTipDesiredForwards = Mathf.Min(20, (targetVect.magnitude / (thisBodyAccel * 4))) * -1f; //degrees to tip, make negative to tip away
            float degTipDesiredSideways = Mathf.Min(20, (sideWaysVect.magnitude / (thisBodyAccel * 4)));// * -1f; //degrees to tip, make negative to tip away
            Vector3 sasDirectionSidewaysOnly = Vector3.RotateTowards(worldUp, vslRef.TransformDirection(sideWaysVect), (Mathf.Deg2Rad * degTipDesiredSideways), 0f);
            Vector3 sasDirection = Vector3.RotateTowards(sasDirectionSidewaysOnly, vslRef.TransformDirection(targetVect), (Mathf.Deg2Rad * degTipDesiredForwards), 0f);
            if (frameCount == 0)
            {
                //print("seting sas");
                this.vessel.Autopilot.SAS.LockHeading(Quaternion.LookRotation(sasDirection, vslUpRef) * vslRefQuant, false);  //no longer locking directlyup

            }
            frameCount = frameCount + 1;
            if (frameCount == 5)
            {
                frameCount = 0;
            }
        }

        

        public enum vslDirection { UP, DOWN, LEFT, RIGHT, FORWARD, BACK }


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

       
    }

    
}
