# Migration vers Unity 6.6

Source : Unity3D-Mario-Kart-Racing-Game, Ishaan Patel (Ishaan35).
Le README d'origine est conserve dans SOURCE-README.md. Le dossier GitHub original n'a pas ete modifie.

Cette copie conserve les cinq circuits, les assets des personnages et les objets du projet d'origine. Les courses sont configurees en solo et les touches clavier sont remappees. Notre KartController et ML-Agents ne sont pas integres a cette etape.

## Circuits jouables (configuration actuelle)

Le menu WhiteScreen propose uniquement Moo Moo Meadows et Mario Circuit. La liste des scenes de compilation est, dans cet ordre : WhiteScreen, MooMooMeadows, Mario Circuit. Le menu charge les circuits par leur nom, sans dependre d'un index numerique.

Toad Harbor, Rainbow Road et Water Park restent dans Assets/Scenes avec leurs ressources ; leurs boutons sont desactives dans le menu et leurs scenes sont retirees de la liste de compilation. Ce changement n'est pas un nettoyage des assets : les ressources partagees ou chargees depuis Resources peuvent toujours etre incluses dans le jeu.

Les outils MigrationAudit, MigrationPlayAudit et DriverPoseAudit utilisent les deux circuits actifs. Les tests Play ouvrent chaque circuit via son bouton du menu, verifient la conduite solo, puis simulent les passages dans les colliders de checkpoints pour verifier la validation d'un tour. Ils ne remplacent pas une course complete pilotee par un humain. Les outils de test reviennent au menu avant de quitter Unity.

## Ouvrir

Ouvrir ce dossier de projet avec Unity 6000.6.4f1. Dans Unity, choisir **Mario Kart > Ouvrir le menu du jeu migre**, puis Play. Le menu d'origine correspond a `Assets/Scenes/WhiteScreen.unity`.

Commandes : fleche haut / Z / W pour accelerer ; fleche bas / S pour freiner puis reculer ; fleches gauche/droite ou Q/D (A/D en QWERTY) pour tourner ; Espace pour sauter/drifter en maintenant une direction a vitesse suffisante ; Maj pour lancer un objet devant ; Ctrl pour le lancer derriere ; B pour regarder derriere. Les touches 1 et 2 changent la camera. Cliquer dans la vue Game apres Play pour lui donner le focus.

KartKeyboard centralise les touches du jeu importe : les scripts existants lui demandent leurs actions logiques. Ce remappage ne remplace pas encore la future interface de commandes normalisees du KartController pour ML-Agents.

## Adaptations

- Packages URP et Visual Effect Graph mis a jour vers 17.6.0, interfaces Unity UI vers 2.6.0.
- Ancienne API Beautify adaptee au Render Graph de Unity 6.6 ; effets et volumes conserves.
- API obsolete des mots-cles de shaders et activation du module UI corrigees.
- 24 fichiers Blender exportes en FBX avec les parametres du convertisseur Unity, leurs GUID et reglages d'import conserves. Les sources restent dans Migration/BlenderSources, hors du dossier Assets.
- Blender portable 2.79b a servi a la conversion finale, correspondant aux fichiers source BLENDER-v279. L'export binaire utilise l'axe principal des os -X et l'axe secondaire Y, avec les noms normalises (points remplaces par underscores), pour retrouver les references et axes des squelettes des scenes d'origine. Un export avec les axes par defaut deformait Mario. Blender n'est pas necessaire pour ouvrir les FBX.

Les rapports `scene-audit.txt` et `play-audit.txt` ainsi que les captures dans `Previews` documentent les controles effectivement executes. Un test de demarrage ne valide pas a lui seul tous les objets ni chaque tour de course.

- Reglage des entrees : Both (ancien et nouveau systemes), pour conserver les commandes et menus d'origine.
- Dynamic Batching desactive sur les profils URP High/Medium, car retire dans Unity 6.6.
- Shader Beautify : adaptateur de vertex/UV pour les passes CommandBuffer.Blit, evitant l'ecran magenta avec les structures URP 17.


