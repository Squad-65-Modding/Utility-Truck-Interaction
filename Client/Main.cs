/*
 * Utility Truck Interaction - FiveM Port 1.0 Beta
 * 
 * Copyright (c) 2020 PNWParksFan & Christopher M, Inferno Collection. All rights reserved.
 */

using System;
using System.Linq;
using CitizenFX.Core;
using Newtonsoft.Json;
using CitizenFX.Core.UI;
using CitizenFX.Core.Native;
using System.Threading.Tasks;
using System.Collections.Generic;
using InfernoCollection.UtilityTruckInteraction.Client.Models;

namespace InfernoCollection.UtilityTruckInteraction.Client
{
    public class Main : BaseScript
    {
        #region Configuration Variables
        internal const int PROP_OFFSET = -20;

        internal const string
            CONFIG_FILE = "config.json",

            BOOM_ANIM_DICT = "va_utillitruck",
            BOOM_ANIM_NAME = "crane",

            ROT_ANIM_DICT = "v_boomtruck",
            ROT_ANIM_NAME = "rotate_crane_base",

            AMBIENT_AUDIO_BANK = "Crane",
            SCRIPT_AUDIO_BANK = "Container_Lifter",
            AUDIO_SCENE = "DOCKS_HEIST_USING_CRANE",
            SOUND_NAME = "Move_U_D",
            AUDIO_REF = "CRANE_SOUNDS";

        internal static readonly IReadOnlyList<string> TEST_BONES = new List<string>()
        {
            "arm_1",
            "arm_2",
            "bucket"
        };

        internal static readonly IReadOnlyList<AttachedProp> ATTACHMENT_PROPS = new List<AttachedProp>()
        {
            new AttachedProp()
            {
                Model = new Model("prop_crate_06a"),
                Bone = "bucket",
                Position = new Vector3(0.0f, -0.36f, -0.86f),
                Rotation = new Vector3(0.0f, 0.0f, 90.0f)
            },

            new AttachedProp()
            {
                Model = new Model("prop_skate_rail"),
                Bone = "arm_1",
                Position = new Vector3(0.0f, 3.0f, 0.06f)
            },

            new AttachedProp()
            {
                Model = new Model("prop_skate_rail"),
                Bone = "arm_2",
                Position = new Vector3(0.0f, -3.4f, 0.1f)
            }
        };
        #endregion

        #region General Variables
        internal static bool
            _inBoomMode = false,
            _inRotationMode = false;

        internal static int _soundId = -1;

        internal static BucketController _activeBc;

        internal static Config _config = new Config();

        internal static List<int> _inUseTrucks = new List<int>();
        #endregion

        #region Load configuration file
        public Main()
        {
            string ConfigFile = null;

            try
            {
                ConfigFile = API.LoadResourceFile("utility-truck-interaction", CONFIG_FILE);
            }
            catch (Exception exception)
            {
                Debug.WriteLine("Error loading configuration from file, could not load file contents. Reverting to default configuration values.");
                Debug.WriteLine(exception.ToString());
            }

            if (ConfigFile != null && ConfigFile != "")
            {
                try
                {
                    _config = JsonConvert.DeserializeObject<Config>(ConfigFile);
                }
                catch (Exception exception)
                {
                    Debug.WriteLine("Error loading configuration from file, contents are invalid. Reverting to default configuration values.");
                    Debug.WriteLine(exception.ToString());
                }
            }
            else
            {
                Debug.WriteLine("Loaded configuration file is empty, reverting to defaults.");
            }
        }
        #endregion

