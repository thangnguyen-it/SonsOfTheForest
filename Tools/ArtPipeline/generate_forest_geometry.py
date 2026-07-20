"""Generate the project's independent, closed-surface forest geometry.

Run with Blender 4.5 LTS:
  blender --background --python Tools/ArtPipeline/generate_forest_geometry.py -- \
    Assets/_Game/Art/Models/World/Independent/ForestGeometry \
    tmp/forest_geometry_preview.png

The generator deliberately does not create foliage cards or alpha-cutout planes.
Every needle, grass blade and fern pinna has measurable volume and a closed mesh.
"""

from __future__ import annotations

import math
import os
import random
import sys
from dataclasses import dataclass

import bpy
from mathutils import Vector


TAU = math.tau


@dataclass(frozen=True)
class TreeSpec:
    name: str
    seed: int
    height: float
    base_radius: float
    crown_start: float
    crown_radius: float
    lean_x: float
    lean_y: float
    branch_levels: int


class MeshBuilder:
    def __init__(self, name: str):
        self.name = name
        self.vertices: list[tuple[float, float, float]] = []
        self.faces: list[tuple[int, ...]] = []
        self.material_indices: list[int] = []

    def add_face(self, indices, material_index: int):
        self.faces.append(tuple(indices))
        self.material_indices.append(material_index)

    def add_closed_tube(
        self,
        points: list[Vector],
        radii: list[float],
        sides: int,
        material_index: int,
        phase: float = 0.0,
        elliptical_scale: float = 1.0,
    ):
        if len(points) < 2 or len(points) != len(radii):
            raise ValueError("A tube needs matching point and radius lists")

        rings: list[list[int]] = []
        for index, point in enumerate(points):
            if index == 0:
                tangent = (points[1] - point).normalized()
            elif index == len(points) - 1:
                tangent = (point - points[index - 1]).normalized()
            else:
                tangent = (points[index + 1] - points[index - 1]).normalized()

            axis_a, axis_b = perpendicular_axes(tangent)
            ring = []
            for side in range(sides):
                angle = phase + TAU * side / sides
                offset = (
                    axis_a * math.cos(angle) * radii[index]
                    + axis_b
                    * math.sin(angle)
                    * radii[index]
                    * elliptical_scale
                )
                ring.append(len(self.vertices))
                self.vertices.append(tuple(point + offset))
            rings.append(ring)

        for ring_index in range(len(rings) - 1):
            current = rings[ring_index]
            following = rings[ring_index + 1]
            for side in range(sides):
                next_side = (side + 1) % sides
                self.add_face(
                    (
                        current[side],
                        current[next_side],
                        following[next_side],
                        following[side],
                    ),
                    material_index,
                )

        start_center = len(self.vertices)
        self.vertices.append(tuple(points[0]))
        end_center = len(self.vertices)
        self.vertices.append(tuple(points[-1]))
        for side in range(sides):
            next_side = (side + 1) % sides
            self.add_face(
                (start_center, rings[0][next_side], rings[0][side]),
                material_index,
            )
            self.add_face(
                (end_center, rings[-1][side], rings[-1][next_side]),
                material_index,
            )

    def add_needle(
        self,
        base: Vector,
        direction: Vector,
        length: float,
        width: float,
        material_index: int,
    ):
        direction = direction.normalized()
        tip = base + direction * length
        self.add_closed_tube(
            [base, tip],
            [width, width * 0.045],
            3,
            material_index,
            phase=math.pi / 6.0,
            elliptical_scale=0.72,
        )

    def add_lanceolate_leaf(
        self,
        base: Vector,
        tip: Vector,
        up_hint: Vector,
        half_width: float,
        thickness: float,
        material_index: int,
    ):
        length_axis = (tip - base).normalized()
        width_axis = length_axis.cross(up_hint).normalized()
        if width_axis.length_squared < 0.001:
            width_axis, _ = perpendicular_axes(length_axis)
        normal_axis = width_axis.cross(length_axis).normalized()
        center = base.lerp(tip, 0.48)
        base_half = half_width * 0.16
        tip_half = half_width * 0.08

        lower = [
            base - width_axis * base_half - normal_axis * thickness,
            center - width_axis * half_width - normal_axis * thickness,
            tip - width_axis * tip_half - normal_axis * thickness,
            tip + width_axis * tip_half - normal_axis * thickness,
            center + width_axis * half_width - normal_axis * thickness,
            base + width_axis * base_half - normal_axis * thickness,
        ]
        upper = [point + normal_axis * thickness * 2.0 for point in lower]
        offset = len(self.vertices)
        self.vertices.extend(tuple(point) for point in lower + upper)
        lower_indices = list(range(offset, offset + 6))
        upper_indices = list(range(offset + 6, offset + 12))
        self.add_face(tuple(reversed(lower_indices)), material_index)
        self.add_face(tuple(upper_indices), material_index)
        for index in range(6):
            next_index = (index + 1) % 6
            self.add_face(
                (
                    lower_indices[index],
                    lower_indices[next_index],
                    upper_indices[next_index],
                    upper_indices[index],
                ),
                material_index,
            )

    def create_object(self, materials: list[bpy.types.Material]):
        mesh = bpy.data.meshes.new(self.name + "_Mesh")
        mesh.from_pydata(self.vertices, [], self.faces)
        mesh.materials.clear()
        for material in materials:
            mesh.materials.append(material)
        for polygon, material_index in zip(mesh.polygons, self.material_indices):
            polygon.material_index = material_index
            polygon.use_smooth = True
        mesh.update(calc_edges=True)
        obj = bpy.data.objects.new(self.name, mesh)
        bpy.context.collection.objects.link(obj)
        return obj


