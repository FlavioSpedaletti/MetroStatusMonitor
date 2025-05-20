using System;
using System.Windows.Forms;
using MetroStatusMonitor.Core;

namespace MetroStatusMonitor.WinForms
{
    public partial class SettingsForm : Form
    {
        private Settings settings;
        private NumericUpDown numIntervalo;
        private CheckBox chkIniciarComWindows;
        private CheckBox chkNotificarApenasAlteracoes;
        private CheckBox chkMinimizarAoIniciar;
        private CheckBox chkModoDebug;
        private CheckBox chkPersistirEmJson;
        
        // Checkboxes para as linhas de metrô
        private CheckBox chkLinha1Azul;
        private CheckBox chkLinha2Verde;
        private CheckBox chkLinha3Vermelha;
        private CheckBox chkLinha4Amarela;
        private CheckBox chkLinha5Lilas;
        private CheckBox chkLinha15Prata;
        
        private Button btnSalvar;
        private Button btnCancelar;
        
        public SettingsForm(Settings settings)
        {
            this.settings = settings;
            InitializeComponent();
            LoadSettings();
        }
        
        private void InitializeComponent()
        {
            this.SuspendLayout();
            
            // Definir propriedades do formulário
            this.Text = "Configurações";
            this.ClientSize = new System.Drawing.Size(400, 450);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.StartPosition = FormStartPosition.CenterParent;
            
            // Configuração de intervalo
            Label lblIntervalo = new Label();
            lblIntervalo.Text = "Intervalo de verificação (segundos):";
            lblIntervalo.Location = new System.Drawing.Point(20, 20);
            lblIntervalo.AutoSize = true;
            this.Controls.Add(lblIntervalo);
            
            numIntervalo = new NumericUpDown();
            numIntervalo.Location = new System.Drawing.Point(250, 18);
            numIntervalo.Size = new System.Drawing.Size(80, 20);
            numIntervalo.Minimum = 10;
            numIntervalo.Maximum = 3600;
            numIntervalo.Value = 60;
            this.Controls.Add(numIntervalo);
            
            // Opções gerais
            Label lblOpcoesGerais = new Label();
            lblOpcoesGerais.Text = "Opções Gerais:";
            lblOpcoesGerais.Location = new System.Drawing.Point(20, 60);
            lblOpcoesGerais.AutoSize = true;
            lblOpcoesGerais.Font = new System.Drawing.Font(lblOpcoesGerais.Font, System.Drawing.FontStyle.Bold);
            this.Controls.Add(lblOpcoesGerais);
            
            // Checkbox para iniciar com Windows
            chkIniciarComWindows = new CheckBox();
            chkIniciarComWindows.Text = "Iniciar com o Windows";
            chkIniciarComWindows.Location = new System.Drawing.Point(40, 90);
            chkIniciarComWindows.AutoSize = true;
            this.Controls.Add(chkIniciarComWindows);
            
            // Checkbox para notificar apenas alterações
            chkNotificarApenasAlteracoes = new CheckBox();
            chkNotificarApenasAlteracoes.Text = "Notificar apenas alterações de status";
            chkNotificarApenasAlteracoes.Location = new System.Drawing.Point(40, 120);
            chkNotificarApenasAlteracoes.AutoSize = true;
            this.Controls.Add(chkNotificarApenasAlteracoes);
            
            // Checkbox para minimizar ao iniciar
            chkMinimizarAoIniciar = new CheckBox();
            chkMinimizarAoIniciar.Text = "Iniciar minimizado na área de notificação";
            chkMinimizarAoIniciar.Location = new System.Drawing.Point(40, 150);
            chkMinimizarAoIniciar.AutoSize = true;
            this.Controls.Add(chkMinimizarAoIniciar);
            
            // Checkbox para modo debug
            chkModoDebug = new CheckBox();
            chkModoDebug.Text = "Modo de depuração (gera logs)";
            chkModoDebug.Location = new System.Drawing.Point(40, 180);
            chkModoDebug.AutoSize = true;
            this.Controls.Add(chkModoDebug);
            
            // Checkbox para persistir em JSON
            chkPersistirEmJson = new CheckBox();
            chkPersistirEmJson.Text = "Persistir histórico em JSON";
            chkPersistirEmJson.Location = new System.Drawing.Point(40, 210);
            chkPersistirEmJson.AutoSize = true;
            this.Controls.Add(chkPersistirEmJson);
            
            // Linhas a monitorar
            Label lblLinhasMonitorar = new Label();
            lblLinhasMonitorar.Text = "Linhas a Monitorar:";
            lblLinhasMonitorar.Location = new System.Drawing.Point(20, 250);
            lblLinhasMonitorar.AutoSize = true;
            lblLinhasMonitorar.Font = new System.Drawing.Font(lblLinhasMonitorar.Font, System.Drawing.FontStyle.Bold);
            this.Controls.Add(lblLinhasMonitorar);
            
            // Checkboxes para cada linha
            chkLinha1Azul = new CheckBox();
            chkLinha1Azul.Text = "Linha 1-Azul";
            chkLinha1Azul.Location = new System.Drawing.Point(40, 280);
            chkLinha1Azul.AutoSize = true;
            this.Controls.Add(chkLinha1Azul);
            
            chkLinha2Verde = new CheckBox();
            chkLinha2Verde.Text = "Linha 2-Verde";
            chkLinha2Verde.Location = new System.Drawing.Point(40, 310);
            chkLinha2Verde.AutoSize = true;
            this.Controls.Add(chkLinha2Verde);
            
            chkLinha3Vermelha = new CheckBox();
            chkLinha3Vermelha.Text = "Linha 3-Vermelha";
            chkLinha3Vermelha.Location = new System.Drawing.Point(40, 340);
            chkLinha3Vermelha.AutoSize = true;
            this.Controls.Add(chkLinha3Vermelha);
            
            chkLinha4Amarela = new CheckBox();
            chkLinha4Amarela.Text = "Linha 4-Amarela";
            chkLinha4Amarela.Location = new System.Drawing.Point(200, 280);
            chkLinha4Amarela.AutoSize = true;
            this.Controls.Add(chkLinha4Amarela);
            
            chkLinha5Lilas = new CheckBox();
            chkLinha5Lilas.Text = "Linha 5-Lilás";
            chkLinha5Lilas.Location = new System.Drawing.Point(200, 310);
            chkLinha5Lilas.AutoSize = true;
            this.Controls.Add(chkLinha5Lilas);
            
            chkLinha15Prata = new CheckBox();
            chkLinha15Prata.Text = "Linha 15-Prata";
            chkLinha15Prata.Location = new System.Drawing.Point(200, 340);
            chkLinha15Prata.AutoSize = true;
            this.Controls.Add(chkLinha15Prata);
            
            // Botões Salvar e Cancelar
            btnSalvar = new Button();
            btnSalvar.Text = "Salvar";
            btnSalvar.Location = new System.Drawing.Point(200, 380);
            btnSalvar.Size = new System.Drawing.Size(80, 30);
            btnSalvar.Click += BtnSalvar_Click;
            this.Controls.Add(btnSalvar);
            
            btnCancelar = new Button();
            btnCancelar.Text = "Cancelar";
            btnCancelar.Location = new System.Drawing.Point(290, 380);
            btnCancelar.Size = new System.Drawing.Size(80, 30);
            btnCancelar.Click += BtnCancelar_Click;
            this.Controls.Add(btnCancelar);
            
            this.ResumeLayout(false);
            this.PerformLayout();
        }
        
