# Entrainement et verification du kart PPO

## Jouer contre le modele

Ouvrir le projet avec Unity 6000.6.4f1, puis choisir **Mario Kart > IA > Jouer contre Luigi (modele PPO)**. Le joueur conduit Mario au clavier ; Luigi utilise `Assets/Resources/Kart.onnx`. Aucun Python ne doit tourner pour jouer. Le meme duel est disponible en choisissant Moo Moo Meadows depuis le menu normal.

Les menus marques **diagnostic / direction fixe** ne testent pas un modele entraine. Les scenes `Training` et leurs menus sont des outils distincts du vrai duel.

## Resultats et fichiers a remettre

- `Reports/03-duel-evaluations.md` : mesures du vrai duel sur trois tours, cadences et donnees individuelles.
- `Reports/02-evaluations.md` : historique de l ancien modele a 24 observations, teste principalement seul sur un tour. Ces chiffres ne valident pas le modele actuel du duel.
- `Models/kart-ppo.onnx` et `Assets/Resources/Kart.onnx` : copies du modele livre.
- `Models/kart-ppo-checkpoint.pt` : poids Python pour poursuivre l apprentissage.
- `Models/model-manifest.json` : provenance, contrat d observations et empreinte du modele.
- `Reports/duel-data` : CSV mesures, conserves volontairement avec le projet.

## Installation Windows

Depuis PowerShell a la racine :

    ./Training/Setup.ps1

Le projet utilise Python 3.10.11 x64 local, ML-Agents Python 1.1.0, PyTorch 2.2.2 et ML-Agents Unity 4.2.0. Les versions sont fixees dans les fichiers requirements et Packages. Les dossiers `.tools`, `.venv-mlagents`, `Builds`, `results` et `Training/Results` sont ignores par Git.

## Compiler et entrainer le duel

Dans Unity : **Mario Kart > Apprentissage > Compiler les essais IA sans affichage**. Cette compilation inclut la vraie scene MooMooMeadows et produit `Builds/TrainingHeadless/KartTrainingHeadless.exe`. Les shaders sont exclus uniquement de cette compilation sans affichage ; le jeu manuel conserve ses ressources.

Copier `Training/Models/kart-ppo-checkpoint.pt` dans `results/livraison/Kart/checkpoint.pt`, puis lancer une nouvelle serie :

    ./Training/Run.ps1 -Mode train -RunId ma-suite-duel -Source livraison -Track duel -Profile balanced -Goal 0 -Steps 30000

Le modele apprend dans le kart de Luigi, depuis sa place sur la grille, sur trois tours. Certaines positions initiales d entrainement sont legerement decalees lateralement et en orientation. Les evaluations emploient le depart normal. Le profil duel conserve sa physique et ses commandes propres a chaque kart.

Evaluer les poids sans nouvel apprentissage :

    ./Training/Run.ps1 -Mode evaluate -RunId ma-verification -Source ma-suite-duel -Track duel -Profile balanced -Goal 0 -Episodes 10 -Seed 50042

Un nom de serie neuf conserve les anciennes mesures. `-Resume` reprend une serie d apprentissage. La commande Python `Training/run.py` accepte aussi `--exe` pour choisir une compilation separee, `--base-port` pour des tests paralleles et `--capture-fps` pour la cadence simulee. L executif doit etre recompile apres une modification des scripts ou du modele embarque.

## Contrat de pilotage

Le comportement `Kart` recoit **31 observations**. Les 24 premieres sont les vitesses locales, la direction de route, le prochain checkpoint, la progression, le gazon, le contact au sol, le segment suivant et neuf distances vers les obstacles. Les sept suivantes decrivent le drift, sa charge, le boost et trois mesures de sol devant le kart.

La direction de route utilise la trajectoire ItemPath detaillee, controlee sur Moo Moo Meadows (473 echantillons sur route, aucun sur gazon). Les checkpoints ordonnes continuent de valider la progression et les tours. Cette trajectoire fournit une observation au reseau, sans choisir ses commandes : ce n est pas un pilote par script ni une conduite a partir des pixels de la camera.

Les sorties restent deux valeurs continues (pedale puis direction) et un choix de drift. La pedale du reseau [-1,1] est convertie en acceleration/relachement [0,1], sans marche arriere moteur. L acceleration et la direction sont dosees ; la zone neutre de direction est 0,02. Le drift n applique pas de force laterale avec une direction neutre. Les boutons de l IA sont echantillonnes au pas physique, avec une decision toutes les cinq etapes physiques. Les controles du joueur restent au clavier.

L ajout des observations est reproductible avec `upgrade_observations.py` : les anciens poids sont conserves, les colonnes nouvelles commencent a zero, la normalisation est etendue et les moments d optimisation sont reinitialises. Un test numerique verifie que les nouveaux capteurs ne changent pas les anciennes activations au depart. Les poids ont ensuite ete affines avec PPO ; consulter le manifeste pour la sauvegarde exacte.

## Recompenses et fin d essai

Le duel d apprentissage recompense uniquement la nouvelle progression sur la route (0,05 point/unite), ajoute +100 par tour, retire 2 points par impact significatif, 1 point/s de gazon et 0,05 point/s de temps. Un echec retire 5 points. La progression effectuee sur le gazon est consommee sans recompense, pour ne pas etre payee au retour sur la route.

L essai demande trois tours, se termine apres six secondes sans nouvelle progression, une sortie ou 180 secondes, puis recharge toute la scene. Il n y a pas de remise sur piste pendant l apprentissage. Les reglages manuels par defaut de KartEpisode restent distincts.

En course normale, Luigi peut etre replace apres une chute ou huit secondes de blocage. Les benchmarks stricts utilisent `--duel-rescue=0` ; les autres comptent les remises sur piste. Un test qui utilise cette aide ne doit pas etre presente comme une reussite autonome sans assistance.

## Verifications

- Tests de progression, entrees propres a chaque kart, zone neutre, dosage analogique, boutons au pas physique et capteurs de route executes pendant la compilation.
- Verification de l import ONNX et d une inference CPU avec `KartModelValidation.Run`.
- `python -m unittest Training/test_results.py` : une course duel exige trois tours ; les essais partiels et echoues ne contaminent pas le temps des reussites.
- `python Training/build_duel_report.py` : archive et regenere le tableau du duel a partir des mesures presentes localement.

La connexion Unity/Python a ete validee precedemment avec le script officiel Ball3DAgent release_23, conserve dans Assets/Scripts/Training/Samples avec sa licence. Le guide source du projet et les rapports historiques conservent ce contexte.

Le modele est travaille sur Moo Moo Meadows. Les mesures sur ce circuit ne garantissent pas sa maitrise de Mario Circuit, de tous les comportements humains ou des objets de combat. Des collisions et de courts passages sur le gazon peuvent encore survenir ; consulter les mesures plutot qu un score seul.

Les ajouts de course (objets automatiques, tremplins et premier virage) sont decrits dans [le rapport du rival](Reports/04-rival-objets-bonus.md). Ces regles completent PPO ; elles ne modifient pas les poids du modele.