def perpendicular_axes(direction: Vector):
    direction = direction.normalized()
    reference = Vector((0.0, 0.0, 1.0))
    if abs(direction.dot(reference)) > 0.9:
        reference = Vector((1.0, 0.0, 0.0))
    axis_a = direction.cross(reference).normalized()
    axis_b = direction.cross(axis_a).normalized()
    return axis_a, axis_b


def material(name, color, roughness=0.7, metallic=0.0):
    result = bpy.data.materials.new(name)
    result.diffuse_color = (*color, 1.0)
    result.use_nodes = True
    principled = result.node_tree.nodes.get("Principled BSDF")
    principled.inputs["Base Color"].default_value = (*color, 1.0)
    principled.inputs["Roughness"].default_value = roughness
    principled.inputs["Metallic"].default_value = metallic
    return result


def trunk_center(spec: TreeSpec, height: float):
    ratio = height / spec.height
    return Vector(
        (
            spec.lean_x * ratio + math.sin(ratio * math.pi * 2.3) * 0.07,
            spec.lean_y * ratio + math.sin(ratio * math.pi * 1.7 + 0.8) * 0.055,
            height,
        )
    )


def crown_profile(spec: TreeSpec, height: float):
    t = max(0.0, min(1.0, (height - spec.crown_start) / (spec.height - spec.crown_start)))
    lower_fill = smoothstep(0.0, 0.12, t)
    upper_taper = max(0.0, (1.0 - t) ** 0.72)
    middle_bulge = 0.86 + 0.18 * math.sin(t * math.pi)
    return spec.crown_radius * lower_fill * upper_taper * middle_bulge


def smoothstep(edge0, edge1, value):
    if edge0 == edge1:
        return 0.0
    value = max(0.0, min(1.0, (value - edge0) / (edge1 - edge0)))
    return value * value * (3.0 - 2.0 * value)


