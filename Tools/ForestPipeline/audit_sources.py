import argparse
import json
import os
import sys

import bpy


def reset_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", required=True)
    parser.add_argument("--manifest", required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:])

    root = os.path.realpath(args.project_root)
    with open(args.manifest, "r", encoding="utf-8") as stream:
        manifest = json.load(stream)

    sources = {}
    for variant in manifest["variants"]:
        sources.setdefault(variant["sourceFbx"], []).append(variant)

    report = {"schemaVersion": 1, "sources": []}
    for relative, variants in sorted(sources.items()):
        source = os.path.realpath(os.path.join(root, relative))
        if os.path.commonpath([root, source]) != root or not os.path.isfile(source):
            raise RuntimeError("Missing or escaping source FBX: " + relative)
        reset_scene()
        bpy.ops.import_scene.fbx(filepath=source)
        meshes = []
        for obj in sorted((o for o in bpy.context.scene.objects if o.type == "MESH"), key=lambda o: o.name):
            meshes.append({
                "object": obj.name,
                "mesh": obj.data.name,
                "vertices": len(obj.data.vertices),
                "polygons": len(obj.data.polygons),
                "materials": [slot.material.name if slot.material else "" for slot in obj.material_slots],
                "dimensions": [round(value, 6) for value in obj.dimensions],
            })
        report["sources"].append({"path": relative, "requested": [v["sourceMesh"] for v in variants], "meshes": meshes})

    output = os.path.realpath(args.output)
    os.makedirs(os.path.dirname(output), exist_ok=True)
    with open(output, "w", encoding="utf-8", newline="\n") as stream:
        json.dump(report, stream, indent=2, sort_keys=True)
        stream.write("\n")


if __name__ == "__main__":
    main()
