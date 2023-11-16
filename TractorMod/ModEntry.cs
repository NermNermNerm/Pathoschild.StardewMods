using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Pathoschild.Stardew.Common;
using Pathoschild.Stardew.Common.Utilities;
using Pathoschild.Stardew.TractorMod.Framework;
using Pathoschild.Stardew.TractorMod.Framework.Attachments;
using Pathoschild.Stardew.TractorMod.Framework.Config;
using Pathoschild.Stardew.TractorMod.Framework.ModAttachments;
using Pathoschild.Stardew.TractorMod.Questable;
using StardewModdingAPI;
using StardewModdingAPI.Events;
using StardewModdingAPI.Utilities;
using StardewValley;
using StardewValley.Buildings;
using StardewValley.Characters;
using StardewValley.Extensions;
using StardewValley.GameData.Buildings;
using StardewValley.GameData.Objects;
using StardewValley.Locations;
using StardewValley.Objects;
using StardewValley.TerrainFeatures;

namespace Pathoschild.Stardew.TractorMod
{
    /// <summary>The mod entry point.</summary>
    internal class ModEntry : Mod
    {
        /*********
        ** Fields
        *********/
        /****
        ** Constants
        ****/
        /// <summary>The update rate when only one player is in a location (as a frame multiple).</summary>
        private readonly uint TextureUpdateRateWithSinglePlayer = 30;

        /// <summary>The update rate when multiple players are in the same location (as a frame multiple). This should be more frequent due to sprite broadcasts, new horses instances being created during NetRef&lt;Horse&gt; syncs, etc.</summary>
        private readonly uint TextureUpdateRateWithMultiplePlayers = 3;

        /// <summary>The unique ID for the stable building in <c>Data/Buildings</c>.</summary>
        private readonly string GarageBuildingId = "Pathoschild.TractorMod_Stable";

        /// <summary>The unique ID for the tractor chunk in <c>Data/Objects</c>.</summary>
        private readonly string TractorChunkObjectId = "Pathoschild.TractorMod_TractorChunk";

        /// <summary>The minimum version the host must have for the mod to be enabled on a farmhand.</summary>
        private readonly string MinHostVersion = "4.15.0";

        /// <summary>The base path for assets loaded through the game's content pipeline so other mods can edit them.</summary>
        private readonly string PublicAssetBasePath = "Mods/Pathoschild.TractorMod";

        /// <summary>The message ID for a request to warp a tractor to the given farmhand.</summary>
        private readonly string RequestTractorMessageID = "TractorRequest";

        /****
        ** State
        ****/
        /// <summary>The mod settings.</summary>
        private ModConfig Config = null!; // set in Entry

        /// <summary>The configured key bindings.</summary>
        private ModConfigKeys Keys => this.Config.Controls;

        /// <summary>Manages audio effects for the tractor.</summary>
        private AudioManager AudioManager = null!; // set in Entry

        /// <summary>Manages textures loaded for the tractor and garage.</summary>
        private TextureManager TextureManager = null!; // set in Entry

        /// <summary>The backing field for <see cref="TractorManager"/>.</summary>
        private PerScreen<TractorManager> TractorManagerImpl = null!; // set in Entry

        /// <summary>The tractor being ridden by the current player.</summary>
        private TractorManager TractorManager => this.TractorManagerImpl.Value;

        /// <summary>Whether the mod is enabled for the current farmhand.</summary>
        private bool IsEnabled = true;

        private Texture2D? derelictTractorTexture;


