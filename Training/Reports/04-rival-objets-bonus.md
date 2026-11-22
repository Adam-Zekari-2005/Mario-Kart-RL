# Objets et bonus du rival — 22 novembre 2026

Le modèle PPO à 31 observations reste identique. Des règles de course complètent ses décisions : utilisation automatique des objets réellement obtenus, préférence de route vers un tremplin accessible, figures et accélération sur ligne droite dégagée. La direction reste choisie par PPO. La vitesse maximale du kart reste inchangée.

## Comparaison dans la vraie scène

Trois courses de trois tours, mêmes graines 71042–71044, 60 images/s, humain accélérant cinq secondes puis roulant librement. Aucun repositionnement de secours autorisé. Ce test ne remplace pas une course contre un humain actif.

| Mesure | Avant ces ajouts | Avec ces ajouts |
|---|---:|---:|
| Courses terminées | 3/3 | 3/3 |
| Temps moyen, trois tours | 157,33 s | 143,80 s |
| Gazon total moyen | 12,17 s | 8,92 s |
| Objets utilisés par course | 0 | 6 |
| Repositionnements | 0 | 0 |

Les passages sur les rampes sont effectivement enregistrés : 5–6 contacts par course, 2–3 contacts sur panneaux et 4–6 figures. Les contacts répétés ne représentent pas autant de tremplins distincts. Un seul bonus supplémentaire satisfait les vérifications d'approche sur route ; un détour coupant le gazon vers un autre panneau n'est pas imposé.

## Premier secteur de gazon

La trajectoire de référence est décalée vers l'extérieur du virage proche du checkpoint 8, uniquement sur un sol vérifié praticable. Sur les mêmes trois graines, cette dernière correction fait passer le temps de gazon de ce secteur de 3,34 à 2,68 secondes en moyenne sur trois tours. Le passage est réduit, pas supprimé. Le compteur historique `early_grass_seconds` couvre seulement les checkpoints 0–4 : utiliser l'entrée 8 de `grass_by_checkpoint` pour cette comparaison.

## Objets et vérifications

Les 14 types d'objets ont été sélectionnés par le vrai système d'inventaire puis consommés automatiquement dans un test Play Unity, sans exception et sans modification de l'interface humaine. Les objets triples consomment leurs trois charges. Bananes et bombes sont déposées derrière ; les projectiles sont lancés devant. Ce sont des règles simples, pas un apprentissage tactique du meilleur moment pour attaquer.

Les interfaces d'inventaire, compteurs de pièces, effets de caméra et musique du joueur humain sont isolés du rival. Les carapaces utilisent les participants réellement présents dans la course. Les vols prévus après tremplin ne déclenchent plus le secours de chute.

Une seconde série à 30 images/s (graines 72042–72044) termine aussi 3/3 courses, avec six objets utilisés par course et aucun secours. Temps : 134,40, 138,53 et 141,74 secondes. Cette série a révélé une référence à un checkpoint détruit lors du rechargement de scène ; la collecte terminale des observations a été protégée dans la version livrée. Les [données de cette série](tactics-data/rival-tactics-final-30.csv) restent conservées.

Sources : [courses de référence](tactics-data/rival-tactics-baseline.csv), [version finale](tactics-data/rival-tactics-final.csv), [avant correction du virage](tactics-data/rival-tactics-v2.csv), [test des 14 objets](rival-items-check.txt). Les chocs et quelques passages sur le gazon restent possibles ; cette version ne garantit pas de battre un joueur expérimenté.

Verification apres recompilation du correctif de changement de scene : 2/2 courses terminees, sans exception dans le journal, six objets utilises par course et aucun secours. [Donnees finales de livraison](tactics-data/rival-tactics-release.csv).
