# Mario Kart RL — Projet à trois à l’ENSISA

Notre projet, développé par 3 personnes y compris moi, part d’une question simple : comment apprendre à un kart à boucler un circuit, puis prendre le départ contre lui ? Sur **Moo Moo Meadows**, le joueur conduit Mario au clavier et affronte Luigi, piloté par le modèle que nous avons entraîné.

## Du premier checkpoint au duel

Nous avons adapté le jeu à Unity 6 et préparé un environnement où le kart peut répéter la même course. À chaque décision, il reçoit notamment sa vitesse, la direction du prochain checkpoint et des informations sur les obstacles. Il peut accélérer, tourner et déraper. Le passage des checkpoints dans l’ordre nous permet de mesurer sa progression, même quand il abandonne avant la ligne d’arrivée.

Avec Unity ML-Agents et l’algorithme PPO, nous avons d’abord entraîné le kart à franchir six checkpoints, puis seize, avant de lui demander un tour complet. Nous avons ensuite exporté le modèle au format ONNX pour que Unity puisse l’utiliser directement en course aux commandes de Luigi.

## Ce que montrent nos essais

Sur Moo Moo Meadows, le modèle embarqué a terminé **20 tours sur 20** en simulation sans affichage, en **35,21 secondes de moyenne**. Il a aussi passé en moyenne **5,08 secondes par essai sur le gazon**. Sur Mario Circuit, utilisé pour tester le même modèle sur une autre piste, il a terminé **0 tour sur 10**. Ces résultats mesurent sa conduite autonome sur un tour. [Protocole et résultats détaillés](Training/Reports/02-evaluations.md).

## Essayer la course

1. Ouvrir le projet avec Unity **6000.6.4f1**.
2. Dans le dossier des scènes, ouvrir **WhiteScreen** et cliquer sur **▶ Play**.
3. Choisir **Moo Moo Meadows** et prendre le départ avec Mario. Le modèle de Luigi est fourni avec le jeu.

Pour reproduire nos expériences ou entraîner un nouveau modèle, suivre le [guide d’entraînement](Training/README.md).


