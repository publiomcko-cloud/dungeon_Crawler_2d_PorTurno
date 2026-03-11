Documento de Continuidade do Projeto
Dungeon Crawler 2D Turn-Based (Unity)
Objetivo do projeto

Criar um roguelike / dungeon crawler 2D baseado em grid com sistema de turnos.

Características planejadas:

Movimento em grid quadrado

Sistema turn-based

Player move → inimigos movem

Combate corpo a corpo

Geração procedural de dungeon

Loop roguelike simples

O projeto está sendo desenvolvido em Unity Core 2D (projeto limpo).

Estado atual do projeto

O jogo já possui um protótipo funcional com movimento e turnos.

Atualmente o sistema permite:

Player

Movimento por setas do teclado

Movimento em grid

Movimento suavizado (lerp)

Animação de movimento

Bounce visual ao parar

Inimigos

Seguem o player

Movimento em grid

Turnos controlados pelo TurnManager

Grid

Sistema centralizado de ocupação

Registro de entidades

Controle de colisão entre entidades

Turnos

Fluxo atual:

Player Move
↓
EndPlayerTurn()
↓
EnemyTurn()
↓
PlayerTurn
Arquitetura atual do código

O projeto usa arquitetura simples baseada em entidades.

Classe central
Entity

Responsável por:

posição no grid

HP

ataque

defesa

movimentação

dano

morte

Funções principais:

MoveTo()
TakeDamage()
Die()

Registro no grid:

GridManager.Instance.RegisterEntity()

fullcode

Sistema de Grid

Classe:

GridManager

Responsabilidades:

armazenar entidades no grid

verificar ocupação

mover entidades

remover entidades

Estrutura usada:

Dictionary<Vector2Int, Entity>

Funções principais:

IsCellOccupied()
GetEntityAt()
RegisterEntity()
MoveEntity()
RemoveEntity()

fullcode

Sistema de Turnos

Classe:

TurnManager

Estados:

PlayerTurn
EnemyTurn

Fluxo:

PlayerMove
↓
EndPlayerTurn()
↓
EnemyTurn coroutine
↓
Enemies move
↓
PlayerTurn

Inimigos são registrados automaticamente no início:

FindObjectsOfType<EnemyAI>()

fullcode

Sistema de Movimento do Player

Classe:

PlayerGridMovement

Funções:

ler input

validar movimento

iniciar animação

mover entidade

finalizar turno

Fluxo:

Input
↓
TryMove()
↓
Entity.MoveTo()
↓
AnimateMovement()
↓
EndPlayerTurn()

Inclui:

animação

sprite flip

bounce effect

fullcode

IA do Inimigo

Classe:

EnemyAI

Lógica atual:

Calcula direção para o player

Escolhe eixo dominante

Move uma célula

Evita célula ocupada

Não entra na célula do player

Vector2Int direction =
player.gridPosition - entity.gridPosition;

fullcode

Ferramenta de Debug

Classe:

GridDebug

Função:

Desenhar gizmos do grid na cena.

Isso facilita visualizar:

centros das células

alinhamento do player

alinhamento dos inimigos

fullcode

Configurações importantes na Unity
Player

Componentes:

Player
 ├ Entity
 ├ PlayerGridMovement
 ├ Animator
 └ SpriteRenderer

Tag obrigatória:

Player
Enemy
Enemy
 ├ Entity
 └ EnemyAI
Managers

Na cena devem existir:

GridManager
TurnManager

Cada um como GameObject vazio com seus scripts.

Sistema de coordenadas

Células do grid usam:

Vector2Int

Posição no mundo:

x + 0.5
y + 0.5

Isso garante que sprites fiquem no centro da célula.

O que já foi resolvido durante o desenvolvimento

Problemas que já foram corrigidos:

player entre células

inimigos ocupando mesma célula

animação durante movimento

movimento suave

turnos funcionando

grid debug

entidade centralizando no grid

Próximos passos do MVP

Ordem recomendada para continuar o desenvolvimento.

1️⃣ Sistema de combate

Quando player tenta entrar na célula do inimigo:

Player Attack
Enemy TakeDamage
Enemy Die

Implementar:

GridManager.GetEntityAt()
Entity.TakeDamage()
2️⃣ Animação de ataque

Adicionar no player:

Attack animation
Hit effect
3️⃣ Feedback visual de dano

Adicionar:

Flash sprite
Damage popup
Knockback opcional
4️⃣ Spawn de inimigos

Sistema simples:

SpawnEnemy(gridPosition)
5️⃣ Dungeon procedural

Gerar:

rooms
corridors
spawn points
6️⃣ Loop roguelike

Adicionar:

escada
próximo andar
reset dungeon
Melhorias estruturais futuras

Recomendadas após MVP.

Grid System mais robusto

Criar:

GridPosition struct
GridObject
OccupancyMap
Sistema de estados de entidade

Adicionar:

Idle
Moving
Attacking
Dead
Sistema de eventos

Para desacoplar:

OnTurnStart
OnTurnEnd
OnEntityMoved
OnEntityDamaged
Estado atual do MVP

✔ Grid funcionando
✔ Player movimento
✔ Enemy AI básica
✔ Turn system
✔ Animações básicas
✔ Grid debug

Falta para MVP jogável:

Combat
Damage feedback
Enemy death
Dungeon generation