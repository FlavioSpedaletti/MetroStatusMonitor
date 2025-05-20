using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MetroStatusMonitor.Core
{
    public class Settings
    {
        private static readonly string configFile = "config.json";
        
        // Configurações padrão
        private static readonly int defaultIntervaloVerificacao = 10; // 10 segundos
        
        // Propriedades que serão salvas nas configurações
        public int IntervaloVerificacao { get; set; } = defaultIntervaloVerificacao;
        public bool IniciarComWindows { get; set; } = false;
        public bool NotificarApenasAlteracoes { get; set; } = true;
        public bool MinimizarAoIniciar { get; set; } = true;
        public bool ModoDebug { get; set; } = false;
        public bool PersistirEmJson { get; set; } = false;
        
        // Lista de linhas a monitorar (true = monitorar, false = ignorar)
        public bool MonitorarLinha1Azul { get; set; } = true;
        public bool MonitorarLinha2Verde { get; set; } = true;
        public bool MonitorarLinha3Vermelha { get; set; } = true;
        public bool MonitorarLinha4Amarela { get; set; } = true;
        public bool MonitorarLinha5Lilas { get; set; } = true;
        public bool MonitorarLinha15Prata { get; set; } = true;
        
        // Método para carregar as configurações
        public static Settings Load()
        {
            try
            {
                if (File.Exists(configFile))
                {
                    string json = File.ReadAllText(configFile);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    return JsonSerializer.Deserialize<Settings>(json, options) ?? new Settings();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao carregar configurações: {ex.Message}");
            }
            
            // Se o arquivo não existe ou ocorreu um erro, retorna configurações padrão
            return new Settings();
        }
        
        // Método para salvar as configurações
        public void Save()
        {
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                string json = JsonSerializer.Serialize(this, options);
                File.WriteAllText(configFile, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao salvar configurações: {ex.Message}");
            }
        }
        
        // Método para configurar inicialização com o Windows
        public void ConfigurarInicioComWindows(bool iniciar)
        {
            try
            {
                string appPath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                string startupPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Startup),
                    "MetroStatusMonitor.lnk");
                
                if (iniciar)
                {
                    // Criar atalho no startup
                    // Nota: Isso requer uma biblioteca para criar atalhos ou usar PowerShell/CMD
                    // Em uma implementação completa, usaria algo como IWshRuntimeLibrary
                    
                    // Código simplificado para criar um atalho
                    File.WriteAllText(startupPath, $"Atalho para {appPath}");
                }
                else
                {
                    // Remover atalho do startup se existir
                    if (File.Exists(startupPath))
                    {
                        File.Delete(startupPath);
                    }
                }
                
                // Atualizar configuração
                IniciarComWindows = iniciar;
                Save();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao configurar inicialização automática: {ex.Message}");
            }
        }
        
        // Verificar se uma linha deve ser monitorada
        public bool DeveMonitorarLinha(string nomeLinha)
        {
            if (string.IsNullOrEmpty(nomeLinha)) return false;
            
            // Verificar qual linha está sendo consultada
            if (nomeLinha.Contains("1-Azul", StringComparison.OrdinalIgnoreCase))
                return MonitorarLinha1Azul;
            else if (nomeLinha.Contains("2-Verde", StringComparison.OrdinalIgnoreCase))
                return MonitorarLinha2Verde;
            else if (nomeLinha.Contains("3-Vermelha", StringComparison.OrdinalIgnoreCase))
                return MonitorarLinha3Vermelha;
            else if (nomeLinha.Contains("4-Amarela", StringComparison.OrdinalIgnoreCase))
                return MonitorarLinha4Amarela;
            else if (nomeLinha.Contains("5-Lilás", StringComparison.OrdinalIgnoreCase))
                return MonitorarLinha5Lilas;
            else if (nomeLinha.Contains("15-Prata", StringComparison.OrdinalIgnoreCase))
                return MonitorarLinha15Prata;
                
            // Por padrão, monitorar linhas desconhecidas
            return true;
        }
    }
} 