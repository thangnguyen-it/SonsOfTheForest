import argparse
import json
import math
import os
import sys

import bpy
from mathutils import Vector


def visible_bounds(objects):
    corners = []
    for obj in objects:
        if obj.hide_render:
            continue
        corners.extend(obj.matrix_world @ Vector(corner) for corner in obj.bound_box)
    if not corners:
        raise RuntimeError("Turntable source has no visible mesh bounds.")
    minimum = Vector((min(value.x for value in corners), min(value.y for value in corners),
                      min(value.z for value in corners)))
    maximum = Vector((max(value.x for value in corners), max(value.y for value in corners),
                      max(value.z for value in corners)))
    return (minimum + maximum) * 0.5, maximum - minimum


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", required=True)
    parser.add_argument("--manifest", required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:])
    root = os.path.realpath(args.project_root)
    build_report_path = os.path.join(root, "Artifacts", "ForestPipeline", "build.json")
    with open(build_report_path, "r", encoding="utf-8") as stream:
        build = json.load(stream)
    target = os.path.join(root, build["variants"][0]["path"])
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)
    bpy.ops.import_scene.fbx(filepath=target)
    objects = [obj for obj in bpy.context.scene.objects if obj.type == "MESH"]
    for obj in objects:
        obj.hide_render = not (obj.name.startswith("Upper_LOD0") or obj.name.startswith("Stump_LOD0") or obj.name == "Seam_Intact")
    center, size = visible_bounds(objects)
    radius = max(size.x, size.y, size.z) * 0.5
    distance = max(4.0, radius / math.tan(math.radians(24.0)) * 1.12)
    bpy.ops.object.camera_add(location=(center.x + distance, center.y - distance, center.z))
    camera = bpy.context.object
    bpy.context.scene.camera = camera
    camera.data.lens = 50.0
    direction = center - camera.location
    camera.rotation_euler = direction.to_track_quat("-Z", "Y").to_euler()
    bpy.ops.object.light_add(type="AREA", location=(4.0, -4.0, 10.0))
    bpy.context.object.data.energy = 1600.0
    bpy.context.object.data.shape = "DISK"
    bpy.context.object.data.size = 6.0
    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE_NEXT"
    scene.render.resolution_x = 768
    scene.render.resolution_y = 768
    scene.render.resolution_percentage = 100
    preview_root = os.path.join(root, "Artifacts", "ForestPipeline", "Turntable")
    os.makedirs(preview_root, exist_ok=True)
    frames = []
    for index in range(8):
        angle = math.tau * index / 8.0
        camera.location = (center.x + math.cos(angle) * distance,
                           center.y + math.sin(angle) * distance,
                           center.z)
        camera.rotation_euler = (center - camera.location).to_track_quat("-Z", "Y").to_euler()
        path = os.path.join(preview_root, "frame_%02d.png" % index)
        scene.render.filepath = path
        bpy.ops.render.render(write_still=True)
        frames.append(os.path.relpath(path, root).replace("\\", "/"))
    with open(args.output, "w", encoding="utf-8", newline="\n") as stream:
        json.dump({"schemaVersion": 1, "source": build["variants"][0]["path"], "frames": frames}, stream, indent=2)
        stream.write("\n")


if __name__ == "__main__":
    main()
