# Partie 2 - Entrainement PPO

## Ce qui est prepare

- Unity ML-Agents 4.2.0 et son moteur d'inference sont declares dans Packages/manifest.json. La version 4.0.0 ne compile pas avec Unity 6000.6 (ancienne API GetInstanceID dans Match3).
- Python 3.10.11 64 bits est installe dans .tools/python310. L'environnement .venv-mlagents contient ML-Agents Python 1.1.0 et PyTorch 2.2.2. Aucun changement du Python habituel n'est necessaire.
- Training/requirements.txt donne les versions principales ; requirements-lock.txt conserve toutes les versions effectivement installees.
- KartAgent traduit les observations du jeu en commandes. Il recoit exactement les recompenses calculees par KartEpisode.
- Les copies MooMooTraining et MarioCircuitTraining sont creees avec les outils Unity. Les scenes de conduite manuelle restent disponibles.
- KartRunRecorder enregistre chaque essai, y compris les checkpoints dans l'ordre et leur instant de passage, les tours, les chocs, le gazon, le score et le temps.

## Dans Unity

Laisser Unity finir l'installation des packages a l'ouverture.
Menu **Mario Kart > Apprentissage > Preparer les scenes IA** : cree les scenes d'entrainement.
Menu **Mario Kart > Apprentissage > Compiler les essais IA sans affichage** : produit Builds/TrainingHeadless/KartTrainingHeadless.exe (Windows 64 bits), utilise par defaut par les scripts Python.
La compilation avec affichage reste disponible via **Compiler le jeu IA** et produit Builds/Training/KartTraining.exe, mais demande la preparation de tous les effets graphiques.
Ces operations ne changent pas la liste des scenes de compilation du jeu manuel.
Le programme demarre par une scene de chargement et choisit le circuit demande. Pour voir le modele livre sans Python : choisir Mario Kart > Apprentissage > Tester le modele - Moo Moo Meadows (ou Mario Circuit). Ces menus chargent Assets/Resources/Kart.onnx et lancent le pilote autonome avec le profil balanced et un objectif de tour complet. Il ne continue pas a apprendre pendant ce test.

La verification de connexion utilise le script officiel Ball3DAgent du tag release_23, sans modification. Une geometrie simple est reconstruite autour du script ; ce n'est pas la scene d'exemple originale complete. La licence Unity ML-Agents et le lien source sont conserves ici.

## Lancer depuis PowerShell, a la racine du projet

Test de connexion sur l'exemple officiel, apres compilation de l'exemple :

    ./Training/Run.ps1 -Mode connection -RunId connection-first

Reference sans apprentissage (commandes aleatoires) :

    ./Training/Run.ps1 -Mode baseline -RunId baseline-moo -Track moo -Episodes 10 -Seed 42

Premier essai court d'entrainement :

    ./Training/Run.ps1 -Mode train -RunId kart-smoke -Steps 10000 -Seed 42

Entrainement complet configure pour un million de decisions :

    ./Training/Run.ps1 -Mode train -RunId kart-first -Seed 42

Reprendre la meme serie sans perdre ses progres :

    ./Training/Run.ps1 -Mode train -RunId kart-first -Resume -Seed 42

Comparer la politique entrainee, sans continuer a apprendre :

    ./Training/Run.ps1 -Mode evaluate -RunId eval-moo -Source kart-first -Track moo -Episodes 10 -Seed 42
    ./Training/Run.ps1 -Mode baseline -RunId baseline-mario -Track mario -Episodes 10 -Seed 42
    ./Training/Run.ps1 -Mode evaluate -RunId eval-mario -Source kart-first -Track mario -Episodes 10 -Seed 42

Ajouter -Graphics pour utiliser la version avec affichage, apres l'avoir compilee separement. La version sans affichage ne permet pas de regarder le jeu. Les essais de reference et d'evaluation tournent a vitesse normale ; l'entrainement utilise une acceleration de simulation de 5.
Chaque essai relance la scene et le compte a rebours du jeu. Cela garantit une remise a zero complete, mais ralentit les entrainements par rapport a un environnement specialise qui remet les objets a zero sur place.

## Resultats et modele

Les donnees sont ecrites dans Training/Results/episodes.csv. Un recapitulatif comparison.md et comparison.json est produit par :

    ./Training/Run.ps1 -Mode summarize

Chaque serie possede un nom unique. Ne pas reutiliser le meme nom pour un nouvel entrainement ; utiliser -Resume pour continuer une serie existante.
Les poids et courbes d'entrainement sont dans results/<nom-de-serie>.
Le modele exporte par ML-Agents est results/<nom-de-serie>/Kart.onnx.
Pour utiliser le modele sans Python : copier ce modele dans Assets/Resources/Kart.onnx, laisser Unity l'importer et recompiler le jeu IA. Lancer ensuite le programme avec --kart-mode=model. Le mode model refuse de demarrer si le modele est absent, pour eviter de confondre une conduite au clavier avec une inference reelle.

