import argparse
import json
import os
import sys


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", required=True)
    parser.add_argument("--manifest", required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:])
    root = os.path.realpath(args.project_root)
    build_report_path = os.path.join(root, "Artifacts", "ForestPipeline", "build.json")
    if not os.path.isfile(build_report_path):
        raise RuntimeError("Build report is missing: " + build_report_path)
    with open(args.manifest, "r", encoding="utf-8") as stream:
        manifest = json.load(stream)
    with open(build_report_path, "r", encoding="utf-8") as stream:
        build = json.load(stream)
    expected = {item["variantId"] for item in manifest["variants"]}
    actual = {item["variantId"] for item in build["variants"]}
    errors = []
    if actual != expected:
        errors.append("Variant set differs from manifest.")
    for entry in build["variants"] + build["logs"] + [build["axe"], build["chips"]]:
        path = os.path.realpath(os.path.join(root, entry["path"]))
        if os.path.commonpath([root, path]) != root or not os.path.isfile(path):
            errors.append("Missing or escaping output: " + entry["path"])
        if len(entry.get("checksum", "")) != 64:
            errors.append("Invalid geometry checksum: " + entry.get("path", ""))
    report = {"schemaVersion": 1, "valid": not errors, "errors": errors,
              "variantCount": len(actual), "logSpeciesCount": len(build["logs"]),
              "chipMeshCount": len(build["chips"].get("objects", []))}
    output = os.path.realpath(args.output)
    os.makedirs(os.path.dirname(output), exist_ok=True)
    with open(output, "w", encoding="utf-8", newline="\n") as stream:
        json.dump(report, stream, indent=2, sort_keys=True)
        stream.write("\n")
    if errors:
        raise RuntimeError("; ".join(errors))


if __name__ == "__main__":
    main()
