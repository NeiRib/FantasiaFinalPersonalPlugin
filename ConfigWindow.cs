using System;
using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace AutoFCInvite
{
    public class ConfigWindow : Window, IDisposable
    {
        private Configuration Configuration;
        private Plugin Plugin;

        public ConfigWindow(Plugin plugin) : base(
            "Configuración - Auto FC Invite",
            ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoScrollbar |
            ImGuiWindowFlags.NoScrollWithMouse)
        {
            Size = new Vector2(400, 300);
            SizeCondition = ImGuiCond.Always;

            Plugin = plugin;
            Configuration = plugin.Configuration;
        }

        public void Dispose() { }

        public override void Draw()
        {
            ImGui.Text("Configuración del Plugin Auto FC Invite");
            ImGui.Separator();

            // Configuración de distancia máxima
            ImGui.Text("Distancia Máxima (yalms):");
            var maxDistance = Configuration.MaxDistance;
            if (ImGui.SliderFloat("##maxDistance", ref maxDistance, 5.0f, 100.0f, "%.1f"))
            {
                Configuration.MaxDistance = maxDistance;
                Configuration.Save();
            }
            ImGui.SameLine();
            if (ImGui.Button("?##distanceHelp"))
            {
                ImGui.OpenPopup("DistanceHelp");
            }

            if (ImGui.BeginPopup("DistanceHelp"))
            {
                ImGui.Text("Distancia máxima para detectar jugadores.");
                ImGui.Text("1 yalm ≈ 1 metro aproximadamente.");
                ImGui.Text("Valores típicos: 30-50 yalms");
                ImGui.EndPopup();
            }

            ImGui.Spacing();

            // Configuración de delay entre invitaciones
            ImGui.Text("Delay entre invitaciones (ms):");
            var delay = Configuration.DelayBetweenInvites;
            if (ImGui.SliderInt("##delay", ref delay, 1000, 10000, "%d ms"))
            {
                Configuration.DelayBetweenInvites = delay;
                Configuration.Save();
            }
            ImGui.SameLine();
            if (ImGui.Button("?##delayHelp"))
            {
                ImGui.OpenPopup("DelayHelp");
            }

            if (ImGui.BeginPopup("DelayHelp"))
            {
                ImGui.Text("Tiempo de espera entre cada invitación.");
                ImGui.Text("Recomendado: 2-5 segundos para evitar spam.");
                ImGui.Text("Valores muy bajos pueden causar problemas.");
                ImGui.EndPopup();
            }

            ImGui.Spacing();

            // Configuración de invitación automática
            var autoInvite = Configuration.AutoInviteEnabled;
            if (ImGui.Checkbox("Invitación Automática", ref autoInvite))
            {
                Configuration.AutoInviteEnabled = autoInvite;
                Configuration.Save();
            }
            ImGui.SameLine();
            if (ImGui.Button("?##autoHelp"))
            {
                ImGui.OpenPopup("AutoHelp");
            }

            if (ImGui.BeginPopup("AutoHelp"))
            {
                ImGui.Text("FUNCIÓN NO IMPLEMENTADA TODAVÍA");
                ImGui.Text("Cuando esté activa, enviará invitaciones");
                ImGui.Text("automáticamente cuando detecte jugadores sin FC.");
                ImGui.EndPopup();
            }

            ImGui.Spacing();

            // Configuración de mensajes de debug
            var showDebug = Configuration.ShowDebugMessages;
            if (ImGui.Checkbox("Mostrar mensajes de debug", ref showDebug))
            {
                Configuration.ShowDebugMessages = showDebug;
                Configuration.Save();
            }

            ImGui.Separator();

            // Información y controles
            ImGui.Text("Controles:");
            ImGui.TextColored(new Vector4(0.7f, 0.7f, 1.0f, 1.0f), "/fcinvite");
            ImGui.SameLine();
            ImGui.Text("- Ejecutar invitaciones");

            ImGui.TextColored(new Vector4(0.7f, 0.7f, 1.0f, 1.0f), "/fcinvite config");
            ImGui.SameLine();
            ImGui.Text("- Abrir configuración");

            ImGui.Spacing();

            // Botón de prueba
            if (ImGui.Button("Ejecutar Invitaciones Ahora"))
            {
                Plugin.OnCommand("/fcinvite", "");
            }

            ImGui.Spacing();

            // Advertencias
            ImGui.TextColored(new Vector4(1.0f, 0.8f, 0.0f, 1.0f), "⚠️ ADVERTENCIAS:");
            ImGui.TextWrapped("• Asegúrate de tener permisos para invitar en tu FC");
            ImGui.TextWrapped("• Usa delays apropiados para evitar spam");
            ImGui.TextWrapped("• Ten cuidado con las políticas del juego sobre automatización");

            ImGui.Spacing();

            // Información de estado
            ImGui.Separator();
            ImGui.Text($"Versión: {Configuration.Version}");
            ImGui.Text($"Estado: {(Plugin._isInviting ? "Invitando..." : "Listo")}");
        }
    }
}