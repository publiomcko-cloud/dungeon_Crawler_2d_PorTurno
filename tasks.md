# Tasks do Projeto
## Dungeon Crawler 2D Turn-Based

Documento de planejamento baseado no `escopo.md` e no estado atual do projeto.

Legenda de status:
- `[feito]` implementado
- `[parcial]` implementado em parte ou precisa de refinamento
- `[pendente]` ainda nao implementado

---

# 0. Estado atual resumido

0.1 Sistemas ja existentes
- `[feito]` exploracao em grid por teclado
- `[feito]` combate em grid por grupo/celula
- `[feito]` XP, level e atributos
- `[feito]` itens, equipamentos e raridade
- `[feito]` loot no chao
- `[feito]` inventario compartilhado da party
- `[feito]` NPCs de recrutamento, quest e comercio
- `[feito]` moeda da party
- `[feito]` portais entre cenas
- `[feito]` sistema de bau com UI de transferencia
- `[feito]` boss de dungeon com recompensa e persistencia

0.2 Sistemas faltando do escopo principal
- `[pendente]` boss final
- `[pendente]` estrutura fechada das 4 dungeons
- `[pendente]` regras fechadas de recompensa por dungeon
- `[pendente]` uso de item em combate
- `[pendente]` sistema de animacao de personagem e arma

---

# 1. Implementar sistema de animacao de personagem e arma

1.1 Objetivo
- adicionar animacoes visuais legiveis para player e inimigos, separando corpo e arma equipada

1.2 Dependencias
- equipamentos funcionando
- identificacao do item equipado
- combate e exploracao ja integrados
- estrutura visual do personagem com ponto de ancoragem para arma

1.3 Tarefas principais
1.3.1 definir arquitetura visual
- separar corpo e arma em hierarquia clara
- criar `WeaponAnchor` e, quando necessario, `WeaponPivot`
- permitir trocar o visual da arma equipada sem trocar o personagem inteiro

1.3.2 definir controlador de animacao
- criar `CharacterAnimationController`
- reagir a `idle`, `walk`, `attack`, `hurt` e `death`
- manter o corpo desacoplado da animacao especifica da arma

1.3.3 definir animacao por tipo de arma
- espada: semigiro com pivô fixo
- arco: recuo curto para tras
- cajado: leve elevacao
- preparar extensao futura para novos tipos

1.3.4 definir perfis configuraveis
- criar `WeaponAnimationProfile`
- configurar offsets, rotacoes, duracao e curva
- permitir override por item especifico

1.3.5 integrar com o equipamento atual
- detectar arma equipada no `EquipmentSlots`
- trocar visual e perfil da arma automaticamente
- garantir fallback quando nao houver arma valida

1.3.6 integrar com o fluxo de acao
- tocar animacao ao atacar
- tocar animacao ao mover
- tocar animacao ao receber dano e morrer
- manter compatibilidade com combate e exploracao

1.4 Tarefas intermediarias recomendadas
1.4.1 criar `WeaponAnimationType`
1.4.2 criar `WeaponAnimationProfile`
1.4.3 criar `WeaponAnimationDriver`
1.4.4 criar `CharacterAnimationController`
1.4.5 ligar eventos de ataque com frame correto

1.5 Criterio de conclusao
- player e inimigos possuem `idle` e `walk`
- espada gira corretamente
- arco recua corretamente
- cajado ergue corretamente
- a animacao respeita a arma equipada

---

# 2. Implementar uso de item em combate

2.1 Objetivo
- permitir consumir item ou acionar item de suporte durante batalha

2.2 Dependencias
- inventario compartilhado
- UI de combate
- sistema de turno

2.3 Tarefas principais
2.3.1 definir categoria de item usavel
- cura
- buff
- revival opcional

2.3.2 criar fluxo de uso
- abrir lista de usaveis
- escolher alvo
- consumir item

2.3.3 integrar com turno
- custo de acao
- validar se pode usar naquele turno

2.4 Tarefas intermediarias recomendadas
2.4.1 extender `ItemData`
2.4.2 criar metodo de uso em combate
2.4.3 integrar com UI de combate

2.5 Criterio de conclusao
- item de cura pode ser usado em combate
- efeito aplica no alvo correto
- item sai da mochila compartilhada

---

# 3. Fechar sistema de dungeons

3.1 Objetivo
- transformar o prototipo sistemico em progressao jogavel por capitulos

3.2 Dependencias
- portais entre cenas
- bau
- boss
- tabela de drops

