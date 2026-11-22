# Duel humain contre PPO : verification sur trois tours

## Modele courant

Sauvegarde duel-nav-v1 a 30031 decisions. SHA-256 : `b4cbc38e32632a43246639911186d5bba5f2a160dacee71f9f6ea331e75f520b`.

Validation du fichier ONNX directement dans Unity : **20/20 courses de trois tours**, **0 remise sur piste**, cadences 30/60/144 images par seconde. Cinq courses incluent le depart d un second kart pendant cinq secondes.
Temps moyen sur le gazon : **12.92 s par course de trois tours**. Il ne s agit donc pas d une conduite sans erreur.
Les dix evaluations supplementaires via Python terminent aussi les trois tours. Le test Editor avec introduction, commandes humaines et chute forcee est valide.
Les contacts releves dans les benchmarks natifs sont avec le joueur immobile, les vaches et le tremplin (voir les tags d impacts dans le manifeste). Aucun blocage contre un mur n a empeche de finir ces courses ; cela ne garantit pas toutes les parties possibles.

Les anciens tests sur un kart seul et un tour ne valident pas le duel. Ici le rival utilise la scene reelle MooMooMeadows, sa propre place de depart, le kart humain et les obstacles du jeu. Une reussite demande trois tours, pas un seul.

| Serie | Courses | Trois tours termines | Gazon moyen / course (s) | Temps des courses reussies (s) | Retours sur piste |
|---|---:|---:|---:|---:|---:|
| duel-before-144 | 5 | 0/5 | 32.63 | - | non mesure |
| duel-before-30 | 5 | 0/5 | 45.07 | - | non mesure |
| duel-before-60 | 5 | 2/5 | 26.08 | 111.96 | non mesure |
| duel-final-144-strict | 5 | 5/5 | 12.74 | 158.35 | 0 |
| duel-final-30-strict | 5 | 5/5 | 12.75 | 163.29 | 0 |
| duel-final-60-strict | 5 | 5/5 | 13.74 | 159.91 | 0 |
| duel-final-human | 5 | 5/5 | 12.44 | 158.01 | 0 |
| duel-physics-only-60 | 5 | 0/5 | 19.16 | - | non mesure |
| episodes-duel-analog-start-eval | 5 | 5/5 | 24.13 | 156.76 | non mesure |
| episodes-duel-clean-20k-eval | 5 | 0/5 | 27.44 | - | non mesure |
| episodes-duel-nav-20k-eval | 5 | 5/5 | 10.09 | 155.11 | non mesure |
| episodes-duel-nav-final-eval | 10 | 10/10 | 13.70 | 159.34 | non mesure |
| episodes-duel-nav-start-eval | 5 | 5/5 | 7.07 | 154.35 | non mesure |
| episodes-duel-road-10k-eval | 5 | 1/5 | 14.25 | 102.49 | non mesure |
| episodes-duel-road-20k-eval | 5 | 2/5 | 8.21 | 101.07 | non mesure |
| episodes-duel-road-30k-eval | 5 | 0/5 | 5.44 | - | non mesure |
| episodes-duel-road-40k-eval | 5 | 3/5 | 12.20 | 102.51 | non mesure |
| episodes-duel-road-50k-eval | 5 | 1/5 | 9.55 | 104.72 | non mesure |
| episodes-duel-trace-eval | 1 | 0/1 | 1.65 | - | non mesure |
| episodes-duel-v1-20k-eval | 5 | 3/5 | 19.45 | 104.99 | non mesure |

## Corrections et apprentissage

- Les commandes de drift de l'IA sont echantillonnees au pas physique, et non au rythme des images. Le clavier humain conserve son fonctionnement.
- La zone neutre est coherente pour les axes, les touches et les forces du drift. L acceleration, la rotation et les forces laterales sont dosees, sans modifier le clavier humain.
- Le modele recoit 31 observations, dont son etat de drift/boost et trois mesures de route devant lui. La direction de piste est issue de ItemPath (50 noeuds, 473 echantillons sur route, aucun sur gazon), sans remplacer les actions du reseau.
- Le pilote d'entrainement et le rival utilisent des commandes propres a chaque kart ; aucune commande globale ne pilote les deux karts.
- L'apprentissage du duel utilise trois tours et parfois un petit decalage lateral/angulaire au depart. La scene, les checkpoints et la physique sont ceux de la course.
- La phase de finition retire 2 points par impact significatif, 1 point par seconde sur le gazon, et ne recompense pas la progression parcourue sur le gazon. Le bonus de tour reste +100. Ces regles ne modifient pas les reglages manuels par defaut.
- L'entrainement termine l'essai apres un blocage ou une sortie, sans remise sur piste. En course normale, une remise sur piste est disponible apres un blocage prolonge ou une chute ; les tests stricts la desactivent avec --duel-rescue=0 et son utilisation est comptee dans les autres tests.

## Protocole et limites

Tests sans affichage et inference deterministe. L'introduction est sautee pour les benchmarks natifs ; les essais sont mesures depuis le depart. Le joueur de test reste immobile, sauf les series human/launch : il accelere tout droit pendant cinq secondes puis relache l'accelerateur. Cela ne remplace pas tous les comportements possibles d'un joueur humain ou les attaques d'objets.
Les cadences de rendu sont indiquees dans les noms des benchmarks. Les graines et les resultats individuels sont archives dans duel-data. Changer la graine ne modifie pas le circuit. Les series de selection de sauvegardes ne sont pas des essais independants de generalisation.
Les premiers compteurs d'impacts du benchmark before etaient bruts ; ne pas les comparer aux impacts significatifs filtres des evaluations recentes. Les mesures recentes filtrent par normale, vitesse d'impact et delai de 0,75 s. Le tableau expose le temps sur le gazon et les courses terminees, plutot qu'un score qui change avec les regles.
La vitesse du rival reste inferieure a celle d un joueur experimente. Le controle Editor avec introduction normale, deux secondes de commande humaine et une chute forcee est conserve dans duel-real-race-check.txt. Un bon taux sur ces essais ne garantit ni zero collision, ni zero passage sur le gazon dans toutes les parties. Le manifeste du modele identifie exactement le fichier livre et ses series de verification.
