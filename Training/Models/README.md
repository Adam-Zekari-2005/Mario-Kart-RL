# Modeles

- `kart-ppo.onnx` : modele retenu pour terminer Moo Moo Meadows. Copie identique a `Assets/Resources/Kart.onnx`. Les resultats de verification sont dans `../Reports/02-evaluations.md`.
- `kart-ppo-checkpoint.pt` : poids Python permettant de poursuivre son apprentissage, sans repartir de zero.
- `model-manifest.json` : provenance precise, empreinte SHA-256 et noms des evaluations.
- `phase-six-v1.onnx` : objectif intermediaire de six checkpoints ; ce n est pas le modele final.
- `phase-full-v1.onnx` : dernier poids de la premiere phase tour complet. Moins regulier sur Moo Moo Meadows que la sauvegarde retenue, mais a termine 4/10 essais Mario Circuit. Ce resultat n est pas attribue au modele livre.
- `diagnostic-forward-only.onnx` : ancien diagnostic, conserve pour retracer les essais ; ne pas l utiliser comme modele abouti.

Les grandes series de travail restent dans `results/` et `Training/Results/`, ignores par Git. Les CSV du rapport sont copies dans `Training/Reports/data/` pour pouvoir partager les preuves avec le modele.