## Courses solo

Les instances des sept adversaires et leurs icones de mini-carte sont retirees des cinq scenes. Les listes de classement et les references de cibles sont nettoyees. Le resultat n'affiche que le joueur humain. Les modeles, animations et prefabs d'origine restent disponibles pour ajouter plus tard un autre personnage, puis le controle RL. Aucun agent RL ni personnage supplementaire n'est ajoute maintenant.

Les scenes avant cette operation sont sauvegardees dans Migration/BeforeSolo (hors Assets). Le KartController du prototype n'est pas encore raccorde a ce projet.


## Assistance clavier et animations

F1 affiche ou masque les commandes dans le jeu. R replace le kart a une position recente sur la piste. Une immobilisation de plus de quatre secondes en accelerant declenche aussi cette assistance. Elle ne valide aucun checkpoint et ne change pas le nombre de tours. Les collisions des barrieres restent actives.

Le boost provient d'un champignon utilise avec Maj, d'une bande d'acceleration ou d'un drift charge relache. Il n'y a pas de touche de boost illimite. Les objets se recuperent dans les boites avec un point d'interrogation. Ctrl permet de preparer/lancer certains objets vers l'arriere suivant leur type.

Les actions Blender de Mario sont exportees individuellement et les clips Unity sont relies a leur prise correspondante, notamment Normal au repos, au lieu de reutiliser une meme prise sur toutes les animations.


Validation finale : essais Play de 35 secondes par circuit (acceleration, recul, direction, drift, consommation d'un champignon avec Maj, un seul joueur et classement solo). Le controle de retour sur piste a ete rejoue sur Mario Circuit apres synchronisation du Transform et du Rigidbody : zero erreur, progression conservee et animation Normal au repos. Les essais courts ne constituent pas une validation exhaustive de tous les objets et de tous les virages.
Rapports : play-audit-five-circuits.txt (avant la derniere correction de synchronisation), play-audit.txt (verification finale Mario Circuit), solo-polish.txt (animations et references).


Validation des deux circuits : les deux boutons du menu ont ete testes en Play, avec conduite, objets, classement solo et validation des checkpoints/tours (simulation des passages). Aucune erreur d'execution dans ces essais. Compilation Windows reussie avec zero erreur : Builds/TwoTracks/MarioKart.exe. Rapport : two-track-build.txt. Le dossier Builds est ignore par Git.

Correction du bloc invisible au tremplin de MooMooMeadows (2026-11-06) : desactivation du BoxCollider 1550386877 du Cube invisible, qui depassait de la route et du bord d'entree du tremplin. Les colliders du sol et du tremplin ainsi que le trigger BoostPanel restent actifs. Verification Unity : JumpCollisionAudit.VerifyBlockFix, 87 trajectoires auparavant bloquees liberees, 425 points de route avec support, sonde Rigidbody : 0,361 unite avant / 2,742 apres. Rapport : jump-block-regression.txt. Test physique cible en mode editeur ; pas de nouvelle compilation de l'executable Windows.

Correction du passage a gauche du tremplin, confirmee sur la video utilisateur : le premier Cube corrige ne suffisait pas. Le BoxCollider 929256789 de Cube (1), centre (-16617.34, 23241.54, -11616.49), bloquait encore les deux voies de gauche. Sa collision est maintenant desactivee. Reproduction en Play avec le vrai Player et son script anti-traversee : avant, arret a 12.82 et 15.61 unites (vitesse quasi nulle, obstacle Cube (1)); apres, progression de 70.63 et 77.55 unites dans les memes voies. Six trajectoires validees par JumpLaneAudit.Verify, dont le tremplin central avec boost actif. Rapports : jump-lane-before.txt et jump-lane-audit.txt. Le menu WhiteScreen est remis comme scene ouverte apres les tests. L'executable Windows n'a pas ete recompile.