        private void LoadSettings()
        {
            // Carregar configurações para os controles
            numIntervalo.Value = Math.Max(numIntervalo.Minimum, Math.Min(numIntervalo.Maximum, settings.IntervaloVerificacao));
            chkIniciarComWindows.Checked = settings.IniciarComWindows;
            chkNotificarApenasAlteracoes.Checked = settings.NotificarApenasAlteracoes;
            chkMinimizarAoIniciar.Checked = settings.MinimizarAoIniciar;
            chkModoDebug.Checked = settings.ModoDebug;
            chkPersistirEmJson.Checked = settings.PersistirEmJson;
            
            // Carregar configurações das linhas
            chkLinha1Azul.Checked = settings.MonitorarLinha1Azul;
            chkLinha2Verde.Checked = settings.MonitorarLinha2Verde;
            chkLinha3Vermelha.Checked = settings.MonitorarLinha3Vermelha;
            chkLinha4Amarela.Checked = settings.MonitorarLinha4Amarela;
            chkLinha5Lilas.Checked = settings.MonitorarLinha5Lilas;
            chkLinha15Prata.Checked = settings.MonitorarLinha15Prata;
        }
        
        private void BtnSalvar_Click(object sender, EventArgs e)
        {
            // Salvar configurações
            settings.IntervaloVerificacao = (int)numIntervalo.Value;
            settings.NotificarApenasAlteracoes = chkNotificarApenasAlteracoes.Checked;
            settings.MinimizarAoIniciar = chkMinimizarAoIniciar.Checked;
            settings.ModoDebug = chkModoDebug.Checked;
            settings.PersistirEmJson = chkPersistirEmJson.Checked;
            
            // Salvar configurações das linhas
            settings.MonitorarLinha1Azul = chkLinha1Azul.Checked;
            settings.MonitorarLinha2Verde = chkLinha2Verde.Checked;
            settings.MonitorarLinha3Vermelha = chkLinha3Vermelha.Checked;
            settings.MonitorarLinha4Amarela = chkLinha4Amarela.Checked;
            settings.MonitorarLinha5Lilas = chkLinha5Lilas.Checked;
            settings.MonitorarLinha15Prata = chkLinha15Prata.Checked;
            
            // Tratar configuração de inicialização com o Windows
            if (settings.IniciarComWindows != chkIniciarComWindows.Checked)
            {
                settings.ConfigurarInicioComWindows(chkIniciarComWindows.Checked);
            }
            
            // Salvar configurações no arquivo
            settings.Save();
            
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
        
        private void BtnCancelar_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }
    }
} 