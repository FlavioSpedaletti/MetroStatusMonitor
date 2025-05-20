using System;
using System.Drawing;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using System.IO;
using MetroStatusMonitor.Core;
using System.Linq;

namespace MetroStatusMonitor.WinForms
{
    public partial class Form1 : Form, INotificationService
    {
        private NotifyIcon notifyIcon;
        private ContextMenuStrip contextMenu;
        private Core.MetroStatusMonitor metroMonitor;
        private CancellationTokenSource cts;
        private bool isMonitoring = false;
        private Settings settings;
        private ListView listViewStatus;
        private Label lblUltimaAtualizacao;
        private Icon metroIcon;
        private NotificationManager notificationManager;
        private PictureBox indicadorConsulta;
        private Icon iconConsulta;

        public Form1()
        {
            InitializeComponent();
            CarregarIconeMetro();
            InitializeNotifyIcon();
            
            // Inicializar configurações
            settings = Settings.Load();
            
            // Inicializar monitor
            metroMonitor = new Core.MetroStatusMonitor();
            
            // Inicializar gerenciador de notificações
            notificationManager = new NotificationManager(this, settings);
            metroMonitor.StatusChanged += notificationManager.ProcessStatusChanges;
            
            // Configurar para iniciar minimizado
            this.WindowState = FormWindowState.Minimized;
            this.ShowInTaskbar = false;
            this.FormClosing += Form1_FormClosing;
            this.Resize += Form1_Resize;
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // Criar o label de última atualização
            lblUltimaAtualizacao = new Label();
            lblUltimaAtualizacao.Text = "Última atualização: -";
            lblUltimaAtualizacao.Location = new Point(10, 10);
            lblUltimaAtualizacao.AutoSize = true;
            this.Controls.Add(lblUltimaAtualizacao);
            
            // Criar indicador de consulta
            indicadorConsulta = new PictureBox();
            indicadorConsulta.Size = new Size(12, 12);
            indicadorConsulta.Location = new Point(lblUltimaAtualizacao.Location.X + 290, lblUltimaAtualizacao.Location.Y + 2);
            indicadorConsulta.BackColor = Color.Gray;
            this.Controls.Add(indicadorConsulta);
            
            // Criar o ListView para status
            listViewStatus = new ListView();
            listViewStatus.Location = new Point(10, 40);
            listViewStatus.Size = new Size(380, 250);
            listViewStatus.View = View.Details;
            listViewStatus.FullRowSelect = true;
            listViewStatus.GridLines = true;
            
            // Adicionar colunas
            listViewStatus.Columns.Add("Linha", 150);
            listViewStatus.Columns.Add("Status", 230);
            
            // Preencher com as linhas conhecidas
            string[] linhas = { 
                "Linha 1-Azul", 
                "Linha 2-Verde", 
                "Linha 3-Vermelha", 
                "Linha 4-Amarela", 
                "Linha 5-Lilás", 
                "Linha 15-Prata" 
            };
            
            foreach (var linha in linhas)
            {
                ListViewItem item = new ListViewItem(linha);
                item.SubItems.Add("Aguardando...");
                listViewStatus.Items.Add(item);
            }
            
            this.Controls.Add(listViewStatus);
            
            // Configuração do formulário
            this.ClientSize = new System.Drawing.Size(400, 300);
            this.Name = "Form1";
            this.Text = "Monitor de Status do Metrô";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private void CarregarIconeMetro()
        {
            try
            {
                // Tentar caminho 1: Diretório do executável
                string iconPath1 = Path.Combine(Application.StartupPath, "Resources", "metro_icon.ico");
                
                // Tentar caminho 2: Base Directory do domínio atual
                string iconPath2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Resources", "metro_icon.ico");
                
                // Tentar caminho 3: Um nível acima
                string iconPath3 = Path.Combine(Directory.GetParent(Application.StartupPath).FullName, "Resources", "metro_icon.ico");
                
                // Verificar qual caminho funciona
                string iconPath = null;
                if (File.Exists(iconPath1))
                {
                    iconPath = iconPath1;
                    Console.WriteLine("Ícone encontrado no caminho 1: " + iconPath1);
                }
                else if (File.Exists(iconPath2))
                {
                    iconPath = iconPath2;
                    Console.WriteLine("Ícone encontrado no caminho 2: " + iconPath2);
                }
                else if (File.Exists(iconPath3))
                {
                    iconPath = iconPath3;
                    Console.WriteLine("Ícone encontrado no caminho 3: " + iconPath3);
                }
                else
                {
                    // Nenhum caminho funcionou, registrar informações de diagnóstico
                    Console.WriteLine("Ícone não encontrado nos caminhos testados:");
                    Console.WriteLine("Caminho 1: " + iconPath1);
                    Console.WriteLine("Caminho 2: " + iconPath2);
                    Console.WriteLine("Caminho 3: " + iconPath3);
                    Console.WriteLine("Diretório atual: " + Directory.GetCurrentDirectory());
                    Console.WriteLine("Application.StartupPath: " + Application.StartupPath);
                    Console.WriteLine("AppDomain.CurrentDomain.BaseDirectory: " + AppDomain.CurrentDomain.BaseDirectory);
                    
                    // Fazer fallback para o ícone padrão
                    metroIcon = SystemIcons.Application;
                    
                    // Criar arquivo de log para ajudar no diagnóstico
                    string logPath = Path.Combine(Application.StartupPath, "iconload_error.log");
                    File.WriteAllText(logPath, 
                        $"Falha ao carregar ícone - {DateTime.Now}\n" +
                        $"Caminho 1: {iconPath1} - Existe: {File.Exists(iconPath1)}\n" +
                        $"Caminho 2: {iconPath2} - Existe: {File.Exists(iconPath2)}\n" +
                        $"Caminho 3: {iconPath3} - Existe: {File.Exists(iconPath3)}\n" +
                        $"Diretório atual: {Directory.GetCurrentDirectory()}\n" +
                        $"Application.StartupPath: {Application.StartupPath}\n" +
                        $"AppDomain.CurrentDomain.BaseDirectory: {AppDomain.CurrentDomain.BaseDirectory}\n"
                    );
                    
                    return;
                }
                
                // Carregar o ícone do caminho encontrado
                metroIcon = new Icon(iconPath);
                this.Icon = metroIcon; // Também define o ícone do formulário
                
                // Criar o ícone para consulta (usando um ícone do sistema)
                iconConsulta = SystemIcons.Information;
            }
            catch (Exception ex)
            {
                metroIcon = SystemIcons.Application;
                Console.WriteLine($"Erro ao carregar o ícone: {ex.Message}");
                
                // Criar arquivo de log para ajudar no diagnóstico
                string logPath = Path.Combine(Application.StartupPath, "iconload_exception.log");
                File.WriteAllText(logPath, 
                    $"Exceção ao carregar ícone - {DateTime.Now}\n" +
                    $"Erro: {ex.Message}\n" +
                    $"Stack trace: {ex.StackTrace}\n" +
                    $"Application.StartupPath: {Application.StartupPath}\n" +
                    $"AppDomain.CurrentDomain.BaseDirectory: {AppDomain.CurrentDomain.BaseDirectory}\n"
                );
                
                // Definir ícone de consulta como fallback
                iconConsulta = SystemIcons.Information;
            }
        }

        private void InitializeNotifyIcon()
        {
            // Criar menu de contexto
            contextMenu = new ContextMenuStrip();
            
            // Adicionar itens com ícones correspondentes às ações
            contextMenu.Items.Add("Mostrar Status", SystemIcons.Application.ToBitmap(), ShowStatus_Click);
            contextMenu.Items.Add("Verificar Agora", SystemIcons.Information.ToBitmap(), CheckNow_Click);
            contextMenu.Items.Add("Testar Notificação", SystemIcons.Exclamation.ToBitmap(), TestNotification_Click);
            contextMenu.Items.Add("Configurações", SystemIcons.Shield.ToBitmap(), Settings_Click);
            contextMenu.Items.Add("-"); // Separador
            contextMenu.Items.Add("Sair", SystemIcons.Hand.ToBitmap(), Exit_Click);

            // Inicializar NotifyIcon
            notifyIcon = new NotifyIcon();
            notifyIcon.Icon = metroIcon; // Usar o ícone personalizado do metrô
            notifyIcon.Text = "Monitor de Status do Metrô";
            notifyIcon.ContextMenuStrip = contextMenu;
            notifyIcon.Visible = true;
            notifyIcon.DoubleClick += NotifyIcon_DoubleClick;
        }

        private void Form1_Resize(object sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized)
            {
                this.Hide();
            }
        }

        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
        }

