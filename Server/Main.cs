/*
 * Utility Truck Interaction - FiveM Port 1.0 Beta
 * 
 * Copyright (c) 2020 PNWParksFan & Christopher M, Inferno Collection. All rights reserved.
 */

using System;
using CitizenFX.Core;
using Newtonsoft.Json;
using System.Collections.Generic;

namespace InfernoCollection.VehicleCollection.Server
{
    public class Main : BaseScript
    {
        #region General Variables
        internal List<int> _inUseTrucks = new List<int>();
        internal Dictionary<int, string> _existingBcs = new Dictionary<int, string>();
        #endregion

        #region Event Handlers
        [EventHandler("UTI:Server:ClaimBC")]
        internal void OnUpdateBC([FromSource] Player source, int networkdId)
        {
            if (_inUseTrucks.Contains(networkdId))
            {
                source.TriggerEvent("UTI:Client:Fail");
            }
            else
            {
                _inUseTrucks.Add(networkdId);

                if (_existingBcs.ContainsKey(networkdId))
                {
                    source.TriggerEvent("UTI:Client:ExisitingBC", networkdId, _existingBcs[networkdId]);
                }
                else
                {
                    source.TriggerEvent("UTI:Client:NewBC", networkdId);
                }

                
                TriggerClientEvent("UTI:Client:UpdateList", JsonConvert.SerializeObject(_inUseTrucks));
            }
        }

        [EventHandler("UTI:Server:FreeBC")]
        internal void OnRemoveBC(int networkId, string json = null)
        {
            if (_inUseTrucks.Contains(networkId))
            {
                _inUseTrucks.Remove(networkId);
            }

            if (json != null)
            {
                _existingBcs[networkId] = json;
            }
            else if (_existingBcs.ContainsKey(networkId))
            {
                _existingBcs.Remove(networkId);
            }

            TriggerClientEvent("UTI:Client:UpdateList", JsonConvert.SerializeObject(_inUseTrucks));
        }
        #endregion
    }
}