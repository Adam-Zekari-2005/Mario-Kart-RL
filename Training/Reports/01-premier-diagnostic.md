# Premier diagnostic PPO - avant reequilibrage

Serie : kart-forward-only, MooMooTraining, graine initiale 42.
Entrainement termine a 4 107 decisions (limite demandee : 4 096, depassement du dernier lot).
12 essais complets enregistres, aucun tour termine.
Moyenne : 3,50 checkpoints ; score -39,30 ; 6,92 collisions.

Ce sont les essais d'exploration pendant l'entrainement, pas une evaluation deterministe du modele final. Ils ne prouvent pas une progression reguliere. Le modele diagnostic-forward-only.onnx est conserve comme artefact de diagnostic et ne doit pas etre presente comme un pilote abouti.

Probleme observe : la progression de l'essai a 6 checkpoints rapportait environ 5,33 points, tandis que 17 collisions coutaient 34 points. Un kart immobile pouvait terminer a environ -13 points, alors que certaines tentatives plus avancees obtenaient un score beaucoup plus negatif. Le bonus de tour est reste hors d'atteinte dans cette serie courte.

La serie suivante utilise un profil separe : progression 0,05 point/unite, choc 0,25 point, gazon 0,2 point/s, temps 0,02 point/s, echec 2 points, blocage apres 5 s et objectif partiel a 6 checkpoints avec bonus 10. Les scores de ces deux profils ne sont pas directement comparables. La comparaison suivante doit employer les memes regles avant et apres apprentissage.