3.3 Tarefas principais
3.3.1 definir Dungeon 1 como vertical slice completa
- inicio
- salas
- inimigos
- 1 bau principal
- 1 boss
- saida

3.3.2 definir progressao por mapa
- dungeon 1 facil
- dungeon 2 media
- dungeon 3 dificil
- dungeon 4 final

3.3.3 definir tiers predominantes por dungeon
- comum
- comum/raro
- raro/epico
- epico/lendario

3.3.4 definir recompensas garantidas e drops
- bau por dungeon
- drop pool por dungeon
- recompensas de boss

3.3.5 implementar boss final
- encontro final separado ou ultima dungeon
- recompensa e fechamento do jogo
- persistencia e progressao final

3.4 Tarefas intermediarias recomendadas
3.4.1 criar documento de tabela de conteudo
3.4.2 criar prefab set por dungeon
3.4.3 validar pacing e dificuldade

3.5 Criterio de conclusao
- existe ao menos 1 dungeon totalmente fechada e jogavel
- a estrutura para replicar as demais 3 esta pronta
- boss final tem fluxo claro de vitoria

---

# 4. Polimento da exploracao e combate

4.1 Objetivo
- melhorar leitura e satisfacao do loop principal

4.2 Tarefas principais
4.2.1 intencao inimiga
- mostrar quem o inimigo vai atacar
- indicar risco no proximo turno

4.2.2 highlights no grid
- movimento
- alvo
- interacao
- perigo

4.2.3 feedback audiovisual
- dano
- critico
- morte
- coleta
- abertura de bau

4.2.4 log de combate
- dano causado
- critico
- morte
- ganho de XP

4.3 Criterio de conclusao
- combate fica mais legivel sem aumentar complexidade

---

# 5. Refinar itens e builds

5.1 Objetivo
- reforcar o foco do jogo em build por item

5.2 Tarefas principais
5.2.1 criar afixos simples
- bonus de stat
- efeito curto
- sinergia facil de entender

5.2.2 ampliar diversidade util
- armas
- armaduras
- acessorios

5.2.3 revisar tiers
- comum
- raro
- epico
- lendario

5.2.4 criar sinergias curtas
- exemplo: veneno + acerto extra
- exemplo: AP + critico

5.3 Criterio de conclusao
- itens mudam decisao do jogador, nao apenas numero

---

# 6. Refinar NPCs e economia

6.1 Objetivo
- consolidar NPCs como loop secundario importante

6.2 Tarefas principais
6.2.1 polir UI dos NPCs
- consistencia visual
- revisar estados de compra/venda/quest/recruit

6.2.2 ampliar quests
- mais objetivos de exterminio
- recompensas melhores por progressao

6.2.3 ampliar comercio
- estoque por dungeon
- itens melhores em mapas avancados

6.2.4 revisar recrutamento
- custo por progressao
- NPCs recrutaveis especiais

6.3 Criterio de conclusao
- NPCs passam a enriquecer progressao e build, nao apenas servir de teste de sistema

---

# 7. Qualidade de vida

7.1 Tarefas principais
7.1.1 salvar e carregar
7.1.2 filtros e ordenacao no inventario
7.1.3 remapeamento de teclas
7.1.4 tutorial contextual curto
7.1.5 codex de inimigos e itens

7.2 Criterio de conclusao
- jogar, testar e iterar fica mais rapido e confiavel

---

# 8. Ordem sugerida de execucao

8.1 Fase 1
- implementar sistema de animacao de personagem e arma
- fechar dungeon 1

8.2 Fase 2
- implementar uso de item em combate
- implementar highlights no grid
- implementar intencao inimiga

8.3 Fase 3
- refinar itens e sinergias
- refinar loot por dungeon
- avancar dungeons 2 e 3

8.4 Fase 4
- polir NPCs, economia e quests
- adicionar eventos de sala
- revisar progressao geral

8.5 Fase 5
- salvar/carregar
- codex
- qol final
- dungeon 4 e boss final

---

# 9. Proxima entrega recomendada

9.1 Proxima implementacao ideal
- sistema de animacao de personagem e arma

9.2 Motivo
- melhora muito a leitura de combate e exploracao
- conversa diretamente com o sistema de equipamentos
- prepara bem o jogo para polimento audiovisual

9.3 Considerar concluido quando
- houver animacao base de corpo
- houver animacao distinta para espada, arco e cajado
- a arma equipada alterar corretamente a apresentacao visual
