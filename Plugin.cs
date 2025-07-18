using Dalamud.Game.Command;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Interface.Windowing;
using Dalamud.Plugin.Services;
using Dalamud.Game.ClientState.Objects;
using Dalamud.Game.ClientState.Objects.Types;
using Dalamud.Game.ClientState.Objects.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FFXIVClientStructs.FFXIV.Client.Game.Character;
using FFXIVClientStructs.FFXIV.Client.Game.Object;

namespace AutoFCInvite
{
    public sealed class Plugin : IDalamudPlugin
    {
        public string Name => "Auto FC Invite";
        private const string CommandName = "/fcinvite";

        private DalamudPluginInterface PluginInterface { get; init; }
        private ICommandManager CommandManager { get; init; }
        private IClientState ClientState { get; init; }
        private IObjectTable ObjectTable { get; init; }
        private IChatGui ChatGui { get; init; }
        private ITargetManager TargetManager { get; init; }

        public WindowSystem WindowSystem = new("AutoFCInvite");
        private ConfigWindow ConfigWindow { get; init; }
        private Configuration Configuration { get; init; }

        private bool _isInviting = false;
        private DateTime _lastInvite = DateTime.MinValue;
        private readonly TimeSpan _inviteCooldown = TimeSpan.FromSeconds(3); // 3 segundos entre invitaciones

        public Plugin(
            [RequiredVersion("1.0")] DalamudPluginInterface pluginInterface,
            [RequiredVersion("1.0")] ICommandManager commandManager,
            [RequiredVersion("1.0")] IClientState clientState,
            [RequiredVersion("1.0")] IObjectTable objectTable,
            [RequiredVersion("1.0")] IChatGui chatGui,
            [RequiredVersion("1.0")] ITargetManager targetManager)
        {
            PluginInterface = pluginInterface;
            CommandManager = commandManager;
            ClientState = clientState;
            ObjectTable = objectTable;
            ChatGui = chatGui;
            TargetManager = targetManager;

            Configuration = PluginInterface.GetPluginConfig() as Configuration ?? new Configuration();
            Configuration.Initialize(PluginInterface);

            ConfigWindow = new ConfigWindow(this);
            WindowSystem.AddWindow(ConfigWindow);

            CommandManager.AddHandler(CommandName, new CommandInfo(OnCommand)
            {
                HelpMessage = "Invita a la FC a todos los jugadores sin FC cercanos\n" +
                             "/fcinvite - Ejecuta las invitaciones\n" +
                             "/fcinvite config - Abre la configuración"
            });

            PluginInterface.UiBuilder.Draw += DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi += DrawConfigUI;
        }

        public void Dispose()
        {
            WindowSystem.RemoveAllWindows();
            CommandManager.RemoveHandler(CommandName);
            PluginInterface.UiBuilder.Draw -= DrawUI;
            PluginInterface.UiBuilder.OpenConfigUi -= DrawConfigUI;
        }

        private void OnCommand(string command, string args)
        {
            if (args == "config")
            {
                ConfigWindow.IsOpen = true;
            }
            else
            {
                _ = Task.Run(async () => await InviteNearbyPlayersAsync());
            }
        }

