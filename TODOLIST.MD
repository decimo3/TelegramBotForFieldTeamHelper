# Transformação do chatbot síncrono em assíncrono

## Fluxograma de operações do chatbot:

1. Recebimento das solicitações:
    1. Verificar a autorização do usuário;
    2. Checar a solicitação;
    3. Gravar a solicitação no banco de dados;
2. Processamento das solicitações:
    1. Coletar as solicitações do banco de dados;
    2. Marcar a solicitação como a instrância do `trabalhador (worker)`;
    2. Executar o programa correspondente;
    3. Recolher a respostas e enviar pelo Telegram;
3. Funções do `trabalhador (worker)`:
    1. Centralizar e organizar as solicitações;
    2. Checar a saúde dos subsistemas e expirações;

## Modificações necessárias para conversão

`TODO` - Adicionar as coluna 'instancia' na tabela 'logsmodel' do banco de dados;  
`TODO` - Mover verificações de autorização, validações e gravação para um método próprio _(HandleAsynchronous.Soiree)_;  
`TODO` - Mover coleta das solicitações e execução da solicitação dos métodos correspondentes para um método próprio _(HandleAsynchronous.Cooker)_;  
`TODO` - Mover coleta e entrega das respostas para um método próprio _(HandleAsynchronous.Waiter)_;

## Particularidades do Telegram a considerar

`TODO` - Adicionar método para tratar diferentes tipos de mensagens, como 'photo', 'video', 'document' e 'location' antes de tratar as solicitações do tipo 'text';

```csharp
// Tipos de update
public enum UpdateType
{
Unknown, Message, InlineQuery, ChosenInlineResult, CallbackQuery, EditedMessage, ChannelPost, Poll, EditedChannelPost, ShippingQuery, PreCheckoutQuery, ChatMember, PollAnswer, MyChatMember, ChatJoinRequest
}
// Tipos de UpdateType.Message
public enum MessageType
{
    Text,
    Photo,
    Video,
    Contact,
    Document,
    Location,
    Voice, // Answer "Eu não consigo te ouvir, poderia escrever para mim?"
    Sticker, // Answer "Eu não consigo enxergar, poderia escrever para mim?"
}
```
