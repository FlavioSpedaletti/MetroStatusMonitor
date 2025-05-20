using System;
using MetroStatusMonitor.Core;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace MetroStatusMonitor.ConsoleApp
{
    public class ConsoleNotificationService : INotificationService
    {
        private readonly ConsoleColor defaultForegroundColor;
        private readonly bool colorOutput;
        
        // Armazenar a posição do contador na tela
        private int countdownLinePosition = -1;
        
        // Armazenar dados da última consulta para possíveis redesenhos
        private Dictionary<string, string> ultimosResultados;
        private string ultimoHorarioAtualizacao;
        private Dictionary<string, string> ultimoStatusAnterior;
        private DateTime? proximaAtualizacao;
        
        public ConsoleNotificationService(bool colorOutput = true)
        {
            this.colorOutput = colorOutput;
            this.defaultForegroundColor = Console.ForegroundColor;
        }
        
        public void ShowNotification(string title, string message)
        {
            if (colorOutput)
            {
                // Salvar a cor atual
                var originalColor = Console.ForegroundColor;
                
                // Colorir o título
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine($"\n=== {title} ===");
                
                // Colorir a mensagem
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine(message);
                
                // Adicionar uma linha separadora
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.WriteLine(new string('-', 40));
                
                // Restaurar a cor original
                Console.ForegroundColor = originalColor;
            }
            else
            {
                Console.WriteLine($"\n=== {title} ===");
                Console.WriteLine(message);
                Console.WriteLine(new string('-', 40));
            }
        }
        
        // Método para mostrar o status das linhas no console
        public void DisplayStatus(System.Collections.Generic.Dictionary<string, string> resultados, 
                                  string horarioAtualizacao, 
                                  DateTime? proximaAtualizacao = null,
                                  System.Collections.Generic.Dictionary<string, string> statusAnterior = null)
        {
            // Armazenar os dados para potenciais redesenhos ou atualizações
            this.ultimosResultados = new Dictionary<string, string>(resultados);
            this.ultimoHorarioAtualizacao = horarioAtualizacao;
            this.proximaAtualizacao = proximaAtualizacao;
            
            if (statusAnterior != null)
            {
                this.ultimoStatusAnterior = new Dictionary<string, string>(statusAnterior);
            }
            
            Console.Clear();
            Console.WriteLine($"=== Monitor de Status do Metrô ===");
            Console.WriteLine($"Última consulta: {DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss")}");
            Console.WriteLine($"Horário informado pelo site do Metrô: {horarioAtualizacao}\n");
            
            // Exibir contagem regressiva para próxima atualização
            if (proximaAtualizacao.HasValue)
            {
                // Guardar a posição atual para atualizações futuras
                countdownLinePosition = Console.CursorTop;
                
                TimeSpan tempoRestante = proximaAtualizacao.Value - DateTime.Now;
                if (tempoRestante.TotalSeconds > 0)
                {
                    if (colorOutput)
                    {
                        Console.ForegroundColor = ConsoleColor.Cyan;
                    }
                    Console.WriteLine($"Próxima atualização em: {(int)tempoRestante.TotalSeconds} segundos\n");
                    if (colorOutput)
                    {
                        Console.ForegroundColor = defaultForegroundColor;
                    }
                }
                else
                {
                    Console.WriteLine("Atualizando...\n");
                }
            }
            
            foreach (var linha in resultados)
            {
                if (colorOutput)
                {
                    // Definir cor com base no status
                    if (linha.Value.ToLower().Contains("normal") || linha.Value.ToLower().Contains("operando normalmente"))
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                    }
                    else if (linha.Value.ToLower().Contains("paralisada") || linha.Value.ToLower().Contains("interrompida"))
                    {
                        Console.ForegroundColor = ConsoleColor.Red;
                    }
                    else
                    {
                        Console.ForegroundColor = ConsoleColor.Yellow;
                    }
                }
                
                string infoStatus = $"{linha.Key}: {linha.Value}";
                
                // Verificar se há status anterior para comparação
                if (statusAnterior != null && statusAnterior.TryGetValue(linha.Key, out string statusAnt))
                {
                    if (statusAnt != linha.Value)
                    {
                        infoStatus += $" (Era: {statusAnt})";
                    }
                    else
                    {
                        infoStatus += " (não alterado)";
                    }
                }
                
                Console.WriteLine(infoStatus);
                
                if (colorOutput)
                {
                    Console.ForegroundColor = defaultForegroundColor;
                }
            }
            
            Console.WriteLine("\nPressione 'R' para atualizar, 'Q' para sair");
        }
        
        // Método para atualizar apenas o contador regressivo
        public void UpdateCountdown(int segundosRestantes)
        {
            // Se não temos posição válida ou próxima atualização, não fazer nada
            if (countdownLinePosition < 0 || !proximaAtualizacao.HasValue)
                return;
                
            try
            {
                // Verificar se o console foi redimensionado ou a posição está fora dos limites
                if (countdownLinePosition >= Console.BufferHeight)
                {
                    // Se temos os dados armazenados, redesenhar toda a tela
                    if (ultimosResultados != null)
                    {
                        DateTime novaProximaAtualizacao = DateTime.Now.AddSeconds(segundosRestantes);
                        DisplayStatus(ultimosResultados, ultimoHorarioAtualizacao, novaProximaAtualizacao, ultimoStatusAnterior);
                    }
                    return;
                }
                
                // Salvar posição atual do cursor
                int currentTop = Console.CursorTop;
                int currentLeft = Console.CursorLeft;
                
                // Posicionar no início da linha do contador
                Console.SetCursorPosition(0, countdownLinePosition);
                
                // Limpar a linha
                Console.Write(new string(' ', Console.WindowWidth - 1));
                
                // Posicionar novamente e escrever o novo contador
                Console.SetCursorPosition(0, countdownLinePosition);
                
                if (colorOutput)
                {
                    Console.ForegroundColor = ConsoleColor.Cyan;
                }
                
                if (segundosRestantes > 0)
                {
                    Console.Write($"Próxima atualização em: {segundosRestantes} segundos");
                }
                else
                {
                    Console.Write("Atualizando...");
                }
                
                if (colorOutput)
                {
                    Console.ForegroundColor = defaultForegroundColor;
                }
                
                // Restaurar posição do cursor
                try
                {
                    Console.SetCursorPosition(currentLeft, currentTop);
                }
                catch
                {
                    // Ignora erro caso dimensão do console tenha mudado
                }
            }
            catch (Exception)
            {
                // Em caso de qualquer erro, simplesmente ignoramos
            }
        }
    }
} 