using System;
using System.Windows.Forms;
using MetroStatusMonitor.Core;

namespace MetroStatusMonitor.WinForms
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            // Processar argumentos de linha de comando
            bool modoDebug = false;
            bool persistirEmJson = false;
            bool argModoDebug = false;
            bool argPersistirEmJson = false;
            
            if (args.Length > 0)
            {
                foreach (var arg in args)
                {
                    if (arg.ToLower() == "--debug" || arg.ToLower() == "-d")
                    {
                        modoDebug = true;
                        argModoDebug = true;
                    }
                    else if (arg.ToLower().StartsWith("--log="))
                    {
                        // Configurar arquivo de log se necessário
                    }
                    else if (arg.ToLower() == "--persistir" || arg.ToLower() == "--persistiremjson")
                    {
                        persistirEmJson = true;
                        argPersistirEmJson = true;
                    }
                }
            }
            
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            
            try
            {
                // Carregar configurações
                var settings = Settings.Load();
                
                // Aplicar opções da linha de comando se especificadas
                if (argModoDebug)
                    settings.ModoDebug = modoDebug;
                
                if (argPersistirEmJson)
                    settings.PersistirEmJson = persistirEmJson;
                
                // Salvar configurações atualizadas
                settings.Save();
                
                Application.Run(new Form1());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Erro ao iniciar o aplicativo: {ex.Message}", "Erro",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
} 