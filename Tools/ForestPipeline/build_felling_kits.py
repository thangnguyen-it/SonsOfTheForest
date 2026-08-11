import argparse
import hashlib
import json
import math
import os
import sys

import bpy
import bmesh
from mathutils import Vector


def reset_scene():
    bpy.ops.object.select_all(action="SELECT")
    bpy.ops.object.delete(use_global=False)


def safe_path(root, relative):
    value = os.path.realpath(os.path.join(root, relative))
    if os.path.commonpath([root, value]) != root:
        raise RuntimeError("Path escapes repository: " + relative)
    return value


def find_mesh(prefix, lod):
    expected = prefix + "_LOD" + str(lod)
    matches = [obj for obj in bpy.context.scene.objects
               if obj.type == "MESH" and obj.name == expected]
    if len(matches) != 1:
        raise RuntimeError("Expected one source object named %s, found %d" % (expected, len(matches)))
    return matches[0]


def normalize_tree(obj, species, target_height):
    bpy.context.view_layer.objects.active = obj
    obj.select_set(True)
    if species in ("Fir", "Maple"):
        obj.rotation_euler.x += math.radians(90.0)
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    minimum = min((obj.matrix_world @ Vector(corner)).z for corner in obj.bound_box)
    maximum = max((obj.matrix_world @ Vector(corner)).z for corner in obj.bound_box)
    height = maximum - minimum
    if height <= 0.001:
        raise RuntimeError("Source tree has invalid height: " + obj.name)
    scale = target_height / height
    obj.scale = (scale, scale, scale)
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    world_corners = [obj.matrix_world @ Vector(corner) for corner in obj.bound_box]
    minimum = min(corner.z for corner in world_corners)
    center_x = (min(corner.x for corner in world_corners) +
                max(corner.x for corner in world_corners)) * 0.5
    center_y = (min(corner.y for corner in world_corners) +
                max(corner.y for corner in world_corners)) * 0.5
    # Source packs arrange LOD meshes side-by-side in authoring space. Each
    # derived felling LOD must share one trunk origin in the exported kit.
    obj.location.x -= center_x
    obj.location.y -= center_y
    obj.location.z -= minimum
    bpy.ops.object.transform_apply(location=True, rotation=False, scale=False)


def clip_upper(source, cut_height, name):
    result = source.copy()
    result.data = source.data.copy()
    bpy.context.collection.objects.link(result)
    result.name = name
    mesh = result.data
    bm = bmesh.new()
    bm.from_mesh(mesh)
    geometry = list(bm.verts) + list(bm.edges) + list(bm.faces)
    bmesh.ops.bisect_plane(
        bm, geom=geometry, dist=0.00001,
        plane_co=Vector((0.0, 0.0, cut_height)),
        plane_no=Vector((0.0, 0.0, 1.0)),
        clear_inner=True, clear_outer=False)
    for vertex in bm.verts:
        vertex.co.z -= cut_height
    bm.to_mesh(mesh)
    bm.free()
    mesh.update()
    return result


def add_cylinder(name, radius, depth, segments, z, material_names, top_radius=None):
    top_radius = radius if top_radius is None else top_radius
    vertices = []
    faces = []
    for ring, ring_radius in enumerate((radius, top_radius)):
        ring_z = z + (depth if ring else 0.0)
        for index in range(segments):
            angle = index * math.tau / segments
            vertices.append((math.cos(angle) * ring_radius, math.sin(angle) * ring_radius, ring_z))
    for index in range(segments):
        nxt = (index + 1) % segments
        faces.append((index, nxt, segments + nxt, segments + index))
    faces.append(tuple(reversed(range(segments))))
    faces.append(tuple(range(segments, segments * 2)))
    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.materials.clear()
    for material_name in material_names:
        material = bpy.data.materials.get(material_name) or bpy.data.materials.new(material_name)
        mesh.materials.append(material)
    for polygon in mesh.polygons:
        polygon.material_index = 1 if len(material_names) > 1 and len(polygon.vertices) == segments else 0
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return obj


def add_notch_sleeve(name, radius, bottom, top, missing_degrees, segments=32):
    half_missing = math.radians(missing_degrees * 0.5)
    angles = []
    for index in range(segments + 1):
        angle = half_missing + (math.tau - 2.0 * half_missing) * index / segments
        angles.append(angle)
    verts = []
    for z in (bottom, top):
        for angle in angles:
            verts.append((math.cos(angle) * radius, math.sin(angle) * radius, z))
    count = len(angles)
    faces = []
    for index in range(count - 1):
        faces.append((index, index + 1, count + index + 1, count + index))
    # Two radial cut walls turn the missing arc into a genuine 3D cavity.
    verts.extend([(0.0, 0.0, bottom), (0.0, 0.0, top)])
    center_bottom, center_top = len(verts) - 2, len(verts) - 1
    faces.append((center_bottom, 0, count, center_top))
    faces.append((count - 1, center_bottom, center_top, count * 2 - 1))
    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(verts, [], faces)
    mesh.materials.append(bpy.data.materials.get("Bark") or bpy.data.materials.new("Bark"))
    mesh.materials.append(bpy.data.materials.get("Cut") or bpy.data.materials.new("Cut"))
    for polygon in mesh.polygons:
        polygon.material_index = 1 if polygon.index >= len(faces) - 2 else 0
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return obj


