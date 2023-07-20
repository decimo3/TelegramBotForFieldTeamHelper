# BOT de escritório do MestreRuan

BOT para atendimento automatizado de equipes de campo. Ele extraí as informações do sistema SAP, e envia para a equipe em segundos.

O bot tem proteção contra uso indevido com uso **autorizações**. Todas as solicitações enviadas para ele serão rejeitadas até que o chat seja liberado pelo administrador, que deve ter seu identificador definido na variável de ambiente `ID_ADM_BOT`.

### Configurando o ambiente de desenvolvimento

> É necessário o DotNet SDK 6.0

1. Necessário ter um BOT do Telegram. Ele deve ser criado no próprio Telegram através do [BotFather](https://t.me/BotFather);
2. Defina 
2. Crie um arquivo `.env` ou renomeie o arquivo `.env.dev` para `.env` e insira o tokem do BOT do Telegram fornecido pelo [BotFather](https://t.me/BotFather).
> O formato do arquivo abaixo:
```
BOT_TOKEN=seu_token_do_telegram_bot_aqui
ID_ADM_BOT=seu_id_do_telegram_aqui
```

### Build

BOT programado para interagir com o programa de automação do SAP FrontEnd [SapAutomationForCoreBaixada](https://github.com/decimo3/SapAutomationForCoreBaixada), portanto esse BOT depende dele.

> As instruções de build e instalação estão lá, as instruções abaixo são específicas desse projeto.

Para construir esse projeto separadamente, utilize o comando abaixo (necessário DotNet Runtime para executar):

```sh
dotnet publish -c Release
```

Ou para construir uma aplicação, que possa ser executada sem a necessidade do DotNet Runtime, utilize o comando abaixo:

```sh
dotnet publish --runtime win-x64 -p:PublishSingleFile=true --self-contained true
```

### Configurando o ambiente de execução

Necessário definir as variáveis de ambiente BOT_TOKEN e ID_ADM_BOT com os comandos abaixo:

```sh
set BOT_TOKEN=seu_token_do_telegram_bot_aqui
set ID_ADM_BOT=seu_id_do_telegram_aqui
```