def create_tree(spec: TreeSpec, materials):
    rng = random.Random(spec.seed)
    woody = MeshBuilder(spec.name + "_Woody")
    foliage = MeshBuilder(spec.name + "_Needles3D")

    trunk_points = []
    trunk_radii = []
    for segment in range(25):
        height = spec.height * segment / 24.0
        trunk_points.append(trunk_center(spec, height))
        radius = spec.base_radius * (1.0 - 0.76 * (height / spec.height))
        radius *= 1.0 + 0.035 * math.sin(segment * 1.71 + spec.seed)
        trunk_radii.append(max(0.055, radius))
    woody.add_closed_tube(trunk_points, trunk_radii, 14, 0, phase=0.13)

    for root_index in range(8):
        angle = TAU * root_index / 8.0 + rng.uniform(-0.2, 0.2)
        direction = Vector((math.cos(angle), math.sin(angle), 0.0))
        length = rng.uniform(1.05, 1.8)
        start = direction * rng.uniform(0.03, 0.12) + Vector((0.0, 0.0, 0.12))
        end = direction * length + Vector((0.0, 0.0, rng.uniform(-0.28, -0.12)))
        woody.add_closed_tube(
            [start, start.lerp(end, 0.45) + Vector((0.0, 0.0, 0.04)), end],
            [spec.base_radius * 0.34, spec.base_radius * 0.17, 0.035],
            8,
            0,
            phase=rng.random() * TAU,
            elliptical_scale=0.56,
        )

    level_heights = []
    for level in range(spec.branch_levels):
        normalized = (level + 0.35 + rng.uniform(-0.2, 0.2)) / spec.branch_levels
        height = spec.crown_start + normalized * (spec.height - spec.crown_start) * 0.94
        level_heights.append(height)

    for level, height in enumerate(level_heights):
        branches = rng.choice((5, 6, 6, 7))
        level_rotation = rng.uniform(0.0, TAU)
        if level % 5 == 0:
            branches = max(3, branches - 1)
        for branch_index in range(branches):
            angle = level_rotation + TAU * branch_index / branches
            angle += rng.uniform(-0.23, 0.23)
            radial = Vector((math.cos(angle), math.sin(angle), 0.0))
            side = Vector((-radial.y, radial.x, 0.0))
            branch_length = crown_profile(spec, height) * rng.uniform(0.78, 1.1)
            branch_length *= 1.0 - 0.08 * abs(math.sin(angle + spec.seed))
            if branch_length < 0.24:
                continue

            start = trunk_center(spec, height)
            sag = branch_length * rng.uniform(0.045, 0.11)
            tip_rise = branch_length * rng.uniform(0.08, 0.2)
            branch_points = [
                start,
                start + radial * branch_length * 0.3 + side * rng.uniform(-0.1, 0.1) - Vector((0, 0, sag * 0.25)),
                start + radial * branch_length * 0.72 + side * rng.uniform(-0.18, 0.18) - Vector((0, 0, sag)),
                start + radial * branch_length + side * rng.uniform(-0.22, 0.22) + Vector((0, 0, tip_rise - sag * 0.55)),
            ]
            branch_base = max(0.024, spec.base_radius * 0.18 * (1.0 - height / spec.height) + 0.018)
            woody.add_closed_tube(
                branch_points,
                [branch_base, branch_base * 0.72, branch_base * 0.38, 0.012],
                7,
                0,
                phase=rng.random() * TAU,
            )

            secondary_count = max(5, int(branch_length * 2.45))
            for secondary_index in range(secondary_count):
                along = 0.25 + 0.67 * (secondary_index + 0.35) / secondary_count
                base = polyline_point(branch_points, along)
                alternating = -1.0 if secondary_index % 2 == 0 else 1.0
                lateral = side * alternating
                secondary_length = min(
                    rng.uniform(0.68, 1.18),
                    branch_length * rng.uniform(0.28, 0.42),
                )
                secondary_direction = (
                    lateral * rng.uniform(0.72, 0.96)
                    + radial * rng.uniform(0.15, 0.38)
                    + Vector((0.0, 0.0, rng.uniform(0.2, 0.46)))
                ).normalized()
                secondary_tip = base + secondary_direction * secondary_length
                secondary_mid = base.lerp(secondary_tip, 0.56) - Vector((0, 0, secondary_length * 0.045))
                twig_points = [base, secondary_mid, secondary_tip]
                woody.add_closed_tube(
                    twig_points,
                    [0.016, 0.009, 0.0038],
                    6,
                    0,
                    phase=rng.random() * TAU,
                )
                populate_needles(
                    foliage,
                    twig_points,
                    rng,
                    material_index=1 if (level + branch_index + secondary_index) % 3 else 2,
                    density=20,
                )

                twig_tangent = (secondary_tip - base).normalized()
                twig_axis_a, twig_axis_b = perpendicular_axes(twig_tangent)
                for tertiary_index, tertiary_t in enumerate((0.32, 0.58, 0.82)):
                    tertiary_base = polyline_point(twig_points, tertiary_t)
                    tertiary_angle = (
                        tertiary_index * math.pi * 0.88
                        + secondary_index * 0.51
                        + rng.uniform(-0.22, 0.22)
                    )
                    tertiary_direction = (
                        twig_axis_a * math.cos(tertiary_angle)
                        + twig_axis_b * math.sin(tertiary_angle) * 0.72
                        + twig_tangent * rng.uniform(0.2, 0.46)
                    ).normalized()
                    tertiary_length = secondary_length * rng.uniform(0.3, 0.48)
                    tertiary_tip = tertiary_base + tertiary_direction * tertiary_length
                    tertiary_points = [
                        tertiary_base,
                        tertiary_base.lerp(tertiary_tip, 0.55),
                        tertiary_tip,
                    ]
                    woody.add_closed_tube(
                        tertiary_points,
                        [0.007, 0.0045, 0.0015],
                        5,
                        0,
                        phase=rng.random() * TAU,
                    )
                    populate_needles(
                        foliage,
                        tertiary_points,
                        rng,
                        material_index=1 if tertiary_index % 3 else 2,
                        density=11,
                    )

            populate_needles(
                foliage,
                branch_points[1:],
                rng,
                material_index=1 if level % 4 else 2,
                density=max(10, int(branch_length * 5.0)),
            )

    leader_start = trunk_center(spec, spec.height * 0.91)
    leader_end = trunk_center(spec, spec.height) + Vector((0.0, 0.0, 0.2))
    leader_points = [leader_start, leader_start.lerp(leader_end, 0.5), leader_end]
    populate_needles(foliage, leader_points, rng, 1, density=38)
    for apical_level in range(7):
        height_ratio = 0.84 + apical_level * 0.022
        apical_height = spec.height * height_ratio
        apical_center = trunk_center(spec, apical_height)
        apical_length = max(0.24, spec.crown_radius * (1.0 - height_ratio) * 1.55)
        for apical_branch in range(4):
            angle = apical_branch * math.pi * 0.5 + apical_level * 0.73 + rng.uniform(-0.2, 0.2)
            radial = Vector((math.cos(angle), math.sin(angle), 0.0))
            apical_tip = (
                apical_center
                + radial * apical_length * rng.uniform(0.8, 1.08)
                + Vector((0.0, 0.0, apical_length * rng.uniform(0.14, 0.34)))
            )
            apical_points = [
                apical_center,
                apical_center.lerp(apical_tip, 0.52),
                apical_tip,
            ]
            woody.add_closed_tube(
                apical_points,
                [0.018, 0.009, 0.0025],
                6,
                0,
                phase=rng.random() * TAU,
            )
            populate_needles(foliage, apical_points, rng, 1 if apical_branch % 3 else 2, density=18)

    parent = bpy.data.objects.new(spec.name, None)
    bpy.context.collection.objects.link(parent)
    parent.empty_display_type = "PLAIN_AXES"
    woody_object = woody.create_object(materials)
    foliage_object = foliage.create_object(materials)
    woody_object.parent = parent
    foliage_object.parent = parent
    return parent, woody_object, foliage_object


