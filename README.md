#  Projeto de Sistemas de Redes para Jogos
## Used Git Repo: https://github.com/TomasCardoso46/R.O.S

## Author

### Tomás Ricardo Branco Cardoso a22303462



## Premissa do jogo
É um jogo de corrida onde cada jogador está encarregue de gerir a estratégia do seu piloto.
Dois jogadores competem para ganhar uma corrida de 50 voltas pelo Circuito de Catalunha/Barcelona, usado por campeonatos como F1 e MotoGP.
(O jogo pode receber até 4 jogadores para teste, mas o jogo é feito para 2, apesar de poder ser extendido com poucas mudanças)

![Catalunya](MarkdownImages/Catalunya.jpg)

A gameplay durante corridas é semelhante a jogos como F1 Manager, F1 Clash e Motorsport Manager.

Os carros começam parados na reta principal, estando o host em primeiro lugar, após ambos os jogadores estarem conectados começa a corrida, com ambos os jogadores nos pneus meédios.
Ao começarem nos mesmos pneus, os jogadores começam também com a mesma velocidade.
A velocidade vai diminuindo a cada checkpoint passado, pelo que jogadores devem trocar de pneus para obter um tempo final mais rápido que o seu adversário.
Jogadores também têm a opção de dar "Push", isto aumenta a sua velocidade nas curvas, mas aumenta também a velocidade perdida por checkpoint.

Os Jogadores não sabem se o outro vai parar nesta volta até a troca de pneus começar, pelo que estar em segundo lugar pode oferecer uma vantagem estratégica a nivel de informação obtida.

## Como jogar

Jogadores clicam num dos inputs de pneus para planear uma pit stop, esta será executada no fim da volta.
Os Inputs de pneus são:
S - Softs
M - Mediums
H - Hards

Jogadores podem também clicar no P para ativar e desativar o modo "Push".

## Como funciona a simulação
Os carros, são bolas coloridas, inspiradas pelos verdadeiros icones usados pela transmissão oficial de F1 quando o traçado da pista é mostrado.

![BroadcastBalls](MarkdownImages/BroadcastBalls.png)

Estes têm uma variavel de velocidade pura e uma de degradação dos pneus, que juntas resultam na verdadeira velocidade.
Nas curvas a velocidade pura é diminuida, este efeito é reduzido quando o jogador está no modo "Push".

Existem três pneus, Softs, Mediums e Hards.

Softs - Velocidade pura maior, Degradação maior.

![Soft](MarkdownImages/Soft.png)

Mediums - Velocidade pura normal, Degradação normal.

![Medium](MarkdownImages/Medium.png)

Hards - Velocidade pura menor, Degradação menor

![Hard](MarkdownImages/Hard.png)

A degradação é aplicada a cada checkpoint, logo, partes da pista com mais curvas, ou curvas mais longas, degradam mais os pneus.
Um jogador que está a usar os Softs, será inicialmente o mais rápido, mas se não trocar de pneus mais cedo irá ser mais lento que os restantes

Durante as trocas de pneus, os jogadores ficam parados por 5 segundos, sendo essencial coordenar as trocas, de modo a que o beneficio da troca de pneus seja superior aos 5 segundos da paragem, pelo menos até
à próxima troca.

Usar o modo "Push", aumenta a degradação, pelo que não deve estar sempre ativo, especialmente nos Softs.



## Funcionalidade Online
Processo de iniciar uma sessão:
1. O host faz login anónimo nos serviços Unity.
2. Um Allocation é criado via RelayService.CreateAllocationAsync.
3. Um código de entrada é gerado GetJoinCodeAsync.
4. O host define os dados de transporte DTLS SetRelayServerData.
5. A sessão começa com NetworkManager.StartHost().
6. Clientes entram com o código, fazendo então JoinAllocationAsync, seguido de StartClient().


O jogo usa Relay do unity, disponivel em "cloud.unity.com".

O código é gerado pelo próprio serviço de relay do unity.

O movimento dos carros é controlado por cada client no script PathFollower.cs com base em seguir checkpoints de uma lista até chegar ao fim e repetir, apesar disto o prefab do carro em si é movido no servidor.
Para outros jogadores verem as ações do meu carro basta o uso do Network Transform, pois nunca precisam de saber sobre os meus estados, pneus, pitting e pushing, precisam apenas de saber a minha posição sempre.