        private void ShowStatus_Click(object sender, EventArgs e)
        {
            this.Show();
            this.WindowState = FormWindowState.Normal;
            this.ShowInTaskbar = true;
        }

        private void CheckNow_Click(object sender, EventArgs e)
        {
            CheckMetroStatusNow();
        }

        private async void TestNotification_Click(object sender, EventArgs e)
        {
            // Obter status atual das linhas do ListView
            var statusAtual = new Dictionary<string, string>();
            foreach (ListViewItem item in listViewStatus.Items)
            {
                statusAtual.Add(item.Text, item.SubItems[1].Text);
            }

            // Criar uma cópia para modificar
            var statusTeste = new Dictionary<string, string>(statusAtual);

            // Lista de possíveis status
            var statusPossiveis = new List<string>
            {
                "Operação Normal",
                "Velocidade Reduzida - Problema técnico",
                "Paralisada - Manutenção emergencial",
                "Operação Parcial - Entre estações",
                "Velocidade Reduzida - Falha em equipamento",
                "Operação Parcial - Falha em sinalização"
            };

            // Selecionar aleatoriamente 1 ou 2 linhas para alterar
            Random random = new Random();
            int quantidadeLinhasParaAlterar = random.Next(1, 3); // 1 ou 2
            
            // Lista de linhas para selecionar aleatoriamente
            var linhas = statusTeste.Keys.ToList();
            
            for (int i = 0; i < quantidadeLinhasParaAlterar && linhas.Count > 0; i++)
            {
                // Selecionar uma linha aleatória
                int indice = random.Next(linhas.Count);
                string linhaParaAlterar = linhas[indice];
                
                // Remover da lista para não selecionar novamente
                linhas.RemoveAt(indice);
                
                // Obter status atual da linha
                string statusAtualDaLinha = statusTeste[linhaParaAlterar];
                
                // Selecionar um novo status diferente do atual
                string novoStatus;
                do
                {
                    novoStatus = statusPossiveis[random.Next(statusPossiveis.Count)];
                } while (novoStatus == statusAtualDaLinha);
                
                // Atualizar o status no dicionário de teste
                statusTeste[linhaParaAlterar] = novoStatus;
            }
            
            // Gerar mensagem informativa sobre o que foi alterado para o MessageBox
            string mensagemAlteracoes = "Status alterados para teste:\n\n";
            
            // Construir mensagem para a notificação incluindo status antigo entre parênteses
            string mensagemNotificacao = "";
            var linhasAlteradas = new List<KeyValuePair<string, string>>();
            
            foreach (var linha in statusTeste)
            {
                if (statusAtual[linha.Key] != linha.Value)
                {
                    // Adicionar à mensagem do MessageBox
                    mensagemAlteracoes += $"{linha.Key}:\n  De: {statusAtual[linha.Key]}\n  Para: {linha.Value}\n\n";
                    
                    // Adicionar à lista de linhas alteradas para a notificação
                    linhasAlteradas.Add(linha);
                    
                    // Adicionar à mensagem da notificação com status antigo entre parênteses
                    mensagemNotificacao += $"{linha.Key}: {linha.Value} (antes: {statusAtual[linha.Key]})\n";
                }
            }
            
            // Mostrar a notificação personalizada
            ShowNotification("Teste de Alteração de Status do Metrô", mensagemNotificacao.TrimEnd());
            
            // Mensagem adicional para confirmar
            MessageBox.Show(mensagemAlteracoes +
                            "Se você não viu a notificação, verifique:\n\n" +
                            "1. As configurações de notificação do Windows\n" +
                            "2. Se seu sistema permite notificações da área de notificação\n" +
                            "3. Se o ícone do programa está ativo na área de notificação",
                "Teste de Notificação", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private void Settings_Click(object sender, EventArgs e)
        {
            // Abrir formulário de configurações
            var settingsForm = new SettingsForm(settings);
            if (settingsForm.ShowDialog() == DialogResult.OK)
            {
                // Recarregar configurações
                settings = Settings.Load();
                
                // Atualizar configurações no monitor
                metroMonitor.AtualizarConfiguracoes();
            }
        }

        private void Exit_Click(object sender, EventArgs e)
        {
            Application.Exit();
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (e.CloseReason == CloseReason.UserClosing)
            {
                e.Cancel = true;
                this.Hide();
                this.ShowInTaskbar = false;
            }
            else
            {
                // Parar monitoramento
                StopMonitoring();
                
                // Limpar recursos
                notifyIcon.Visible = false;
                notifyIcon.Dispose();
            }
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            
            // Esconder a janela na inicialização
            this.Hide();
            
            // Iniciar monitoramento após um pequeno atraso para garantir que tudo está inicializado corretamente
            Task.Delay(1000).ContinueWith(_ => 
            {
                this.Invoke(new Action(() => StartMonitoring()));
            });
        }

        private void StartMonitoring()
        {
            if (!isMonitoring)
            {
                cts = new CancellationTokenSource();
                isMonitoring = true;
                
                Task.Run(async () => 
                {
                    try
                    {
                        // Consultar o status imediatamente na inicialização
                        IniciarIndicadorConsulta();
                        try
                        {
                            var (resultados, horarioAtualizacao) = await metroMonitor.ConsultarStatusLinhasMetroAsync(cts.Token);
                            
                            // Atualizar a interface com os resultados
                            AtualizarStatusNaLista(resultados, horarioAtualizacao);
                            
                            // Forçar uma notificação inicial com o status atual das linhas
                            // Isso deve funcionar mesmo se o mecanismo de detecção de alterações não disparar
                            await ForcarNotificacaoInicial(resultados);
                        }
                        finally
                        {
                            FinalizarIndicadorConsulta();
                        }
                        
                        // Continuar o loop normal de monitoramento
                        while (!cts.Token.IsCancellationRequested)
                        {
                            await Task.Delay(settings.IntervaloVerificacao * 1000, cts.Token);
                            
                            // Consultas subsequentes
                            IniciarIndicadorConsulta();
                            try
                            {
                                var (resultados, horarioAtualizacao) = await metroMonitor.ConsultarStatusLinhasMetroAsync(cts.Token);
                                
                                // Atualizar a interface com os resultados
                                AtualizarStatusNaLista(resultados, horarioAtualizacao);
                            }
                            finally
                            {
                                FinalizarIndicadorConsulta();
                            }
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // Operação cancelada, normal durante o encerramento
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erro no monitoramento: {ex.Message}", "Erro", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                    finally
                    {
                        isMonitoring = false;
                    }
                });
                
                // Mensagem mais detalhada no início do monitoramento
                string mensagemInicio = "Monitoramento iniciado. ";
                
                if (settings.NotificarApenasAlteracoes)
                {
                    mensagemInicio += "Você será notificado quando houver alterações no status das linhas.";
                }
                else
                {
                    mensagemInicio += "Você será notificado sobre o status de todas as linhas periodicamente.";
                }
                
                notifyIcon.ShowBalloonTip(5000, "Monitor de Status do Metrô", mensagemInicio, ToolTipIcon.Info);
            }
        }

        private async Task ForcarNotificacaoInicial(Dictionary<string, string> resultados)
        {
            try
            {
                // Se não há resultados, nada a fazer
                if (resultados == null || resultados.Count == 0)
                {
                    if (settings.ModoDebug)
                        Console.WriteLine("[NOTIFICAÇÃO INICIAL] Não foi possível exibir notificação inicial - Sem resultados");
                    return;
                }
                    
                if (settings.ModoDebug)
                    Console.WriteLine($"[NOTIFICAÇÃO INICIAL] Preparando para exibir notificação inicial com {resultados.Count} linhas");
                    
                // Pequeno atraso para garantir que a notificação de inicialização foi exibida
                await Task.Delay(5000);
                
                // Identificar linhas com problemas
                var linhasComProblemas = resultados
                    .Where(r => !r.Value.Contains("Normal", StringComparison.OrdinalIgnoreCase) && 
                                 settings.DeveMonitorarLinha(r.Key))
                    .ToList();
                
                if (linhasComProblemas.Count > 0)
                {
                    // Criar mensagem para linhas com problemas
                    string mensagem = "Status inicial:\n";
                    foreach (var linha in linhasComProblemas)
                    {
                        mensagem += $"{linha.Key}: {linha.Value}\n";
                    }
                    
                    if (settings.ModoDebug)
                        Console.WriteLine($"[NOTIFICAÇÃO INICIAL] Exibindo notificação de {linhasComProblemas.Count} linhas com problemas");
                        
                    // Mostrar notificação na thread da UI
                    await this.InvokeAsync(new Func<Task>(async () => 
                    {
                        ShowNotification("Status Inicial do Metrô - Atenção", mensagem.TrimEnd());
                        // Garantir tempo para processar a notificação
                        await Task.Delay(500);
                    }));
                }
                else
                {
                    if (settings.ModoDebug)
                        Console.WriteLine("[NOTIFICAÇÃO INICIAL] Exibindo notificação de operação normal para todas as linhas");
                        
                    // SEMPRE exibir notificação inicial se todas as linhas estão normais - não verificar configuração
                    await this.InvokeAsync(new Func<Task>(async () => 
                    {
                        ShowNotification("Status Inicial do Metrô", 
                            "Todas as linhas monitoradas estão operando normalmente.");
                        // Garantir tempo para processar a notificação
                        await Task.Delay(500);
                    }));
                }
                
                if (settings.ModoDebug)
                    Console.WriteLine("[NOTIFICAÇÃO INICIAL] Notificação inicial enviada com sucesso");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERRO] Falha ao forçar notificação inicial: {ex.Message}");
                
                // Tentar um método alternativo em caso de falha
                try
                {
                    // Método alternativo para garantir que pelo menos alguma notificação seja exibida
                    this.Invoke(new Action(() => {
                        notifyIcon.ShowBalloonTip(10000, "Status Inicial do Metrô", 
                            "Verificação concluída. Clique para ver detalhes.", ToolTipIcon.Info);
                    }));
                    
                    if (settings.ModoDebug)
                        Console.WriteLine("[NOTIFICAÇÃO INICIAL] Método alternativo usado como fallback");
                }
                catch (Exception fallbackEx)
                {
                    Console.WriteLine($"[ERRO] Método alternativo de notificação também falhou: {fallbackEx.Message}");
                }
            }
        }

        private Task InvokeAsync(Func<Task> action)
        {
            var tcs = new TaskCompletionSource<bool>();
            
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(async () => 
                {
                    try
                    {
                        await action();
                        tcs.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                }));
            }
            else
            {
                _ = Task.Run(async () => 
                {
                    try
                    {
                        await action();
                        tcs.SetResult(true);
                    }
                    catch (Exception ex)
                    {
                        tcs.SetException(ex);
                    }
                });
            }
            
            return tcs.Task;
        }

        private void StopMonitoring()
        {
            if (isMonitoring && cts != null)
            {
                cts.Cancel();
                cts.Dispose();
                cts = null;
            }
        }

        private void CheckMetroStatusNow()
        {
            if (!isMonitoring)
            {
                StartMonitoring();
            }
            else
            {
                Task.Run(async () => 
                {
                    try
                    {
                        IniciarIndicadorConsulta();
                        try
                        {
                            var (resultados, horarioAtualizacao) = await metroMonitor.ConsultarStatusLinhasMetroAsync(CancellationToken.None);
                            
                            // Atualizar a interface com os resultados
                            AtualizarStatusNaLista(resultados, horarioAtualizacao);
                            
                            // Forçar a notificação também quando o usuário verifica manualmente
                            await ForcarNotificacaoInicial(resultados);
                        }
                        finally
                        {
                            FinalizarIndicadorConsulta();
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Erro ao verificar status: {ex.Message}", "Erro", 
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                });
            }
        }

        public void ShowNotification(string title, string message)
        {
            try
            {
                // Este método será chamado de outra thread, então usamos Invoke
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => ShowNotification(title, message)));
                    return;
                }
                
                // Verificar se é uma notificação inicial (para forçar a exibição mesmo com NotificarApenasAlteracoes=true)
                bool isNotificacaoInicial = title.Contains("Inicial", StringComparison.OrdinalIgnoreCase);
                
                // Se for uma notificação normal (não inicial) e a configuração não permite, ignorar
                if (!isNotificacaoInicial && settings.NotificarApenasAlteracoes && 
                    title.Contains("Status do Metrô") && message.Contains("normalmente"))
                {
                    // Ignorar notificações de status normal em operação regular quando configurado para mostrar apenas alterações
                    if (settings.ModoDebug)
                        Console.WriteLine($"[INFO] Notificação ignorada devido à configuração (NotificarApenasAlteracoes=true): {title}");
                    return;
                }
                
                // Determinar o ícone adequado com base no conteúdo da mensagem
                ToolTipIcon icone = ToolTipIcon.Info;
                
                // Verificar o conteúdo da mensagem para determinar o ícone
                if (message.Contains("Paralisada", StringComparison.OrdinalIgnoreCase) || 
                    message.Contains("Interrompida", StringComparison.OrdinalIgnoreCase))
                {
                    icone = ToolTipIcon.Error;
                }
                else if (message.Contains("Velocidade Reduzida", StringComparison.OrdinalIgnoreCase) || 
                         message.Contains("Parcial", StringComparison.OrdinalIgnoreCase) ||
                         message.Contains("lentidão", StringComparison.OrdinalIgnoreCase))
                {
                    icone = ToolTipIcon.Warning;
                }
                
                // Exibir a notificação com o ícone apropriado
                // Tempo aumentado para 8000ms (8 segundos) para garantir que o usuário tenha tempo de ler
                notifyIcon.ShowBalloonTip(8000, title, message, icone);
                
                // Registrar a notificação no log se o modo debug estiver ativado
                if (settings.ModoDebug)
                {
                    Console.WriteLine($"[NOTIFICAÇÃO EXIBIDA] {title}: {message} (Ícone: {icone})");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao mostrar notificação: {ex.Message}");
                
                // Em caso de erro na notificação, podemos tentar uma abordagem alternativa
                // como abrir o formulário principal com a mensagem
                if (settings.ModoDebug)
                {
                    MessageBox.Show($"Falha ao mostrar notificação:\n\n{title}\n{message}\n\nErro: {ex.Message}",
                        "Erro de Notificação", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
        }
        
        private void IniciarIndicadorConsulta()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(IniciarIndicadorConsulta));
                return;
            }
            
            notifyIcon.Text = "Monitor de Status do Metrô - Consultando...";
            notifyIcon.Icon = iconConsulta;
            
            if (indicadorConsulta != null)
            {
                indicadorConsulta.BackColor = Color.Green;
            }
        }
        
        private void FinalizarIndicadorConsulta()
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new Action(FinalizarIndicadorConsulta));
                return;
            }
            
