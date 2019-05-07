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
    class RouteController
    {

        public class Waypoint
        {
            readonly public EngineController.ShipWorldWaypoint shipWorldWaypoint;
            readonly public GyroController.ShipOrientation shipOrientation;

            public Waypoint(EngineController.ShipWorldWaypoint shipWorldWaypoint, GyroController.ShipOrientation shipOrientation)
            {
                this.shipWorldWaypoint = shipWorldWaypoint;
                this.shipOrientation = shipOrientation;
            }
        }

        readonly private Logger logger;
        readonly private EngineController engineController;
        readonly private GyroController gyroController;


        private Queue<Waypoint> waypointQueue = new Queue<Waypoint>();

        public RouteController(Logger logger, EngineController engineController, GyroController gyroController)
        {
            this.logger = logger;
            this.engineController = engineController;
            this.gyroController = gyroController;
        }

        public bool EnqueueWaypoint(Waypoint waypoint)
        {
            waypointQueue.Enqueue(waypoint);
            return true;
        }


        public void Tick()
        {
            if (waypointQueue.Count > 0)
            {
                Waypoint waypoint = waypointQueue.Peek();
                bool positionReached = false, angleReached = false;
                if (waypoint.shipWorldWaypoint != null)
                {
                    double distance = engineController.GetDirectionVector(waypoint.shipWorldWaypoint).Length();
                    if (Math.Abs(Math.Round(distance, 1)) <= 0.3) positionReached = true;
                }
                else
                {
                    positionReached = true;
                }

                if (waypoint.shipOrientation != null)
                {
                    double pitchLeft = 0, yawLeft = 0;
                    gyroController.GetRotationAngle(waypoint.shipOrientation, out pitchLeft, out yawLeft);
                    if (Math.Round(pitchLeft, 2) == 0 && Math.Round(yawLeft, 2) == 0) angleReached = true;
                }
                else
                {
                    angleReached = true;
                }

                if (positionReached && angleReached)
                {
                    logger.Log($"Popping {waypoint.shipWorldWaypoint.coordinates.ToString()}");
                    waypointQueue.Dequeue();
                    engineController.KillThrust();
                    gyroController.DisableOverride();
                }
                else
                {
                    logger.Log($"Executing {waypoint.shipWorldWaypoint.coordinates.ToString()} positionReached={positionReached} angleReached={angleReached}");
                    engineController.Tick(waypoint.shipWorldWaypoint);
                    gyroController.Tick(waypoint.shipOrientation);

                }
            }
        }
    }
}
