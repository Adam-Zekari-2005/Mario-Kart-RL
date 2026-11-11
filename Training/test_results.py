import contextlib
import csv
import importlib.util
import io
import json
from pathlib import Path
import tempfile
import unittest

spec = importlib.util.spec_from_file_location("kart_run", Path(__file__).with_name("run.py"))
run = importlib.util.module_from_spec(spec)
spec.loader.exec_module(run)

class ResultsChecks(unittest.TestCase):
    def test_failed_trials_do_not_lower_successful_lap_time(self):
        with tempfile.TemporaryDirectory() as directory:
            previous = run.RESULTS
            run.RESULTS = Path(directory)
            try:
                rows = [
                    dict(run="test", mode="evaluate", track="MooMooTraining", score="100", seconds="30", checkpoints="21", laps="1", collisions="0"),
                    dict(run="test", mode="evaluate", track="MooMooTraining", score="-10", seconds="15", checkpoints="0", laps="0", collisions="2"),
                    dict(run="test", mode="evaluate", track="MarioCircuitTraining", score="-10", seconds="15", checkpoints="1", laps="0", collisions="1"),
                ]
                with (run.RESULTS / "episodes.csv").open("w", newline="", encoding="utf-8") as stream:
                    writer = csv.DictWriter(stream, fieldnames=rows[0].keys())
                    writer.writeheader()
                    writer.writerows(rows)
                with contextlib.redirect_stdout(io.StringIO()):
                    run.summarize()
                summary = json.loads((run.RESULTS / "comparison.json").read_text())
                moo = next(group for group in summary if group["track"] == "MooMooTraining")
                mario = next(group for group in summary if group["track"] == "MarioCircuitTraining")
                self.assertEqual(moo["episodes"], 2)
                self.assertEqual(moo["completed"], 1)
                self.assertEqual(moo["mean_successful_lap_seconds"], 30)
                self.assertIsNone(mario["mean_successful_lap_seconds"])
                self.assertEqual(moo["mean_collisions"], 1)
            finally:
                run.RESULTS = previous

    def test_partial_goal_does_not_count_as_a_completed_lap(self):
        with tempfile.TemporaryDirectory() as directory:
            previous = run.RESULTS
            run.RESULTS = Path(directory)
            try:
                row = dict(run="lesson", mode="evaluate", track="MooMooTraining", score="30", seconds="10", checkpoints="6", laps="0", collisions="0", reason="objectif checkpoints atteint", checkpoint_goal="6")
                with (run.RESULTS / "episodes-lesson.csv").open("w", newline="", encoding="utf-8") as stream:
                    writer = csv.DictWriter(stream, fieldnames=row.keys())
                    writer.writeheader()
                    writer.writerow(row)
                with contextlib.redirect_stdout(io.StringIO()):
                    run.summarize()
                summary = json.loads((run.RESULTS / "comparison.json").read_text())[0]
                self.assertEqual(summary["completed"], 0)
                self.assertEqual(summary["goals_reached"], 1)
                self.assertIsNone(summary["mean_successful_lap_seconds"])
            finally:
                run.RESULTS = previous

if __name__ == "__main__":
    unittest.main()
