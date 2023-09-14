# Instruções de operação do chatbot

O chatbot após o `build`, criará uma pasta chamada `MestreRuan` onde estará os arquivos necessários para seu funcionamento: 

```
%USERPROFILE%/Mestre Ruan/
|
├── tmp/
├── telbot.exe
├── sap.exe
├── img.exe
├── fileDialog.vbs
├── database.db
├── sni.dll
├── SQLite.Interop.dll
└── telbot.pdb
```

O arquivo `telbot.exe` pode ser executado diretamente pelo explorer, com dois cliques, usando as configurações padrões. Para alterar o comportamento do programa conforme a necessidade é necessário executar o programa **via linha de comando**.

## Argumentos via linha de comando

O chatbot contém definições determinadas pelas `variáveis de ambiente`, porém para comportamento em tempo de execução são usados os argumentos fornecidos via linha de comando.

#### --em-desenvolvimento

Esse argumento fará o chatbot carregar as definições das variáveis de ambiente através do arquivo `.env`, ignorando as variáveis do sistema.

> Esse argumento só deve ser utilizado em ambiente de desenvolvimento! Não utilizar se não souber exaamente o que ele faz!

#### --sap-instancia

Esse argumento é utilizado para definir uma instancia SAP diferente da padrão (0), e deve ser usada no formato de atribuição (--sap-instancia=[número da instancia desejada]).

> Utilizar ele quando a instancia padrão do SAP não estiver disponível. Os números começam do **zero**: a primeira (1) janela do SAP é a instância zero (0), a segunda janela (2) é a instância um (1), e etc..

#### --sap-offline

Esse argumento fará o chatbot ignorar todas as solicitações recebidas, enviando uma mensagem notificando que o sistema SAP está indisponível.

> Usado quando houver problema para acessar o sistema Light, e mesmo que não tenha internet, para que as equipes estejam cientes do acontecido.

#### --sem-faturas

Esse argumento fará o chatbot  ignorar todas as solicitações de faturas, enviando uma mensagem notificando que o sistema SAP não está gerando faturas.

> Usado quando houver problema para o sistema da Light gerar faturas, para que as equipes estejam cientes.