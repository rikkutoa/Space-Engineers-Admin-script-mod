namespace midspace.adminscripts
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using Sandbox.Common.ObjectBuilders;
    using Sandbox.Definitions;
    using Sandbox.ModAPI;
    using Sandbox.ModAPI.Interfaces;
    using VRage;
    using VRage.ModAPI;
    using VRage.ObjectBuilders;
    using VRage.Utils;
    using VRageMath;

    public static class Extensions
    {
        #region grid

        #region attached grids

        /// <summary>
        /// Find all grids attached to the specified grid, either by piston, rotor, connector or landing gear.
        /// This will iterate through all attached grids, until all are found.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="type">Specifies if all attached grids will be found or only grids that are attached either by piston or rotor.</param>
        /// <returns>A list of all attached grids, including the original.</returns>
        public static List<IMyCubeGrid> GetAttachedGrids(this IMyEntity entity, AttachedGrids type = AttachedGrids.All)
        {
            var cubeGrid = entity as IMyCubeGrid;

            if (cubeGrid == null)
                return new List<IMyCubeGrid>();

            var results = new List<IMyCubeGrid> { cubeGrid };
            GetAttachedGrids(cubeGrid, ref results, type);
            return results;
        }

        private static void GetAttachedGrids(IMyCubeGrid cubeGrid, ref List<IMyCubeGrid> results, AttachedGrids type)
        {
            if (cubeGrid == null)
                return;

            var blocks = new List<IMySlimBlock>();
            cubeGrid.GetBlocks(blocks, b => b != null && b.FatBlock != null && !b.FatBlock.BlockDefinition.TypeId.IsNull);

            foreach (var block in blocks)
            {
                //MyAPIGateway.Utilities.ShowMessage("Block", string.Format("{0}", block.FatBlock.BlockDefinition.TypeId));

                if (block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_MotorAdvancedStator) ||
                    block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_MotorStator) ||
                    block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_MotorSuspension) ||
                    block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_MotorBase))
                {
                    // The MotorStator which inherits from MotorBase.
                    var motorBase = block.GetObjectBuilder() as MyObjectBuilder_MotorBase;
                    if (motorBase == null || motorBase.RotorEntityId == 0 || !MyAPIGateway.Entities.EntityExists(motorBase.RotorEntityId))
                        continue;
                    var entityParent = MyAPIGateway.Entities.GetEntityById(motorBase.RotorEntityId).Parent as IMyCubeGrid;
                    if (entityParent == null)
                        continue;
                    if (!results.Any(e => e.EntityId == entityParent.EntityId))
                    {
                        results.Add(entityParent);
                        GetAttachedGrids(entityParent, ref results, type);
                    }
                }
                else if (block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_MotorAdvancedRotor) ||
                    block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_MotorRotor) ||
                    block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_RealWheel) ||
                    block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_Wheel))
                {
                    // The Rotor Part.
                    var motorCube = Support.FindRotorBase(block.FatBlock.EntityId);
                    if (motorCube == null)
                        continue;
                    var entityParent = (IMyCubeGrid)motorCube.Parent;
                    if (!results.Any(e => e.EntityId == entityParent.EntityId))
                    {
                        results.Add(entityParent);
                        GetAttachedGrids(entityParent, ref results, type);
                    }
                }
                else if (block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_PistonTop))
                {
                    var pistonTop = block.GetObjectBuilder() as MyObjectBuilder_PistonTop;
                    if (pistonTop == null || pistonTop.PistonBlockId == 0 || !MyAPIGateway.Entities.EntityExists(pistonTop.PistonBlockId))
                        continue;
                    var entityParent = MyAPIGateway.Entities.GetEntityById(pistonTop.PistonBlockId).Parent as IMyCubeGrid;
                    if (entityParent == null)
                        continue;
                    if (!results.Any(e => e.EntityId == entityParent.EntityId))
                    {
                        results.Add(entityParent);
                        GetAttachedGrids(entityParent, ref results, type);
                    }
                }
                else if (block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_ExtendedPistonBase) ||
                    block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_PistonBase))
                {
                    var pistonBase = block.GetObjectBuilder() as MyObjectBuilder_PistonBase;
                    if (pistonBase == null || pistonBase.TopBlockId == 0 || !MyAPIGateway.Entities.EntityExists(pistonBase.TopBlockId))
                        continue;
                    var entityParent = MyAPIGateway.Entities.GetEntityById(pistonBase.TopBlockId).Parent as IMyCubeGrid;
                    if (entityParent == null)
                        continue;
                    if (!results.Any(e => e.EntityId == entityParent.EntityId))
                    {
                        results.Add(entityParent);
                        GetAttachedGrids(entityParent, ref results, type);
                    }
                }
                else if (block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_ShipConnector) && type == AttachedGrids.All)
                {
                    // There isn't a non-Ingame interface for IMyShipConnector at this time.
                    var connector = (Sandbox.ModAPI.Ingame.IMyShipConnector)block.FatBlock;

                    if (connector.IsConnected == false || connector.IsLocked == false || connector.OtherConnector == null)
                        continue;

                    var otherGrid = (IMyCubeGrid)connector.OtherConnector.CubeGrid;

                    if (!results.Any(e => e.EntityId == otherGrid.EntityId))
                    {
                        results.Add(otherGrid);
                        GetAttachedGrids(otherGrid, ref results, type);
                    }
                }
                else if (block.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_LandingGear) && type == AttachedGrids.All)
                {
                    var landingGear = (IMyLandingGear)block.FatBlock;
                    if (landingGear.IsLocked == false)
                        continue;

                    var entity = landingGear.GetAttachedEntity();
                    if (entity == null || !(entity is IMyCubeGrid))
                        continue;

                    var otherGrid = (IMyCubeGrid)entity;
                    if (!results.Any(e => e.EntityId == otherGrid.EntityId))
                    {
                        results.Add(otherGrid);
                        GetAttachedGrids(otherGrid, ref results, type);
                    }
                }
            }

            // Loop through all other grids, find their Landing gear, and figure out if they are attached to <cubeGrid>.
            var allShips = new HashSet<IMyEntity>();
            var checkList = results; // cannot use ref paramter in Lambada expression!?!.
            MyAPIGateway.Entities.GetEntities(allShips, e => e is IMyCubeGrid && !checkList.Contains(e));

            if (type == AttachedGrids.All)
            {
                foreach (IMyCubeGrid ship in allShips)
                {
                    blocks = new List<IMySlimBlock>();
                    ship.GetBlocks(blocks,
                        b =>
                            b != null && b.FatBlock != null && !b.FatBlock.BlockDefinition.TypeId.IsNull &&
                            b.FatBlock is IMyLandingGear);

                    foreach (var block in blocks)
                    {
                        var landingGear = (IMyLandingGear) block.FatBlock;
                        if (landingGear.IsLocked == false)
                            continue;

                        var entity = landingGear.GetAttachedEntity();

                        if (entity == null || entity.EntityId != cubeGrid.EntityId)
                            continue;

                        if (!results.Any(e => e.EntityId == ship.EntityId))
                        {
                            results.Add(ship);
                            GetAttachedGrids(ship, ref results, type);
                        }
                    }
                }
            }
        }

        #endregion

        public static IMyControllableEntity[] FindWorkingCockpits(this IMyEntity entity)
        {
            var cubeGrid = entity as Sandbox.ModAPI.IMyCubeGrid;

            if (cubeGrid != null)
            {
                var blocks = new List<Sandbox.ModAPI.IMySlimBlock>();
                cubeGrid.GetBlocks(blocks, f => f.FatBlock != null && f.FatBlock.IsWorking
                    && f.FatBlock is IMyControllableEntity
                    && f.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_Cockpit));
                return blocks.Select(f => (IMyControllableEntity)f.FatBlock).ToArray();
            }

            return new IMyControllableEntity[0];
        }

        public static void EjectControllingPlayers(this IMyCubeGrid cubeGrid)
        {
            var blocks = new List<Sandbox.ModAPI.IMySlimBlock>();
            cubeGrid.GetBlocks(blocks, f => f.FatBlock != null
                && f.FatBlock is IMyControllableEntity
                && (f.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_Cockpit)
                || f.FatBlock.BlockDefinition.TypeId == typeof(MyObjectBuilder_RemoteControl)));

            foreach (var block in blocks)
            {
                var objectBuilder = block.GetObjectBuilder();
                var cockpitBuilder = block.GetObjectBuilder() as MyObjectBuilder_Cockpit;
                if (cockpitBuilder != null)
                {
                    var controller = block.FatBlock as IMyControllableEntity;

                    if (controller != null)
                    {
                        controller.Use();
                        continue;
                    }
                    continue;
                }

                var remoteBuilder = block.GetObjectBuilder() as MyObjectBuilder_RemoteControl;
                if (remoteBuilder != null)
                {
                    var controller = block.FatBlock as IMyControllableEntity;

                    if (controller != null)
                    {
                        controller.Use();
                        continue;
                    }
                }
            }
        }

        public static bool StopShip(this IMyEntity shipEntity)
        {
            var grids = shipEntity.GetAttachedGrids();

            foreach (var grid in grids)
            {
                var shipCubeGrid = grid.GetObjectBuilder(false) as MyObjectBuilder_CubeGrid;

                if (shipCubeGrid.IsStatic)
                    continue;

                var cockPits = grid.FindWorkingCockpits();

                if (!shipCubeGrid.DampenersEnabled && cockPits.Length > 0)
                {
                    cockPits[0].SwitchDamping();
                }

                foreach (var cockPit in cockPits)
                {
                    cockPit.MoveAndRotateStopped();
                }

                grid.Physics.ClearSpeed();

                // TODO : may need to iterate through thrusters and turn off any thrust override.
                // 01.064.010 requires using the Action("DecreaseOverride") repeatbly until override is 0.
            }

            return true;
        }

        /// <summary>
        /// Generates a list of all owners of the cubegrid and all grids that are statically attached to it.
        /// </summary>
        /// <param name="cubeGrid"></param>
        /// <returns></returns>
        public static List<long> GetAllSmallOwners(this IMyCubeGrid cubeGrid)
        {
            List<IMyCubeGrid> allGrids = cubeGrid.GetAttachedGrids(AttachedGrids.Static);
            HashSet<long> allSmallOwners = new HashSet<long>();

            foreach (var owner in allGrids.SelectMany(myCubeGrid => myCubeGrid.SmallOwners))
            {
                allSmallOwners.Add(owner);
            }

            return allSmallOwners.ToList();
        }

        #endregion

        #region block

        public static bool IsShipControlEnabled(this Sandbox.ModAPI.Ingame.IMyCubeBlock cockpitBlock)
        {
            var definition = MyDefinitionManager.Static.GetCubeBlockDefinition(cockpitBlock.BlockDefinition);
            var cockpitDefintion = definition as MyCockpitDefinition;
            var remoteDefintion = definition as MyRemoteControlDefinition;

            if (cockpitDefintion != null && cockpitDefintion.EnableShipControl)
                return true;
            if (remoteDefintion != null && remoteDefintion.EnableShipControl)
                return true;

            // is Passenger chair.
            return false;
        }

        /// <summary>
        /// Changes owner of invividual cube block.
        /// </summary>
        /// <param name="cube"></param>
        /// <param name="playerId">new owner id</param>
        /// <param name="shareMode">new share mode</param>
        public static void ChangeOwner(this IMyCubeBlock cube, long playerId, MyOwnershipShareModeEnum shareMode)
        {
            var block = (Sandbox.Game.Entities.MyCubeBlock)cube;

            // TODO: Unsure which of these are required. needs further investigation.
            block.ChangeOwner(playerId, shareMode);
            block.ChangeBlockOwnerRequest(playerId, shareMode);
        }

        #endregion

        #region player

        /// <summary>
        /// Determines if the player is an Administrator of the active game session.
        /// </summary>
        /// <param name="player"></param>
        /// <returns>True if is specified player is an Administrator in the active game.</returns>
        public static bool IsAdmin(this IMyPlayer player)
        {
            // Offline mode. You are the only player.
            if (MyAPIGateway.Session.OnlineMode == MyOnlineModeEnum.OFFLINE)
            {
                return true;
            }

            // Hosted game, and the player is hosting the server.
            if (player.IsHost())
            {
                return true;
            }

            // determine if client is admin of Dedicated server.
            var clients = MyAPIGateway.Session.GetCheckpoint("null").Clients;
            if (clients != null)
            {
                var client = clients.FirstOrDefault(c => c.SteamId == player.SteamUserId && c.IsAdmin);
                return client != null;
                // If user is not in the list, automatically assume they are not an Admin.
            }

            // clients is null when it's not a dedicated server.
            // Otherwise Treat everyone as Normal Player.

            return false;
        }

        /// <summary>
        /// Determines if the player is an Author/Creator.
        /// This is used expressly for testing of commands that are not yet ready 
        /// to be released to the public, and should not be visible to the Help command list or accessible.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static bool IsExperimentalCreator(this IMyPlayer player)
        {
            switch (player.SteamUserId)
            {
                case 76561197961224864L:
                    return true;
                case 76561198048142826L:
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Deals 1000 hp of damage to player, killing them instantly.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="damageType"></param>
        public static bool KillPlayer(this IMyPlayer player, MyStringHash damageType)
        {
            var destroyable = player.Controller.ControlledEntity as IMyDestroyableObject;
            if (destroyable == null)
                return false;

            destroyable.DoDamage(1000f, damageType, true);
            return true;
        }

        public static bool TryGetPlayer(this IMyPlayerCollection collection, string name, out IMyPlayer player)
        {
            player = null;
            if (string.IsNullOrEmpty(name))
                return false;
            var players = new List<IMyPlayer>();
            collection.GetPlayers(players, p => p != null);

            player = players.FirstOrDefault(p => p.DisplayName.Equals(name, StringComparison.InvariantCultureIgnoreCase));
            if (player == null)
                return false;

            return true;
        }

        public static bool TryGetPlayer(this IMyPlayerCollection collection, ulong steamId, out IMyPlayer player)
        {
            player = null;
            if (steamId == null)
                return false;
            var players = new List<IMyPlayer>();
            collection.GetPlayers(players, p => p != null);

            player = players.FirstOrDefault(p => p.SteamUserId == steamId);
            if (player == null)
                return false;

            return true;
        }

        public static IMyPlayer Player(this IMyIdentity identity)
        {
            var listPlayers = new List<IMyPlayer>();
            MyAPIGateway.Players.GetPlayers(listPlayers, p => p.PlayerID == identity.PlayerId);
            return listPlayers.FirstOrDefault();
        }

        public static IMyIdentity Player(this IMyPlayer player)
        {
            var listIdentites = new List<IMyIdentity>();
            MyAPIGateway.Players.GetAllIdentites(listIdentites, p => p.IdentityId == player.IdentityId);
            return listIdentites.FirstOrDefault();
        }

        /// <summary>
        /// Used to find the Character Entity (which is the physical representation in game) from the Player (the network connected human).
        /// This is a kludge as a proper API doesn't exist, even though the game code could easily expose this and save all this processing we are forced to do.
        /// </summary>
        /// <param name="player"></param>
        /// <returns></returns>
        public static IMyCharacter GetCharacter(this IMyPlayer player)
        {
            var character = player.Controller.ControlledEntity as IMyCharacter;
            if (character != null)
                return character;

            var cubeBlock = player.Controller.ControlledEntity as IMyCubeBlock;
            if (cubeBlock == null)
                return null;

            var controller = cubeBlock as Sandbox.Game.Entities.MyShipController;
            if (controller != null)
                return controller.Pilot;

            // TODO: test conditions for Cryochamber block.

            // Cannot determine Character controlling MyLargeTurretBase as class is internal.
            // TODO: find if the player is controlling a turret.

            //var charComponent = cubeBlock.Components.Get<MyCharacterComponent>();

            //if (charComponent != null)
            //{
            //    var entity = charComponent.Entity;
            //    MyAPIGateway.Utilities.ShowMessage("Entity", "Good");
            //}
            //var turret = cubeBlock as Sandbox.Game.Weapons.MyLargeTurretBase;
            //var turret = cubeBlock as IMyControllableEntity;

            return null;
        }

        public static bool IsHost(this IMyPlayer player)
        {
            return MyAPIGateway.Multiplayer.IsServerPlayer(player.Client);
        }

        #endregion

        #region entity

        /// <summary>
        /// Creates the objectbuilder in game, and syncs it to the server and all clients.
        /// </summary>
        /// <param name="entity"></param>
        public static void CreateAndSyncEntity(this MyObjectBuilder_EntityBase entity)
        {
            CreateAndSyncEntities(new List<MyObjectBuilder_EntityBase> { entity });
        }

        /// <summary>
        /// Creates the objectbuilders in game, and syncs it to the server and all clients.
        /// </summary>
        /// <param name="entities"></param>
        public static void CreateAndSyncEntities(this List<MyObjectBuilder_EntityBase> entities)
        {
            MyAPIGateway.Entities.RemapObjectBuilderCollection(entities);
            entities.ForEach(item => MyAPIGateway.Entities.CreateFromObjectBuilderAndAdd(item));
            MyAPIGateway.Multiplayer.SendEntitiesCreated(entities);
        }

        public static bool Stop(this IMyEntity entity)
        {
            if (entity is IMyCubeGrid)
            {
                return entity.StopShip();
            }
            else if (entity.Physics != null)
            {
                entity.Physics.ClearSpeed();
                entity.Physics.UpdateAccelerations();
                return true;
            }
            return false;
        }

        #endregion

        #region misc/util

        public static Vector3 ToHsvColor(this Color color)
        {
            var hsvColor = color.ColorToHSV();
            return new Vector3(hsvColor.X, hsvColor.Y * 2f - 1f, hsvColor.Z * 2f - 1f);
        }

        public static Color ToColor(this Vector3 hsv)
        {
            return new Vector3(hsv.X, (hsv.Y + 1f) / 2f, (hsv.Z + 1f) / 2f).HSVtoColor();
        }

        public static SerializableVector3 ToSerializableVector3(this Vector3D v)
        {
            return new SerializableVector3((float)v.X, (float)v.Y, (float)v.Z);
        }

        public static SerializableVector3D ToSerializableVector3D(this Vector3D v)
        {
            return new SerializableVector3D(v.X, v.Y, v.Z);
        }

        public static float ToGridLength(this MyCubeSize cubeSize)
        {
            return MyDefinitionManager.Static.GetCubeSize(cubeSize);
        }

        public static double RoundUpToNearest(this double value, int scale)
        {
            return Math.Ceiling(value / scale) * scale;
        }

        /// <summary>
        /// Replaces the chars from the given string that are not allowed for filenames with a whitespace.
        /// </summary>
        /// <returns>A string where the characters are replaced with a whitespace.</returns>
        public static string ReplaceForbiddenChars(this string originalText)
        {
            if (String.IsNullOrWhiteSpace(originalText))
                return originalText;

            var convertedText = originalText;

            foreach (char invalidChar in Path.GetInvalidFileNameChars())
                if (convertedText.Contains(invalidChar))
                    convertedText = convertedText.Replace(invalidChar, ' ');

            return convertedText;
        }

        /// <summary>
        /// Time elapsed since the start of the game.
        /// This is saved in checkpoint, instead of GameDateTime.
        /// </summary>
        /// <remarks>Copied from Sandbox.Game.World.MySession</remarks>
        public static TimeSpan ElapsedGameTime(this IMySession session)
        {
            return MyAPIGateway.Session.GameDateTime - new DateTime(2081, 1, 1, 0, 0, 0, DateTimeKind.Utc);
        }

        /// <summary>
        /// Adds an element with the provided key and value to the System.Collections.Generic.IDictionary&gt;TKey,TValue&lt;.
        /// If the provide key already exists, then the existing key is updated with the newly supplied value.
        /// </summary>
        /// <typeparam name="TKey"></typeparam>
        /// <typeparam name="TValue"></typeparam>
        /// <param name="dictionary"></param>
        /// <param name="key">The object to use as the key of the element to add.</param>
        /// <param name="value">The object to use as the value of the element to add.</param>
        /// <exception cref="System.ArgumentNullException">key is null</exception>
        /// <exception cref="System.NotSupportedException">The System.Collections.Generic.IDictionary&gt;TKey,TValue&lt; is read-only.</exception>
        public static void Update<TKey, TValue>(this IDictionary<TKey, TValue> dictionary, TKey key, TValue value)
        {
            if (dictionary.ContainsKey(key))
                dictionary[key] = value;
            else
                dictionary.Add(key, value);
        }

        public static void ShowMessage(this IMyUtilities utilities, string sender, string messageText, params object[] args)
        {
            utilities.ShowMessage(sender, string.Format(messageText, args));
        }

        #endregion
    }

    /// <summary>
    /// Specifies which attached grids are found.
    /// </summary>
    public enum AttachedGrids
    {
        /// <summary>
        /// All attached grids will be found.
        /// </summary>
        All,
        /// <summary>
        /// Only grids statically attached to that grid, such as by piston or rotor will be found.
        /// </summary>
        Static
    }
}
