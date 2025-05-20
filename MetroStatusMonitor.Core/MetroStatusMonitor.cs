using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using HtmlAgilityPack;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;

namespace MetroStatusMonitor.Core
{
    public class StatusChangedEventArgs : EventArgs
    {
        public List<StatusChangedInfo> Mudancas { get; }

        public StatusChangedEventArgs(List<StatusChangedInfo> mudancas)
        {
            Mudancas = mudancas;
        }
    }

    public class StatusChangedInfo
    {
        public string NomeLinha { get; set; }
        public string StatusAnterior { get; set; }
        public string StatusNovo { get; set; }
    }

    public class MetroStatusMonitor
    {
        private static readonly HttpClient httpClient = new HttpClient();
        private static readonly string metroUrl = "https://www.metro.sp.gov.br/wp-content/themes/metrosp/direto-metro.php";
        private bool modoDebug = false;
        private bool persistirEmJson = false;
        private static string arquivoLog = "metro_status_log.txt";
        private static string arquivoHistorico = "historico_status.json";
        private Settings settings;
        
        // Lista de linhas conhecidas
        private static readonly List<string> linhasConhecidas = new List<string>
        {
            "Linha 1-Azul", "Linha 2-Verde", "Linha 3-Vermelha", 
            "Linha 4-Amarela", "Linha 5-Lilás", "Linha 15-Prata"
        };
        
        // Dicionário para armazenar o histórico de status
        private Dictionary<string, LinhaStatus> statusAnterior = new Dictionary<string, LinhaStatus>();
        
        // Evento para notificar quando o status muda
        public event EventHandler<StatusChangedEventArgs> StatusChanged;

        public MetroStatusMonitor()
        {
            // Carregar configurações
            settings = Settings.Load();
            this.modoDebug = settings.ModoDebug;
            this.persistirEmJson = settings.PersistirEmJson;
            
            // Ajustando o timeout do HttpClient para 30 segundos
            httpClient.Timeout = TimeSpan.FromSeconds(30);
            
            // Carregar histórico somente se persistirEmJson estiver ativado
            if (persistirEmJson)
            {
                CarregarHistorico();
            }
        }
        
        // Método para atualizar configurações
        public void AtualizarConfiguracoes()
        {
            settings = Settings.Load();
            this.modoDebug = settings.ModoDebug;
            this.persistirEmJson = settings.PersistirEmJson;
        }
        
        // Carregar o histórico de estados
        private void CarregarHistorico()
        {
            try
            {
                if (File.Exists(arquivoHistorico))
                {
                    var json = File.ReadAllText(arquivoHistorico);
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    };
                    statusAnterior = JsonSerializer.Deserialize<Dictionary<string, LinhaStatus>>(json, options);
                    
                    // Converter os horários de string para DateTime
                    foreach (var linha in statusAnterior.Values)
                    {
                        if (!string.IsNullOrEmpty(linha.HorarioConsultaStr))
                        {
                            DateTime.TryParse(linha.HorarioConsultaStr, out var horario);
                            linha.HorarioConsulta = horario;
                        }
                    }
                    
                    if (modoDebug) Log($"Histórico carregado com {statusAnterior.Count} registros.");
                }
            }
            catch (Exception ex)
            {
                if (modoDebug) Log($"Erro ao carregar histórico: {ex.Message}");
                statusAnterior = new Dictionary<string, LinhaStatus>();
            }
        }
        
        // Salvar o histórico de estados
        private void SalvarHistorico()
        {
            if (!persistirEmJson) return;
            
            try
            {
                var options = new JsonSerializerOptions
                {
                    WriteIndented = true
                };
                var json = JsonSerializer.Serialize(statusAnterior, options);
                File.WriteAllText(arquivoHistorico, json);
                if (modoDebug) Log($"Histórico salvo em {arquivoHistorico}");
            }
            catch (Exception ex)
            {
                if (modoDebug) Log($"Erro ao salvar histórico: {ex.Message}");
            }
        }

        // Método principal que consulta o status das linhas
        public async Task<(Dictionary<string, string>, string)> ConsultarStatusLinhasMetroAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (modoDebug) Log($"Consultando status às {DateTime.Now:HH:mm:ss}...");
                
