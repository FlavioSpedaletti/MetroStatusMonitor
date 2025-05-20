using System;
using System.Linq;

namespace MetroStatusMonitor.Core
{
    public interface INotificationService
    {
        void ShowNotification(string title, string message);
    }
    
    public class NotificationManager
    {
        private readonly INotificationService _notificationService;
        private readonly Settings _settings;
        
        public NotificationManager(INotificationService notificationService, Settings settings)
        {
            _notificationService = notificationService ?? throw new ArgumentNullException(nameof(notificationService));
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
        }
        
        public void ProcessStatusChanges(object sender, StatusChangedEventArgs e)
        {
            // Verificar se é uma primeira execução (todas as mudanças tem StatusAnterior = "Desconhecido" ou "Primeira verificação")
            bool isPrimeiraExecucao = e.Mudancas.Count > 0 && 
                                     (e.Mudancas[0].StatusAnterior == "Desconhecido" || 
                                      e.Mudancas[0].StatusAnterior == "Primeira verificação");
            
            // Na primeira execução, sempre mostrar uma notificação de status inicial, independente de configuração
            if (isPrimeiraExecucao)
            {
                // Verificar se há linhas com problemas (não operando normalmente)
                var linhasComProblemas = e.Mudancas
                    .Where(m => _settings.DeveMonitorarLinha(m.NomeLinha) && 
                               !m.StatusNovo.Contains("Normal", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                
                if (linhasComProblemas.Count > 0)
                {
                    // Mostrar notificação apenas para linhas com problemas
                    string mensagem = "Status atual:\n";
                    foreach (var mudanca in linhasComProblemas)
                    {
                        mensagem += $"{mudanca.NomeLinha}: {mudanca.StatusNovo}\n";
                    }
                    
                    ShowNotification("Status Inicial do Metrô", mensagem.TrimEnd());
                }
                else
                {
                    // SEMPRE mostrar notificação inicial se todas as linhas estão operando normalmente
                    ShowNotification(
                        "Status Inicial do Metrô", 
                        "Todas as linhas monitoradas operando normalmente.");
                }
            }
            // Para execuções seguintes, seguir a lógica normal
            else if (_settings.NotificarApenasAlteracoes)
            {
                // Notificar para cada linha com alteração
                foreach (var mudanca in e.Mudancas)
                {
                    // Apenas notificar se a linha deve ser monitorada
                    if (_settings.DeveMonitorarLinha(mudanca.NomeLinha))
                    {
                        ShowNotification(
                            "Alteração no Status do Metrô",
                            $"{mudanca.NomeLinha}: {mudanca.StatusNovo}\n(Era: {mudanca.StatusAnterior})");
                    }
                }
            }
            // Se devemos notificar sempre (não apenas alterações), exibe o status atual
            else if (e.Mudancas.Count > 0)
            {
                // Criar uma mensagem agregada com todas as linhas que não estão em operação normal
                var linhasComProblemas = e.Mudancas
                    .Where(m => _settings.DeveMonitorarLinha(m.NomeLinha) && 
                                 !m.StatusNovo.Contains("Normal", StringComparison.OrdinalIgnoreCase))
                    .ToList();
                    
                if (linhasComProblemas.Count > 0)
                {
                    string mensagem = "Status atual:\n";
                    foreach (var mudanca in linhasComProblemas)
                    {
                        mensagem += $"{mudanca.NomeLinha}: {mudanca.StatusNovo}\n";
                    }
                    
                    ShowNotification("Status do Metrô", mensagem.TrimEnd());
                }
                else
                {
                    // Se todas as linhas estão operando normalmente
                    ShowNotification(
                        "Status do Metrô", 
                        "Todas as linhas monitoradas operando normalmente.");
                }
            }
        }
        
        // Método para mostrar notificação usando o serviço fornecido
        private void ShowNotification(string title, string message)
        {
            try
            {
                _notificationService.ShowNotification(title, message);
                
                if (_settings.ModoDebug)
                {
                    Console.WriteLine($"[NOTIFICAÇÃO] {title}: {message}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao mostrar notificação: {ex.Message}");
            }
        }
    }
} 