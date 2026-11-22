# Joueur contre le modele PPO

Depuis le menu normal, choisir Moo Moo Meadows, ou utiliser **Mario Kart > IA > Jouer contre Luigi (modele PPO)** dans Unity. Mario reste au clavier ; Luigi charge automatiquement Assets/Resources/Kart.onnx. Python n est pas necessaire pour jouer.

Le modele courant utilise le comportement Kart et 31 observations. Ses commandes sont propres a chaque Player, dosees et synchronisees avec la physique. La direction de route detaillee, les checkpoints et les capteurs sont des observations : le reseau choisit la direction, l acceleration et le drift.

Les tests de livraison demandent trois tours dans la vraie scene. Le rapport, les donnees individuelles et les limites sont dans [Training/Reports/03-duel-evaluations.md](../Training/Reports/03-duel-evaluations.md). Le guide d installation et de reprise est [Training/README.md](../Training/README.md).

## Ne pas confondre avec les diagnostics

Les menus **Diagnostic des commandes (direction fixe, sans modele)** et **Ajouter un kart de test (direction fixe)** verifient une ancienne demonstration separee. Son agent MarioKart.Demo.KartAgent utilise neuf observations et le comportement KartPPO. Il ne doit pas recevoir le modele courant a 31 observations.

Les scenes et menus Training permettent des essais d apprentissage independants. Un ancien resultat sur un seul tour avec un kart seul ne valide pas une course humain contre IA de trois tours.

En course normale, une recuperation replace le rival apres une chute ou un blocage prolonge. Les evaluations strictes des performances apprises la desactivent ; les autres comptent son utilisation. Des collisions ou de courts passages sur le gazon peuvent encore arriver. La vitesse et le transfert vers d autres circuits restent des axes d amelioration.
