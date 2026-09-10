#!/usr/bin/env python3
"""Summarize exported Kael body measurements without starting Unity or changing inputs.

Usage: python scripts/summarize-kael-body-review.py [capture-folder] [--label TEXT]
Only completed manifests are read. This is geometry evidence, never visual approval.
"""

import argparse
from collections import Counter
from datetime import datetime, timezone
import hashlib
import json
import math
from pathlib import Path
import sys


def digest(data):
    return hashlib.sha256(data).hexdigest()


def distance(a, b):
    return math.dist([a[k] for k in "xyz"], [b[k] for k in "xyz"])


def matrix_delta(a, b):
    if len(a) != 16 or len(b) != 16:
        raise ValueError("Expected two 16-element transform matrices")
    return max(abs(x - y) for x, y in zip(a, b))


def unique(items, key, value):
    matches = [item for item in items if item.get(key) == value]
    if len(matches) != 1:
        raise ValueError(f"Expected exactly one {key}={value}, found {len(matches)}")
    return matches[0]


def summarize(folder, label):
    folder = folder.resolve(strict=True)
    source_bytes = {}

    def read(name):
        path = (folder / name).resolve(strict=True)
        if not path.is_relative_to(folder):
            raise ValueError(f"Manifest path escapes capture folder: {name}")
        key = path.relative_to(folder).as_posix()
        if key in source_bytes:
            raise ValueError(f"Duplicate manifest input: {key}")
        source_bytes[key] = path.read_bytes()
        return json.loads(source_bytes[key].decode("utf-8-sig"))

    manifest = read("manifest.json")
    if manifest.get("captureStatus") != "complete":
        raise ValueError("Capture manifest is not complete; do not summarize an in-progress export")
    poses = [(entry["data"], read(entry["data"])) for entry in manifest["poses"]]
    rows = [(name, pose, gap) for name, pose in poses for gap in pose["endpointGaps"]]
    pairs = sorted({gap["name"] for _, _, gap in rows})
    kinds = ("renderer", "skinned")
    availability = {}
    recomputed = {}
    available_rows = {}
    for kind in kinds:
        available_rows[kind] = [row for row in rows if row[2][kind + "MeasurementAvailable"]]
        availability[kind] = {
            "available": len(available_rows[kind]),
            "unavailable": [
                {"pose": name, "pair": gap["name"], "status": gap.get("status", "")}
                for name, _, gap in rows if not gap[kind + "MeasurementAvailable"]
            ],
        }
        differences = []
        for name, _, gap in available_rows[kind]:
            measured = gap[kind + "DistanceWorld"]
            if not math.isfinite(measured) or measured < 0:
                raise ValueError(f"Invalid {kind} distance in {name}: {gap['name']}")
            calculated = distance(gap[kind + "EndpointA"], gap[kind + "EndpointB"])
            differences.append(abs(calculated - measured))
        recomputed[kind] = max(differences, default=None)

    def maximum(kind, candidates):
        if not candidates:
            return None
        name, pose, gap = max(candidates, key=lambda row: row[2][kind + "DistanceWorld"])
        return {
            "pose": name,
            "clipOrPoseLabel": pose["clip"],
            "timeSeconds": pose["requestedTime"],
            "pair": gap["name"],
            "distanceWorld": gap[kind + "DistanceWorld"],
            "distanceCapturePixels": gap[kind + "DistancePixels"],
        }

    maxima = {kind: maximum(kind, available_rows[kind]) for kind in kinds}
    neutral = {}
    for name, pose in poses:
        if pose.get("neutralAssembly"):
            key = (pose["neutralView"], pose["headNodDegrees"])
            if key in neutral:
                raise ValueError(f"Duplicate neutral pose: {key}")
            neutral[key] = (name, pose)
    nods = []
    for (view, angle), (name, pose) in sorted(neutral.items()):
        if angle == 0:
            continue
        _, baseline = neutral[(view, 0)]
        torso = unique(pose["hierarchy"], "name", "Torso")
        base_torso = unique(baseline["hierarchy"], "name", "Torso")
        torso_renderer = unique(pose["renderers"], "layoutBone", "Torso")
        base_renderer = unique(baseline["renderers"], "layoutBone", "Torso")
        head = unique(pose["hierarchy"], "name", "Head")
        base_head = unique(baseline["hierarchy"], "name", "Head")
        neck = unique(pose["endpointGaps"], "name", "neck")
        nods.append({
            "view": view, "pose": name, "requestedHeadNodDegrees": angle,
            "recordedHeadLocalZDegrees": (head["localEulerAngles"]["z"] + 180) % 360 - 180,
            "torsoBoneMatrixMaxDelta": matrix_delta(torso["localToWorldMatrix"], base_torso["localToWorldMatrix"]),
            "torsoRendererMatrixMaxDelta": matrix_delta(torso_renderer["localToWorldMatrix"], base_renderer["localToWorldMatrix"]),
            "torsoWorldPositionDelta": distance(torso["worldPosition"], base_torso["worldPosition"]),
            "headPivotWorldPositionDelta": distance(head["worldPosition"], base_head["worldPosition"]),
            "headMatrixMaxDelta": matrix_delta(head["localToWorldMatrix"], base_head["localToWorldMatrix"]),
            "neckRendererGapWorld": neck["rendererDistanceWorld"] if neck["rendererMeasurementAvailable"] else None,
            "neckSkinnedGapWorld": neck["skinnedDistanceWorld"] if neck["skinnedMeasurementAvailable"] else None,
        })
    torso_matrix_max = max((max(n["torsoBoneMatrixMaxDelta"], n["torsoRendererMatrixMaxDelta"]) for n in nods), default=None)
    torso_position_max = max((n["torsoWorldPositionDelta"] for n in nods), default=None)

    def describe_max(kind):
        value = maxima[kind]
        if value is None:
            return "keine Messung"
        return (f"{value['distanceWorld']:.9g} Welteinheiten / {value['distanceCapturePixels']:.9g} Aufnahmepixel "
                f"({value['pose']}, {value['pair']})")

    assessment = (
        f"{len(poses)} Posen, {len(rows)} Endpunktpaare. "
        f"Renderer-Messungen {availability['renderer']['available']}/{len(rows)} und "
        f"Skin-Messungen {availability['skinned']['available']}/{len(rows)} verf\u00fcgbar. "
        f"Maximum Renderer: {describe_max('renderer')}; Maximum Skin: {describe_max('skinned')}. "
    )
    if nods:
        assessment += (f"Bei {len(nods)} Kopfneigungen betr\u00e4gt die maximale Torso-Matrix\u00e4nderung "
                       f"{torso_matrix_max:.9g}, die maximale Torso-Positions\u00e4nderung {torso_position_max:.9g}. ")
    assessment += ("Dies misst registrierte Endpunkte und Transformationen. Gemalte Konturen, Alpha-Abdeckung, "
                   "\u00dcberlappung und Bewegungsqualit\u00e4t sind damit nicht freigegeben. "
                   "Es wird keine Toleranz und kein gestalterisches Bestanden-Ergebnis gesetzt.")
    report = {
        "schemaVersion": 1,
        "generatedUtc": datetime.now(timezone.utc).isoformat(),
        "source": {
            "folder": str(folder), "captureUtc": manifest.get("utc"), "captureStatus": manifest["captureStatus"],
            "manifestSha256": digest(source_bytes["manifest.json"]),
            "reviewIdentification": "Current generated body review", "label": label,
        },
        "scope": "Quantitative endpoint and transform analysis only. No image review, visual approval, runtime gameplay approval, or tolerance-based pass rating.",
        "poseCount": len(poses),
        "poseClasses": {
            "unanimatedBind": sum(bool(p.get("unanimatedBindPose")) for _, p in poses),
            "neutral": sum(bool(p.get("neutralAssembly")) for _, p in poses),
            "authoredAnimation": sum(not p.get("unanimatedBindPose") and not p.get("neutralAssembly") for _, p in poses),
            "transitionSequenceSnapshots": sum(bool(p.get("transitionSequence")) for _, p in poses),
        },
        "captureDimensions": [manifest["imageWidth"], manifest["imageHeight"]],
        "orthographicSize": manifest["orthographicSize"],
        "pairNames": pairs, "pairCount": len(pairs), "measurementPairCount": len(rows),
        "perPoseGapCountDistribution": dict(sorted(Counter(str(len(p["endpointGaps"])) for _, p in poses).items())),
        "availability": availability, "globalMaxima": maxima,
        "pairMaxima": [{"pair": pair, **{kind: maximum(kind, [row for row in available_rows[kind] if row[2]["name"] == pair]) for kind in kinds}} for pair in pairs],
        "headNodTorsoChecks": nods,
        "headNodTorsoMaximumMatrixDelta": torso_matrix_max,
        "headNodTorsoMaximumPositionDelta": torso_position_max,
        "headNodMaximumNeckGapWorld": max((n["neckSkinnedGapWorld"] for n in nods if n["neckSkinnedGapWorld"] is not None), default=None),
        "recordedDistanceRecalculationMaxDifference": recomputed,
        "sourceFilesSha256": {name: digest(data) for name, data in source_bytes.items()},
        "assessmentDe": assessment,
    }
    # Protect against a concurrent Unity export. Never modify pose files or the manifest.
    for name, original in source_bytes.items():
        if (folder / name).read_bytes() != original:
            raise RuntimeError(f"Source changed while being analyzed: {name}; rerun after capture completes")
    return report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    default_folder = Path(__file__).resolve().parents[1] / "artifacts/kael/body-fix-review/current"
    parser.add_argument("folder", nargs="?", type=Path, default=default_folder)
    parser.add_argument("--output", type=Path)
    parser.add_argument("--label", default="current exported capture")
    args = parser.parse_args()
    report = summarize(args.folder, args.label)
    output = (args.output or (args.folder.resolve().parent / "body-assembly-measurements.json")).resolve()
    source_folder = args.folder.resolve()
    if output.is_relative_to(source_folder):
        raise ValueError("Write the summary outside the capture folder to preserve source evidence")
    output.parent.mkdir(parents=True, exist_ok=True)
    output.write_text(json.dumps(report, ensure_ascii=False, indent=2, allow_nan=False) + "\n", encoding="utf-8")
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    print(report["assessmentDe"])
    print(f"Summary: {output}")


if __name__ == "__main__":
    main()
