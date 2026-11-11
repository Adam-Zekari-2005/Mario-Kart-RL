import csv
import hashlib
import json
import statistics
from pathlib import Path

root = Path(__file__).resolve().parents[1]
results = root / 'Training/Results'
out = root / 'Training/Reports'
out.mkdir(parents=True, exist_ok=True)
plan = [
    ('Moo Moo Meadows - avant apprentissage', 'balanced-before-full-moo'),
    ('Moo Moo Meadows - apres la lecon 6 checkpoints', 'balanced-six-on-full-moo'),
    ('Moo Moo Meadows - apres la lecon 16 checkpoints', 'balanced-sixteen-on-full-moo'),
    ('Moo Moo Meadows - dernier poids 50k', 'balanced-after-full-v1-moo'),
    ('Moo Moo Meadows - autres graines', 'balanced-full-holdout'),
    ('Moo Moo Meadows - sauvegarde 20k, selection', 'select-full-v1-20k'),
    ('Moo Moo Meadows - sauvegarde 40k, selection', 'select-full-v1-40k'),
    ('Moo Moo Meadows - consolidation 20k, selection', 'select-full-v2-20k'),
    ('Moo Moo Meadows - sauvegarde 40k, nouveaux essais', 'selected-40k-holdout'),
    ('Moo Moo Meadows - ONNX autonome livre', 'native-selected-moo'),
    ('Mario Circuit - avant apprentissage', 'balanced-before-full-mario'),
    ('Mario Circuit - dernier poids 50k', 'balanced-after-full-mario'),
    ('Mario Circuit - sauvegarde 40k retenue', 'selected-40k-mario'),
]
lines = ['# Evaluations du kart PPO', '',
    'Modele livre : sauvegarde Kart-39951 de balanced-full-v1, copiee dans Assets/Resources/Kart.onnx. Selection : 10/10 tours Moo Moo Meadows ; verification ulterieure : 19/20. Sur Mario Circuit, ce modele atteint 12 checkpoints mais termine 0/10 tours. Le test ONNX autonome est distingue dans le tableau.', '',
    'Les lignes ci-dessous sont des evaluations sans apprentissage ni exploration aleatoire. Les graines sont conservees dans les CSV. Les essais partiels ne sont pas comptes comme des tours.', '',
    '| Evaluation | Essais | Tours termines | Checkpoints moyens | Chocs moyens | Gazon moyen (s) | Temps moyen des tours reussis (s) |',
    '|---|---:|---:|---:|---:|---:|---:|']
data = []
for label, name in plan:
    path = results / f'episodes-{name}.csv'
    if not path.exists():
        continue
    rows = list(csv.DictReader(path.open(encoding='utf-8-sig')))
    if not rows:
        continue
    mean = lambda key: statistics.mean(float(r[key]) for r in rows)
    wins = [r for r in rows if int(r['laps']) > 0]
    lap_time = statistics.mean(float(r['seconds']) for r in wins) if wins else None
    display_time = f'{lap_time:.2f}' if lap_time is not None else '-'
    lines.append(f'| {label} | {len(rows)} | {len(wins)}/{len(rows)} | {mean("checkpoints"):.2f} | {mean("collisions"):.2f} | {mean("grass_seconds"):.2f} | {display_time} |')
    data.append(dict(label=label, run=name, episodes=len(rows), laps_completed=len(wins), mean_checkpoints=mean('checkpoints'), mean_collisions=mean('collisions'), mean_grass_seconds=mean('grass_seconds'), mean_successful_lap_seconds=lap_time, seeds=[int(r['seed']) for r in rows]))
lines += ['', '## Protocole', '',
    '- Profil balanced identique avant/apres : progression 0,05 point/unite, choc -0,25, gazon -0,2 point/s, temps -0,02 point/s, echec -2.',
    '- Pour un tour complet : objectif partiel desactive, bonus de tour +100, maximum 90 secondes, echec apres 5 secondes sans nouvelle progression.',
    '- Meme physique, memes scenes, memes observations et memes commandes. Simulation sans affichage ; vitesse de simulation 1 pour les evaluations. Le temps dans le tableau est du temps simule de course, hors introduction.',
    '- Les evaluations principales emploient les graines 42 a 51. Les premieres verifications emploient 10042 a 10051 ; la serie de vingt nouveaux essais emploie 20042 a 20061.',
    '- Entrainement uniquement sur Moo Moo Meadows : 20 000 decisions demandees pour la premiere lecon, 40 000 pour la suivante, puis 50 000 pour le tour complet. Le dernier lot peut depasser legerement ces limites. La sauvegarde intermediaire a 39 951 decisions de la derniere phase est comparee au dernier poids ; une consolidation supplementaire est evaluee separement.',
    '- Mario Circuit est teste sans nouvel apprentissage sur ce circuit.',
    '- La selection compare plusieurs sauvegardes sur les memes dix essais ; les vingt essais a partir de 20042 servent ensuite a verifier le modele retenu. Le test ONNX natif emploie 30042 a 30061.',
    '- Les graines font varier les mecanismes aleatoires du jeu ; la position de depart et la geometrie du circuit ne sont pas randomisees. Ce test ne mesure pas toutes les situations possibles.',
    '- Le modele recoit 24 observations numeriques, dont la direction et la cible du circuit fournies par ses checkpoints. Il ne conduit pas a partir des images de la camera.',
    '- La marche arriere moteur et les objets sont exclus de cette premiere version. Le kart peut relacher l accelerateur, tourner et drifter.', '',
    '## Limites', '',
    'Une serie de dix essais reste une petite mesure, pas une garantie sur toutes les situations. Comparer les scores uniquement a regles identiques. Un tour termine ne prouve pas une conduite parfaite : consulter aussi les chocs et le temps sur le gazon. Les anciennes series de diagnostic avec d autres regles restent dans Training/Results et ne figurent pas dans ce tableau.', '',
    'Les performances du modele natif sont mesurees sans affichage, a cadence simulee de 60 images/s. Le menu Unity permet de le voir avec affichage ; ces series ne constituent pas une mesure du taux de reussite a toutes les cadences graphiques.', '',
    'Les donnees mesurees sont archivees avec ce rapport dans le sous-dossier data. Les CSV complets de travail et les journaux sont dans Training/Results (ignore par Git).']
(out / '02-evaluations.md').write_text('\n'.join(lines)+'\n', encoding='utf-8')
(out / 'evaluation-results.json').write_text(json.dumps(data, indent=2, ensure_ascii=False), encoding='utf-8')
archive = out / 'data'
archive.mkdir(exist_ok=True)
for item in data:
    name = item['run']
    (archive / f'episodes-{name}.csv').write_bytes((results / f'episodes-{name}.csv').read_bytes())
print('\n'.join(lines[:len(data)+6]))
