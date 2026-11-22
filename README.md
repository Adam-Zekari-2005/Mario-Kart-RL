# Mario Kart RL

Projet Unity 6000.6.4f1 : un joueur humain affronte Luigi pilote par un modele PPO sur Moo Moo Meadows.

Pour jouer : **Mario Kart > IA > Jouer contre Luigi (modele PPO)**, ou choisir Moo Moo Meadows depuis le menu normal. Aucun Python n est necessaire pour jouer. Les menus de diagnostic a direction fixe ne testent pas le modele.

- [Guide du modele, installation et entrainement](Training/README.md)
- [Resultats du vrai duel sur trois tours](Training/Reports/03-duel-evaluations.md)
- [Objets, tremplins et ameliorations du rival](Training/Reports/04-rival-objets-bonus.md)
- [Modele livre et provenance](Training/Models/model-manifest.json)
- [Environnement d apprentissage manuel](Migration/Apprentissage.md)
- [Historique du modele solo a 24 observations](Training/Reports/02-evaluations.md)
- [Jeu source](SOURCE-README.md) et [migration Unity](Migration/README.md)

Le modele actuel recoit 31 observations, dont la direction detaillee de la route, les obstacles, le sol devant le kart et son etat de drift. Il choisit ses commandes : la trajectoire fournit une observation, pas un pilote de remplacement.

La validation demande trois tours dans la vraie scene de course, sans aide dans les tests stricts. Des collisions et des passages sur le gazon restent possibles ; les chiffres exacts et les conditions figurent dans le rapport. Les anciens resultats sur un seul tour ne doivent pas etre attribues a cette version.
