# Evaluations du kart PPO

Modele livre : sauvegarde Kart-39951 de balanced-full-v1, copiee dans Assets/Resources/Kart.onnx. Selection : 10/10 tours Moo Moo Meadows ; verification ulterieure : 19/20. Sur Mario Circuit, ce modele atteint 12 checkpoints mais termine 0/10 tours. Le test ONNX autonome est distingue dans le tableau.

Les lignes ci-dessous sont des evaluations sans apprentissage ni exploration aleatoire. Les graines sont conservees dans les CSV. Les essais partiels ne sont pas comptes comme des tours.

| Evaluation | Essais | Tours termines | Checkpoints moyens | Chocs moyens | Gazon moyen (s) | Temps moyen des tours reussis (s) |
|---|---:|---:|---:|---:|---:|---:|
| Moo Moo Meadows - avant apprentissage | 10 | 0/10 | 3.00 | 4.80 | 0.06 | - |
| Moo Moo Meadows - apres la lecon 6 checkpoints | 10 | 0/10 | 10.60 | 8.90 | 8.67 | - |
| Moo Moo Meadows - apres la lecon 16 checkpoints | 10 | 6/10 | 18.60 | 3.10 | 2.29 | 32.60 |
| Moo Moo Meadows - dernier poids 50k | 10 | 7/10 | 18.90 | 4.50 | 4.79 | 35.71 |
| Moo Moo Meadows - autres graines | 10 | 5/10 | 17.50 | 4.30 | 4.39 | 34.80 |
| Moo Moo Meadows - sauvegarde 20k, selection | 10 | 8/10 | 20.20 | 4.70 | 4.86 | 33.90 |
| Moo Moo Meadows - sauvegarde 40k, selection | 10 | 10/10 | 21.00 | 3.70 | 5.04 | 35.62 |
| Moo Moo Meadows - consolidation 20k, selection | 10 | 10/10 | 21.00 | 2.30 | 3.29 | 32.88 |
| Moo Moo Meadows - sauvegarde 40k, nouveaux essais | 20 | 19/20 | 20.65 | 3.65 | 5.06 | 35.35 |
| Moo Moo Meadows - ONNX autonome livre | 20 | 20/20 | 21.00 | 3.60 | 5.08 | 35.21 |
| Mario Circuit - avant apprentissage | 10 | 0/10 | 1.00 | 2.90 | 6.71 | - |
| Mario Circuit - dernier poids 50k | 10 | 4/10 | 26.00 | 1.10 | 7.32 | 50.21 |
| Mario Circuit - sauvegarde 40k retenue | 10 | 0/10 | 12.00 | 1.00 | 5.14 | - |

## Protocole

- Profil balanced identique avant/apres : progression 0,05 point/unite, choc -0,25, gazon -0,2 point/s, temps -0,02 point/s, echec -2.
- Pour un tour complet : objectif partiel desactive, bonus de tour +100, maximum 90 secondes, echec apres 5 secondes sans nouvelle progression.
- Meme physique, memes scenes, memes observations et memes commandes. Simulation sans affichage ; vitesse de simulation 1 pour les evaluations. Le temps dans le tableau est du temps simule de course, hors introduction.
- Les evaluations principales emploient les graines 42 a 51. Les premieres verifications emploient 10042 a 10051 ; la serie de vingt nouveaux essais emploie 20042 a 20061.
- Entrainement uniquement sur Moo Moo Meadows : 20 000 decisions demandees pour la premiere lecon, 40 000 pour la suivante, puis 50 000 pour le tour complet. Le dernier lot peut depasser legerement ces limites. La sauvegarde intermediaire a 39 951 decisions de la derniere phase est comparee au dernier poids ; une consolidation supplementaire est evaluee separement.
- Mario Circuit est teste sans nouvel apprentissage sur ce circuit.
- La selection compare plusieurs sauvegardes sur les memes dix essais ; les vingt essais a partir de 20042 servent ensuite a verifier le modele retenu. Le test ONNX natif emploie 30042 a 30061.
- Les graines font varier les mecanismes aleatoires du jeu ; la position de depart et la geometrie du circuit ne sont pas randomisees. Ce test ne mesure pas toutes les situations possibles.
- Le modele recoit 24 observations numeriques, dont la direction et la cible du circuit fournies par ses checkpoints. Il ne conduit pas a partir des images de la camera.
- La marche arriere moteur et les objets sont exclus de cette premiere version. Le kart peut relacher l accelerateur, tourner et drifter.

## Limites

Une serie de dix essais reste une petite mesure, pas une garantie sur toutes les situations. Comparer les scores uniquement a regles identiques. Un tour termine ne prouve pas une conduite parfaite : consulter aussi les chocs et le temps sur le gazon. Les anciennes series de diagnostic avec d autres regles restent dans Training/Results et ne figurent pas dans ce tableau.

Les performances du modele natif sont mesurees sans affichage, a cadence simulee de 60 images/s. Le menu Unity permet de le voir avec affichage ; ces series ne constituent pas une mesure du taux de reussite a toutes les cadences graphiques.

Les donnees mesurees sont archivees avec ce rapport dans le sous-dossier data. Les CSV complets de travail et les journaux sont dans Training/Results (ignore par Git).