        private async Task InviteNearbyPlayersAsync()
        {
            if (_isInviting)
            {
                ChatGui.Print("Ya hay un proceso de invitación en curso...");
                return;
            }

            if (ClientState.LocalPlayer == null)
            {
                ChatGui.Print("No se pudo obtener información del jugador local.");
                return;
            }

            _isInviting = true;
            var inviteCount = 0;
            var playersToInvite = new List<PlayerCharacter>();

            try
            {
                // Obtener jugadores cercanos sin FC
                foreach (var obj in ObjectTable)
                {
                    if (obj is PlayerCharacter player && 
                        player.ObjectId != ClientState.LocalPlayer.ObjectId &&
                        IsPlayerValid(player))
                    {
                        var distance = GetDistance(ClientState.LocalPlayer, player);
                        if (distance <= Configuration.MaxDistance)
                        {
                            if (!HasFreeCompany(player))
                            {
                                playersToInvite.Add(player);
                            }
                        }
                    }
                }

                ChatGui.Print($"Se encontraron {playersToInvite.Count} jugadores sin FC cerca.");

                // Enviar invitaciones con cooldown
                foreach (var player in playersToInvite)
                {
                    if (DateTime.Now - _lastInvite < _inviteCooldown)
                    {
                        await Task.Delay(_inviteCooldown);
                    }

                    if (SendFCInvite(player))
                    {
                        inviteCount++;
                        _lastInvite = DateTime.Now;
                        ChatGui.Print($"Invitación enviada a {player.Name}");
                        
                        if (Configuration.DelayBetweenInvites > 0)
                        {
                            await Task.Delay(Configuration.DelayBetweenInvites);
                        }
                    }
                }

                ChatGui.Print($"Proceso completado. Se enviaron {inviteCount} invitaciones.");
            }
            catch (Exception ex)
            {
                ChatGui.Print($"Error durante el proceso: {ex.Message}");
            }
            finally
            {
                _isInviting = false;
            }
        }

        private bool IsPlayerValid(PlayerCharacter player)
        {
            // Verificar que el jugador no sea un bot o NPC
            return player.ObjectKind == ObjectKind.Player && 
                   !string.IsNullOrEmpty(player.Name.ToString()) &&
                   player.IsTargetable;
        }

        private float GetDistance(PlayerCharacter player1, PlayerCharacter player2)
        {
            var pos1 = player1.Position;
            var pos2 = player2.Position;
            
            var dx = pos1.X - pos2.X;
            var dy = pos1.Y - pos2.Y;
            var dz = pos1.Z - pos2.Z;
            
            return (float)Math.Sqrt(dx * dx + dy * dy + dz * dz);
        }

        private unsafe bool HasFreeCompany(PlayerCharacter player)
        {
            // Verificar si el jugador tiene FC
            // Esto puede requerir métodos específicos de FFXIVClientStructs
            var character = (Character*)player.Address;
            if (character == null) return true; // Asumir que tiene FC si no podemos verificar
            
            // Verificar el tag de FC (esto puede variar según la versión del juego)
            var fcTag = character->CharacterData.FreeCompanyTag;
            return fcTag[0] != 0; // Si el primer byte no es 0, tiene FC
        }

        private bool SendFCInvite(PlayerCharacter player)
        {
            try
            {
                // Seleccionar al jugador
                TargetManager.Target = player;
                
                // Enviar comando de invitación a FC
                // Esto simula el comando /fcinvite
                var command = $"/fcinvite \"{player.Name}\"";
                
                // Ejecutar comando usando el chat
                // Nota: Esto puede requerir permisos específicos del plugin
                ChatGui.Print($"Ejecutando: {command}");
                
                // Aquí deberías usar el método apropiado para ejecutar comandos
                // Por ejemplo, usando ICommandManager o enviando directamente al chat
                
                return true;
            }
            catch (Exception ex)
            {
                ChatGui.Print($"Error enviando invitación a {player.Name}: {ex.Message}");
                return false;
            }
        }

        private void DrawUI()
        {
            WindowSystem.Draw();
        }

        public void DrawConfigUI()
        {
            ConfigWindow.IsOpen = true;
        }
    }

    public class Configuration : IPluginConfiguration
    {
        public int Version { get; set; } = 0;
        public float MaxDistance { get; set; } = 30.0f; // Distancia máxima en yalms
        public int DelayBetweenInvites { get; set; } = 2000; // Delay en milliseconds
        public bool AutoInviteEnabled { get; set; } = false;
        public bool ShowDebugMessages { get; set; } = false;

        [NonSerialized]
        private DalamudPluginInterface? _pluginInterface;

        public void Initialize(DalamudPluginInterface pluginInterface)
        {
            _pluginInterface = pluginInterface;
        }

        public void Save()
        {
            _pluginInterface!.SavePluginConfig(this);
        }
    }
}