def add_cap(name, radius, z, normal_up, segments=32):
    vertices = [(0.0, 0.0, z)]
    for index in range(segments):
        angle = index * math.tau / segments
        vertices.append((math.cos(angle) * radius, math.sin(angle) * radius, z))
    faces = []
    for index in range(segments):
        nxt = (index + 1) % segments
        face = (0, index + 1, nxt + 1)
        faces.append(face if normal_up else tuple(reversed(face)))
    mesh = bpy.data.meshes.new(name + "_Mesh")
    mesh.from_pydata(vertices, [], faces)
    mesh.materials.append(bpy.data.materials.get("Cut") or bpy.data.materials.new("Cut"))
    obj = bpy.data.objects.new(name, mesh)
    bpy.context.collection.objects.link(obj)
    return obj


def mesh_checksum(objects):
    digest = hashlib.sha256()
    for obj in sorted(objects, key=lambda value: value.name):
        digest.update(obj.name.encode("utf-8"))
        for vertex in obj.data.vertices:
            digest.update(("%.6f,%.6f,%.6f;" % tuple(vertex.co)).encode("ascii"))
        for polygon in obj.data.polygons:
            digest.update((",".join(str(value) for value in polygon.vertices) + ";").encode("ascii"))
    return digest.hexdigest()


def export_selected(path, objects):
    os.makedirs(os.path.dirname(path), exist_ok=True)
    bpy.ops.object.select_all(action="DESELECT")
    for obj in objects:
        obj.select_set(True)
    bpy.context.view_layer.objects.active = objects[0]
    bpy.ops.export_scene.fbx(
        filepath=path, use_selection=True, apply_unit_scale=True,
        bake_space_transform=False, add_leaf_bones=False,
        mesh_smooth_type="FACE", use_mesh_modifiers=True,
        path_mode="AUTO", embed_textures=False)


def build_variant(root, output_root, variant):
    reset_scene()
    source = safe_path(root, variant["sourceFbx"])
    bpy.ops.import_scene.fbx(filepath=source)
    source_objects = []
    for lod in range(3):
        obj = find_mesh(variant["sourceMesh"], lod)
        normalize_tree(obj, variant["species"], float(variant["targetHeight"]))
        source_objects.append(obj)

    outputs = []
    cut_height = float(variant["cutHeight"])
    radius = float(variant["trunkRadius"])
    for lod, source_obj in enumerate(source_objects):
        upper = clip_upper(source_obj, cut_height, "Upper_LOD%d" % lod)
        outputs.append(upper)
    outputs.append(add_cylinder("Stump_LOD0", radius * 1.18, cut_height - 0.08, 32, 0.0, ["Bark", "Cut"], radius))
    outputs.append(add_cylinder("Stump_LOD1", radius * 1.16, cut_height - 0.08, 16, 0.0, ["Bark", "Cut"], radius))
    outputs.append(add_notch_sleeve("Seam_Intact", radius * 1.015, cut_height - 0.10, cut_height + 0.10, 0.0))
    outputs.append(add_notch_sleeve("Notch_Stage1", radius * 1.02, cut_height - 0.10, cut_height + 0.10, 42.0))
    outputs.append(add_notch_sleeve("Notch_Stage2", radius * 1.02, cut_height - 0.10, cut_height + 0.10, 76.0))
    outputs.append(add_notch_sleeve("Notch_Stage3", radius * 1.02, cut_height - 0.10, cut_height + 0.10, 112.0))
    outputs.append(add_cap("Upper_CutCap", radius, 0.0, False))
    outputs.append(add_cap("Stump_CutCap", radius, cut_height - 0.08, True))

    for obj in source_objects:
        bpy.data.objects.remove(obj, do_unlink=True)
    relative = os.path.join(output_root, "FellingKits", variant["species"], "KIT_" + variant["variantId"] + ".fbx")
    export_selected(safe_path(root, relative), outputs)
    return {"variantId": variant["variantId"], "path": relative.replace("\\", "/"),
            "checksum": mesh_checksum(outputs), "objects": [obj.name for obj in outputs]}


