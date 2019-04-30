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
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRage.Game;
using VRageMath;

namespace IngameScript
{
    class EngineController
    {

        readonly Action<string> Echo;
        private List<IMyThrust> thrusterList;
        readonly private IMyRemoteControl mainController;

        readonly private PID UpDownPID;
        readonly private PID LeftRightPID;
        readonly private PID ForwardBackwardPID;

        public class ShipDestination
        {
            public Vector3D coordinates;
            public IMyCubeBlock reference;
            public double desiredDistance;

            public ShipDestination(Vector3D coordinates, IMyCubeBlock reference, double desiredDistance)
            {
                this.coordinates = coordinates;
                this.reference = reference;
                this.desiredDistance = desiredDistance;
            }
        }

        public ShipDestination shipDestination;

        public EngineController(List<IMyThrust> thrusterList, Action<string> Echo, IMyRemoteControl mainController, double kP, double kI, double kD, double timeStep)
        {
            this.thrusterList = thrusterList;
            this.Echo = Echo;
            this.mainController = mainController;
            UpDownPID = new PID(kP, kI, kD, timeStep);
            LeftRightPID = new PID(kP, kI, kD, timeStep);
            ForwardBackwardPID = new PID(kP, kI, kD, timeStep);
        }

        private PID GetDirectionalPID(Base6Directions.Direction direction)
        {
            switch (direction)
            {
                case Base6Directions.Direction.Up:
                case Base6Directions.Direction.Down:
                    return UpDownPID;
                case Base6Directions.Direction.Left:
                case Base6Directions.Direction.Right:
                    return LeftRightPID;
                case Base6Directions.Direction.Forward:
                case Base6Directions.Direction.Backward:
                    return ForwardBackwardPID;
                default:
                    Echo("Bad direction!");
                    throw new Exception("");
            }
        }

        private Dictionary<Base6Directions.Direction, List<IMyThrust>> GetDirectionalThrusters(IMyCubeBlock reference)
        {
            Dictionary<Base6Directions.Direction, List<IMyThrust>> thrustersDict = new Dictionary<Base6Directions.Direction, List<IMyThrust>>();
            foreach (var thruster in thrusterList)
            {
                var direction = reference.Orientation.TransformDirectionInverse(Base6Directions.GetFlippedDirection(thruster.Orientation.Forward));
                if (!thrustersDict.ContainsKey(direction))
                {
                    thrustersDict.Add(direction, new List<IMyThrust>());
                }
                thrustersDict[direction].Add(thruster);
            }
            return thrustersDict;
        }

        private void ApplySpeedInDirection(Base6Directions.Direction direction, IMyCubeBlock reference, float percent)
        {
            List<IMyThrust> dirThrusters = GetDirectionalThrusters(reference)[direction];
            foreach (var thruster in dirThrusters)
            {
                float clampedThrust = MathHelper.Clamp(percent, 0, 100);
                //Echo($"Applying thrust of {clampedThrust}% in {direction.ToString()} direction");
                thruster.ThrustOverridePercentage = clampedThrust;
            }
        }

        private Dictionary<Base6Directions.Direction, double> CalculateDirectionalDistance(Vector3D directionVector)
        {

            Dictionary<Base6Directions.Direction, double> distanceDict = new Dictionary<Base6Directions.Direction, double>();

            foreach (var dirVector in new Dictionary<Base6Directions.Direction, Vector3D>
                {
                    { Base6Directions.Direction.Up, shipDestination.reference.WorldMatrix.Up },
                    { Base6Directions.Direction.Left, shipDestination.reference.WorldMatrix.Left },
                    { Base6Directions.Direction.Forward, shipDestination.reference.WorldMatrix.Forward },
                })
            {
                Vector3D proj = VectorMath.VectorProjection(directionVector, dirVector.Value);
                double projLen = proj.Length();
                if (dirVector.Value.Dot(proj) < 0)
                {
                    distanceDict.Add(Base6Directions.GetFlippedDirection(dirVector.Key), projLen);
                }
                else
                {
                    distanceDict.Add(dirVector.Key, projLen);
                }
            }

            return distanceDict;
        }

        // INVARIANT: accDict must not contain opposite directions
        private Dictionary<Base6Directions.Direction, double> GetZeroAccelerationVectors(Dictionary<Base6Directions.Direction, double> accDict)
        {
            Dictionary<Base6Directions.Direction, double> zeroedAccDict = new Dictionary<Base6Directions.Direction, double>();
            foreach (var acc in accDict)
            {
                zeroedAccDict.Add(Base6Directions.GetFlippedDirection(acc.Key), 0);
            }
            return zeroedAccDict;
        }

        public void KillThrust()
        {
            foreach (var thruster in thrusterList)
            {
                thruster.ThrustOverride = 0;
            }
        }

        public void Tick()
        {
            if (shipDestination != null)
            {
                Vector3D curPosn = shipDestination.reference.GetPosition();
                Vector3D directionVector = shipDestination.coordinates - curPosn;
                Dictionary<Base6Directions.Direction, double> directionalDistance = CalculateDirectionalDistance(directionVector);
                Echo("Applying PID control...");

                // Apply thrust
                foreach (var dirThrust in directionalDistance)
                {
                    var pid = this.GetDirectionalPID(dirThrust.Key);
                    var thrust = pid.Update(dirThrust.Value);
                    Echo($"Direction: {dirThrust.Key.ToString()} Distance: {Math.Round(dirThrust.Value, 2)}m Thrust: {Math.Round(thrust, 2)}%");
                    this.ApplySpeedInDirection(dirThrust.Key, shipDestination.reference, (float)thrust);
                }
                foreach (var dirThrust in this.GetZeroAccelerationVectors(directionalDistance))
                {
                    this.ApplySpeedInDirection(dirThrust.Key, shipDestination.reference, 0);
                }
            }
        }
    }
}
