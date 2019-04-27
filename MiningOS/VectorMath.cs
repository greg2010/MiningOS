using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Text;
using System;
using VRage.Collections;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRage;
using VRageMath;

namespace IngameScript
{
    // Inspired by Whip's scripts
    static class VectorMath
    {

        public static Vector3D GetBodyDirection(Vector3D worldDirection, IMyCubeBlock reference)
        {
            return Vector3D.TransformNormal(worldDirection, MatrixD.Transpose(reference.WorldMatrix));
        }

        public static Vector3D GetWorldDirection(Vector3D bodyDirection, IMyCubeBlock reference)
        {
            return Vector3D.TransformNormal(bodyDirection, reference.WorldMatrix);
        }

        public static void GetRotationAngles(Vector3D vTarget, Vector3D vFront, Vector3D vLeft, Vector3D vUp, out double yaw, out double pitch)
        {
            var projectTargetUp = VectorProjection(vTarget, vUp);
            var projTargetFrontLeft = vTarget - projectTargetUp;

            yaw = VectorAngleBetween(vFront, projTargetFrontLeft);
            pitch = VectorAngleBetween(vTarget, projTargetFrontLeft);

            //---Check if yaw angle is left or right   
            //multiplied by -1 to convert from right hand rule to left hand rule 
            yaw = -1 * Math.Sign(vLeft.Dot(vTarget)) * yaw;

            //---Check if pitch angle is up or down     
            pitch = Math.Sign(vUp.Dot(vTarget)) * pitch;

            //---Check if target vector is pointing opposite the front vector 
            if (pitch == 0 && yaw == 0 && vTarget.Dot(vFront) < 0)
            {
                yaw = Math.PI;
            }
        }

        public static Vector3D VectorProjection(Vector3D a, Vector3D b) //proj a onto b    
        {
            Vector3D projection = a.Dot(b) / b.LengthSquared() * b;
            return projection;
        }

        public static double VectorAngleBetween(Vector3D a, Vector3D b) //returns radians  
        {
            if (a.LengthSquared() == 0 || b.LengthSquared() == 0)
                return 0;
            else
                return Math.Acos(MathHelper.Clamp(a.Dot(b) / a.Length() / b.Length(), -1, 1));
        }
    }
}

