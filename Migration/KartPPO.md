# KartPPO — branchement de l'agent

Projet Unity 6000.6.4f1. La dépendance C# ML-Agents est fixée à 4.2.0 dans Packages/manifest.json. La 4.0.0 échoue à compiler sur cet éditeur dans Match3ActuatorComponent (ancienne API GetInstanceID).

Dans Unity, **Mario Kart → IA → Tester joueur et IA (Moo Moo Meadows)** lance un essai temporaire sur le circuit. Le kart humain conserve les commandes du jeu. Un second kart reçoit une consigne de conduite fixe via ML-Agents. La console affiche son déplacement et sa rotation. Cette consigne sert à valider les commandes avant l'entraînement.

Pour conduire soi-même, démarrer une course et, une fois le départ donné, choisir **Mario Kart → IA → Ajouter un kart IA en Play**. Le kart ajouté utilise la consigne de démonstration en l'absence de modèle. Il disparaît à la sortie du mode Play.

Pour préparer une scène de travail, sélectionner la racine possédant le composant Player du kart destiné à l'IA, puis **Mario Kart → IA → Configurer le kart sélectionné**. Enregistrer dans la scène de travail de l'équipe. Pour une course avec un joueur, laisser son kart sans KartAgent. Chaque Player possède son propre KartDriveInput. La physique reste dans Player.

**Agent de démonstration (MarioKart.Demo).** Le modèle entraîné utilise le comportement Kart, 24 observations et les scènes de Training. Pour ce modèle, suivre Training/README.md. Le comportement suivant concerne la démonstration indépendante. Behavior Name `KartPPO`, deux actions continues dans cet ordre : direction (−1 gauche, +1 droite), pédale (−1 frein puis recul, +1 accélération). Une décision tous les cinq pas physiques, commande maintenue entre deux décisions. Le mode Default utilise le trainer connecté ou le modèle assigné, avec une heuristique pour le test sans modèle. Pour la démo entraînée, assigner le modèle à Behavior Parameters et choisir Inference Only.

**Vecteur d'observation (9 valeurs).** Vitesses longitudinale et latérale locales divisées par 120 et bornées à [−1, 1], direction horizontale normalisée en coordonnées locales (x puis z), puis distances aux obstacles à −75°, −35°, 0°, +35°, +75°, divisées par 35 unités (1 si aucun obstacle). Les rayons ignorent les triggers, le kart lui-même et les faces orientées vers le haut. Ils mesurent des obstacles physiques, pas les limites d'une route peinte.

**Contrat environnement.** Assigner au champ Track Provider un composant implémentant IKartTrackDirection.TryGetDirection(position, out worldDirection). Sans fournisseur, l'agent vise le prochain checkpoint de LapCounter. Le module d'environnement attribuera les récompenses via AddReward et terminera les essais via EndEpisode. Après une téléportation, synchroniser également le suivi du script DontGoThroughThings. L'agent remet ses actions à zéro au début de chaque épisode.