def polyline_point(points: list[Vector], normalized_distance: float):
    normalized_distance = max(0.0, min(1.0, normalized_distance))
    lengths = [(points[index + 1] - points[index]).length for index in range(len(points) - 1)]
    total = sum(lengths)
    target = total * normalized_distance
    accumulated = 0.0
    for index, length in enumerate(lengths):
        if accumulated + length >= target:
            t = (target - accumulated) / length if length else 0.0
            return points[index].lerp(points[index + 1], t)
        accumulated += length
    return points[-1].copy()


def polyline_tangent(points: list[Vector], normalized_distance: float):
    epsilon = 0.015
    before = polyline_point(points, max(0.0, normalized_distance - epsilon))
    after = polyline_point(points, min(1.0, normalized_distance + epsilon))
    return (after - before).normalized()


def populate_needles(builder, twig_points, rng, material_index, density):
    for node in range(density):
        t = 0.08 + 0.84 * (node + rng.uniform(0.1, 0.9)) / density
        base = polyline_point(twig_points, t)
        tangent = polyline_tangent(twig_points, t)
        axis_a, axis_b = perpendicular_axes(tangent)
        needles_around = 4 if node % 3 else 5
        for around in range(needles_around):
            angle = TAU * around / needles_around + rng.uniform(-0.18, 0.18)
            outward = axis_a * math.cos(angle) + axis_b * math.sin(angle)
            direction = (outward * rng.uniform(0.82, 1.0) + tangent * rng.uniform(0.18, 0.42)).normalized()
            length = rng.uniform(0.105, 0.175)
            width = rng.uniform(0.008, 0.0125)
            builder.add_needle(base, direction, length, width, material_index)