        #region Command Handlers
        [Command("uti")]
        internal async void UtiCommand(string[] args)
        {
            if (args?[0] == "help")
            {
                ShowControls();
                return;
            }
            else if (args?[0] == "unlock")
            {
                if (_activeBc != null)
                {
                    Screen.ShowNotification("~r~You have already unlocked a set of controls. Lock them to unlock another!", true);
                    return;
                }

                if (!Game.Player.Character.IsOnFoot)
                {
                    Screen.ShowNotification("~r~You need to be outside the vehicle!");
                    return;
                }

                RaycastResult raycast = World.RaycastCapsule(
                    Game.PlayerPed.Position,
                    Game.PlayerPed.GetOffsetPosition(new Vector3(0.0f, 10.0f, 0.0f)),
                    0.3f, (IntersectOptions)10, Game.PlayerPed
                );

                if (!raycast.DitHitEntity || !raycast.HitEntity.Model.IsVehicle || !Entity.Exists(raycast.HitEntity) || !IsCompatibleModel((Vehicle)raycast.HitEntity))
                {
                    Screen.ShowNotification("~r~No compatible vehicle found!");
                    return;
                }

                Vehicle vehicle = (Vehicle)raycast.HitEntity;

                if (_inUseTrucks.Contains(vehicle.NetworkId))
                {
                    Screen.ShowNotification($"~r~A player is using this truck at the moment!", true);
                    return;
                }

                if (vehicle.Occupants.Count() > 0)
                {
                    Screen.ShowNotification("~r~Vehicle must be empty!", true);
                    return;
                }

                int timeout = 0;
                while (!API.NetworkHasControlOfNetworkId(vehicle.NetworkId) && timeout < 4)
                {
                    timeout++;
                    API.NetworkRequestControlOfNetworkId(vehicle.NetworkId);
                    await Delay(500);
                }

                if (!API.NetworkHasControlOfNetworkId(vehicle.NetworkId))
                {
                    Screen.ShowNotification("~r~Could not use this vehicle!", true);
                    return;
                }

                TriggerServerEvent("UTI:Server:ClaimBC", vehicle.NetworkId);
            }
            else if (args?[0] == "lock")
            {
                if (_activeBc == null)
                {
                    Screen.ShowNotification("~r~You do not have any controls to lock!", true);
                    return;
                }

                if (Entity.Exists(_activeBc.Truck) && API.NetworkDoesNetworkIdExist(_activeBc.NetworkId))
                {
                    _activeBc.Truck.LockStatus = VehicleLockStatus.Unlocked;

                    _activeBc.Truck = null;

                    TriggerServerEvent("UTI:Server:FreeBC", _activeBc.NetworkId, JsonConvert.SerializeObject(_activeBc));
                }
                else
                {
                    TriggerServerEvent("UTI:Server:FreeBC", _activeBc.NetworkId);
                }

                Screen.ShowNotification("~g~Controls locked!");

                _activeBc = null;
            }
            else
            {
                TriggerEvent("chatMessage", "Utility Truck Interaction", new[] { 0, 0, 255 }, "'/uti help' for controls, '/uti unlock' to use truck, '/uti lock' to finish using truck.");
            }
        }
        #endregion

        #region Event Handlers
        [EventHandler("UTI:Client:Fail")]
        internal void OnFail() => Screen.ShowNotification("~r~A player is using this truck at the moment!", true);

        [EventHandler("UTI:Client:UpdateList")]
        internal void OnUpdateList(string json) => _inUseTrucks = JsonConvert.DeserializeObject<List<int>>(json);

        [EventHandler("UTI:Client:NewBC")]
        internal void OnNewBC(int networkId)
        {
            Vehicle vehicle = VehicleFromNetwork(networkId);

            if (vehicle == null)
            {
                return;
            }

            vehicle.LockStatus = VehicleLockStatus.CannotBeTriedToEnter;

            _activeBc = new BucketController()
            {
                Truck = vehicle,
                NetworkId = vehicle.NetworkId,
                TopSpeed = API.GetVehicleEstimatedMaxSpeed(vehicle.Handle),
                WasEngineOn = vehicle.IsEngineRunning
            };

            if (_config.EnableCollisionProps)
            {
                EnableAttachments();
            }

            Screen.ShowNotification("~g~Controls unlocked!");

            ShowControls();

            Update();
        }

        [EventHandler("UTI:Client:ExisitingBC")]
        internal void OnExisitingBC(int networkId, string json)
        {
            Vehicle vehicle = VehicleFromNetwork(networkId);

            if (vehicle == null)
            {
                return;
            }

            vehicle.LockStatus = VehicleLockStatus.CannotBeTriedToEnter;

            _activeBc = JsonConvert.DeserializeObject<BucketController>(json);

            _activeBc.Truck = vehicle;

            if (_config.EnableCollisionProps)
            {
                EnableAttachments();
            }

            Screen.ShowNotification("~g~Controls unlocked!");

            ShowControls();

            Update();
        }
        #endregion

