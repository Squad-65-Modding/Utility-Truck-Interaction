/*
 * Utility Truck Interaction - FiveM Port 1.0 Beta
 * 
 * Copyright (c) 2020 PNWParksFan & Christopher M, Inferno Collection. All rights reserved.
 */

namespace InfernoCollection.UtilityTruckInteraction.Client.Models
{
    public class Config
    {
        public float OffsetX { get; } = 0f;
        public float OffsetY { get; set; } = -0.35f;
        public float OffsetZ { get; set; } = 0.2f;

        public float RaiseSpeed { get; set; } = 0.3f;
        public float LowerSpeed { get; set; } = 0.25f;
        public float RotateSpeed { get; set; } = 0.15f;
        public bool EnableCollisionProps { get; set; } = true;

        public bool EnableEngine { get; set; } = true;
        public float LimitSpeed { get; set; } = 5f;
        public bool AutoAttachOnMove { get; set; } = true;
    }
}