            notifyIcon.Text = "Monitor de Status do Metrô";
            notifyIcon.Icon = metroIcon;
            
            if (indicadorConsulta != null)
            {
                indicadorConsulta.BackColor = Color.Gray;
            }
        }

        public void AtualizarStatusNaLista(Dictionary<string, string> resultados, string horarioAtualizacao)
        {
            try
            {
                // Verificar se estamos na thread da UI
                if (this.InvokeRequired)
                {
                    this.Invoke(new Action(() => AtualizarStatusNaLista(resultados, horarioAtualizacao)));
                    return;
                }
                
                // Atualizar o horário
                if (!string.IsNullOrEmpty(horarioAtualizacao))
                {
                    lblUltimaAtualizacao.Text = $"Última atualização: {horarioAtualizacao}";
                }
                else
                {
                    lblUltimaAtualizacao.Text = $"Última consulta: {DateTime.Now:HH:mm:ss}";
                }
                
                // Atualizar cada linha na lista
                foreach (ListViewItem item in listViewStatus.Items)
                {
                    string nomeLinha = item.Text;
                    if (resultados.TryGetValue(nomeLinha, out string status))
                    {
                        item.SubItems[1].Text = status;
                        
                        // Definir cor com base no status
                        if (status.ToLower().Contains("normal") || status.ToLower().Contains("operando normalmente"))
                        {
                            item.BackColor = Color.LightGreen;
                        }
                        else if (status.ToLower().Contains("paralisada") || status.ToLower().Contains("interrompida"))
                        {
                            item.BackColor = Color.LightCoral;
                        }
                        else
                        {
                            item.BackColor = Color.LightYellow;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao atualizar lista: {ex.Message}");
            }
        }
    }
} 