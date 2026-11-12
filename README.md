# Mario Kart RL — Projet à trois à l’ENSISA

Ce projet prepare puis entraine un kart avec l'apprentissage par renforcement PPO dans Unity. Les deux circuits de travail sont Moo Moo Meadows et Mario Circuit.

- **Conduite au clavier et environnement d'apprentissage** : [mode d'emploi](Migration/Apprentissage.md).
- **Installation, entrainement et evaluations PPO** : [guide de la partie 2](Training/README.md).
- **Resultats mesures** : [rapport des evaluations](Training/Reports/02-evaluations.md).
- **Origine du jeu et migration Unity** : [présentation de notre projet et crédits](SOURCE-README.md) et [migration](Migration/README.md).

Ouvrir le projet avec **Unity 6000.6.4f1**. Le mode de conduite normal reste disponible dans la scene WhiteScreen. Pour voir le modele exporte sans installer Python, choisir **Mario Kart > Apprentissage > Tester le modele - Moo Moo Meadows** ou **Tester le modele - Mario Circuit**. Le modele est dans `Assets/Resources/Kart.onnx` ; son niveau reel est indique dans le rapport, sans confondre les objectifs partiels avec un tour termine.

Pour installer les outils d'entrainement sur Windows depuis PowerShell a la racine du projet :

```powershell
./Training/Setup.ps1
```

Python est installe dans le dossier local `.tools` et ML-Agents dans `.venv-mlagents`. Ces dossiers, les executables de test et les journaux ne sont pas versions. Les versions Python principales sont verrouillees dans `Training/requirements.txt` et l'environnement complet dans `Training/requirements-lock.txt`.

Le modele livre a termine **20/20 essais autonomes sans affichage sur Moo Moo Meadows**, avec un temps moyen simule de **35,21 s**, et **19/20** lors d une autre serie via Python. Sa conduite conserve des collisions et des passages sur le gazon. Sur Mario Circuit, le modele retenu termine **0/10** tours : le transfert reste a ameliorer. Consulter le rapport pour les conditions et les autres sauvegardes.
