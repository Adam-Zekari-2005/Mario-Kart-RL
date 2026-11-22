# Modeles

- `kart-ppo.onnx` : modele actuel du duel, copie identique a Assets/Resources/Kart.onnx ; 31 observations, commandes analogiques par kart.
- `kart-ppo-checkpoint.pt` : checkpoint Python pour poursuivre son apprentissage.
- `model-manifest.json` : provenance, empreinte et mesures de verification.
- `solo24-kart-ppo.onnx`, `solo24-kart-ppo-checkpoint.pt`, `solo24-model-manifest.json` : ancien modele solo, conserve pour reproduire l historique a 24 observations.
- `phase-six-v1.onnx`, `phase-full-v1.onnx`, `diagnostic-forward-only.onnx` : anciens essais, incompatibles avec le contrat actuel a 31 observations ; ne pas les assigner au rival actuel.

Le rapport courant est ../Reports/03-duel-evaluations.md. Les grandes series de travail restent dans results et Training/Results, ignores par Git ; les donnees a partager sont archivees dans Reports/duel-data.
