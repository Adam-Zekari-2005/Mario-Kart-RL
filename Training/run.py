"""Project-local entry point. Uses the Python environment selected by the caller."""
import argparse
import csv
import json
import os
from pathlib import Path
import statistics
import subprocess
import sys
import time
import yaml

ROOT = Path(__file__).resolve().parents[1]
EXE = ROOT / "Builds/TrainingHeadless/KartTrainingHeadless.exe"
RESULTS = ROOT / "Training/Results"


def summarize():
    paths = sorted(RESULTS.glob("episodes*.csv"))
    if not paths:
        raise SystemExit("Aucun resultat : lancer d'abord des essais.")
    groups = {}
    for path in paths:
        with path.open(encoding="utf-8-sig", newline="") as stream:
            for row in csv.DictReader(stream):
                groups.setdefault((row["run"], row["mode"], row["track"]), []).append(row)
    lines = ["# Resultats des essais", "", "Les temps de tour concernent uniquement les tours termines. Un objectif partiel n'est pas un tour termine. Une serie courte ne prouve pas la regularite.", "", "| Serie | Mode | Circuit | Essais | Tours termines | Score moyen | Checkpoints moyens | Chocs moyens | Temps moyen des tours (s) | Objectifs partiels atteints |", "|---|---|---|---:|---:|---:|---:|---:|---:|---:|"]
    data = []
    for (run, mode, track), rows in groups.items():
        mean = lambda key: statistics.mean(float(row[key]) for row in rows)
        completed = [row for row in rows if int(row["laps"]) > 0]
        lap_time = statistics.mean(float(row["seconds"]) for row in completed) if completed else None
        goals = sum(row.get("reason", "") == "objectif checkpoints atteint" for row in rows)
        partial = f"{goals}/{len(rows)}" if any(int(row.get("checkpoint_goal") or 0) > 0 for row in rows) else "-"
        lines.append(f"| {run} | {mode} | {track} | {len(rows)} | {len(completed)}/{len(rows)} | {mean('score'):.2f} | {mean('checkpoints'):.2f} | {mean('collisions'):.2f} | {lap_time if lap_time is not None else '-'} | {partial} |")
        data.append(dict(run=run, mode=mode, track=track, episodes=len(rows), completed=len(completed), goals_reached=goals, mean_score=mean("score"), mean_checkpoints=mean("checkpoints"), mean_collisions=mean("collisions"), mean_successful_lap_seconds=lap_time))
    RESULTS.mkdir(parents=True, exist_ok=True)
    (RESULTS / "comparison.md").write_text("\n".join(lines) + "\n", encoding="utf-8")
    (RESULTS / "comparison.json").write_text(json.dumps(data, indent=2), encoding="utf-8")
    print("\n".join(lines))