Un fichier ONNX produit apres un essai court prouve que l'export fonctionne ; il ne prouve pas que le kart sait finir le circuit.
La reference aleatoire n'est pas le meme reseau neuronal avant apprentissage : elle mesure une conduite sans apprentissage. Le protocole de comparaison stricte des memes poids initiaux est detaille plus bas.

## Observations et commandes

Le modele recoit 24 nombres : vitesse locale, direction de piste, position relative du prochain checkpoint, vitesse dans le sens du circuit, presence de gazon, contact avec le sol, direction du segment suivant et neuf distances vers les obstacles.
Il choisit l'acceleration ou le relachement, la direction et le drift. La marche arriere moteur est desactivee pour cette premiere version. Les objets sont exclus de ce premier entrainement.
La direction de piste et la position cible sont fournies par les checkpoints du circuit : ce n'est pas une conduite a partir de la seule image de la camera. Le transfert vers Mario Circuit reutilise les checkpoints de ce circuit, sans entrainer sur ce dernier.

Les reglages manuels de la partie 1 restent disponibles. Les essais PPO livres utilisent le profil balanced decrit plus bas. La fin d'un essai transmet la derniere recompense a ML-Agents avant de recharger la scene. Le script d'entree conserve la physique et les effets du Player existant.

## Critere de reussite

Comparer des series avec les memes graines, limites de temps, regles de score et parametres physiques. Examiner d'abord le nombre de tours termines et les checkpoints, puis le temps des seuls tours reussis et les chocs. Un score moyen seul ne prouve pas l'apprentissage.
Proposition de cible a confirmer : au moins 9 tours termines sur 10 essais sur Moo Moo Meadows, puis evaluation sur Mario Circuit sans nouvel apprentissage. Si aucun tour n'est termine, indiquer clairement le resultat et poursuivre les reglages ; ne pas presenter le modele comme abouti.

## Sources

- Installation officielle : https://github.com/Unity-Technologies/ml-agents/blob/release_23/docs/Installation.md
- Exemple : https://github.com/Unity-Technologies/ml-agents/blob/release_23/Project/Assets/ML-Agents/Examples/3DBall/Scripts/Ball3DAgent.cs
- Licence : Training/MLAgents-LICENSE.md

Les dossiers .tools, .venv-mlagents, Builds, results et Training/Results sont ignores par Git. Les resultats a remettre doivent etre copies volontairement dans un dossier de livraison, apres verification de leur contenu.

## Comparaison stricte du meme modele avant/apres

Pour conserver exactement le point de depart du modele, produire d'abord un checkpoint avec une seule decision (aucune mise a jour PPO, car le tampon demande 2048 decisions) :

    ./Training/Run.ps1 -Mode train -RunId kart-initial -Steps 1 -Seed 42
    ./Training/Run.ps1 -Mode evaluate -RunId before-moo -Source kart-initial -Track moo -Episodes 10 -Seed 42
    ./Training/Run.ps1 -Mode train -RunId kart-first -Source kart-initial -Seed 42
    ./Training/Run.ps1 -Mode evaluate -RunId after-moo -Source kart-first -Track moo -Episodes 10 -Seed 42

Repeter les evaluations avant/apres avec -Track mario et des noms distincts pour le test de transfert. La reference aleatoire demeure une verification facultative ; le protocole ci-dessus permet la comparaison avant/apres sur la meme initialisation du reseau.

## Compilation allegee du jeu IA

La commande de compilation IA exclut les shaders et kernels GPU du moteur d'inference, qui ne servent pas a notre petit reseau execute sur CPU. Les graphismes du jeu sont conserves. Ce filtrage ne s'applique qu'a BuildKart ; les autres compilations ne sont pas modifiees. KartAgent impose donc le backend d'inference CPU. L'entrainement Python reste egalement sur CPU pour ces premiers essais.

La version sans affichage exclut tous les shaders graphiques, puisque ses observations sont uniquement numeriques. Cela accelere la premiere compilation. Les scenes manuelles et les ressources originales ne sont pas supprimees.


## Premier apprentissage sans marche arriere

KartAgent convertit maintenant la commande PPO [-1,1] en une commande [0,1] : -1 relache l'accelerateur (ralentissement naturel), 0 ou 1 permettent l'acceleration selon le seuil du controleur d'origine. Aucune commande negative n'est transmise au kart. La direction et le drift restent disponibles. Cette restriction concerne le pilote IA ; les commandes manuelles d'origine conservent le recul.