        #region Tick Handlers
        [Tick]
        internal async Task ModeUpdateTick()
        {
            if (_activeBc == null)
            {
                await Delay(3000);
                return;
            }

            if (!Entity.Exists(_activeBc.Truck))
            {
                TriggerServerEvent("UTI:Server:FreeBC", _activeBc.NetworkId);

                _activeBc.AttachedProps.ForEach(netId => Entity.FromNetworkId(netId)?.Delete());

                _activeBc = null;

                return;
            }

            _inBoomMode =
                API.IsEntityPlayingAnim(_activeBc.Truck.Handle, BOOM_ANIM_DICT, BOOM_ANIM_NAME, 3) ||
                API.HasEntityAnimFinished(_activeBc.Truck.Handle, BOOM_ANIM_DICT, BOOM_ANIM_NAME, 3);

            _inRotationMode =
                API.IsEntityPlayingAnim(_activeBc.Truck.Handle, ROT_ANIM_DICT, ROT_ANIM_NAME, 3) ||
                API.HasEntityAnimFinished(_activeBc.Truck.Handle, ROT_ANIM_DICT, ROT_ANIM_NAME, 3);
        }

        [Tick]
        internal async Task TruckControlTick()
        {
            if (_activeBc == null)
            {
                await Delay(3000);
                return;
            }

            if (Vector3.DistanceSquared(_activeBc.Truck.Bones["bucket"].Position, Game.Player.Character.Position) > 0.5f)
            {
                await Delay(1000);
                return;
            }

            if (Game.IsControlJustPressed(0, Control.VehicleFlySelectTargetRight))
            {
                _activeBc.PlayerControlled = true;

                await Raise();
            }
            else if (Game.IsControlJustPressed(0, Control.VehicleFlySelectTargetLeft))
            {
                _activeBc.PlayerControlled = true;

                await Lower();
            }
            else if (Game.IsControlJustPressed(0, Control.VehicleFlyRollRightOnly))
            {
                _activeBc.PlayerControlled = true;

                await RotateLeft(true);
            }
            else if (Game.IsControlJustPressed(0, Control.VehicleFlyRollLeftOnly))
            {
                _activeBc.PlayerControlled = true;

                await RotateRight(true);
            }
            else if (_activeBc.PlayerControlled && (_inBoomMode || _inRotationMode))
            {
                Stop();

                _activeBc.PlayerControlled = false;
            }
            else
            {
                _activeBc.PlayerControlled = false;
            }
        }
        #endregion

        #region Functions
        #region Controls
        internal void ShowControls()
        {
            TriggerEvent(
                "chatMessage", "Utility Truck Interaction", new[] { 0, 0, 255 },
                $"\nNUMPAD 9 Raise bucket\n" +
                $"NUMPAD 7 Lower bucket\n" +
                $"NUMPAD 4 Rotate Left\n" +
                $"NUMPAD 6 Rotate Right\n"
            );
        }

        internal async Task Raise()
        {
            while (Game.IsControlPressed(0, Control.VehicleFlySelectTargetRight))
            {
                StartBoomMode();

                API.SetEntityAnimSpeed(_activeBc.Truck.Handle, BOOM_ANIM_DICT, BOOM_ANIM_NAME, _config.RaiseSpeed);

                await Delay(0);
            }
        }

        internal async Task Lower()
        {
            while (Game.IsControlPressed(0, Control.VehicleFlySelectTargetLeft))
            {
                StartBoomMode();

                API.SetEntityAnimSpeed(_activeBc.Truck.Handle, BOOM_ANIM_DICT, BOOM_ANIM_NAME, -1 * _config.LowerSpeed);

                await Delay(0);
            }
        }

        internal async Task RotateLeft(bool continuous)
        {
            while (Game.IsControlPressed(0, Control.VehicleFlyRollRightOnly))
            {
                StartRotMode(continuous);

                API.SetEntityAnimSpeed(_activeBc.Truck.Handle, ROT_ANIM_DICT, ROT_ANIM_NAME, _config.RotateSpeed);

                await Delay(0);
            }
        }