                // Obter o conteúdo HTML do site do metrô
                string html = await httpClient.GetStringAsync(metroUrl);
                
                if (modoDebug)
                {
                    Log($"Recebidos {html.Length} caracteres HTML.");
                }
                
                // Carregar o HTML no HtmlAgilityPack
                var htmlDoc = new HtmlAgilityPack.HtmlDocument();
                htmlDoc.LoadHtml(html);
                
                // Armazenar os resultados encontrados
                var resultados = new Dictionary<string, string>();
                
                // Extrair horário de atualização
                string horarioAtualizacao = ExtrairHorarioAtualizacao(html, htmlDoc);
                
                // Obter status das linhas usando todos os métodos
                ObterStatusLinhas(html, htmlDoc, resultados);
                
                // Se ainda não temos todos os resultados, assumir operação normal para as linhas faltantes
                foreach (var linhaConhecida in linhasConhecidas)
                {
                    if (!resultados.ContainsKey(linhaConhecida))
                    {
                        resultados[linhaConhecida] = "Operação Normal";
                        if (modoDebug) Log($"Assumindo 'Operação Normal' para {linhaConhecida} (não encontrado explicitamente)");
                    }
                }
                
                // Se há resultados, processá-los
                if (resultados.Count > 0)
                {
                    // Criar objetos LinhaStatus para o status atual
                    var statusAtual = new Dictionary<string, LinhaStatus>();
                    DateTime agora = DateTime.Now;
                    
                    // Lista para armazenar informações sobre mudanças de status
                    var mudancasStatus = new List<StatusChangedInfo>();
                    
                    // Flag para verificar se é a primeira execução (não há histórico de status anterior)
                    bool isPrimeiraExecucao = statusAnterior.Count == 0;
                    
                    // Para cada linha encontrada
                    foreach (var nomeLinha in resultados.Keys)
                    {
                        var statusLinha = resultados[nomeLinha];
                        var novoStatus = new LinhaStatus
                        {
                            Nome = nomeLinha,
                            Status = statusLinha,
                            HorarioConsulta = agora,
                            HorarioConsultaStr = agora.ToString("dd/MM/yyyy HH:mm:ss"),
                            HorarioAtualizacaoMetro = horarioAtualizacao
                        };
                        
                        // Verificar se houve mudança de status
                        if (statusAnterior.TryGetValue(nomeLinha, out var statusAnteriorLinha))
                        {
                            if (statusAnteriorLinha.Status != statusLinha)
                            {
                                // Verificar se devemos monitorar esta linha
                                if (settings.DeveMonitorarLinha(nomeLinha))
                                {
                                    // Adicionar à lista de mudanças
                                    mudancasStatus.Add(new StatusChangedInfo
                                    {
                                        NomeLinha = nomeLinha,
                                        StatusAnterior = statusAnteriorLinha.Status,
                                        StatusNovo = statusLinha
                                    });
                                    
                                    if (modoDebug) 
                                        Log($"Mudança de status para {nomeLinha}: {statusAnteriorLinha.Status} -> {statusLinha}");
                                }
                            }
                        }
                        else if (modoDebug)
                        {
                            Log($"Primeira ocorrência de {nomeLinha}: {statusLinha}");
                            
                            // Na primeira execução, adicionar todas as linhas à lista de mudanças
                            if (isPrimeiraExecucao && settings.DeveMonitorarLinha(nomeLinha))
                            {
                                mudancasStatus.Add(new StatusChangedInfo
                                {
                                    NomeLinha = nomeLinha,
                                    StatusAnterior = "Desconhecido",
                                    StatusNovo = statusLinha
                                });
                            }
                        }
                        
                        // Adicionar ao status atual
                        statusAtual[nomeLinha] = novoStatus;
                    }
                    
                    // Atualizar o status anterior com o atual
                    statusAnterior = statusAtual;
                    
                    // Salvar o histórico
                    SalvarHistorico();
                    
                    // Se houve mudanças, disparar o evento
                    if (mudancasStatus.Count > 0)
                    {
                        OnStatusChanged(new StatusChangedEventArgs(mudancasStatus));
                    }
                    // Se for a primeira execução e não há mudanças (porque tudo foi adicionado como primeira ocorrência)
                    else if (isPrimeiraExecucao)
                    {
                        // Criar uma lista com todas as linhas monitoradas
                        var statusInicial = new List<StatusChangedInfo>();
                        foreach (var linha in statusAtual)
                        {
                            if (settings.DeveMonitorarLinha(linha.Key))
                            {
                                statusInicial.Add(new StatusChangedInfo
                                {
                                    NomeLinha = linha.Key,
                                    StatusAnterior = "Primeira verificação",
                                    StatusNovo = linha.Value.Status
                                });
                            }
                        }
                        
                        if (statusInicial.Count > 0)
                        {
                            OnStatusChanged(new StatusChangedEventArgs(statusInicial));
                            if (modoDebug) Log("Notificando status inicial das linhas");
                        }
                    }
                }
                
