"""Extend the 24-input policy to 31 inputs without changing its initial outputs.

New input weights start at zero; old normalization and weights are preserved.
The critic is extended too. Optimizer moments are reset for the changed shape.
"""
import argparse
import copy
from pathlib import Path
import numpy as np
import torch
from mlagents_envs.base_env import ActionSpec, BehaviorSpec, ObservationSpec, ObservationType, DimensionProperty
from mlagents.trainers.policy.torch_policy import TorchPolicy
from mlagents.trainers.settings import NetworkSettings
from mlagents.trainers.torch_entities.networks import SimpleActor
from mlagents.trainers.torch_entities.model_serialization import ModelSerializer


def upgrade(state, old=24, new=31):
    result = copy.deepcopy(state)
    for name in ("Policy", "Optimizer:critic"):
        module = result[name]
        for key, value in list(module.items()):
            if key.endswith("running_mean") and value.shape == (old,):
                module[key] = torch.cat((value, torch.zeros(new - old)))
            elif key.endswith("running_variance") and value.shape == (old,):
                steps = module[key.replace("running_variance", "normalization_steps")].item()
                module[key] = torch.cat((value, torch.full((new - old,), float(steps))))
            elif key.endswith("seq_layers.0.weight") and value.shape[1] == old:
                module[key] = torch.cat((value, torch.zeros(value.shape[0], new - old)), dim=1)
    result.pop("Optimizer:value_optimizer", None)
    return result


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("source")
    parser.add_argument("destination")
    parser.add_argument("--export", help="ONNX output stem, without extension")
    args = parser.parse_args()
    state = torch.load(args.source, map_location="cpu")
    changed = upgrade(state)
    # Check preservation of the first hidden activation for arbitrary new inputs.
    before, after = state["Policy"], changed["Policy"]
    prefix = "network_body.processors.0.normalizer."
    def normalize(x, module):
        return torch.clamp((x - module[prefix + "running_mean"]) /
                           torch.sqrt(module[prefix + "running_variance"] / module[prefix + "normalization_steps"]), -5, 5)
    x = torch.randn(128, 24)
    first = "network_body._body_endoder.seq_layers.0.weight"
    torch.testing.assert_close(normalize(x, before) @ before[first].T,
                               normalize(torch.cat((x, torch.randn(128, 7)), 1), after) @ after[first].T)
    destination = Path(args.destination)
    destination.parent.mkdir(parents=True, exist_ok=True)
    torch.save(changed, destination)
    if args.export:
        spec = BehaviorSpec([ObservationSpec((31,), (DimensionProperty.NONE,), ObservationType.DEFAULT, "VectorSensor")], ActionSpec(2, np.array([2])))
        policy = TorchPolicy(42, spec, NetworkSettings(normalize=True, hidden_units=128, num_layers=2),
                             SimpleActor, {"conditional_sigma": False, "tanh_squash": False})
        policy.actor.load_state_dict(changed["Policy"], strict=True)
        ModelSerializer(policy).export_policy_model(args.export)
    print("PASS: 24 -> 31 inputs; original policy activations preserved; optimizer reset.")


if __name__ == "__main__":
    main()
