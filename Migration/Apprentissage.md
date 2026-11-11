# Partie 1 : essais au clavier

## Lancer un essai

1. Ouvrir le projet avec Unity 6000.6.4f1.
2. Ouvrir Assets/Scenes/MooMooMeadows.unity, puis cliquer sur Play.
3. Cliquer dans la vue Game et attendre la fin du compte a rebours.
4. Conduire avec les fleches ou ZQSD. Espace + direction permet le drift.
5. Le panneau « Environnement d'apprentissage » affiche le score, le temps et les checkpoints.

F5 recommence un essai. Le jeu recharge le circuit : le kart, ses objets, ses animations, la vitesse, les compteurs et le compte a rebours retrouvent leur etat initial. Le resultat precedent reste visible et est aussi inscrit dans la Console Unity.

## Regles actuelles (a ajuster apres les essais)

- Nouvelle progression : +0,01 point par unite Unity parcourue le long du segment attendu.
- Un tour valide : +100 points puis nouvel essai.
- Temps : -0,2 point par seconde.
- Sortie de piste signalee par les zones du jeu, ou 15 secondes sans nouvelle progression : -10 points puis nouvel essai.
- Un essai dure au maximum 300 secondes.
- La progression recompensee correspond au maximum atteint. Reculer puis revenir au meme endroit ne rapporte donc pas une seconde fois les memes points.
- Les checkpoints doivent etre franchis dans l'ordre et avec une vitesse dirigee vers le prochain point. Tous les checkpoints puis la ligne d'arrivee sont requis pour le bonus.

Les valeurs se reglent sur le composant KartEpisode du joueur, hors mode Play.
L'assistance de retour sur piste du jeu est desactivee pendant ces essais afin de ne pas masquer les echecs.

## Comparaison manuelle a effectuer

Garder les memes reglages et noter le score, le temps, les checkpoints et les tours pour :

| Essai | Score | Temps | Checkpoints | Tours |
| --- | --- | --- | --- | --- |
| Rester immobile | | | | |
| Faire des allers-retours pres d'un checkpoint | | | | |
| Faire un tour dans le bon sens | | | | |

Le tour correct doit obtenir le meilleur score. Pour les allers-retours, le score de progression ne doit plus augmenter quand le maximum precedent n'est pas depasse. Verifier egalement une sortie de piste et F5 : retour au depart, vitesse et compteurs remis a zero.

## Organisation

TrackProgress.cs fournit la progression, la direction locale, la vitesse dans cette direction et les passages valides. Il utilise les checkpoints de LapCounter sans modifier le classement d'origine.
KartEpisode.cs calcule le score et termine les essais.
Le menu Mario Kart > Apprentissage > Activer dans le circuit ouvert ajoute ces composants au joueur d'un autre circuit. Enregistrer la scene avant Play. Retirer les deux composants permet de revenir a la course normale.

## Verification realisee et limites

Unity a compile les scripts et execute sept controles sur un circuit synthetique : initialisation, progression, recul, passage inverse refuse, checkpoint saute refuse, passage ordonne et doublon refuse.
Ces controles ne remplacent pas un tour reel au clavier. La geometrie entre checkpoints est approximee par des segments droits ; la direction est donc indicative dans les virages. La detection de sortie repose sur les zones Water/OutOfBounds existantes, pas sur une nouvelle limite de route. Un passage dans l'herbe n'est pas automatiquement une sortie.
Aucun entrainement PPO ni modele IA n'est integre a ce stade.


## Mise a jour : qualite de conduite

Le score retire maintenant 0,2 point par seconde, 1 point par seconde sur le gazon (surface Dirt sous le kart) et 2 points par choc lateral avec une vitesse d'impact d'au moins 2 unites/s. Les contacts avec le sol et les atterrissages ne comptent pas comme chocs. Une pause de 0,75 s entre deux penalites de collision evite de compter plusieurs contacts d'un meme impact. Ces valeurs restent a ajuster apres comparaison manuelle.

Le panneau affiche le nombre de chocs, le temps sur le gazon et les points retires pour chaque cause. Un saut ne compte pas comme du gazon si aucune surface n'est detectee dans les 3 unites sous le kart. La progression sur le gazon reste mesuree, mais son cout penalise ce choix. Sur ce circuit, Dirt identifie la pelouse ; cette convention devra etre verifiee sur les autres circuits.

Comparer un tour propre, un tour avec des chocs volontaires et un passage prolonge sur le gazon. Verifier qu'un saut et une conduite sur la route laissent les compteurs correspondants a zero. Les anciens scores ne sont plus directement comparables aux nouveaux.

L'unite affichee est l'unite du monde Unity, sans supposer une conversion en metres reels.

