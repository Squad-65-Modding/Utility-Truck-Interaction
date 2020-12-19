/*
 * Utility Truck Interaction - FiveM Port 1.0 Beta
 * 
 * Copyright (c) 2020 PNWParksFan & Christopher M, Inferno Collection. All rights reserved.
 */

using CitizenFX.Core;

namespace InfernoCollection.UtilityTruckInteraction.Client.Models
{
    public class AttachedProp
    {
        public Model Model { get; set; }
        public string Bone { get; set; }
        public Vector3 Position { get; set; }
        public Vector3 Rotation { get; set; } = Vector3.Zero;
    }
}