        /*********
        ** Public methods
        *********/
        /// <summary>The mod entry point, called after the mod is first loaded.</summary>
        /// <param name="helper">Provides simplified APIs for writing mods.</param>
        public override void Entry(IModHelper helper)
        {
            this.derelictTractorTexture = helper.ModContent.Load<Texture2D>("assets/QuestSprites.png");

            CommonHelper.RemoveObsoleteFiles(this, "TractorMod.pdb"); // removed in 4.16.5

            // read config
            this.Config = helper.ReadConfig<ModConfig>();

            // init
            I18n.Init(helper.Translation);
            this.AudioManager = new AudioManager(this.Helper.DirectoryPath, isActive: () => this.Config.SoundEffects == TractorSoundType.Tractor);
            this.TextureManager = new(
                directoryPath: this.Helper.DirectoryPath,
                publicAssetBasePath: this.PublicAssetBasePath,
                contentHelper: helper.ModContent,
                monitor: this.Monitor
            );
            this.TractorManagerImpl = new(() =>
            {
                var manager = new TractorManager(this.Config, this.Keys, this.Helper.Reflection, () => this.TextureManager.BuffIconTexture, this.AudioManager);
                this.UpdateConfigFor(manager);
                return manager;
            });
            this.UpdateConfig();


            // hook events
            IModEvents events = helper.Events;
            events.Content.AssetRequested += this.OnAssetRequested;
            events.Content.LocaleChanged += this.OnLocaleChanged;
            events.GameLoop.GameLaunched += this.OnGameLaunched;
            events.GameLoop.SaveLoaded += this.OnSaveLoaded;
            events.GameLoop.DayStarted += this.OnDayStarted;
            events.GameLoop.DayEnding += this.OnDayEnding;
            events.GameLoop.Saved += this.OnSaved;
            events.Display.RenderedWorld += this.OnRenderedWorld;
            events.Input.ButtonsChanged += this.OnButtonsChanged;
            events.World.NpcListChanged += this.OnNpcListChanged;
            events.World.LocationListChanged += this.OnLocationListChanged;
            events.GameLoop.UpdateTicked += this.OnUpdateTicked;
            events.Multiplayer.ModMessageReceived += this.OnModMessageReceived;
            events.Player.Warped += this.OnWarped;

            // validate translations
            if (!helper.Translation.GetTranslations().Any())
                this.Monitor.Log("The translation files in this mod's i18n folder seem to be missing. The mod will still work, but you'll see 'missing translation' messages. Try reinstalling the mod to fix this.", LogLevel.Warn);
        }


        /*********
        ** Private methods
        *********/
        /****
        ** Event handlers
        ****/
        /// <inheritdoc cref="IGameLoopEvents.GameLaunched"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnGameLaunched(object? sender, GameLaunchedEventArgs e)
        {
            // add Generic Mod Config Menu integration
            new GenericModConfigMenuIntegrationForTractor(
                getConfig: () => this.Config,
                reset: () =>
                {
                    this.Config = new ModConfig();
                    this.Helper.WriteConfig(this.Config);
                    this.UpdateConfig();
                },
                saveAndApply: () =>
                {
                    this.Helper.WriteConfig(this.Config);
                    this.UpdateConfig();
                },
                modRegistry: this.Helper.ModRegistry,
                monitor: this.Monitor,
                manifest: this.ModManifest
            ).Register();

            // warn about incompatible mods
            if (this.Helper.ModRegistry.IsLoaded("bcmpinc.HarvestWithScythe"))
                this.Monitor.Log("The 'Harvest With Scythe' mod is compatible with Tractor Mod, but it may break some tractor scythe features. You can ignore this warning if you don't have any scythe issues.", LogLevel.Warn);
        }

        /// <inheritdoc cref="IGameLoopEvents.SaveLoaded"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnSaveLoaded(object? sender, SaveLoadedEventArgs e)
        {
            // load legacy data
            Migrator.AfterLoad(this.Helper, this.Monitor, this.ModManifest.Version);

            // check if mod should be enabled for the current player
            this.IsEnabled = Context.IsMainPlayer;
            if (!this.IsEnabled)
            {
                ISemanticVersion? hostVersion = this.Helper.Multiplayer.GetConnectedPlayer(Game1.MasterPlayer.UniqueMultiplayerID)?.GetMod(this.ModManifest.UniqueID)?.Version;
                if (hostVersion == null)
                {
                    this.IsEnabled = false;
                    this.Monitor.Log("This mod is disabled because the host player doesn't have it installed.", LogLevel.Warn);
                }
                else if (hostVersion.IsOlderThan(this.MinHostVersion))
                {
                    this.IsEnabled = false;
                    this.Monitor.Log($"This mod is disabled because the host player has {this.ModManifest.Name} {hostVersion}, but the minimum compatible version is {this.MinHostVersion}.", LogLevel.Warn);
                }
                else
                    this.IsEnabled = true;
            }
        }

        private static class ModDataKeys
        {
            public const string QuestStatus = "QuestableTractorMod.QuestStatus";
            public const string DerelictPosition = "QuestableTractorMod.DerelictPosition";
        }

        private static bool TryParse(string s, out Vector2 position)
        {
            string[] split = s.Split(",");
            if (split.Length == 2
                && int.TryParse(split[0], out int x)
                && int.TryParse(split[1], out int y))
            {
                position = new Vector2(x,y);
                return true;
            }
            else
            {
                position = new Vector2();
                return false;
            }

        }

