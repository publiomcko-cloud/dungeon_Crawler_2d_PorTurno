Documento de Continuidade do Projeto
Dungeon Crawler 2D Turn-Based (Unity)

Este documento descreve o estado atual do projeto, arquitetura do código e próximos passos.
Ele serve como checkpoint de desenvolvimento para continuar o projeto em novas conversas.

------------------------------------------------

VISÃO GERAL DO PROJETO

Objetivo:

Criar um Dungeon Crawler / Roguelike 2D baseado em grid com sistema de turnos.

Características principais do design:

• Movimento em grid quadrado
• Sistema turn-based
• Player move → inimigos movem
• Combate corpo a corpo
• Dungeon procedural (planejado)
• Loop roguelike simples (planejado)

O projeto está sendo desenvolvido em Unity usando um projeto Core 2D limpo.

------------------------------------------------

ESTADO ATUAL DO PROJETO

O projeto possui um protótipo funcional com:

✔ grid system
✔ player movement
✔ enemy movement
✔ turn system
✔ animação de movimento
✔ bounce ao parar
✔ debug visual do grid

Ainda NÃO possui:

✘ combate implementado
✘ dano
✘ morte de entidades
✘ habilidades
✘ geração procedural
✘ loop roguelike

------------------------------------------------

ARQUITETURA DO CÓDIGO

A arquitetura é simples e baseada em entidades.

Classes principais:

Entity
PlayerGridMovement
EnemyAI
GridManager
TurnManager
GridDebug

------------------------------------------------

ENTITY

Classe central do jogo.

Responsável por:

• posição no grid
• HP
• ataque
• defesa
• movimentação
• dano
• morte

Variáveis principais:

Vector2Int gridPosition
int maxHP
int currentHP
int attack
int defense

Funções principais:

MoveTo(Vector2Int newPos)
TakeDamage(int damage)
Die()

A entidade se registra automaticamente no grid no Start():

GridManager.Instance.RegisterEntity()

------------------------------------------------

GRID SYSTEM

Classe:

GridManager

Responsabilidades:

• armazenar entidades no grid
• verificar ocupação
• mover entidades
• remover entidades

Estrutura atual:

Dictionary<Vector2Int, Entity>

Cada célula do grid contém apenas UMA entidade.

Funções principais:

IsCellOccupied(Vector2Int pos)
GetEntityAt(Vector2Int pos)
RegisterEntity(Vector2Int pos, Entity entity)
MoveEntity(Vector2Int oldPos, Vector2Int newPos, Entity entity)
RemoveEntity(Vector2Int pos)

------------------------------------------------

TURN SYSTEM

Classe:

TurnManager

Estados:

PlayerTurn
EnemyTurn

Fluxo de turno atual:

PlayerMove
↓
EndPlayerTurn()
↓
EnemyTurn coroutine
↓
Todos os inimigos executam TakeTurn()
↓
PlayerTurn

Inimigos são registrados automaticamente no Start():

FindObjectsOfType<EnemyAI>()

------------------------------------------------

PLAYER MOVEMENT

Classe:

PlayerGridMovement

Funções principais:

• ler input do teclado
• validar movimento
• iniciar animação
• mover entidade
• finalizar turno

Fluxo:

Input
↓
TryMove()
↓
Entity.MoveTo()
↓
AnimateMovement()
↓
BounceEffect()
↓
EndPlayerTurn()

Características:

• movimento suave com Lerp
• flip automático do sprite
• bounce visual ao parar

------------------------------------------------

ENEMY AI

Classe:

EnemyAI

Lógica atual:

• localiza o player
• calcula direção dominante
• tenta mover uma célula em direção ao player

Pseudo lógica:

direction = player.gridPosition - entity.gridPosition

Escolhe eixo dominante (X ou Y)

Vector2Int target = entity.gridPosition + direction

Regras atuais:

• inimigo NÃO entra na célula do player
• inimigo NÃO entra em célula ocupada

Atualmente o ataque ainda não está implementado.

Quando adjacente ao player aparece log:

"Enemy atacaria o player aqui"

------------------------------------------------

GRID DEBUG

Classe:

GridDebug

Função:

Desenhar gizmos do grid na cena.

Isso ajuda a visualizar:

• centros das células
• alinhamento do player
• alinhamento dos inimigos

------------------------------------------------

CONFIGURAÇÃO NA UNITY

PLAYER

Componentes:

Player
 ├ Entity
 ├ PlayerGridMovement
 ├ Animator
 └ SpriteRenderer

Tag obrigatória:

Player

------------------------------------------------

ENEMY

Componentes:

Enemy
 ├ Entity
 └ EnemyAI

------------------------------------------------

MANAGERS NA SCENE

Devem existir na cena:

GridManager
TurnManager

Cada um em um GameObject vazio com seu script.

------------------------------------------------

SISTEMA DE COORDENADAS

Grid usa:

Vector2Int

Conversão para mundo:

x + 0.5
y + 0.5

Isso centraliza os sprites na célula.

------------------------------------------------

PROBLEMAS JÁ RESOLVIDOS

Durante o desenvolvimento foram resolvidos:

✔ player entre células
✔ alinhamento do grid
✔ inimigos ocupando mesma célula
✔ movimento suave
✔ animação de movimento
✔ bounce visual
✔ turn system funcional
✔ debug visual do grid

------------------------------------------------

EXPERIMENTOS REALIZADOS

Foi iniciado o design de um novo sistema:

GRID MULTI-ENTIDADE

Objetivo:

Permitir até 4 entidades na mesma célula.

Isso permitiria combate em grupo.

Exemplo:

Célula Player
P1
P2
P3
P4

Célula Enemy
E1
E2

Combate planejado:

P1 ataca
P2 ataca
P3 ataca
P4 ataca

Dano dividido entre defensores.

Esse sistema ainda NÃO foi implementado completamente e está pausado.

------------------------------------------------

PRÓXIMA FEATURE DO MVP

A próxima funcionalidade recomendada é:

SISTEMA DE COMBATE

Quando player tenta entrar na célula de um inimigo:

Player Attack
Enemy TakeDamage
Enemy Die

Fluxo esperado:

TryMove()
↓
Detect enemy
↓
Attack()
↓
TakeDamage()
↓
Die()

------------------------------------------------

PRÓXIMOS PASSOS DO MVP

Ordem recomendada:

1️⃣ Combate básico

Player ataca inimigo.

2️⃣ Animação de ataque

Ataque visual.

3️⃣ Feedback de dano

• Flash
• Damage popup

4️⃣ Spawn de inimigos

Spawner simples.

5️⃣ Dungeon procedural

• rooms
• corridors
• spawn points

6️⃣ Loop roguelike

• escada
• próximo andar
• reset dungeon

------------------------------------------------

MELHORIAS ESTRUTURAIS FUTURAS

Após o MVP:

• Grid multi-entidade
• Sistema de habilidades
• Buff / debuff
• Turn order system
• Pathfinding inimigo
• Sistema de status

------------------------------------------------

VERSÃO DO PROJETO

Checkpoint gerado para continuidade em nova conversa com ChatGPT.