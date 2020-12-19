--
-- Utility Truck Interaction - FiveM Port 1.0 Beta
--
-- Copyright (c) 2020 PNWParksFan & Christopher M, Inferno Collection. All rights reserved.
--

data_file "VEHICLE_METADATA_FILE" "data/vehicles.meta"
data_file "CARCOLS_FILE" "data/carcols.meta"
data_file "VEHICLE_VARIATION_FILE" "data/carvariations.meta"

client_script "UtilityTruckInteraction.Client.net.dll"

server_script "UtilityTruckInteraction.Server.net.dll"

files {
    "data/*.meta",
    "config.json",
    "Newtonsoft.Json.dll"
}

fx_version "cerulean"

game "gta5"