def create_grass(name, seed, materials, blade_count, radius):
    rng = random.Random(seed)
    builder = MeshBuilder(name + "_Blades3D")
    for blade_index in range(blade_count):
        angle = rng.uniform(0.0, TAU)
        distance = radius * math.sqrt(rng.random())
        base = Vector((math.cos(angle) * distance, math.sin(angle) * distance, 0.0))
        height = rng.uniform(0.22, 0.68)
        bend_angle = angle + rng.uniform(-1.1, 1.1)
        bend = Vector((math.cos(bend_angle), math.sin(bend_angle), 0.0))
        points = [
            base,
            base + Vector((0.0, 0.0, height * 0.3)),
            base + bend * height * 0.08 + Vector((0.0, 0.0, height * 0.64)),
            base + bend * height * 0.22 + Vector((0.0, 0.0, height)),
        ]
        width = rng.uniform(0.009, 0.018)
        builder.add_closed_tube(
            points,
            [width, width * 0.78, width * 0.42, width * 0.035],
            4,
            3 if blade_index % 4 else 4,
            phase=angle,
            elliptical_scale=0.28,
        )
    return builder.create_object(materials)


def create_fern(name, seed, materials, frond_count):
    rng = random.Random(seed)
    builder = MeshBuilder(name + "_Fronds3D")
    for frond_index in range(frond_count):
        angle = TAU * frond_index / frond_count + rng.uniform(-0.2, 0.2)
        radial = Vector((math.cos(angle), math.sin(angle), 0.0))
        side = Vector((-radial.y, radial.x, 0.0))
        length = rng.uniform(0.62, 1.05)
        start = radial * rng.uniform(0.01, 0.06) + Vector((0.0, 0.0, 0.02))
        frond_points = [
            start,
            start + radial * length * 0.22 + Vector((0.0, 0.0, length * 0.44)),
            start + radial * length * 0.65 + Vector((0.0, 0.0, length * 0.58)),
            start + radial * length + Vector((0.0, 0.0, length * 0.43)),
        ]
        builder.add_closed_tube(
            frond_points,
            [0.014, 0.011, 0.006, 0.0018],
            6,
            5,
            phase=angle,
        )
        pairs = rng.randint(10, 14)
        for pair_index in range(pairs):
            t = 0.16 + 0.7 * pair_index / max(1, pairs - 1)
            center = polyline_point(frond_points, t)
            tangent = polyline_tangent(frond_points, t)
            taper = math.sin(t * math.pi) ** 0.72
            leaf_length = length * rng.uniform(0.13, 0.19) * taper
            for sign in (-1.0, 1.0):
                leaf_direction = (
                    side * sign * rng.uniform(0.88, 1.0)
                    + tangent * rng.uniform(0.08, 0.2)
                    + Vector((0.0, 0.0, rng.uniform(-0.08, 0.05)))
                ).normalized()
                tip = center + leaf_direction * leaf_length
                builder.add_lanceolate_leaf(
                    center,
                    tip,
                    Vector((0.0, 0.0, 1.0)),
                    half_width=max(0.014, leaf_length * rng.uniform(0.16, 0.22)),
                    thickness=0.0023,
                    material_index=5 if pair_index % 4 else 4,
                )
    return builder.create_object(materials)


