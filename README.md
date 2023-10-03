# BOT de escritório do MestreRuan

BOT para atendimento automatizado de equipes de campo. Ele extraí as informações do sistema SAP, e envia para a equipe em segundos.

O bot tem proteção contra uso indevido com uso **autorizações**. Todas as solicitações enviadas para ele serão rejeitadas até que o chat seja liberado pelo administrador, que deve ter seu identificador definido na propriedade `id_adm_bot` do token `BOT_LICENCE`.

> Necessário ter um BOT do Telegram. Ele deve ser criado no próprio Telegram através do [BotFather](https://t.me/BotFather);

## Gerando um token JWT com as propriedades

O token JWT, é armazenado na variável de ambiente `BOT_LICENCE`, usado para agrupar as mudanças no comportamento do chatbot e proteger contra uso não autorizado.

A sua estrutura está descrita no arquivo [jwt_scheme.json](./jwt_scheme.json), e ele pode ser montado no site [JWT.io](https://jwt.io/).

As propriedades são:

* **adm_id_bot (int64):** indentificador do Telegram da pessoa que ficará responsável pelo chatbot. Essa pessoa terá acesso sem restrições as funcionalidades do chatbot e poderá autorizar qualquer outro usuário;
* **allowed_pc (string):** o nome da máquina onde será instalado o chatbot. Propriedade definida para proteger contra uso indevido em outras máquinas. O chatbot só roda em uma máquina por vez, sendo necessária sua migração (copiar seu banco de dados) para outra máquina para que ela continue de onde a outra estava; 
* **exp (timedelta):** tempo de expiração do token, usada para limitar o tempo de vida da aplicação, pois após essa data o programa ficará inacessível, obrigando o administrador a atualizar a sua versão e seu token de acordo com o tempo de expiração.
* **sap_access (bool):** usada para mudar o comportamento do programa de automação do SAP FrontEnd, para limitação de acesso a transação "ZARC140", fazendo com que ele use o FPL9 no seu lugar (mais lento e menos informação);

É necessário definir uma senha para o token, pois a mesma é validada.

## Configurando o ambiente de execução:

Necessário definir as variáveis de ambiente BOT_TOKEN e BOT_LICENCE com os comandos abaixo:

```sh
set BOT_TOKEN=seu_token_do_telegram_bot_aqui
set BOT_LICENCE=seu_token_JWT_com_as_definições
```

## Configurando o ambiente de desenvolvimento:

> É necessário o DotNet SDK 6.0

Defina as variáveis de ambiente:
  * BOT_TOKEN com o tokem do bot fornecido pelo [BotFather](https://t.me/BotFather);
  * BOT_LICENCE com o token JWT com as propriedades descritas acima e a senha de segurança;
  * DOTNET_ENVIRONMENT para `Development`, assim o programa identificará automáticamente que está em ambiente de desenvolvimento;

```sh
set BOT_TOKEN=seu_token_do_telegram_bot_aqui
set BOT_LICENCE=seu_token_JWT_com_as_definições
set DOTNET_ENVIRONMENT=Development
```

Ou:

3. Crie um arquivo `.env` ou renomeie o arquivo `.env.dev` para `.env` e insira o tokem do BOT do Telegram fornecido pelo [BotFather](https://t.me/BotFather).

> O formato do arquivo `.env` abaixo:

```
BOT_TOKEN=seu_token_do_telegram_bot_aqui
BOT_LICENCE=seu_token_JWT_com_as_definições
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