def build_logs(root, output_root, species):
    reset_scene()
    outputs = []
    for variant in range(2):
        radius = (0.31 + variant * 0.045) * (0.92 if species == "Fir" else 1.0)
        length = 2.55 + variant * 0.35
        outputs.append(add_cylinder("Log%d_LOD0" % (variant + 1), radius, length, 24, -length * 0.5, ["Bark", "Cut"], radius * 0.96))
        outputs.append(add_cylinder("Log%d_LOD1" % (variant + 1), radius, length, 12, -length * 0.5, ["Bark", "Cut"], radius * 0.96))
    relative = os.path.join(output_root, "Logs", species, "LOGS_" + species + ".fbx")
    export_selected(safe_path(root, relative), outputs)
    return {"species": species, "path": relative.replace("\\", "/"),
            "checksum": mesh_checksum(outputs), "objects": [obj.name for obj in outputs]}


def build_axe(root, output_root):
    reset_scene()
    outputs = []
    handle = add_cylinder("Axe_Handle", 0.027, 0.66, 16, -0.52, ["Handle"], 0.023)
    outputs.append(handle)
    bpy.ops.mesh.primitive_cube_add(location=(0.0, 0.0, 0.10), scale=(0.055, 0.10, 0.055))
    head = bpy.context.object
    head.name = "Axe_Head"
    head.data.materials.append(bpy.data.materials.new("Metal"))
    outputs.append(head)
    blade_vertices = [(-0.055, -0.02, 0.15), (-0.055, -0.02, 0.05),
                      (-0.055, -0.24, 0.02), (-0.055, -0.24, 0.18),
                      (0.055, -0.02, 0.15), (0.055, -0.02, 0.05),
                      (0.012, -0.24, 0.02), (0.012, -0.24, 0.18)]
    blade_faces = [(0,1,2,3),(4,7,6,5),(0,4,5,1),(3,2,6,7),(0,3,7,4),(1,5,6,2)]
    mesh = bpy.data.meshes.new("Axe_Blade_Mesh")
    mesh.from_pydata(blade_vertices, [], blade_faces)
    mesh.materials.append(bpy.data.materials.get("Metal"))
    blade = bpy.data.objects.new("Axe_Blade", mesh)
    bpy.context.collection.objects.link(blade)
    outputs.append(blade)
    relative = os.path.join(output_root, "Tools", "AXE_Survival.fbx")
    export_selected(safe_path(root, relative), outputs)
    return {"path": relative.replace("\\", "/"), "checksum": mesh_checksum(outputs),
            "objects": [obj.name for obj in outputs]}


def build_chips(root, output_root):
    reset_scene()
    outputs = []
    shapes = {
        "WoodChip": [(-0.018, 0.0, 0.0), (0.022, -0.006, 0.004),
                     (0.012, 0.008, 0.055), (-0.008, 0.004, 0.032)],
        "BarkChip": [(-0.026, -0.004, 0.0), (0.024, -0.006, 0.004),
                     (0.018, 0.005, 0.072), (-0.014, 0.007, 0.058)]
    }
    faces = [(0, 1, 2), (0, 2, 3), (0, 3, 1), (1, 3, 2)]
    for name, vertices in shapes.items():
        mesh = bpy.data.meshes.new(name + "_Mesh")
        mesh.from_pydata(vertices, [], faces)
        mesh.materials.append(bpy.data.materials.get("Cut") or bpy.data.materials.new("Cut"))
        obj = bpy.data.objects.new(name, mesh)
        bpy.context.collection.objects.link(obj)
        outputs.append(obj)
    relative = os.path.join(output_root, "Effects", "CHIPS_Harvest.fbx")
    export_selected(safe_path(root, relative), outputs)
    return {"path": relative.replace("\\", "/"), "checksum": mesh_checksum(outputs),
            "objects": [obj.name for obj in outputs]}


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--project-root", required=True)
    parser.add_argument("--manifest", required=True)
    parser.add_argument("--output", required=True)
    args = parser.parse_args(sys.argv[sys.argv.index("--") + 1:])
    root = os.path.realpath(args.project_root)
    with open(args.manifest, "r", encoding="utf-8") as stream:
        manifest = json.load(stream)
    output_root = manifest["outputRoot"]
    report = {"schemaVersion": 1, "generatorVersion": manifest["generatorVersion"],
              "variants": [], "logs": [], "axe": None, "chips": None}
    for variant in manifest["variants"]:
        report["variants"].append(build_variant(root, output_root, variant))
    for species in ("Pine", "Fir", "Maple"):
        report["logs"].append(build_logs(root, output_root, species))
    report["axe"] = build_axe(root, output_root)
    report["chips"] = build_chips(root, output_root)
    output = safe_path(root, os.path.relpath(args.output, root))
    os.makedirs(os.path.dirname(output), exist_ok=True)
    with open(output, "w", encoding="utf-8", newline="\n") as stream:
        json.dump(report, stream, indent=2, sort_keys=True)
        stream.write("\n")


if __name__ == "__main__":
    main()