As posições dos carros na visão de um cliente são interpoladas (ver fun fact 3).
O host vê movimentos com pouca ou nada de interpolação por estar no seu próprio servidor.
O cliente é também forçado a ver interpolação no seu próprio carro por ainda o ter de sincronizar no Network transform.

Todas as instâncias de carros têm um Network Transform que é responsável por sincronizar apenas as posições, em X, Y e Z.
Não é necessário sincronizar as rotações, visto que os carros são apenas circulos.

Todos os inputs são feitos localmente se o carro pertencer ao cliente.
Os inputs são validados pelo servidor, mas estados do carro não têm qualquer tipo de proteção.
Na visão do dono do carro, os seus estados são alterados, mas na visão de quem não é dono do carro apenas o movimento é alterado, que já estava sincronizado pelo Network Transform
O Server sabe quando qualquer jogador quer trocar de pneus ou dar push, mas o cliente que não é dono do carro nem recebe essa informação, apenas vê os acontecimentos.

![Transform](MarkdownImages/Transform.png)

Inputs passam por um ServerRpc enquanto estado de vitória passa pelo ClientRpc.

As variáveis que diretamente impactam a visão do jogo ou velocidades, raceLap, tireLap, pitRequested, tireType, isPushing são sincronizadas apenas com o uso de NetworkVariable< T >.

Nesta versão do jogo, os textos relacionados aos estados do jogo de ambos os jogadores existem ao mesmo tempo nos dois clientes, sendo cada cliente apenas responsável por dar display ao UI do seu carro, verificando a ownership do mesmo.

Metodo SpawnCarForClient instancia o carro do novo jogador tanto para o novo jogador como para o Host, assim seria possivel hipoteticamente extender o jogo para ter mais jogadores, seria ainda necessário alterar a maneira como a UI do jogo funciona.

No script de relay, é feito um login anonimo e conexão aos serviços Unity, CreateRelay solicita uma alocação de servidor Relay, o transporte segue o protocolo DTLS, Apenas após isto começa a sessão.


Diagrama de Arquitetura de Redes:
```
              +----------------+
              |  Unity Relay   |
              |   (DTLS/UDP)   |
              +--------+-------+
                       |
           +-----------+-----------+
           |                       |
     +-----v-----+           +-----v-----+
     |   Host    |           |  Client 1 |
     |(Server+UI)|           |(Client+UI)|
     +-----+-----+           +-----+-----+
           |                       |
     +-----v-----+           +-----v-----+
     | Netcode   |           | Netcode   |
     | Transport |           | Transport |
     +-----------+           +-----------+

     +-----+-----+           +-----+-----+
     | Game Logic|           | Game Logic|
     | (Server)  |           | (Client)  |
     +-----------+           +-----------+
     | UI Local  |           | UI Local  |
     +-----------+           +-----------+

```




Diagrama de Protocolo:
```
[Host]                               [Relay Server]                               [Cliente]

   |                                       |                                           |
   |-- CreateAllocationAsync() ----------->|                                           |
   |                                       |                                           |
   |<-- Allocation Info -------------------|                                           |
   |                                       |                                           |
   |-- BIND ------------------------------>|                                           |
   |                                       |                                           |
   |<-- BIND_RECEIVED ---------------------|                                           |
   |                                       |                                           |
   |-- GetJoinCodeAsync() ---------------->|                                           |
   |                                       |                                           |
   |<-- Join Code -------------------------|                                           |
   |                                       |                                           |
   |                                       |<-- JoinAllocationAsync(joinCode) --------|
   |                                       |                                           |
   |                                       |--> Allocation Info ----------------------|
   |                                       |                                           |
   |                                       |<-- BIND ---------------------------------|
   |                                       |                                           |
   |                                       |--> BIND_RECEIVED ------------------------|
   |                                       |                                           |
   |                                       |<-- CONNECT_REQUEST ----------------------|
   |                                       |                                           |
   |                                       |--> ACCEPTED -----------------------------|
   |                                       |                                           |
   |<-- RELAY (Data) ----------------------|-------------------------> RELAY (Data) -->|
   |                                       |                                           |
```