        internal async Task RotateRight(bool continuous)
        {

            while (Game.IsControlPressed(0, Control.VehicleFlyRollLeftOnly))
            {
                StartRotMode(continuous);

                API.SetEntityAnimSpeed(_activeBc.Truck.Handle, ROT_ANIM_DICT, ROT_ANIM_NAME, -_config.RotateSpeed);

                await Delay(0);
            }
        }
        #endregion

        #region Actions
        internal void StartBoomMode() => SetMode(() => _inBoomMode, () => _activeBc.BoomTime, BOOM_ANIM_DICT, BOOM_ANIM_NAME, false, false);

        internal void StartRotMode(bool continuous = true) => SetMode(() => _inRotationMode, () => _activeBc.RotationTime, ROT_ANIM_DICT, ROT_ANIM_NAME, continuous, !continuous);

        internal async void SetMode(Func<bool> mode, Func<float> time, string animDict, string animName, bool continuous, bool force)
        {
            Update();

            if (!mode() || force)
            {
                if (force)
                {
                    API.StopEntityAnim(_activeBc.Truck.Handle, animName, animDict, 0.0f);
                }

                int timeout = 0;
                while (!API.HasAnimDictLoaded(animDict) && timeout < 4)
                {
                    timeout++;
                    API.RequestAnimDict(animDict);
                    await Delay(500);
                }

                if (!API.IsEntityPlayingAnim(_activeBc.Truck.Handle, animDict, animName, 3))
                {
                    API.PlayEntityAnim(_activeBc.Truck.Handle, animName, animDict, 8.0f, continuous, !continuous, false, 0.0f, 0);
                }

                await Delay(1000);

                return;
            }

            if (time() > 0)
            {
                API.SetEntityAnimCurrentTime(_activeBc.Truck.Handle, animDict, animName, time());
            }

            if (_config.AutoAttachOnMove)
            {
                SetBucketPedsAttached(true);
            }

            PlaySound();
        }

        internal void Update()
        {
            if (_inBoomMode)
            {
                float animTime = API.GetEntityAnimCurrentTime(_activeBc.Truck.Handle, BOOM_ANIM_DICT, BOOM_ANIM_NAME);

                if (
                    (animTime < 0.005f && _activeBc.BoomTime > 0.005f) ||
                    (animTime > 0.999f && _activeBc.BoomTime < 0.999f)
                )
                {
                    API.SetEntityAnimCurrentTime(_activeBc.Truck.Handle, BOOM_ANIM_DICT, BOOM_ANIM_NAME, _activeBc.BoomTime);
                }
                else
                {
                    _activeBc.BoomTime = animTime;
                    API.SetEntityAnimCurrentTime(_activeBc.Truck.Handle, BOOM_ANIM_DICT, BOOM_ANIM_NAME, _activeBc.BoomTime);
                }
            }

            if (_inRotationMode)
            {
                float animTime = API.GetEntityAnimCurrentTime(_activeBc.Truck.Handle, ROT_ANIM_DICT, ROT_ANIM_NAME);

                if
                (
                    (animTime > 0.999f || animTime < 0.005f) &&
                    _activeBc.RotationTime >= 0.0 && _activeBc.RotationTime <= 1.0f &&
                    _activeBc.RotationTime < 0.98f && _activeBc.RotationTime > 0.01f
                )
                {
                    API.SetEntityAnimCurrentTime(_activeBc.Truck.Handle, ROT_ANIM_DICT, ROT_ANIM_NAME, _activeBc.RotationTime);
                }
                else
                {
                    _activeBc.RotationTime = animTime;
                    API.SetEntityAnimCurrentTime(_activeBc.Truck.Handle, BOOM_ANIM_DICT, BOOM_ANIM_NAME, _activeBc.BoomTime);
                }
            }

            if (_config.EnableEngine)
            {
                if (_activeBc.BoomTime != 0 || _activeBc.RotationTime != 0)
                {
                    _activeBc.Truck.IsEngineRunning = true;
                }
                else
                {
                    _activeBc.Truck.IsEngineRunning = _activeBc.WasEngineOn;
                }
            }

            if (_config.LimitSpeed > 0)
            {
                if
                (
                    _activeBc.BoomTime > 0.2f ||
                    (_activeBc.RotationTime > 0.1f && _activeBc.RotationTime < 0.9f)
                )
                {
                    _activeBc.Truck.MaxSpeed = 5f;
                }
                else
                {
                    _activeBc.Truck.MaxSpeed = _activeBc.TopSpeed;
                }
            }

            if (_activeBc.AttachedProps.Count() > 0)
            {
                foreach (int networkdId in _activeBc.AttachedProps)
                {
                    if (!Entity.Exists(Entity.FromNetworkId(networkdId)))
                    {
                        EnableAttachments();
                        break;
                    }
                }
            }
        }