def select_hierarchy(root):
    bpy.ops.object.select_all(action="DESELECT")
    root.select_set(True)
    for child in root.children_recursive:
        child.select_set(True)
    bpy.context.view_layer.objects.active = root


def export_hierarchy(root, path):
    select_hierarchy(root)
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        object_types={"EMPTY", "MESH"},
        mesh_smooth_type="FACE",
        add_leaf_bones=False,
        bake_anim=False,
        axis_forward="-Z",
        axis_up="Y",
    )


def export_object(obj, path):
    bpy.ops.object.select_all(action="DESELECT")
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.export_scene.fbx(
        filepath=path,
        use_selection=True,
        apply_unit_scale=True,
        apply_scale_options="FBX_SCALE_ALL",
        object_types={"MESH"},
        mesh_smooth_type="FACE",
        add_leaf_bones=False,
        bake_anim=False,
        axis_forward="-Z",
        axis_up="Y",
    )


def point_camera(camera, target):
    camera.rotation_euler = (Vector(target) - camera.location).to_track_quat("-Z", "Y").to_euler()


def create_preview(tree_roots, grass_objects, fern_objects, materials, path):
    tree_positions = ((-7.6, 1.7, 0.0), (0.0, 0.0, 0.0), (7.7, 1.0, 0.0))
    for root, position in zip(tree_roots, tree_positions):
        root.location = position

    for index, obj in enumerate(grass_objects):
        obj.location = (-3.0 + index * 2.6, -1.4 + 0.2 * (index % 2), 0.02)
    for index, obj in enumerate(fern_objects):
        obj.location = (2.0 + index * 2.5, -1.8, 0.03)

    bpy.ops.mesh.primitive_plane_add(size=70, location=(0.0, 0.0, -0.04))
    ground = bpy.context.object
    ground.name = "PreviewGround"
    ground.data.materials.append(materials[6])

    bpy.ops.object.light_add(type="SUN", location=(4.0, -6.0, 18.0))
    sun = bpy.context.object
    sun.data.energy = 3.0
    sun.rotation_euler = (math.radians(28), math.radians(-18), math.radians(-32))
    sun.data.angle = math.radians(5.0)

    bpy.ops.object.light_add(type="AREA", location=(-9.0, -9.0, 12.0))
    area = bpy.context.object
    area.data.energy = 1100.0
    area.data.shape = "DISK"
    area.data.size = 9.0
    point_camera(area, (0.0, 0.0, 7.0))

    bpy.ops.object.camera_add(location=(27.0, -37.0, 13.0))
    camera = bpy.context.object
    camera.data.lens = 55
    point_camera(camera, (0.0, 1.0, 7.3))
    bpy.context.scene.camera = camera

    world = bpy.context.scene.world
    if world is None:
        world = bpy.data.worlds.new("PreviewWorld")
        bpy.context.scene.world = world
    world.use_nodes = True
    world.node_tree.nodes["Background"].inputs["Color"].default_value = (0.055, 0.075, 0.095, 1.0)
    world.node_tree.nodes["Background"].inputs["Strength"].default_value = 0.58

    scene = bpy.context.scene
    scene.render.engine = "BLENDER_EEVEE_NEXT"
    scene.render.resolution_x = 1600
    scene.render.resolution_y = 1000
    scene.render.resolution_percentage = 100
    scene.render.image_settings.file_format = "PNG"
    scene.render.filepath = path
    scene.render.film_transparent = False
    scene.render.image_settings.color_mode = "RGBA"
    scene.view_settings.look = "AgX - Medium High Contrast"
    bpy.ops.render.render(write_still=True)


