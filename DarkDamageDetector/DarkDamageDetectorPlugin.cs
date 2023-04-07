using Rocket.Core.Plugins;
using Rocket.Unturned.Events;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace DarkDamageDetector
{
    public class DarkDamageDetectorPlugin : RocketPlugin<DarkDamageDetectorConfiguration>
    {
        private static readonly string art = @"
     ___           __    ___                           ___      __          __          
    / _ \___ _____/ /__ / _ \___ ___ _  ___ ____ ____ / _ \___ / /____ ____/ /____  ____
   / // / _ `/ __/  '_// // / _ `/  ' \/ _ `/ _ `/ -_) // / -_) __/ -_) __/ __/ _ \/ __/
  /____/\_,_/_/ /_/\_\/____/\_,_/_/_/_/\_,_/\_, /\__/____/\__/\__/\__/\__/\__/\___/_/   
                                           /___/                                         
  https://unturnedstore.com/products/1421
  DarkDamageDetector by nn653 with <3
";

        public static DarkDamageDetectorPlugin Instance;
        private Dictionary<CSteamID, Dictionary<EDetectType, short>> _playersUsedKeys;

        private bool _eventsInitialized = false;

        protected override void Load()
        {
            Instance = this;
            _playersUsedKeys = new Dictionary<CSteamID, Dictionary<EDetectType, short>>();
            Level.onLevelLoaded += onLevelLoaded;
            
            Rocket.Core.Logging.Logger.Log(art);
        }
        protected override void Unload()
        {
            _playersUsedKeys.Clear();
            Level.onLevelLoaded -= onLevelLoaded;
            if (_eventsInitialized)
            {
                events(false);
            }
        }


        
        #region -- Methods --

        public void SendUI(ushort id, short key, CSteamID player, string text)
        {
            if (text == null)
            {
                EffectManager.sendUIEffect(id, key, Provider.findTransportConnection(player), false);
            }
            else
            {
                EffectManager.sendUIEffect(id, key, Provider.findTransportConnection(player), false, text ?? string.Empty);
            }
        }

        public short GetKey(CSteamID player, EDetectType detectType, short key)
        {
            switch (detectType)
            {
                case EDetectType.PLAYER_DAMAGE:
                case EDetectType.VEHICLE_DAMAGE:
                case EDetectType.STRUCTURE_DAMAGE:
                case EDetectType.BARRICADE_DAMAGE:
                case EDetectType.ZOMBIE_DAMAGE:
                case EDetectType.RESOURCE_DAMAGE:
                    return getNextKey(player, detectType);
                default:
                    return key;
            }
        }



        private short getNextKey(CSteamID player, EDetectType detectType)
        {
            if (Configuration.Instance.DamageMinKeyRange > Configuration.Instance.DamageMaxKeyRange) { return 0; }

            short key = Configuration.Instance.DamageMinKeyRange;

            if (_playersUsedKeys.ContainsKey(player))
            {
                if (_playersUsedKeys[player].ContainsKey(detectType))
                {
                    key = (++_playersUsedKeys[player][detectType]);
                    if (key > Configuration.Instance.DamageMaxKeyRange)
                    {
                        _playersUsedKeys[player][detectType] = Configuration.Instance.DamageMinKeyRange;
                    }
                }
                else
                {
                    _playersUsedKeys[player].Add(detectType, key);
                }
            }
            else
            {
                _playersUsedKeys.Add(player, new Dictionary<EDetectType, short>());
            }

            return key;
        }

        private void events(bool load)
        {
            if (load)
            {
                DamageTool.damagePlayerRequested += onDamagePlayerRequest;
                UnturnedPlayerEvents.OnPlayerDeath += onPlayerDeath;
                VehicleManager.onDamageVehicleRequested = (DamageVehicleRequestHandler)Delegate.Combine(VehicleManager.onDamageVehicleRequested, new DamageVehicleRequestHandler(onDamageVehicleRequest));
                StructureManager.onDamageStructureRequested = (DamageStructureRequestHandler)Delegate.Combine(StructureManager.onDamageStructureRequested, new DamageStructureRequestHandler(onDamageStructureRequest));
                BarricadeManager.onDamageBarricadeRequested = (DamageBarricadeRequestHandler)Delegate.Combine(BarricadeManager.onDamageBarricadeRequested, new DamageBarricadeRequestHandler(onDamageBarricadeRequest));
                DamageTool.damageZombieRequested += onDamageZombieRequested;
                ResourceManager.onDamageResourceRequested = (DamageResourceRequestHandler)Delegate.Combine(ResourceManager.onDamageResourceRequested, new DamageResourceRequestHandler(onDamageResourceRequest));

                _eventsInitialized = true;
            }
            else
            {
                DamageTool.damagePlayerRequested -= onDamagePlayerRequest;
                UnturnedPlayerEvents.OnPlayerDeath -= onPlayerDeath;
                VehicleManager.onDamageVehicleRequested = (DamageVehicleRequestHandler)Delegate.Remove(VehicleManager.onDamageVehicleRequested, new DamageVehicleRequestHandler(onDamageVehicleRequest));
                StructureManager.onDamageStructureRequested = (DamageStructureRequestHandler)Delegate.Remove(StructureManager.onDamageStructureRequested, new DamageStructureRequestHandler(onDamageStructureRequest));
                BarricadeManager.onDamageBarricadeRequested = (DamageBarricadeRequestHandler)Delegate.Remove(BarricadeManager.onDamageBarricadeRequested, new DamageBarricadeRequestHandler(onDamageBarricadeRequest));
                DamageTool.damageZombieRequested -= onDamageZombieRequested;
                ResourceManager.onDamageResourceRequested = (DamageResourceRequestHandler)Delegate.Remove(ResourceManager.onDamageResourceRequested, new DamageResourceRequestHandler(onDamageResourceRequest));

                _eventsInitialized = false;
            }
        }

        #endregion


        #region -- Events --

        private void onLevelLoaded(int level)
        {
            if (_eventsInitialized == false)
            {
                events(true);
            }
        }
        
        
        private void onDamagePlayerRequest(ref DamagePlayerParameters parameters, ref bool shouldAllow)
        {
            if (shouldAllow == false)
                return;
            
            try
            {
                UnturnedPlayer player = UnturnedPlayer.FromPlayer(parameters.player);
                UnturnedPlayer killer = UnturnedPlayer.FromCSteamID(parameters.killer);

                if (player != null && killer != null && parameters.cause != EDeathCause.ZOMBIE)
                {
                    float times = parameters.times;
                    if (parameters.respectArmor)
                    {
                        times *= DamageTool.getPlayerArmor(parameters.limb, parameters.player);
                    }
                    if (parameters.applyGlobalArmorMultiplier)
                    {
                        times *= Provider.modeConfigData.Players.Armor_Multiplier;
                    }      
                    
                    byte totalDamage = (byte)Mathf.Min(byte.MaxValue, Mathf.FloorToInt(parameters.damage * times));
                    string limbString = parameters.limb.ToString().ToLower();

                    if (Configuration.Instance.TryGetEffectById(EDetectType.PLAYER_DAMAGE, limbString, out UIEffect effect))
                    {
                        SendUI(effect.EffectId, GetKey(killer.CSteamID, EDetectType.PLAYER_DAMAGE, effect.EffectKey), killer.CSteamID, effect.HasText ? string.Format(effect.Text, totalDamage) : null);
                    }
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "OnPlayerDamaged");
            }
        }

        private void onPlayerDeath(UnturnedPlayer player, EDeathCause cause, ELimb limb, CSteamID murderer)
        {
            try
            {
                UnturnedPlayer killer = UnturnedPlayer.FromCSteamID(murderer);

                if (player != null && killer != null && cause != EDeathCause.ZOMBIE && player != killer)
                {
                    if (Configuration.Instance.TryGetEffectById(EDetectType.PLAYER_KILL, limb.ToString().ToLower(), out UIEffect effect))
                    {
                        SendUI(effect.EffectId, GetKey(killer.CSteamID, EDetectType.PLAYER_KILL, effect.EffectKey), killer.CSteamID, effect.HasText ? effect.Text : null);
                    }
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "OnPlayerDeath");
            }
        }

        private void onDamageVehicleRequest(CSteamID instigator, InteractableVehicle vehicle, ref ushort totalDamage, ref bool canRepair, ref bool shouldAllow, EDamageOrigin damageOrigin)
        {
            if (shouldAllow == false)
                return;
            
            try
            {
                UnturnedPlayer player = UnturnedPlayer.FromCSteamID(instigator);

                if (player != null && vehicle?.isDead == false)
                {
                    string damageString = totalDamage.ToString();
                    if (!Configuration.Instance.VehicleDamageViewValue)
                    {
                        damageString = System.Math.Round((((float)vehicle.health / (float)vehicle.asset.health * 100f) - (((float)vehicle.health - (float)totalDamage) / (float)vehicle.asset.health * 100f)), 1).ToString();
                    }

                    UIEffect effect = null;
                    if (Configuration.Instance.TryGetEffectById(EDetectType.VEHICLE_DAMAGE, damageOrigin.ToString().ToLower(), out effect))
                    {
                        SendUI(effect.EffectId, GetKey(player.CSteamID, EDetectType.VEHICLE_DAMAGE, effect.EffectKey), player.CSteamID, effect.Translate(damageString));
                    }

                    if (totalDamage >= vehicle.health && Configuration.Instance.TryGetEffectById(EDetectType.VEHICLE_KILL, damageOrigin.ToString().ToLower(), out effect))
                    {
                        SendUI(effect.EffectId, effect.EffectKey, player.CSteamID, effect.Translate(damageString));
                    }
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "OnDamageVehicleRequest");
            }
        }

        private void onDamageStructureRequest(CSteamID instigator, Transform transform, ref ushort totalDamage, ref bool shouldAllow, EDamageOrigin damageOrigin)
        {
            if (shouldAllow == false)
                return;
            
            try
            {
                UnturnedPlayer player = UnturnedPlayer.FromCSteamID(instigator);

                if (player != null && transform != null)
                {
                    var structureDrop = StructureManager.FindStructureByRootTransform(transform);
                    if (structureDrop != null)
                    {
                        var data = structureDrop.GetServersideData();
                        
                        string damageString = totalDamage.ToString();
                        if (!Configuration.Instance.StructureDamageViewValue)
                        {
                            damageString = System.Math.Round((((float)data.structure.health / (float)data.structure.asset.health * 100f) - ((float)(data.structure.health - totalDamage) / (float)data.structure.asset.health * 100f)), 1).ToString();
                        }

                        UIEffect effect = null;
                        if (Configuration.Instance.TryGetEffectById(EDetectType.STRUCTURE_DAMAGE, damageOrigin.ToString().ToLower(), out effect))
                        {
                            SendUI(effect.EffectId, GetKey(player.CSteamID, EDetectType.STRUCTURE_DAMAGE, effect.EffectKey), player.CSteamID, effect.Translate(damageString));
                        }

                        if (totalDamage >= data.structure.health && Configuration.Instance.TryGetEffectById(EDetectType.STRUCTURE_KILL, damageOrigin.ToString().ToLower(), out effect))
                        {
                            SendUI(effect.EffectId, effect.EffectKey, player.CSteamID, effect.Translate(damageString));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "OnDamageStructureRequest");
            }
        }

        private void onDamageBarricadeRequest(CSteamID instigator, Transform transform, ref ushort totalDamage, ref bool shouldAllow, EDamageOrigin damageOrigin)
        {
            if (shouldAllow == false)
                return;
            
            try
            {
                UnturnedPlayer player = UnturnedPlayer.FromCSteamID(instigator);

                if (player != null && transform != null)
                {
                    var barricadeDrop = BarricadeManager.FindBarricadeByRootTransform(transform);
                    if (barricadeDrop != null)
                    {
                        var data = barricadeDrop.GetServersideData();
                        
                        string damageString = totalDamage.ToString();
                        if (!Configuration.Instance.BarricadeDamageViewValue)
                        {
                            damageString = System.Math.Round((((float)data.barricade.health / (float)data.barricade.asset.health * 100f) - ((float)(data.barricade.health - totalDamage) / (float)data.barricade.asset.health * 100f)), 1).ToString();
                        }

                        UIEffect effect = null;
                        if (Configuration.Instance.TryGetEffectById(EDetectType.BARRICADE_DAMAGE, damageOrigin.ToString().ToLower(), out effect))
                        {
                            SendUI(effect.EffectId, GetKey(player.CSteamID, EDetectType.BARRICADE_DAMAGE, effect.EffectKey), player.CSteamID, effect.Translate(damageString));
                        }

                        if (totalDamage >= data.barricade.health && Configuration.Instance.TryGetEffectById(EDetectType.BARRICADE_KILL, damageOrigin.ToString().ToLower(), out effect))
                        {
                            SendUI(effect.EffectId, effect.EffectKey, player.CSteamID, effect.Translate(damageString));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "OnDamageBarricadeRequest");
            }
        }

        private void onDamageZombieRequested(ref DamageZombieParameters parameters, ref bool shouldAllow)
        {
            if (shouldAllow == false)
                return;

            try
            {
                Player player = parameters.instigator as Player;
                if (player != null)
                {
                    UnturnedPlayer initiator = UnturnedPlayer.FromPlayer(player);
                    if (initiator != null && parameters.zombie != null)
                    {
                        float times = parameters.times;

                        if (parameters.respectArmor)
                        {
                            times *= DamageTool.getZombieArmor(parameters.limb, parameters.zombie);
                        }
                        if (parameters.allowBackstab && Vector3.Dot(parameters.zombie.transform.forward, parameters.direction) > 0.5)
                        {
                            parameters.times *= Provider.modeConfigData.Zombies.Backstab_Multiplier;
                        }

                        ushort totalDamage = (ushort)Mathf.Min(ushort.MaxValue, Mathf.FloorToInt(parameters.damage * times));
                        string limbString = parameters.limb.ToString().ToLower();

                        if (Configuration.Instance.TryGetEffectById(EDetectType.ZOMBIE_DAMAGE, limbString, out UIEffect effect))
                        {
                            SendUI(effect.EffectId, GetKey(initiator.CSteamID, EDetectType.ZOMBIE_DAMAGE, effect.EffectKey), initiator.CSteamID, effect.Translate(totalDamage));
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "OnDamageZombieRequest");
            }
        }

        private void onDamageResourceRequest(CSteamID instigator, Transform transform, ref ushort totalDamage, ref bool shouldAllow, EDamageOrigin damageOrigin)
        {
            if (shouldAllow == false)
                return;

            try
            {
                UnturnedPlayer player = UnturnedPlayer.FromCSteamID(instigator);

                if (player != null && transform != null)
                {
                    if (Regions.tryGetCoordinate(transform.position, out byte x, out byte y))
                    {
                        List<ResourceSpawnpoint> resources = LevelGround.trees[x, y];
                        for (int i = 0; i < resources.Count; i++)
                        {
                            if (resources[i].model == transform)
                            {
                                ResourceSpawnpoint resource = resources[i];

                                string damageString = totalDamage.ToString();
                                if (!Configuration.Instance.ResourceDamageViewValue)
                                {
                                    damageString = System.Math.Round((((float)resource.health / (float)resource.asset.health * 100f) - ((float)(resource.health - totalDamage) / (float)resource.asset.health * 100f)), 1).ToString();
                                }

                                UIEffect effect = null;
                                if (Configuration.Instance.TryGetEffectById(EDetectType.RESOURCE_DAMAGE, damageOrigin.ToString().ToLower(), out effect))
                                {
                                    SendUI(effect.EffectId, GetKey(player.CSteamID, EDetectType.RESOURCE_DAMAGE, effect.EffectKey), player.CSteamID, effect.Translate(damageString));
                                }
                                
                                if (totalDamage >= resource.health && Configuration.Instance.TryGetEffectById(EDetectType.RESOURCE_KILL, damageOrigin.ToString().ToLower(), out effect))
                                {
                                    SendUI(effect.EffectId, effect.EffectKey, player.CSteamID, effect.Translate(damageString));
                                }
                                break;
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Rocket.Core.Logging.Logger.LogException(ex, "OnDamageResourceRequest");
            }
        }

        #endregion
    }
}
