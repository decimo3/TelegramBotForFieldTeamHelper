# BOT de escritório do MestreRuan

BOT para atendimento automatizado de equipes de campo. Ele extraí as informações do sistema SAP, e envia para a equipe em segundos.

O bot tem proteção contra uso indevido com uso **autorizações**. Todas as solicitações enviadas para ele serão rejeitadas até que o chat seja liberado pelo administrador, que deve ter seu identificador definido na variável de ambiente `ID_ADM_BOT`.

> Necessário ter um BOT do Telegram. Ele deve ser criado no próprio Telegram através do [BotFather](https://t.me/BotFather);

## Configurando o ambiente de execução:

Necessário definir as variáveis de ambiente BOT_TOKEN e ID_ADM_BOT com os comandos abaixo:

```sh
set BOT_TOKEN=seu_token_do_telegram_bot_aqui
set ID_ADM_BOT=seu_id_do_telegram_aqui
```

## Configurando o ambiente de desenvolvimento:

> É necessário o DotNet SDK 6.0

Defina as variáveis de ambiente:
  * BOT_TOKEN com o tokem do bot fornecido pelo [BotFather](https://t.me/BotFather);
  * ID_ADM_BOT com o identificador (id) da conta do Telegram proprietária do BOT;
  * DOTNET_ENVIRONMENT para `Development`, assim o programa identificará automáticamente que está em ambiente de desenvolvimento;

```sh
set BOT_TOKEN=seu_token_do_telegram_bot_aqui
set ID_ADM_BOT=seu_id_do_telegram_aqui
set DOTNET_ENVIRONMENT=Development
```

Ou:

3. Crie um arquivo `.env` ou renomeie o arquivo `.env.dev` para `.env` e insira o tokem do BOT do Telegram fornecido pelo [BotFather](https://t.me/BotFather).

> O formato do arquivo `.env` abaixo:

```
BOT_TOKEN=seu_token_do_telegram_bot_aqui
ID_ADM_BOT=seu_id_do_telegram_aqui
```

## O que muda do ambiente de desenvolvimento para o de execução?

1. O programa irá utilizar a segunda sessão (sessão 1) do SAP FrontEnd, para não atrapalhar as atividades do operador na primeira sessão;
2. O programa irá remover limites impostos em ambiente de execução;
3. O programa irá utilizar as variáveis de ambiente definidas no arquivo `.env` se ele existir.

## Build

> BOT foi programado para interagir com o programa de automação do SAP FrontEnd [SapAutomationForCoreBaixada](https://github.com/decimo3/SapAutomationForCoreBaixada), portanto esse BOT depende dele. As instruções de build e instalação estão lá, as instruções abaixo são específicas desse projeto.

Para construir esse projeto separadamente, utilize o comando abaixo (necessário DotNet Runtime para executar):

```sh
dotnet publish -c Release
```

Ou para construir uma aplicação, que possa ser executada sem a necessidade do DotNet Runtime, utilize o comando abaixo:

```sh
dotnet publish --runtime win-x64 -p:PublishSingleFile=true --self-contained true
```