        internal void Stop()
        {
            if (_inBoomMode)
            {
                API.SetEntityAnimSpeed(_activeBc.Truck.Handle, BOOM_ANIM_DICT, BOOM_ANIM_NAME, 0.0f);
            }

            if (_inRotationMode)
            {
                API.SetEntityAnimSpeed(_activeBc.Truck.Handle, ROT_ANIM_DICT, ROT_ANIM_NAME, 0.0f);
            }

            SetBucketPedsAttached(false);

            StopSound();

            Update();
        }
        #endregion

        #region Misc
        internal Vehicle VehicleFromNetwork(int networkId)
        {
            if (networkId == 0 || !API.NetworkDoesNetworkIdExist(networkId) || !API.NetworkHasControlOfNetworkId(networkId))
            {
                Screen.ShowNotification("~r~Could not use this vehicle!", true);

                TriggerServerEvent("UTI:Server:FreeBC", networkId);
                return null;
            }

            Vehicle vehicle = (Vehicle)Entity.FromNetworkId(networkId);

            if (!Entity.Exists(vehicle))
            {
                Screen.ShowNotification("~r~Could not use this vehicle!", true);

                TriggerServerEvent("UTI:Server:FreeBC", networkId);
                return null;
            }

            return vehicle;
        }

        internal void PlaySound()
        {
            if (_soundId == -1)
            {
                API.RequestAmbientAudioBank(AMBIENT_AUDIO_BANK, false);
                API.RequestScriptAudioBank(SCRIPT_AUDIO_BANK, false);

                if (!API.IsAudioSceneActive(AUDIO_SCENE))
                {
                    API.StartAudioScene(AUDIO_SCENE);
                }

                _soundId = API.GetSoundId();
            }

            if (API.HasSoundFinished(_soundId))
            {
                API.PlaySoundFromEntity(_soundId, SOUND_NAME, _activeBc.Truck.Handle, AUDIO_REF, false, 0);
            }
        }

        internal void StopSound()
        {
            if (_soundId == -1)
            {
                return;
            }

            API.StopSound(_soundId);
            API.ReleaseSoundId(_soundId);

            _soundId = -1;
        }

        internal void SetBucketPedsAttached(bool attach)
        {
            if (!attach)
            {
                Game.Player.Character.Detach();
            }
            else
            {
                Game.Player.Character.AttachTo(_activeBc.Truck.Bones["bucket"], new Vector3(_config.OffsetX, _config.OffsetY, _config.OffsetZ));
            }
        }

        internal async void EnableAttachments()
        {
            _activeBc.AttachedProps.ForEach(netId => Entity.FromNetworkId(netId)?.Delete());

            _activeBc.AttachedProps.Clear();

            List<int> propsToAttach = new List<int>();

            foreach (AttachedProp attachedProp in ATTACHMENT_PROPS)
            {
                Prop prop = await World.CreateProp(attachedProp.Model, _activeBc.Truck.GetOffsetPosition(new Vector3(0.0f, 0.0f, PROP_OFFSET)), true, false);
                prop.IsVisible = false;
                prop.IsPersistent = true;
                
                API.AttachEntityToEntity(prop.Handle, _activeBc.Truck.Handle, _activeBc.Truck.Bones[attachedProp.Bone].Index, attachedProp.Position.X, attachedProp.Position.Y, attachedProp.Position.Z, attachedProp.Rotation.X, attachedProp.Rotation.Y, attachedProp.Rotation.Z, false, false, true, false, 2, true);

                propsToAttach.Add(prop.NetworkId);
            }

            _activeBc.AttachedProps = propsToAttach;
        }
        
        internal bool IsCompatibleModel(Vehicle vehicle)
        {
            foreach(string bone in TEST_BONES)
            {
                if (!vehicle.Bones[bone].IsValid)
                {
                    return false;
                }
            }

            return true;
        }
        #endregion
        #endregion
    }
}