Les anciens poids et scores ne sont pas directement comparables a ce nouveau mode de commande. Demarrer une nouvelle serie, sans reprendre les poids entraines avec marche arriere :

    ./Training/Run.ps1 -Mode train -Editor -RunId kart-forward-only -Steps 4096

Attendre que Python indique qu'il ecoute, puis choisir dans Unity Mario Kart > Apprentissage > Ouvrir l'entrainement visible. Le menu verifie automatiquement les commandes (acceleration, relachement, direction et absence de recul), puis reprend le nom de serie et la graine prepares par Python. Le panneau du jeu indique que la marche arriere est desactivee.

Relacher l'accelerateur ne constitue pas un freinage actif. Le kart peut encore reculer physiquement sous l'effet d'une collision ou d'une pente ; il ne doit plus commander une marche arriere moteur.

## Profil equilibre et objectifs partiels

Le profil balanced est distinct des reglages originaux. Il donne 0,05 point par unite de nouvelle progression, retire 0,25 point par choc, 0,2 point/s sur le gazon et 0,02 point/s de temps. Un echec retire 2 points ; un blocage sans nouvelle progression pendant 5 secondes termine l'essai. L'objectif partiel par defaut est 6 checkpoints, avec un bonus de 10 points. --goal 0 demande un vrai tour complet.

L'introduction est sautee dans ce profil. La physique, les checkpoints et les obstacles sont conserves. La camera est activee explicitement et sa desactivation differee du jeu d'origine est annulee. En mode sans affichage, la limite de rendu est retiree ; le pas physique et les parametres de simulation restent ceux de Unity/ML-Agents.

Reference puis apprentissage et evaluation avec les memes regles :

    ./Training/Run.ps1 -Mode evaluate -RunId balanced-before-six-clean -Source kart-initial-v2 -Profile balanced -Goal 6 -Episodes 10
    ./Training/Run.ps1 -Mode train -RunId balanced-six-v1 -Source kart-initial-v2 -Profile balanced -Goal 6 -Steps 20000
    ./Training/Run.ps1 -Mode evaluate -RunId balanced-after-six-v1 -Source balanced-six-v1 -Profile balanced -Goal 6 -Episodes 10

Atteindre six checkpoints n'est pas terminer un tour : les deux resultats sont comptes separement. Ne pas comparer les scores bruts des profils original et balanced. Les nouveaux enregistrements sont repartis en episodes-<serie>.csv ; le recapitulatif lit egalement l'ancien episodes.csv.

Les programmes de test sans affichage utilisent le port local 5010 ; l'editeur utilise 5004. Les journaux Python sont conserves dans Training/Results/<serie>.log. En mode editeur, le demarrage de Play est demande uniquement quand Python ecoute, puis Play est arrete lorsque le programme Python termine.

## Modele livre et reprise

Le fichier charge par Unity est `Assets/Resources/Kart.onnx`. Une copie est conservee dans `Training/Models/kart-ppo.onnx`, avec le checkpoint Python `kart-ppo-checkpoint.pt` et un manifeste qui identifie la sauvegarde, son empreinte et ses series de verification. Le rapport `Training/Reports/02-evaluations.md` et ses CSV donnent les resultats exacts, y compris les echecs.

Pour tester ce modele dans Unity, utiliser les menus **Tester le modele - Moo Moo Meadows** ou **Tester le modele - Mario Circuit**. Il conduit seul ; aucun apprentissage ni installation Python ne sont necessaires pour ces menus. Les essais recommencent automatiquement apres reussite ou blocage. Le bouton Play permet d'arreter.

Pour tester sans fenetre apres compilation du jeu sans affichage :

    ./Training/Run.ps1 -Mode model -RunId mon-test-onnx -Profile balanced -Goal 0 -Episodes 10 -Seed 42

Pour reprendre l'apprentissage depuis le checkpoint livre, copier `Training/Models/kart-ppo-checkpoint.pt` vers `results/livraison/Kart/checkpoint.pt`, puis utiliser un nouveau nom de serie :

    ./Training/Run.ps1 -Mode train -RunId ma-suite -Source livraison -Profile balanced -Goal 0 -Steps 50000

Le modele livre a ete entraine uniquement sur Moo Moo Meadows. Le test Mario Circuit mesure le transfert et ne constitue pas une validation de sa maitrise de tous les circuits. Les positions initiales et la carte restent celles du jeu ; changer la graine ne cree pas un nouveau circuit et ne randomise pas le depart.

Le programme s'arrete si une session train/evaluate perd sa connexion Python, plutot que de continuer sans apprentissage. Les fichiers CSV sont un journal des courses ; le temps des tours reussis exclut les essais echoues. Pour regenerer le tableau de travail : `.venv-mlagents/Scripts/python.exe Training/build_report.py`.
