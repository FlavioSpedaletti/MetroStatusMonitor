using System;
using System.Threading;
using System.Threading.Tasks;
using MetroStatusMonitor.Core;

namespace MetroStatusMonitor.ConsoleApp
{
    class Program
    {
        private static Settings settings;
        private static Core.MetroStatusMonitor metroMonitor;
        private static ConsoleNotificationService notificationService;
        private static NotificationManager notificationManager;
        private static CancellationTokenSource cts;
        private static bool keepRunning = true;
        private static Task monitoringTask;

        static async Task Main(string[] args)
        {
            Console.Title = "Monitor de Status do Metrô - Console";
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            
            try
            {
                // Processar argumentos de linha de comando
                bool modoDebug = false;
                bool persistirEmJson = false;
                bool colorOutput = true;
                
                foreach (var arg in args)
                {
                    if (arg.ToLower() == "--debug" || arg.ToLower() == "-d")
                    {
                        modoDebug = true;
                    }
                    else if (arg.ToLower() == "--persistir" || arg.ToLower() == "--persistiremjson")
                    {
                        persistirEmJson = true;
                    }
                    else if (arg.ToLower() == "--nocolor")
                    {
                        colorOutput = false;
                    }
                }
                
                // Carregar configurações
                settings = Settings.Load();
                
                // Aplicar opções da linha de comando
                if (modoDebug) settings.ModoDebug = modoDebug;
                if (persistirEmJson) settings.PersistirEmJson = persistirEmJson;
                
                // Salvar configurações atualizadas
                settings.Save();
                
                Console.WriteLine("Inicializando Monitor de Status do Metrô...");
                Console.WriteLine("Pressione 'Q' para sair, 'R' para atualizar manualmente");
                
                // Inicializar serviços
                notificationService = new ConsoleNotificationService(colorOutput);
                metroMonitor = new Core.MetroStatusMonitor();
                notificationManager = new NotificationManager(notificationService, settings);
                
                // Configurar eventos
                metroMonitor.StatusChanged += notificationManager.ProcessStatusChanges;
                
                // Iniciar monitoramento em background
                cts = new CancellationTokenSource();
                monitoringTask = StartMonitoring(cts.Token);
                
                // Loop para processar comandos do usuário
                while (keepRunning)
                {
                    if (Console.KeyAvailable)
                    {
                        var key = Console.ReadKey(true);
                        switch (key.Key)
                        {
                            case ConsoleKey.Q:
                                keepRunning = false;
                                break;
                            case ConsoleKey.R:
                                await CheckMetroStatusNow();
                                break;
                        }
                    }
                    
                    await Task.Delay(100); // Pequena pausa para evitar uso excessivo de CPU
                }
                
                // Cancelar monitoramento e aguardar conclusão
                cts.Cancel();
                await monitoringTask;
                
                Console.WriteLine("Programa encerrado.");
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Erro: {ex.Message}");
                Console.ResetColor();
                
                Console.WriteLine("Pressione qualquer tecla para sair...");
                Console.ReadKey();
            }
        }
        
        private static async Task StartMonitoring(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    // Realizar consulta ao status do metrô
                    await CheckMetroStatusNow(cancellationToken);
                    
                    // Definir hora da próxima atualização
                    DateTime proximaConsulta = DateTime.Now.AddSeconds(settings.IntervaloVerificacao);
                    
                    // Loop até a próxima consulta, atualizando o contador a cada segundo
                    while (DateTime.Now < proximaConsulta && !cancellationToken.IsCancellationRequested)
                    {
                        // Calcular segundos restantes
                        int segundosRestantes = (int)(proximaConsulta - DateTime.Now).TotalSeconds;
                        
                        // Atualizar o contador
                        notificationService.UpdateCountdown(segundosRestantes);
                        
                        // Esperar aproximadamente 1 segundo
                        await Task.Delay(1000, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Normal ao encerrar o programa
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Erro no monitoramento: {ex.Message}");
                Console.ResetColor();
            }
        }
        
        private static async Task CheckMetroStatusNow(CancellationToken? token = null)
        {
            try
            {
                var cancellationToken = token ?? CancellationToken.None;
                var (resultados, horarioAtualizacao) = await metroMonitor.ConsultarStatusLinhasMetroAsync(cancellationToken);
                
                // Calcular próxima atualização (se estiver em monitoramento contínuo)
                DateTime? proximaAtualizacao = null;
                if (token != null && !token.Value.IsCancellationRequested)
                {
                    proximaAtualizacao = DateTime.Now.AddSeconds(settings.IntervaloVerificacao);
                }
                
                // Obter histórico de status para comparação
                var statusAnterior = metroMonitor.ObterStatusAnterior();
                
                // Mostrar resultados no console
                notificationService.DisplayStatus(resultados, horarioAtualizacao, proximaAtualizacao, statusAnterior);
            }
            catch (Exception ex)
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"Erro ao verificar status: {ex.Message}");
                Console.ResetColor();
            }
        }
    }
} 