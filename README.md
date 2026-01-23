# ChitterChatter
ChitterChatter is the approach to a more leaned back bullethellgame and 50% less migraine-inducing effects.

[W][A][S][D] für moving
[space] for shooting normal bullets
[q] for shooting one strong bullet which mackes 50 damage - costing one scorepoint. use it sparsely.

_Story
Everyone has questions. And you have the answers!

_Instructions for starting
If you're hosting, click on "Start as host", if you're joining on "Join a Server" and type in the desired Ip-Adress.
After the start, make sure to click into the window first before moving.

__
_Used SyncVars
- string Player1Name
- string Player2Name
- string Player1State
- string Player2State
- float Player1Health
- float Player2Health
- int Player1Score
- int Player2Score
- bool isReady
- float SpawnerHealth
- Color HealthPointColor

_Bullet Logic
Every BulletSpawner, Enemy and Player, has the available bullets to them referenced.
In the method where they goet fired, they get instantiated with the type of bullet and position.
They also get the designated speed, bulletLife and rotation.
Before the bullet get spawned, it also gets a reference to the Player it was shot from (if they're shot from a player).
If they're hitting on an Enemy or Player, depending on their settings, they get destroyed & despawned after damage was calculated.
If they're not despawned before exceeding their lifetime, they do that.

_Spawner Logic
After spawning, they immediatly start firing their designated bullets, either in a straight line or a spiral pattern, depending
on which SpawnerType was spawned.
As sad in [Bullet Logic], they have a reference to the bullets they can use.
In their right bottom corner is their HealthPoint. It's green when over 2/3 of their maxHealth, yellow if under and red if they're
in the critical zone of 1/3.
If they're at 1/3 of their maxHealth, they will spawn more dangerous (more damage) bullets.

_Persistence
There is no persistence between sessions.

_Bonus Features
- ready-system
- spiral-bullet-patterns

_Bugs
- Due to my health atm, this is more of a toolkit to build levels than a game or prototype of a level.
- At this point in time, there are still a lot of bugs and things that previously worked are... not working now.
- BulletSpawner.rotation on client not synced (none on client)
- Player.strongAttack only works on Server
- if you're under 0 health & didn't die, you are immortal now
- Server.Player sometimes locked (clicking into the window before moving seems to work tho)
- Player.ChangeSpriteOnDamage not synced (none on client)
- ChangeSpriteOnDeath not synced (none on client)
- Restart is wonky and not working correctly