def main():
    global EXE
    parser = argparse.ArgumentParser(description="Entrainement et comparaison du kart")
    parser.add_argument("mode", choices=["connection", "train", "baseline", "evaluate", "model", "summarize"])
    parser.add_argument("--run-id", default="kart-first")
    parser.add_argument("--source", help="Serie a evaluer, ou poids initiaux d'un nouvel entrainement")
    parser.add_argument("--track", choices=["moo", "mario"], default="moo")
    parser.add_argument("--episodes", type=int, default=10)
    parser.add_argument("--steps", type=int, help="Limiter le premier essai d'entrainement")
    parser.add_argument("--seed", type=int, default=42)
    parser.add_argument("--resume", action="store_true")
    parser.add_argument("--graphics", action="store_true")
    parser.add_argument("--editor", action="store_true", help="Entrainement visible dans Unity (sans executable)")
    parser.add_argument("--profile", choices=["original", "balanced"], default="original")
    parser.add_argument("--goal", type=int, default=6, help="Nombre de checkpoints de l'objectif partiel ; 0 = tour complet")
    parser.add_argument("--base-port", type=int, help="Port de connexion local (5004 editeur, 5010 executable par defaut)")
    args = parser.parse_args()
    if args.editor and args.mode not in ("train", "evaluate"):
        parser.error("--editor est reserve aux modes train et evaluate")
    if args.graphics and args.mode != "connection":
        EXE = ROOT / "Builds/Training/KartTraining.exe"
    os.chdir(ROOT)
    if args.mode == "summarize":
        summarize()
        return
    RESULTS.mkdir(parents=True, exist_ok=True)
    if args.editor:
        (RESULTS / "editor-session.json").write_text(json.dumps(dict(run_id=args.run_id, seed=args.seed, track=args.track, mode=args.mode, profile=args.profile, goal=args.goal, episodes=args.episodes if args.mode == "evaluate" else 0)), encoding="utf-8")
    common = [f"--kart-run={args.run_id}", f"--kart-track={args.track}", f"--kart-seed={args.seed}", f"--kart-output={RESULTS}", f"--kart-profile={args.profile}", f"--kart-goal={args.goal}"]
    if args.mode in ("baseline", "model"):
        if not EXE.exists():
            parser.error("Compiler d'abord le jeu IA avec le menu Unity.")
        direct_mode = "random" if args.mode == "baseline" else "model"
        cmd = [str(EXE), "-batchmode", "-noaudio", "-logFile", str(RESULTS / f"{args.run_id}-player.log"), f"--kart-mode={direct_mode}", f"--kart-episodes={args.episodes}", *common]
        if not args.graphics:
            cmd.append("-nographics")
    else:
        config_name = "connection_3dball.yaml" if args.mode == "connection" else ("kart_balanced_ppo.yaml" if args.profile == "balanced" else "kart_ppo.yaml")
        config = ROOT / "Training/config" / config_name
        if args.steps or args.editor:
            settings = yaml.safe_load(config.read_text(encoding="utf-8-sig"))
            for behavior in settings["behaviors"].values():
                if args.steps:
                    behavior["max_steps"] = args.steps
                    behavior["checkpoint_interval"] = min(behavior["checkpoint_interval"], args.steps)
                if args.editor:
                    behavior["hyperparameters"]["buffer_size"] = 512
                    behavior["hyperparameters"]["batch_size"] = 128
                    behavior["summary_freq"] = 256
            if args.editor:
                settings["engine_settings"]["time_scale"] = 1
                settings["engine_settings"]["no_graphics"] = False
            config = RESULTS / f"{args.run_id}-config.yaml"
            config.write_text(yaml.safe_dump(settings), encoding="utf-8")
        exe = ROOT / "Builds/TrainingSample/Connection3DBall.exe" if args.mode == "connection" else EXE
        if not args.editor and not exe.exists():
            parser.error("Executable absent : compiler les scenes avec Unity.")
        cmd = [sys.executable, "-m", "mlagents.trainers.learn", str(config), "--run-id", args.run_id, "--seed", str(args.seed), "--results-dir", str(ROOT / "results"), "--torch-device", "cpu", "--max-lifetime-restarts", "0"]
        cmd.extend(["--base-port", str(args.base_port if args.base_port is not None else (5004 if args.editor else 5010))])
        if not args.editor:
            cmd.extend(["--env", str(exe)])
        else:
            cmd.extend(["--timeout-wait", "600"])
        if not args.graphics and not args.editor:
            cmd.append("--no-graphics")
        if args.resume:
            cmd.append("--resume")
        elif args.mode == "train" and args.source:
            cmd.extend(["--initialize-from", args.source])
        if args.mode == "evaluate":
            if not args.source:
                parser.error("--source doit indiquer la serie entrainee")
            cmd.extend(["--inference", "--deterministic", "--initialize-from", args.source, "--time-scale", "1"])
        if args.mode != "connection" and not args.editor:
            cmd.extend(["--env-args", "-noaudio", *common, f"--kart-mode={'evaluate' if args.mode == 'evaluate' else 'train'}"])
            if args.mode == "evaluate":
                cmd.append(f"--kart-episodes={args.episodes}")
    print("Lancement :", subprocess.list2cmdline(cmd), flush=True)
    log_path = RESULTS / f"{args.run_id}.log"
    with log_path.open("a", encoding="utf-8") as log:
        with subprocess.Popen(cmd, cwd=ROOT, stdout=subprocess.PIPE, stderr=subprocess.STDOUT,
                              text=True, encoding="utf-8", errors="replace", bufsize=1) as process:
            for line in process.stdout:
                print(line, end="", flush=True)
                log.write(line)
                log.flush()
                if args.editor and "Listening on port" in line:
                    (RESULTS / "start-preview.request").write_text(f"{args.run_id}-{time.time_ns()}", encoding="utf-8")
            returncode = process.wait()
    if args.editor:
        (RESULTS / "start-preview.request").write_text(f"stop:{args.run_id}-{time.time_ns()}", encoding="utf-8")
    if args.mode == "evaluate" and returncode:
        episode_path = RESULTS / f"episodes-{args.run_id}.csv"
        if episode_path.exists():
            with episode_path.open(encoding="utf-8-sig", newline="") as stream:
                finished = list(csv.DictReader(stream))
            if len(finished) >= args.episodes:
                print("Evaluation terminee : le jeu s'est arrete apres le nombre d'essais demande.")
                returncode = 0
    if list(RESULTS.glob("episodes*.csv")):
        summarize()
    raise SystemExit(returncode)

if __name__ == "__main__":
    main()
