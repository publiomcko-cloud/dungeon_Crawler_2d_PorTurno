
Conceito Geral do Jogo
Gênero: RPG 2D tático em grid (movimento por células), isométrico
Plataforma: PC
Progressão: Dungeon crawler linear
IDE: Unity
Foco: Build por itens (sem classes)
Ideia-chave:
O jogador atravessa 4 dungeons fechadas, enfrentando inimigos em áreas pequenas e controladas, evoluindo personagens através de níveis, status e equipamentos, até enfrentar o boss final.

 Estrutura de Mapas
Quantidade
4 mapas fechados (dungeons)
Cada mapa representa um capítulo do jogo
Cada mapa é feito usando prefabs para entidades, e interativos, sobre um fundo de tiles. passar para outro mapa carrega outra scene.
Estrutura de cada mapa
Cada dungeon possui:
Áreas pequenas conectadas (salas)
Inimigos posicionados estrategicamente
1 baú principal com item garantido
Caminho simples até o boss da dungeon
Progressão de mapas
Mapa
Dificuldade
Tier de itens predominante
Dungeon 1
Fácil
Comum
Dungeon 2
Média
Comum / Raro
Dungeon 3
Difícil
Raro / Épico
Dungeon 4
Final
Épico / Lendário


Movimento & Exploração
Movimento por setas do teclado, ou por duplo click no grid para movimento múltiplo
Cada pressionar = 1 célula
Grid fixo (estilo:
Crypt of the NecroDancer
Final Fantasy Tactics simplificado
Roguelikes clássicos)
Regras básicas:
Não atravessa paredes
Inimigos ocupam células
Interações acontecem ao entrar na célula:
Combate
Baú
Porta / saída

 Personagens
Quantidade
4 personagens jogáveis
Todos começam iguais em status base
Importante
Não existem classes
 Tudo é definido pelos itens equipados
Isso deixa o jogo:
Simples de balancear
Fácil de expandir
Muito focado em build

Sistema de Status
Status base
Cada personagem possui:
HP – Vida
ATK – Dano
DEF – Redução de dano
AP – Pontos de ação
CRIT – Chance de crítico (opcional, mas recomendado)

 Sistema de Níveis
Como funciona
Derrotar inimigos → ganha XP
Ao subir de nível:
Recebe Pontos de Status
Distribuição
Cada nível concede X pontos
Jogador escolhe onde investir
Exemplo:
+2 ATK
+1 DEF
ou
+3 HP

Isso reforça a personalização mesmo sem classes.

 Sistema de Itens
Tiers de raridade (fixos)
Comum
Raro
Épico
Lendário

Tipos de itens
Arma
Armadura
Acessório (anel, amuleto, etc.)

O que os itens fazem
Itens podem:
Aumentar status
Modificar comportamento
Exemplos:
Espada Épica: +ATK +  5% chance de sangramento
Anel Lendário: +AP + 2% matar instantaneamente
Armadura Rara: +DEF + 10% redução de dano mágico

Drops
Inimigos
Drop aleatório
Chance baseada no mapa
Baús
4 itens garantidos
Raridade definida pelo nível da dungeon
Exemplo:
Dungeon 2 → baú sempre Raro
Dungeon 4 → baú sempre Lendário

 Combate (Simples e Direto)
Combate em grid, acionado ao entrar no grid (action)
Entrar no grid abre uma ui de combate
Turnos alternados
A ordem pode usar:
AP- Action Points

Ações básicas:
Atacar
Esperar
(opcional depois) Usar item

 Filosofia de Design (importante)
 Não é complexo
 Não é narrativo
 É sistêmico
 É sobre decisões simples com impacto
O diferencial NÃO é história, é:
Progressão clara
Builds por item
Ritmo rápido
 Escopo Ideal para Protótipo

Apenas 1 dungeon
2 personagens
2 tiers de item (Comum / Raro)
Status básicos
Combate simples
Importante, desenvolver desde o começo pensando em escala, em blocos de funcionalidades, por exemplo usando prefabs com scripts que permitem customizar objetos do mundo, usando metadata para itens pensando em status aleatórios, usando matrizes para controlar as entidades do grid.


Unity 2D
C#
Grid baseado em Tilemap
Escopo de projeto - protótipo 



<Codex>
Recomendacoes complementares para este tipo de projeto

Sistemas que combinam muito bem com o core atual:
- intencao do inimigo, mostrando alvo e risco do proximo turno
- highlights no grid para movimento, alcance, alvo e interacao
- feedback visual e sonoro mais forte para dano, critico, morte e recompensa
- afixos simples de item, com poucas propriedades extras e leitura facil
- efeitos de status curtos e claros, como veneno, sangramento, stun leve e buff
- inimigos elite com 1 modificador forte
- eventos de sala, como altar, shrine, fonte, armadilha e escolhas de risco/recompensa
- meta progressao leve, desbloqueando pool de itens, eventos ou NPCs
- codex interno para itens, inimigos e efeitos descobertos
- seeds para reproducao de runs e testes

Qualidade de vida recomendada:
- salvar e carregar
- log de combate
- tooltips com explicacao de palavras-chave
- comparacao de itens em todas as telas relevantes
- filtros e ordenacao no inventario
- opcao de acelerar animacoes
- remapeamento de teclas
- tutorial contextual curto

Direcao de design recomendada:
- manter poucos atributos, mas fazer cada um importar
- preferir escolhas pequenas e frequentes a sistemas grandes
- fazer cada dungeon introduzir uma novidade clara
- fazer os tiers de item mudarem decisoes, e nao apenas numeros
- priorizar sinergias entre itens acima de bonus brutos

Ordem sugerida de producao:
1. fechar bau, boss e dungeon 1
2. adicionar intencao inimiga e highlights no grid
3. criar eventos de sala
4. refinar pool de itens com sinergias curtas e legiveis
5. expandir para as demais dungeons
<Codex>
