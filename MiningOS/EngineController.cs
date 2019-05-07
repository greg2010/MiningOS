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

        readonly private Action<string> Echo;
        private List<IMyThrust> thrusterList;
        readonly private IMyRemoteControl mainController;

        readonly private PID UpDownPID;
        readonly private PID LeftRightPID;
        readonly private PID ForwardBackwardPID;

        public class ShipWorldWaypoint
        {
            readonly public Vector3D coordinates;
            readonly public IMyCubeBlock reference;
            readonly public double maxSpeed;

            public ShipWorldWaypoint(Vector3D coordinates, IMyCubeBlock reference, double maxSpeed)
            {
                this.coordinates = coordinates;
                this.reference = reference;
                this.maxSpeed = maxSpeed;
            }
        }

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
                thruster.ThrustOverridePercentage = clampedThrust;
            }
        }

        private double GetMaxThrustInDirection(IMyCubeBlock reference, Base6Directions.Direction direction)
        {
            var thrusters = GetDirectionalThrusters(reference)[direction];
            return thrusters.Aggregate(0f, (acc, thruster) =>
            {
                return acc + thruster.MaxEffectiveThrust;
            });
        }

        private double MaxAccelerationInDirection(IMyCubeBlock reference, IMyRemoteControl mainController, Base6Directions.Direction direction)
        {
            var mass = mainController.CalculateShipMass().PhysicalMass;

            return GetMaxThrustInDirection(reference, direction) / mass;
        }

        private Dictionary<Base6Directions.Direction, double> ProjectVectorOntoDirections(Vector3D directionVector, ShipWorldWaypoint shipWorldWaypoint)
        {
            Dictionary<Base6Directions.Direction, double> distanceDict = new Dictionary<Base6Directions.Direction, double>();

            foreach (var dirVector in new Dictionary<Base6Directions.Direction, Vector3D>
                {
                    { Base6Directions.Direction.Up, shipWorldWaypoint.reference.WorldMatrix.Up },
                    { Base6Directions.Direction.Left, shipWorldWaypoint.reference.WorldMatrix.Left },
                    { Base6Directions.Direction.Forward, shipWorldWaypoint.reference.WorldMatrix.Forward },
                })
            {
                Vector3D proj = VectorMath.VectorProjection(directionVector, dirVector.Value);
                double projLen = proj.Length();
                if (dirVector.Value.Dot(proj) < 0)
                {
                    distanceDict.Add(Base6Directions.GetFlippedDirection(dirVector.Key), projLen);
                    distanceDict.Add(dirVector.Key, 0);
                }
                else
                {
                    distanceDict.Add(dirVector.Key, projLen);
                    distanceDict.Add(Base6Directions.GetFlippedDirection(dirVector.Key), 0);
                }
            }

            return distanceDict;
        }

        public Vector3D GetDirectionVector(ShipWorldWaypoint shipWorldWaypoint)
        {
            return shipWorldWaypoint.coordinates - shipWorldWaypoint.reference.GetPosition();

        }

        private double GenerateTargetVelocity(double curVelocity, double distanceToTarget, double reverseAcceleration, double maxSpeed)
        {

            if (distanceToTarget <= 0.1d) return 0;
            double distanceToStop = Math.Pow(curVelocity, 2) / (2 * reverseAcceleration);
            if (distanceToStop + 0.01d * curVelocity + 0.01d * distanceToTarget < distanceToTarget)
            {
                return maxSpeed;
            } else
            {
                return 0;
            }
        }

        public void KillThrust()
        {
            foreach (var thruster in thrusterList)
            {
                thruster.ThrustOverride = 0;
            }
        }

        public void Tick(ShipWorldWaypoint shipWorldWaypoint)
        {
            if (shipWorldWaypoint != null)
            {
                Vector3D curPosn = shipWorldWaypoint.reference.GetPosition();
                Vector3D directionVector = this.GetDirectionVector(shipWorldWaypoint);

                Dictionary<Base6Directions.Direction, double> directionalDistance = this.ProjectVectorOntoDirections(directionVector, shipWorldWaypoint);
                Dictionary<Base6Directions.Direction, double> directionalVelocity = this.ProjectVectorOntoDirections(mainController.GetShipVelocities().LinearVelocity, shipWorldWaypoint);

                foreach (var direction in directionalDistance.Keys)
                {
                    double curDirectionalVelocity = directionalVelocity[direction];
                    double curDirectionalDistance = directionalDistance[direction];

                    double reverseAcceleration = this.MaxAccelerationInDirection(shipWorldWaypoint.reference, mainController, Base6Directions.GetFlippedDirection(direction));

                    double expectedVelocity = this.GenerateTargetVelocity(curDirectionalVelocity, curDirectionalDistance, reverseAcceleration, shipWorldWaypoint.maxSpeed);
                    double error = expectedVelocity - curDirectionalVelocity;

                    var pid = this.GetDirectionalPID(direction);
                    var thrust = pid.Update(error);
                    this.ApplySpeedInDirection(direction, shipWorldWaypoint.reference, (float) thrust);
                }
            } else {
                this.KillThrust();
            }
        }
    }
}