        /// <inheritdoc cref="IGameLoopEvents.DayStarted"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnDayStarted(object? sender, DayStartedEventArgs e)
        {
            if (!this.IsEnabled)
                return;

            // reload textures
            this.TextureManager.UpdateTextures();

            // init garages + tractors
            if (Context.IsMainPlayer)
            {
                this.InitializeQuestable();

                foreach (GameLocation location in this.GetLocations())
                {
                    foreach (Stable garage in this.GetGaragesIn(location))
                    {
                        // spawn new tractor if needed
                        Horse? tractor = this.FindHorse(garage.HorseId);
                        if (!garage.isUnderConstruction())
                        {
                            Vector2 tractorTile = this.GetDefaultTractorTile(garage);
                            if (tractor == null)
                            {
                                tractor = new Horse(garage.HorseId, (int)tractorTile.X, (int)tractorTile.Y);
                                location.addCharacter(tractor);
                            }
                            tractor.DefaultPosition = tractorTile;
                        }

                        // normalize tractor
                        if (tractor != null)
                            TractorManager.SetTractorInfo(tractor, disableHorseSounds: this.Config.SoundEffects != TractorSoundType.Horse);

                        // normalize ownership
                        garage.owner.Value = 0;
                        if (tractor != null)
                            tractor.ownerId.Value = 0;

                        // apply textures
                        this.TextureManager.ApplyTextures(tractor, this.IsTractor);
                    }
                }
            }
        }

        private Vector2 derelictPosition;

        private void InitializeQuestable()
        {
            if (!Game1.player.modData.TryGetValue(ModDataKeys.QuestStatus, out string? statusAsString)
                || !Enum.TryParse(statusAsString, true, out RestorationState restorationStatus))
            {
                if (statusAsString is not null)
                {
                    this.Monitor.Log($"Invalid value for {ModDataKeys.QuestStatus}: {statusAsString} -- reverting to NotStarted", LogLevel.Error);
                }
                restorationStatus = RestorationState.NotStarted;
            }

            if (restorationStatus.IsDerelictInTheFields())
            {
                Game1.player.modData.TryGetValue(ModDataKeys.DerelictPosition, out string? positionAsString);
                if (positionAsString is null || !TryParse(positionAsString, out Vector2 position))
                {
                    if (positionAsString is not null)
                    {
                        this.Monitor.Log($"Invalid value for {ModDataKeys.QuestStatus}: {statusAsString} -- finding a new position", LogLevel.Error);
                    }

                    // TODO: Properly find a position.
                    position = new Vector2(75, 14);
                }
                this.derelictPosition = position;
                var tf = new DerelictTractorTerrainFeature(this.derelictTractorTexture!, position);
                Game1.getFarm().terrainFeatures.Add(position, tf);
                Game1.getFarm().terrainFeatures.Add(position + new Vector2(0, 1), tf);
                Game1.getFarm().terrainFeatures.Add(position + new Vector2(1, 1), tf);
                Game1.getFarm().terrainFeatures.Add(position + new Vector2(1, 0), tf);
            }

            RestoreTractorQuest.RestoreQuest(restorationStatus);
        }

        /// <summary>
        ///   Custom classes, like we're doing with the tractor and the quest, don't serialize without some help.
        ///   This method provides that help by converting the objects to player moddata and deleting the objects
        ///   prior to save.  <see cref="InitializeQuestable"/> restores them.
        /// </summary>
        private void CleanUpQuestable()
        {
            if (this.derelictPosition != new Vector2())
            {
                Game1.getFarm().terrainFeatures.Remove(this.derelictPosition!);
                Game1.getFarm().terrainFeatures.Remove(this.derelictPosition! + new Vector2(0, 1));
                Game1.getFarm().terrainFeatures.Remove(this.derelictPosition! + new Vector2(1, 1));
                Game1.getFarm().terrainFeatures.Remove(this.derelictPosition! + new Vector2(1, 0));
                Game1.player.modData[ModDataKeys.DerelictPosition] = FormattableString.Invariant($"{this.derelictPosition.X},{this.derelictPosition.Y}");
            }
            else
            {
                Game1.player.modData.Remove(ModDataKeys.DerelictPosition);
            }

            string? questState = Game1.player.questLog.OfType<RestoreTractorQuest>().FirstOrDefault()?.Serialize();
            if (questState is null)
            {
                Game1.player.modData.Remove(ModDataKeys.QuestStatus);
            }
            else
            {
                Game1.player.modData[ModDataKeys.QuestStatus] = questState;
                Game1.player.questLog.RemoveWhere(q => q is RestoreTractorQuest);
            }
        }

