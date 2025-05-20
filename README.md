# Monitor de Status do Metrô

Um aplicativo para monitorar o status das linhas de metrô em São Paulo e notificar o usuário quando há alterações, disponível em duas versões:

1. **MetroStatusMonitor.WinForms**: Aplicativo Windows Forms que roda na bandeja do sistema
2. **MetroStatusMonitor.ConsoleApp**: Aplicativo de console para uso em ambientes sem interface gráfica

## Funcionalidades

- Monitoramento contínuo do status das linhas do metrô
- Notificações na área de sistema quando há alterações
- Interface simples mostrando o status atual
- Configurações personalizáveis (intervalo de verificação, linhas a monitorar, etc.)
- Opção para testar notificações

## Bibliotecas e Tecnologias Utilizadas

- **C#/.NET**: Linguagem e framework de desenvolvimento
- **Windows Forms**: Para a interface gráfica da versão desktop
- **HtmlAgilityPack**: Para análise e extração de dados de páginas web
- **System.Text.Json**: Para manipulação de arquivos de configuração JSON

## Requisitos

- Windows 10 ou superior (para a versão Windows Forms)
- Qualquer sistema operacional compatível com .NET para a versão Console
- .NET 6.0 ou superior

## Como usar

1. Execute o aplicativo
2. O ícone será mostrado na área de notificação (system tray)
3. Clique com o botão direito no ícone para acessar o menu
4. Escolha "Mostrar Status" para ver o status atual das linhas
5. Configure o aplicativo conforme necessário

## Estrutura do Projeto

A solução está organizada em três projetos:

- **MetroStatusMonitor.Core**: Biblioteca compartilhada com a lógica principal
- **MetroStatusMonitor.WinForms**: Aplicativo Windows Forms
- **MetroStatusMonitor.ConsoleApp**: Aplicativo de console

## Versão Windows Forms

### Características

- Interface gráfica amigável
- Roda na bandeja do sistema (system tray)
- Notificações visuais de alterações no status
- Visualização em cores do status das linhas

### Uso

```
MetroStatusMonitor.WinForms.exe [opções]

Opções:
  --debug, -d               Ativar modo de depuração
  --persistir               Persistir histórico em JSON
```

## Versão Console

### Características

- Interface de texto colorida 
- Ideal para servidores ou ambientes sem interface gráfica
- Comandos via teclado para atualizar status ou sair

### Uso

```
MetroStatusMonitor.ConsoleApp.exe [opções]

Opções:
  --debug, -d               Ativar modo de depuração
  --persistir               Persistir histórico em JSON
  --nocolor                 Desativar saída colorida
```

Comandos durante a execução:
- `R`: Atualizar o status das linhas manualmente
- `Q`: Sair do aplicativo

## Configurações

O arquivo `config.json` contém as configurações do aplicativo:

- `IntervaloVerificacao`: Intervalo em segundos entre as verificações (padrão: 60)
- `IniciarComWindows`: Iniciar aplicativo com o Windows
- `NotificarApenasAlteracoes`: Notificar apenas quando houver alterações
- `MinimizarAoIniciar`: Iniciar minimizado na bandeja do sistema
- `ModoDebug`: Modo de depuração com logs detalhados
- `PersistirEmJson`: Persistir histórico de status em arquivo JSON
- `MonitorarLinhaX`: Controle sobre quais linhas monitorar 