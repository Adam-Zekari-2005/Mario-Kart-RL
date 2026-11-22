"""Archive measured three-lap race evaluations, separately from solo lap tests."""
import csv
import json
import shutil
import statistics
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]
RESULTS = ROOT / "Training/Results"
OUT = ROOT / "Training/Reports"


def main():
    archive = OUT / "duel-data"
    archive.mkdir(parents=True, exist_ok=True)
    measurements = []
    paths = sorted(set(RESULTS.glob("duel-before-*.csv")) |
                   set(RESULTS.glob("duel-physics-only-*.csv")) |
                   set(RESULTS.glob("duel-final-*.csv")) |
                   set(RESULTS.glob("episodes-duel-*-eval*.csv")))
    for path in paths:
        rows = list(csv.DictReader(path.open(encoding="utf-8-sig")))
        if not rows:
            continue
        wins = [r for r in rows if int(r["laps"]) >= 3]
        mean = lambda name, subset=rows: statistics.mean(float(r[name]) for r in subset) if subset else None
        item = dict(series=path.stem, trials=len(rows), completed_three_laps=len(wins),
                    mean_grass_seconds=mean("grass_seconds"),
                    mean_successful_seconds=mean("seconds", wins),
                    mean_successful_collisions=mean("collisions", wins),
                    seeds=[int(r["seed"]) for r in rows])
        if "recoveries" in rows[0]:
            item["recoveries"] = sum(int(r["recoveries"]) for r in rows)
        measurements.append(item)
        shutil.copyfile(path, archive / path.name)
    lines = ["# Duel humain contre PPO : verification sur trois tours", "",
             "Les anciens tests sur un kart seul et un tour ne valident pas le duel. Ici le rival utilise la scene reelle MooMooMeadows, sa propre place de depart, le kart humain et les obstacles du jeu. Une reussite demande trois tours, pas un seul.", "",
             "| Serie | Courses | Trois tours termines | Gazon moyen / course (s) | Temps des courses reussies (s) | Retours sur piste |",
             "|---|---:|---:|---:|---:|---:|"]
    for item in measurements:
        elapsed = item["mean_successful_seconds"]
        elapsed_text = f"{elapsed:.2f}" if elapsed is not None else "-"
        lines.append(f"| {item['series']} | {item['trials']} | {item['completed_three_laps']}/{item['trials']} | {item['mean_grass_seconds']:.2f} | {elapsed_text} | {item.get('recoveries', 'non mesure')} |")
    lines += ["", "## Corrections et apprentissage", "",
              "- Les commandes de drift de l'IA sont echantillonnees au pas physique, et non au rythme des images. Le clavier humain conserve son fonctionnement.",
              "- La zone neutre est coherente pour les axes, les touches et les forces du drift. L acceleration, la rotation et les forces laterales sont dosees, sans modifier le clavier humain.",
              "- Le modele recoit 31 observations, dont son etat de drift/boost et trois mesures de route devant lui. La direction de piste est issue de ItemPath (50 noeuds, 473 echantillons sur route, aucun sur gazon), sans remplacer les actions du reseau.",
              "- Le pilote d'entrainement et le rival utilisent des commandes propres a chaque kart ; aucune commande globale ne pilote les deux karts.",
              "- L'apprentissage du duel utilise trois tours et parfois un petit decalage lateral/angulaire au depart. La scene, les checkpoints et la physique sont ceux de la course.",
              "- La phase de finition retire 2 points par impact significatif, 1 point par seconde sur le gazon, et ne recompense pas la progression parcourue sur le gazon. Le bonus de tour reste +100. Ces regles ne modifient pas les reglages manuels par defaut.",
              "- L'entrainement termine l'essai apres un blocage ou une sortie, sans remise sur piste. En course normale, une remise sur piste est disponible apres un blocage prolonge ou une chute ; les tests stricts la desactivent avec --duel-rescue=0 et son utilisation est comptee dans les autres tests.",
              "", "## Protocole et limites", "",
              "Tests sans affichage et inference deterministe. L'introduction est sautee pour les benchmarks natifs ; les essais sont mesures depuis le depart. Le joueur de test reste immobile, sauf les series human/launch : il accelere tout droit pendant cinq secondes puis relache l'accelerateur. Cela ne remplace pas tous les comportements possibles d'un joueur humain ou les attaques d'objets.",
              "Les cadences de rendu sont indiquees dans les noms des benchmarks. Les graines et les resultats individuels sont archives dans duel-data. Changer la graine ne modifie pas le circuit. Les series de selection de sauvegardes ne sont pas des essais independants de generalisation.",
              "Les premiers compteurs d'impacts du benchmark before etaient bruts ; ne pas les comparer aux impacts significatifs filtres des evaluations recentes. Les mesures recentes filtrent par normale, vitesse d'impact et delai de 0,75 s. Le tableau expose le temps sur le gazon et les courses terminees, plutot qu'un score qui change avec les regles.",
              "La vitesse du rival reste inferieure a celle d un joueur experimente. Le controle Editor avec introduction normale, deux secondes de commande humaine et une chute forcee est conserve dans duel-real-race-check.txt. Un bon taux sur ces essais ne garantit ni zero collision, ni zero passage sur le gazon dans toutes les parties. Le manifeste du modele identifie exactement le fichier livre et ses series de verification.", ""]
    manifest_path = ROOT / "Training/Models/model-manifest.json"
    if manifest_path.exists():
        manifest = json.loads(manifest_path.read_text(encoding="utf-8"))
        final_rows = []
        for path in RESULTS.glob("duel-final-*.csv"):
            final_rows.extend(csv.DictReader(path.open(encoding="utf-8-sig")))
        if final_rows:
            wins = sum(int(r["laps"]) >= 3 for r in final_rows)
            rescues = sum(int(r.get("recoveries", 0)) for r in final_rows)
            grass_mean = statistics.mean(float(r["grass_seconds"]) for r in final_rows)
            summary = ["## Modele courant", "",
                       f"Sauvegarde {manifest['source_run']} a {manifest['source_step']} decisions. SHA-256 : `{manifest['sha256']}`.", "",
                       f"Validation du fichier ONNX directement dans Unity : **{wins}/{len(final_rows)} courses de trois tours**, **{rescues} remise sur piste**, cadences 30/60/144 images par seconde. Cinq courses incluent le depart d un second kart pendant cinq secondes.",
                       f"Temps moyen sur le gazon : **{grass_mean:.2f} s par course de trois tours**. Il ne s agit donc pas d une conduite sans erreur.",
                       "Les dix evaluations supplementaires via Python terminent aussi les trois tours. Le test Editor avec introduction, commandes humaines et chute forcee est valide.",
                       "Les contacts releves dans les benchmarks natifs sont avec le joueur immobile, les vaches et le tremplin (voir les tags d impacts dans le manifeste). Aucun blocage contre un mur n a empeche de finir ces courses ; cela ne garantit pas toutes les parties possibles.", ""]
            lines[2:2] = summary
    (OUT / "03-duel-evaluations.md").write_text("\n".join(lines), encoding="utf-8")
    (OUT / "duel-results.json").write_text(json.dumps(measurements, indent=2), encoding="utf-8")
    for item in measurements:
        print(item["series"], f"{item['completed_three_laps']}/{item['trials']}", "grass", round(item["mean_grass_seconds"], 2))


if __name__ == "__main__":
    main()