        /// <inheritdoc cref="IContentEvents.AssetRequested"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnAssetRequested(object? sender, AssetRequestedEventArgs e)
        {
            this.AudioManager.OnAssetRequested(e);
            this.TextureManager.OnAssetRequested(e);

            if (e.NameWithoutLocale.IsEquivalentTo("Data/Buildings"))
            {
                e.Edit(editor =>
                {
                    var data = editor.AsDictionary<string, BuildingData>().Data;

                    data[this.GarageBuildingId] = new BuildingData
                    {
                        Name = I18n.Garage_Name(),
                        Description = I18n.Garage_Description(),
                        Texture = $"{this.PublicAssetBasePath}/Garage",
                        BuildingType = typeof(Stable).FullName,
                        SortTileOffset = 1,

                        Builder = Game1.builder_robin,
                        BuildCost = this.Config.BuildPrice,
                        BuildMaterials = this.Config.BuildMaterials
                            .Select(p => new BuildingMaterial
                            {
                                ItemId = p.Key,
                                Amount = p.Value
                            })
                            .ToList(),
                        BuildDays = 2,

                        Size = new Point(4, 2),
                        CollisionMap = "XXXX\nXOOX"
                    };
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Objects"))
            {
                e.Edit(editor =>
                {
                    IDictionary<string, ObjectData> objects = editor.AsDictionary<string, ObjectData>().Data;
                    objects[this.TractorChunkObjectId] = new()
                    {
                        Name = "TractorMod.TractorChunk",
                        DisplayName = $"Tractor Chunk",
                        Description = "A rusted piece of what looks like an old tractor",
                        Type = "Litter",
                        Category = -999,
                        Price = 0,
                        Texture = "Mods/PathosChild.TractorMod/QuestSprites",
                        SpriteIndex = 3,
                    };
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/CraftingRecipes"))
            {
                e.Edit(editor =>
                {
                    IDictionary<string, string> recipes = editor.AsDictionary<string, string>().Data;
                    recipes["TractorMod.TempTractorRecipe"] = $"388 2/Field/{this.TractorChunkObjectId}/false/default/";
                });
            }
            else if (e.NameWithoutLocale.IsEquivalentTo("Data/Mail"))
            {
                e.Edit(editor =>
                {
                    var mailItems = editor.AsDictionary<string, string>().Data;
                    RestoreTractorQuest.AddMailItems(mailItems);
                });
            }
        }

        /// <inheritdoc cref="IContentEvents.LocaleChanged"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnLocaleChanged(object? sender, LocaleChangedEventArgs e)
        {
            this.Helper.GameContent.InvalidateCache("Data/Buildings");
        }

        /// <inheritdoc cref="IWorldEvents.LocationListChanged"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnLocationListChanged(object? sender, LocationListChangedEventArgs e)
        {
            if (!this.IsEnabled)
                return;

            // rescue lost tractors
            if (Context.IsMainPlayer)
            {
                foreach (GameLocation location in e.Removed)
                {
                    foreach (Horse tractor in this.GetTractorsIn(location).ToArray())
                        this.DismissTractor(tractor);
                }
            }
        }

        /// <inheritdoc cref="IWorldEvents.NpcListChanged"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnNpcListChanged(object? sender, NpcListChangedEventArgs e)
        {
            if (!this.IsEnabled)
                return;

            // workaround for instantly-built tractors spawning a horse
            if (Context.IsMainPlayer && e.Location.buildings.Any())
            {
                Horse[] horses = e.Added.OfType<Horse>().ToArray();
                if (horses.Any())
                {
                    HashSet<Guid> tractorIDs = new HashSet<Guid>(this.GetGaragesIn(e.Location).Select(p => p.HorseId));
                    foreach (Horse horse in horses)
                    {
                        if (tractorIDs.Contains(horse.HorseId) && !TractorManager.IsTractor(horse))
                            TractorManager.SetTractorInfo(horse, disableHorseSounds: this.Config.SoundEffects != TractorSoundType.Horse);
                    }
                }
            }
        }

        /// <inheritdoc cref="IPlayerEvents.Warped"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnWarped(object? sender, WarpedEventArgs e)
        {
            if (!e.IsLocalPlayer || !this.TractorManager.IsCurrentPlayerRiding)
                return;

            // fix: warping onto a magic warp while mounted causes an infinite warp loop
            Vector2 tile = CommonHelper.GetPlayerTile(Game1.player);
            string touchAction = Game1.player.currentLocation.doesTileHaveProperty((int)tile.X, (int)tile.Y, "TouchAction", "Back");
            if (this.TractorManager.IsCurrentPlayerRiding && touchAction?.Split(' ', 2).First() is "MagicWarp" or "Warp")
                Game1.currentLocation.lastTouchActionLocation = tile;

            // fix: warping into an event may break the event (e.g. Mr Qi's event on mine level event for the 'Cryptic Note' quest)
            if (Game1.CurrentEvent != null)
                Game1.player.mount.dismount();
        }

        /// <inheritdoc cref="IGameLoopEvents.UpdateTicked"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnUpdateTicked(object? sender, UpdateTickedEventArgs e)
        {
            if (!this.IsEnabled)
                return;

            // multiplayer: override textures in the current location
            if (Context.IsWorldReady && Game1.currentLocation != null)
            {
                uint updateRate = Game1.currentLocation.farmers.Count > 1 ? this.TextureUpdateRateWithMultiplePlayers : this.TextureUpdateRateWithSinglePlayer;
                if (e.IsMultipleOf(updateRate))
                {
                    foreach (Horse horse in this.GetTractorsIn(Game1.currentLocation))
                        this.TextureManager.ApplyTextures(horse, this.IsTractor);
                }
            }

            // update tractor effects
            if (Context.IsPlayerFree)
                this.TractorManager.Update();
        }

        /// <inheritdoc cref="IGameLoopEvents.DayEnding"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnDayEnding(object? sender, DayEndingEventArgs e)
        {
            if (!this.IsEnabled)
                return;

            if (Context.IsMainPlayer)
            {
                // collect valid stable IDs
                HashSet<Guid> validStableIDs = new HashSet<Guid>(
                    from location in this.GetLocations()
                    from garage in this.GetGaragesIn(location)
                    select garage.HorseId
                );

                // get locations reachable by Utility.findHorse
                HashSet<GameLocation> vanillaLocations = new HashSet<GameLocation>(Game1.locations, new ObjectReferenceComparer<GameLocation>());

                // clean up
                foreach (GameLocation location in this.GetLocations())
                {
                    bool isValidLocation = vanillaLocations.Contains(location);

                    foreach (Horse tractor in this.GetTractorsIn(location).ToArray())
                    {
                        // remove invalid tractor (e.g. building demolished)
                        if (!validStableIDs.Contains(tractor.HorseId))
                        {
                            location.characters.Remove(tractor);
                            continue;
                        }

                        // move tractor out of location that Utility.findHorse can't find
                        if (!isValidLocation)
                            Game1.warpCharacter(tractor, "Farm", new Point(0, 0));
                    }
                }

                this.CleanUpQuestable();
            }
        }

        /// <inheritdoc cref="IGameLoopEvents.Saved"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnSaved(object? sender, SavedEventArgs e)
        {
            Migrator.AfterSave();
        }

        /// <inheritdoc cref="IDisplayEvents.RenderedWorld"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnRenderedWorld(object? sender, RenderedWorldEventArgs e)
        {
            if (!this.IsEnabled)
                return;

            // render debug radius
            if (this.Config.HighlightRadius && Context.IsWorldReady && Game1.activeClickableMenu == null && this.TractorManager.IsCurrentPlayerRiding)
                this.TractorManager.DrawRadius(Game1.spriteBatch);
        }

        /// <inheritdoc cref="IInputEvents.ButtonsChanged"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnButtonsChanged(object? sender, ButtonsChangedEventArgs e)
        {
            if (!this.IsEnabled || !Context.IsPlayerFree)
                return;

            if (this.Keys.SummonTractor.JustPressed() && !Game1.player.isRidingHorse())
                this.SummonTractor();
            else if (this.Keys.DismissTractor.JustPressed() && Game1.player.isRidingHorse())
                this.DismissTractor(Game1.player.mount);
        }

        /// <inheritdoc cref="IMultiplayerEvents.ModMessageReceived"/>
        /// <param name="sender">The event sender.</param>
        /// <param name="e">The event data.</param>
        private void OnModMessageReceived(object? sender, ModMessageReceivedEventArgs e)
        {
            // tractor request from a farmhand
            if (e.Type == this.RequestTractorMessageID && Context.IsMainPlayer && e.FromModID == this.ModManifest.UniqueID)
            {
                Farmer player = Game1.getFarmer(e.FromPlayerID);
                if (player is { IsMainPlayer: false })
                {
                    this.Monitor.Log(this.SummonLocalTractorTo(player)
                        ? $"Summon tractor for {player.Name} ({e.FromPlayerID})."
                        : $"Received tractor request for {player.Name} ({e.FromPlayerID}), but no tractor is available."
                    );
                }
                else
                    this.Monitor.Log($"Received tractor request for {e.FromPlayerID}, but no such player was found.");
            }
        }

        /****
        ** Helper methods
        ****/
        /// <summary>Reapply the mod configuration.</summary>
        private void UpdateConfig()
        {
            this.Helper.GameContent.InvalidateCache("Data/Buildings");

            foreach (var pair in this.TractorManagerImpl.GetActiveValues())
                this.UpdateConfigFor(pair.Value);
        }

        /// <summary>Apply the mod configuration to a tractor manager instance.</summary>
        /// <param name="manager">The tractor manager to update.</param>
        private void UpdateConfigFor(TractorManager manager)
        {
            var modRegistry = this.Helper.ModRegistry;
            var reflection = this.Helper.Reflection;
            var toolConfig = this.Config.StandardAttachments;

            manager.UpdateConfig(this.Config, this.Keys, new IAttachment?[]
            {
                new CustomAttachment(this.Config.CustomAttachments, modRegistry, reflection), // should be first so it can override default attachments
                new AxeAttachment(toolConfig.Axe, modRegistry, reflection),
                new FertilizerAttachment(toolConfig.Fertilizer, modRegistry, reflection),
                new GrassStarterAttachment(toolConfig.GrassStarter, modRegistry, reflection),
                new HoeAttachment(toolConfig.Hoe, modRegistry, reflection),
                new MeleeBluntAttachment(toolConfig.MeleeBlunt, modRegistry, reflection),
                new MeleeDaggerAttachment(toolConfig.MeleeDagger, modRegistry, reflection),
                new MeleeSwordAttachment(toolConfig.MeleeSword, modRegistry, reflection),
                new MilkPailAttachment(toolConfig.MilkPail, modRegistry, reflection),
                new PickaxeAttachment(toolConfig.PickAxe, modRegistry, reflection),
                new ScytheAttachment(toolConfig.Scythe, modRegistry, reflection),
                new SeedAttachment(toolConfig.Seeds, modRegistry, reflection),
                modRegistry.IsLoaded(SeedBagAttachment.ModId) ? new SeedBagAttachment(toolConfig.SeedBagMod, modRegistry, reflection) : null,
                new ShearsAttachment(toolConfig.Shears, modRegistry, reflection),
                new SlingshotAttachment(toolConfig.Slingshot, modRegistry, reflection),
                new WateringCanAttachment(toolConfig.WateringCan, modRegistry, reflection)
            });
        }

        /// <summary>Summon an unused tractor to the player's current position, if any are available.</summary>
        private void SummonTractor()
        {
            bool summoned = this.SummonLocalTractorTo(Game1.player);
            if (!summoned && !Context.IsMainPlayer)
            {
                this.Monitor.Log("Sending tractor request to host player.");
                this.Helper.Multiplayer.SendMessage(
                    message: true,
                    messageType: this.RequestTractorMessageID,
                    modIDs: new[] { this.ModManifest.UniqueID },
                    playerIDs: new[] { Game1.MasterPlayer.UniqueMultiplayerID }
                );
            }
        }

        /// <summary>Summon an unused tractor to a player's current position, if any are available. If the player is a farmhand in multiplayer, only tractors in synced locations can be found by this method.</summary>
        /// <param name="player">The target player.</param>
        /// <returns>Returns whether a tractor was successfully summoned.</returns>
        private bool SummonLocalTractorTo(Farmer? player)
        {
            // get player info
            if (player == null)
                return false;
            GameLocation location = player.currentLocation;
            Vector2 tile = player.Tile;

            // find nearest tractor in player's current location (if available), else any location
            Horse? tractor = this
                .GetTractorsIn(location, includeMounted: false)
                .MinBy(match => Utility.distance(tile.X, tile.Y, match.TilePoint.X, match.TilePoint.Y));
            tractor ??= this
                .GetLocations()
                .SelectMany(loc => this.GetTractorsIn(loc, includeMounted: false))
                .FirstOrDefault();

            // create a tractor if needed
            if (tractor == null && this.Config.CanSummonWithoutGarage && Context.IsMainPlayer)
            {
                tractor = new Horse(Guid.NewGuid(), 0, 0);
                TractorManager.SetTractorInfo(tractor, disableHorseSounds: this.Config.SoundEffects != TractorSoundType.Horse);
                this.TextureManager.ApplyTextures(tractor, this.IsTractor);
            }

            // warp to player
            if (tractor != null)
            {
                TractorManager.SetLocation(tractor, location, tile);
                return true;
            }
            return false;
        }

        /// <summary>Send a tractor back home.</summary>
        /// <param name="tractor">The tractor to dismiss.</param>
        private void DismissTractor(Horse? tractor)
        {
            if (tractor == null || !this.IsTractor(tractor))
                return;

            // dismount
            if (tractor.rider != null)
                tractor.dismount();

            // get home position (garage may have been moved since the tractor was spawned)
            Farm location = Game1.getFarm();
            Stable? garage = location.buildings.OfType<Stable>().FirstOrDefault(p => p.HorseId == tractor.HorseId);
            Vector2 tile = garage != null
                ? this.GetDefaultTractorTile(garage)
                : tractor.DefaultPosition;

            // warp home
            TractorManager.SetLocation(tractor, location, tile);
        }

        /// <summary>Get all available locations.</summary>
        private IEnumerable<GameLocation> GetLocations()
        {
            GameLocation[] mainLocations = (Context.IsMainPlayer ? Game1.locations : this.Helper.Multiplayer.GetActiveLocations()).ToArray();

            foreach (GameLocation location in mainLocations.Concat(MineShaft.activeMines).Concat(VolcanoDungeon.activeLevels))
            {
                yield return location;

                foreach (GameLocation indoors in location.GetInstancedBuildingInteriors())
                    yield return indoors;
            }
        }

        /// <summary>Get all tractors in the given location.</summary>
        /// <param name="location">The location to scan.</param>
        /// <param name="includeMounted">Whether to include horses that are currently being ridden.</param>
        private IEnumerable<Horse> GetTractorsIn(GameLocation location, bool includeMounted = true)
        {
            // single-player
            if (!Context.IsMultiplayer || !includeMounted)
                return location.characters.OfType<Horse>().Where(this.IsTractor);

            // multiplayer
            return
                location.characters.OfType<Horse>().Where(this.IsTractor)
                    .Concat(
                        from player in location.farmers
                        where this.IsTractor(player.mount)
                        select player.mount
                    )
                    .Distinct(new ObjectReferenceComparer<Horse>());
        }

        /// <summary>Get all tractor garages in the given location.</summary>
        /// <param name="location">The location to scan.</param>
        private IEnumerable<Stable> GetGaragesIn(GameLocation location)
        {
            return location.buildings
                .OfType<Stable>()
                .Where(this.IsGarage);
        }

        /// <summary>Find all horses with a given ID.</summary>
        /// <param name="id">The unique horse ID.</param>
        private Horse? FindHorse(Guid id)
        {
            foreach (GameLocation location in this.GetLocations())
            {
                foreach (Horse horse in location.characters.OfType<Horse>())
                {
                    if (horse.HorseId == id)
                        return horse;
                }
            }

            return null;
        }

        /// <summary>Get whether a stable is a tractor garage.</summary>
        /// <param name="stable">The stable to check.</param>
        private bool IsGarage([NotNullWhen(true)] Stable? stable)
        {
            return stable?.buildingType.Value == this.GarageBuildingId;
        }

        /// <summary>Get whether a horse is a tractor.</summary>
        /// <param name="horse">The horse to check.</param>
        private bool IsTractor([NotNullWhen(true)] Horse? horse)
        {
            return TractorManager.IsTractor(horse);
        }

        /// <summary>Get the default tractor tile position in a garage.</summary>
        /// <param name="garage">The tractor's home garage.</param>
        private Vector2 GetDefaultTractorTile(Stable garage)
        {
            return new Vector2(garage.tileX.Value + 1, garage.tileY.Value + 1);
        }
    }
}