                return (resultados, horarioAtualizacao);
            }
            catch (Exception ex)
            {
                if (modoDebug) Log($"Erro ao consultar status: {ex.Message}");
                return (new Dictionary<string, string>(), "");
            }
        }
        
        // Extrair o horário de atualização do site do Metrô
        private string ExtrairHorarioAtualizacao(string html, HtmlAgilityPack.HtmlDocument htmlDoc)
        {
            try
            {
                // Primeiro método: procurar o formato completo "Atualizado: dd/MM/yyyy HH:mm:ss"
                var nodeAtualizacao = htmlDoc.DocumentNode.SelectSingleNode("//span[contains(text(), 'Atualizado:')]");
                if (nodeAtualizacao != null)
                {
                    var textoNode = nodeAtualizacao.InnerText.Trim();
                    if (modoDebug) Log($"Texto do nó de atualização: {textoNode}");
                    
                    // Padrão completo com data e hora
                    var matchCompleto = Regex.Match(textoNode, @"Atualizado:\s*(\d{2}/\d{2}/\d{4}\s+\d{2}:\d{2}:\d{2})");
                    if (matchCompleto.Success)
                    {
                        string horarioCompleto = matchCompleto.Groups[1].Value;
                        if (modoDebug) Log($"Horário completo extraído: {horarioCompleto}");
                        return horarioCompleto;
                    }
                    
                    // Tenta outros formatos se o completo não for encontrado
                    var matchSemSegundos = Regex.Match(textoNode, @"Atualizado:\s*(\d{2}/\d{2}/\d{4}\s+\d{2}:\d{2})");
                    if (matchSemSegundos.Success)
                    {
                        string horarioSemSegundos = matchSemSegundos.Groups[1].Value + ":00";
                        if (modoDebug) Log($"Horário sem segundos extraído e completado: {horarioSemSegundos}");
                        return horarioSemSegundos;
                    }
                }
                
                // Segundo método (legado): buscar no texto "Atualizado às"
                nodeAtualizacao = htmlDoc.DocumentNode.SelectSingleNode("//span[contains(text(), 'Atualizado às')]");
                if (nodeAtualizacao != null)
                {
                    var textoNode = nodeAtualizacao.InnerText;
                    var match = Regex.Match(textoNode, @"Atualizado às (\d{1,2}h\d{1,2})");
                    if (match.Success)
                    {
                        string horario = match.Groups[1].Value;
                        if (modoDebug) Log($"Horário legado extraído pelo método 1: {horario}");
                        
                        // Converter para o novo formato
                        string[] partes = horario.Replace("h", ":").Split(':');
                        if (partes.Length == 2)
                        {
                            return $"{DateTime.Now:dd/MM/yyyy} {partes[0].PadLeft(2, '0')}:{partes[1].PadLeft(2, '0')}:00";
                        }
                        return horario;
                    }
                }
                
                // Terceiro método (legado): buscar na classe "hora_status"
                var nodeHora = htmlDoc.DocumentNode.SelectSingleNode("//div[@class='hora_status']");
                if (nodeHora != null)
                {
                    var textoHora = nodeHora.InnerText;
                    var match = Regex.Match(textoHora, @"(\d{1,2}h\d{1,2})");
                    if (match.Success)
                    {
                        string horario = match.Groups[1].Value;
                        if (modoDebug) Log($"Horário legado extraído pelo método 2: {horario}");
                        
                        // Converter para o novo formato
                        string[] partes = horario.Replace("h", ":").Split(':');
                        if (partes.Length == 2)
                        {
                            return $"{DateTime.Now:dd/MM/yyyy} {partes[0].PadLeft(2, '0')}:{partes[1].PadLeft(2, '0')}:00";
                        }
                        return horario;
                    }
                }
                
                // Se nenhum método funcionar, retorna o horário atual
                string horarioAtual = $"{DateTime.Now:dd/MM/yyyy HH:mm:ss}";
                if (modoDebug) Log($"Usando horário atual completo: {horarioAtual}");
                return horarioAtual;
            }
            catch (Exception ex)
            {
                if (modoDebug) Log($"Erro ao extrair horário: {ex.Message}");
                return $"{DateTime.Now:dd/MM/yyyy HH:mm:ss}";
            }
        }
        
        // Obter o status de todas as linhas
        private void ObterStatusLinhas(string html, HtmlAgilityPack.HtmlDocument htmlDoc, Dictionary<string, string> resultados)
        {
            try
            {
                // Método 1: procurar por divs com class que contém 'situacao_linhas'
                var statusNodes = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'situacao_linhas')]/span");
                if (statusNodes != null)
                {
                    foreach (var node in statusNodes)
                    {
                        var texto = node.InnerText.Trim();
                        if (PodeSerStatus(texto))
                        {
                            var nomeLinha = ExtrairNomeLinha(texto);
                            var statusLinha = ExtrairStatusLinha(texto, nomeLinha);
                            
                            if (!string.IsNullOrEmpty(nomeLinha) && !string.IsNullOrEmpty(statusLinha))
                            {
                                resultados[nomeLinha] = statusLinha;
                                if (modoDebug) Log($"Método 1: {nomeLinha} = {statusLinha}");
                            }
                        }
                    }
                }
                
                // Método 2: tentar encontrar elementos de status com seletores alternativos
                if (resultados.Count < linhasConhecidas.Count)
                {
                    // Tenta encontrar pela classe 'status_linha' (nova estrutura possível)
                    var novosStatusNodes = htmlDoc.DocumentNode.SelectNodes("//div[contains(@class, 'status_linha')] | //div[contains(@class, 'linha-metro')] | //span[contains(@class, 'status')] | //p[contains(@class, 'status')] | //div[contains(@class, 'linha')]");
                    
                    if (novosStatusNodes != null)
                    {
                        foreach (var node in novosStatusNodes)
                        {
                            var texto = node.InnerText.Trim();
                            if (PodeSerStatus(texto))
                            {
                                var nomeLinha = ExtrairNomeLinha(texto);
                                var statusLinha = ExtrairStatusLinha(texto, nomeLinha);
                                
                                if (!string.IsNullOrEmpty(nomeLinha) && !string.IsNullOrEmpty(statusLinha) && !resultados.ContainsKey(nomeLinha))
                                {
                                    resultados[nomeLinha] = statusLinha;
                                    if (modoDebug) Log($"Método 2 (novos seletores): {nomeLinha} = {statusLinha}");
                                }
                            }
                        }
                    }
                }
                
                // Método 3: usar regex para encontrar padrões de status no HTML
                if (resultados.Count < linhasConhecidas.Count)
                {
                    foreach (var linha in linhasConhecidas)
                    {
                        if (!resultados.ContainsKey(linha))
                        {
                            // Tenta vários padrões de regex
                            var patterns = new string[]
                            {
                                $@"{linha}:\s*([^<]+)", // Padrão: "Linha X-Cor: Status da linha"
                                $@"{linha.Replace("Linha ", "")}:\s*([^<]+)", // Padrão sem a palavra "Linha"
                                $@"{linha}[^<]*?status[^<]*?([^<]+)", // Procura por "status" próximo ao nome da linha
                                $@"<[^>]*>{linha}[^<]*?<[^>]*>[^<]*?([^<]+)" // Possível estrutura HTML 
                            };
                            
                            foreach (var pattern in patterns)
                            {
                                var match = Regex.Match(html, pattern, RegexOptions.IgnoreCase);
                                if (match.Success)
                                {
                                    var status = match.Groups[1].Value.Trim();
                                    if (!string.IsNullOrEmpty(status))
                                    {
                                        resultados[linha] = status;
                                        if (modoDebug) Log($"Método 3 (regex): {linha} = {status}");
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                
                // Método 4: Extrair dados via JSON no HTML (algumas páginas modernas incluem dados em JSON)
                if (resultados.Count < linhasConhecidas.Count)
                {
                    try
                    {
                        var jsonMatch = Regex.Match(html, @"var\s+linhasStatus\s*=\s*({.*?});", RegexOptions.Singleline);
                        if (jsonMatch.Success)
                        {
                            var jsonStr = jsonMatch.Groups[1].Value;
                            using (var doc = JsonDocument.Parse(jsonStr))
                            {
                                foreach (var linha in linhasConhecidas)
                                {
                                    if (!resultados.ContainsKey(linha))
                                    {
                                        // Tenta encontrar a linha pelo nome sem "Linha " e pegando apenas o número
                                        var numeroLinha = Regex.Match(linha, @"Linha (\d+)").Groups[1].Value;
                                        if (!string.IsNullOrEmpty(numeroLinha) && doc.RootElement.TryGetProperty(numeroLinha, out var linhaElement))
                                        {
                                            if (linhaElement.TryGetProperty("status", out var statusElement))
                                            {
                                                var status = statusElement.GetString();
                                                if (!string.IsNullOrEmpty(status))
                                                {
                                                    resultados[linha] = status;
                                                    if (modoDebug) Log($"Método 4 (JSON): {linha} = {status}");
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        if (modoDebug) Log($"Erro ao extrair JSON: {ex.Message}");
                    }
                }
                
                // Método 5: Extrair dados diretamente do texto bruto para casos extremos
                if (resultados.Count < linhasConhecidas.Count)
                {
                    foreach (var linha in linhasConhecidas)
                    {
                        if (!resultados.ContainsKey(linha))
                        {
                            // Divide o HTML em blocos menores para análise
                            var blocks = html.Split(new[] { '\n', '<', '>' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var block in blocks)
                            {
                                if (block.Contains(linha, StringComparison.OrdinalIgnoreCase) || 
                                    block.Contains(linha.Replace("Linha ", ""), StringComparison.OrdinalIgnoreCase))
                                {
                                    var statusLinha = ExtrairStatusLinha(block.Trim(), linha);
                                    if (!string.IsNullOrEmpty(statusLinha))
                                    {
                                        resultados[linha] = statusLinha;
                                        if (modoDebug) Log($"Método 5 (texto bruto): {linha} = {statusLinha}");
                                        break;
                                    }
                                }
                            }
                        }
                    }
                }
                
                // Usar o método de fallback para linhas não encontradas
                if (resultados.Count < linhasConhecidas.Count && modoDebug)
                {
                    Log($"Não foi possível obter status para todas as linhas. Encontrados: {resultados.Count} de {linhasConhecidas.Count}");
                }
            }
            catch (Exception ex)
            {
                if (modoDebug) Log($"Erro ao obter status das linhas: {ex.Message}");
            }
        }
        
        // Verificar se um texto pode conter informações de status
        private bool PodeSerStatus(string texto)
        {
            if (string.IsNullOrEmpty(texto)) return false;
            
            // Verificar se contém algum identificador de linha
            foreach (var linha in linhasConhecidas)
            {
                if (texto.Contains(linha, StringComparison.OrdinalIgnoreCase) ||
                    texto.Contains(linha.Replace("Linha ", ""), StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            
            // Verificar se contém palavras-chave de status
            string[] statusKeywords = { "operação", "normal", "velocidade", "reduzida", "paralisada" };
            foreach (var keyword in statusKeywords)
            {
                if (texto.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }
            
            return false;
        }
        
        // Extrair o nome da linha a partir do texto
        private string ExtrairNomeLinha(string texto)
        {
            foreach (var linha in linhasConhecidas)
            {
                if (texto.Contains(linha, StringComparison.OrdinalIgnoreCase))
                {
                    return linha;
                }
                else if (texto.Contains(linha.Replace("Linha ", ""), StringComparison.OrdinalIgnoreCase))
                {
                    return linha;
                }
            }
            return "";
        }
        
        // Extrair o status da linha a partir do texto
        private string ExtrairStatusLinha(string texto, string nomeLinha = null)
        {
            if (string.IsNullOrEmpty(texto)) return "";
            
            if (!string.IsNullOrEmpty(nomeLinha) && texto.Contains(nomeLinha, StringComparison.OrdinalIgnoreCase))
            {
                // Remover o nome da linha do texto para ficar só com o status
                texto = texto.Replace(nomeLinha, "").Replace(":", "").Trim();
            }
            
            // Lista expandida de possíveis formatos de status
            Dictionary<string, string[]> statusConhecidos = new Dictionary<string, string[]>
            {
                { "Operação Normal", new[] { 
                    "normal", "operação normal", "operando normalmente", "em operação", "funcionamento normal"
                }},
                { "Velocidade Reduzida", new[] { 
                    "velocidade reduzida", "reduzida", "lenta", "redução", "mais lento", "atraso", "lentidão", "lento"
                }},
                { "Paralisada", new[] { 
                    "paralisada", "interrompida", "paralisação", "interrupção", "parada", "parado", "fechada", "fechado", "não opera", "suspensa"
                }},
                { "Operação Parcial", new[] {
                    "parcial", "operação parcial", "parcialmente", "trecho", "parte da linha"
                }}
            };
            
            // Tentar identificar por padrões conhecidos
            foreach (var statusPadrao in statusConhecidos)
            {
                foreach (var keyword in statusPadrao.Value)
                {
                    if (texto.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                    {
                        return statusPadrao.Key;
                    }
                }
            }
            
            // Se não conseguiu identificar usando os padrões, tenta extrair alguma frase curta
            if (!string.IsNullOrEmpty(texto))
            {
                // Limpar o texto
                texto = texto.Replace(":", "").Trim();
                
                // Se o texto for muito longo, tentar extrair uma frase curta
                if (texto.Length > 50)
                {
                    // Tenta extrair uma frase curta (considerando até 50 caracteres)
                    var matchFraseCurta = Regex.Match(texto, @"(opera\w+\s+\w+|em\s+\w+|com\s+\w+|status\s*:\s*\w+)[^.]{0,30}");
                    if (matchFraseCurta.Success)
                    {
                        return matchFraseCurta.Value.Trim();
                    }
                    
                    // Se ainda assim não conseguiu, pega os primeiros 50 caracteres
                    return texto.Substring(0, 50) + "...";
                }
                
                if (!string.IsNullOrEmpty(texto))
                {
                    return texto;
                }
            }
            
            return "Operação Normal"; // Status padrão
        }
        
        // Método de log para depuração
        private void Log(string mensagem)
        {
            try
            {
                var logMessage = $"[{DateTime.Now:HH:mm:ss}] {mensagem}";
                Console.WriteLine(logMessage);
                
                if (modoDebug)
                {
                    File.AppendAllText(arquivoLog, logMessage + Environment.NewLine);
                }
            }
            catch
            {
                // Ignorar erros de log
            }
        }
        
        // Método para disparar o evento de mudança de status
        protected virtual void OnStatusChanged(StatusChangedEventArgs e)
        {
            StatusChanged?.Invoke(this, e);
        }
        
        // Método para obter o status anterior das linhas para comparação
        public Dictionary<string, string> ObterStatusAnterior()
        {
            var resultado = new Dictionary<string, string>();
            foreach (var entrada in statusAnterior)
            {
                resultado[entrada.Key] = entrada.Value.Status;
            }
            return resultado;
        }
    }
    
    public class LinhaStatus
    {
        public string Nome { get; set; }
        public string Status { get; set; }
        [JsonIgnore] // Ignorar na serialização para evitar problemas
        public DateTime HorarioConsulta { get; set; }
        public string HorarioConsultaStr { get; set; }
        public string HorarioAtualizacaoMetro { get; set; }
        
        public LinhaStatus Clone()
        {
            return new LinhaStatus
            {
                Nome = this.Nome,
                Status = this.Status,
                HorarioConsulta = this.HorarioConsulta,
                HorarioConsultaStr = this.HorarioConsultaStr,
                HorarioAtualizacaoMetro = this.HorarioAtualizacaoMetro
            };
        }
    }
} 