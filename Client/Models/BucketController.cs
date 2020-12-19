/*
 * Utility Truck Interaction - FiveM Port 1.0 Beta
 * 
 * Copyright (c) 2020 PNWParksFan & Christopher M, Inferno Collection. All rights reserved.
 */

using CitizenFX.Core;
using System.Collections.Generic;

namespace InfernoCollection.UtilityTruckInteraction.Client.Models
{
    public class BucketController
    {
        public int NetworkId { get; set; }
        public Vehicle Truck { get; set; }
        public float TopSpeed { get; set; }
        public bool WasEngineOn { get; set; }
        public float BoomTime { get; set; } = -1f;
        public float RotationTime { get; set; } = -1f;
        public bool PlayerControlled { get; set; } = false;
        public List<int> AttachedProps { get; set; } = new List<int>();
    }
}