def print_report(objects):
    total_vertices = 0
    total_triangles = 0
    print("SOTF_FOREST_GEOMETRY_REPORT_BEGIN")
    for obj in objects:
        mesh = obj.data
        triangles = sum(max(0, len(polygon.vertices) - 2) for polygon in mesh.polygons)
        total_vertices += len(mesh.vertices)
        total_triangles += triangles
        print(f"{obj.name}: vertices={len(mesh.vertices)} triangles={triangles}")
    print(f"TOTAL: vertices={total_vertices} triangles={total_triangles}")
    print("SOTF_FOREST_GEOMETRY_REPORT_END")


def main():
    args = sys.argv[sys.argv.index("--") + 1 :] if "--" in sys.argv else []
    if len(args) != 2:
        raise RuntimeError("Expected output directory and preview path after --")
    output_directory = os.path.abspath(args[0])
    preview_path = os.path.abspath(args[1])
    os.makedirs(output_directory, exist_ok=True)
    os.makedirs(os.path.dirname(preview_path), exist_ok=True)

    bpy.ops.wm.read_factory_settings(use_empty=True)
    scene = bpy.context.scene
    scene.unit_settings.system = "METRIC"
    scene.unit_settings.scale_length = 1.0

    materials = [
        material("SOTF_Bark", (0.145, 0.072, 0.035), roughness=0.86),
        material("SOTF_Needles_Deep", (0.025, 0.15, 0.052), roughness=0.78),
        material("SOTF_Needles_Light", (0.065, 0.25, 0.09), roughness=0.74),
        material("SOTF_Grass_Deep", (0.065, 0.18, 0.045), roughness=0.84),
        material("SOTF_Grass_Dry", (0.22, 0.205, 0.075), roughness=0.88),
        material("SOTF_Fern", (0.035, 0.16, 0.048), roughness=0.82),
        material("SOTF_ForestFloor", (0.105, 0.078, 0.04), roughness=0.93),
    ]

    specs = [
        TreeSpec("MSH_Conifer_A", 4181, 15.8, 0.48, 1.65, 4.7, 0.32, -0.18, 35),
        TreeSpec("MSH_Conifer_B", 9187, 18.4, 0.55, 2.1, 5.25, -0.4, 0.24, 39),
        TreeSpec("MSH_Conifer_C", 1229, 13.9, 0.42, 1.35, 4.2, 0.2, 0.42, 33),
    ]
    tree_roots = []
    generated_mesh_objects = []
    for spec in specs:
        root, woody, foliage = create_tree(spec, materials)
        tree_roots.append(root)
        generated_mesh_objects.extend((woody, foliage))
        export_hierarchy(root, os.path.join(output_directory, spec.name + ".fbx"))

    grass_objects = [
        create_grass("MSH_GrassTuft_A", 812, materials, 72, 0.42),
        create_grass("MSH_GrassTuft_B", 955, materials, 58, 0.36),
        create_grass("MSH_GrassTuft_C", 118, materials, 84, 0.48),
    ]
    for obj in grass_objects:
        generated_mesh_objects.append(obj)
        export_object(obj, os.path.join(output_directory, obj.name.replace("_Blades3D", "") + ".fbx"))

    fern_objects = [
        create_fern("MSH_Fern_A", 331, materials, 9),
        create_fern("MSH_Fern_B", 772, materials, 7),
        create_fern("MSH_Fern_C", 905, materials, 11),
    ]
    for obj in fern_objects:
        generated_mesh_objects.append(obj)
        export_object(obj, os.path.join(output_directory, obj.name.replace("_Fronds3D", "") + ".fbx"))

    print_report(generated_mesh_objects)
    create_preview(tree_roots, grass_objects, fern_objects, materials, preview_path)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(os.path.dirname(preview_path), "forest_geometry_source.blend"))


if __name__ == "__main__":
    main()