As duas principais dificuldades que tive ao longo do desenvolvimento foram os textos de vitória e a conexão por código.

Os textos de vitória porque até ao momento, toda a informação mostrada ao cliente dependia apenas de si mesmo. Na altura tentei fazer da exata mesma maneira, mas mostrando a variavel de quem ja acabou a corrida.
Eventualmente fiz com que os textos de vitória fossem duas entidades separadas, isto no entanto apenas funcionava quando o host acabava a corrida, independentemente da sua posição de chegada.
Isto provavelmente acontecia porque as variaveis estavam a perguntar se era o server a correr o codigo em vez de garantir que era o owner do carro.
Ainda há restos deste código no script RaceManager.cs
Em retroespetiva, deveria simplesmente ter uma network variable de vencedor pertencente ao servidor, quando os carros acabassem iriam então os respetivos clientes pedir ao servidor para a alterar para serem eles os vencedores.
Deveria ainda haver um check para garantir que só o primeiro faria isto. Isto poderia ser resolvido parando ambos os carros assim que um deles termina, e aceitar o pedido sem verificação, o que funcionaria se quisesse dar commit a ter apenas 2 jogadores.
Caso queira ter mais, seria expectavel que os restantes corredores consigam ainda competir entre si pelos restantes lugares do podio, pelo que para isto poderia haver uma contagem de pedidos, sendo o primeiro pedido primeiro lugar, segundo pedido segundo lugar.... en que
apenas a primeira atualiza o vencedor.

A conexão por código porque ao seguir um dos tutoriais de relay, passei várias horas a tentar resolver uma linha que dava erro. Colei no google e não obtive resposta, no chatgpt também não, não sei se foi porque pesquisei as coisas erradas ou por a linha problemática poder estar
certa em outros contextos, mas a realidade foi que eu estava a tentar misturar código de duas versões diferentes dos serviços de relay, pelo que um comentário no video foi o que me deu a resposta.
"var relayServerData = new RelayServerData(allocation, "dtls");" precisava agora de ser "var relayServerData = AllocationUtils.ToRelayServerData(allocation, "dtls");".
Felizmente ao resolver isto o jogo ficou logo a funcionar, foi o último código feito.



## Fun Facts
1. Desligar o VPN antes de testar um jogo localmente é uma boa ideia!!!

2. Se o segundo jogador for disconectado por falha da internet (replicavel também ao ligar ou desligar VPN) o seu carro passará a ser propriedade do outro cliente, fazendo assim com que o dono da sessão tenha controlo sobre os dois carros.

3. As mudanças de velocidade do jogo são instantaneas, não passam por um periodo de aceleração ou travagem, mas ao ver os movimentos quando interpolados, muitas vezes dá essa ilusão.


## Conclusões tiradas do projeto
Testar em LAN no unity é fácil e ajuda bastante.
Tive dificuldades em expor variaveis iguais para ambos os jogadores, enquanto cada jogador vê as suas próprias informações, não consegui expor uma mensagem clara de vitória global, apenas local.
Fazer a conexão entre dois jogadores através da geração de um código é muito mais fácil e rápido do que eu esperava.
Gostei de trabalhar no jogo em si, e gostaria ainda de expandir a simulação, tal como adicionar inputs próprios para ultrapassagens, no entanto fazer o jogo funcionar online já não foi tão divertido.
Ainda assim gostaria de seguir com a componente online do jogo, repondo também as pistas que foram cortadas.
UI preicsa de imenso trabalho para ficar apelativa, juntamente com um verdadeiro menu inicial.
O jogo em si precisa de umas boas horas de testes para ficar bem balanceado, poderia também tentar obter valores das corridas reais, mas alguns valores especificos não são fáceis de obter, como é o caso da degradação.


## Webgrafia
https://youtu.be/HWPKlpeZUjM?si=X6M31Svnu8Lcc2_s - Tutorial de Netcode for GameObjects

https://youtu.be/msPNJ2cxWfw?si=SOKyX9Hld-GtDjW2 - Tutorial de Relay

https://youtu.be/fRJlb4t_TXc?si=uwFQBfekkcQb8tIs - Tutorial de Relay, a maior parte do relay vem deste video, com algumas correções devido a mudanças da API.


