"""Build the distributable, project-owned first-person axe viewmodel.

The restricted local intake is deliberately not read by this script. Both hands are solved
against grip targets parented to one tool bone, then Blender/FBX bakes the evaluated rig.
"""

import argparse
import math
import os
import sys

import bpy
from mathutils import Vector


def reset_scene():
    bpy.ops.wm.read_factory_settings(use_empty=True)


def material(name, color, metallic=0.0, roughness=0.5):
    value = bpy.data.materials.new(name)
    value.diffuse_color = (*color, 1.0)
    value.metallic = metallic
    value.roughness = roughness
    return value


def cylinder_between(name, start, end, radius, segments, mat, armature, bone_name,
                     radius_end=None):
    radius_end = radius if radius_end is None else radius_end
    direction = Vector(end) - Vector(start)
    middle = (Vector(start) + Vector(end)) * 0.5
    bpy.ops.mesh.primitive_cone_add(
        vertices=segments, radius1=radius, radius2=radius_end,
        depth=direction.length, location=middle)
    obj = bpy.context.object
    obj.name = name
    obj.rotation_mode = 'QUATERNION'
    obj.rotation_quaternion = direction.to_track_quat('Z', 'Y')
    bpy.ops.object.transform_apply(location=False, rotation=True, scale=True)
    obj.data.materials.append(mat)
    for polygon in obj.data.polygons:
        polygon.use_smooth = True
    group = obj.vertex_groups.new(name=bone_name)
    group.add(range(len(obj.data.vertices)), 1.0, 'REPLACE')
    modifier = obj.modifiers.new(name='Armature', type='ARMATURE')
    modifier.object = armature
    obj.parent = armature
    return obj


def box(name, location, scale, mat, armature, bone_name, bevel=0.0):
    bpy.ops.mesh.primitive_cube_add(location=location, scale=scale)
    obj = bpy.context.object
    obj.name = name
    bpy.ops.object.transform_apply(location=False, rotation=False, scale=True)
    if bevel > 0.0:
        bevel_mod = obj.modifiers.new(name='EdgeSoftening', type='BEVEL')
        bevel_mod.width = bevel
        bevel_mod.segments = 3
    obj.data.materials.append(mat)
    group = obj.vertex_groups.new(name=bone_name)
    group.add(range(len(obj.data.vertices)), 1.0, 'REPLACE')
    armature_mod = obj.modifiers.new(name='Armature', type='ARMATURE')
    armature_mod.object = armature
    obj.parent = armature
    return obj


def create_rig():
    armature_data = bpy.data.armatures.new('RIG_AxeViewmodel')
    armature = bpy.data.objects.new('RIG_AxeViewmodel', armature_data)
    bpy.context.collection.objects.link(armature)
    bpy.context.view_layer.objects.active = armature
    armature.select_set(True)
    bpy.ops.object.mode_set(mode='EDIT')

    def bone(name, head, tail, parent=None, connected=False):
        value = armature_data.edit_bones.new(name)
        value.head = head
        value.tail = tail
        value.parent = parent
        value.use_connect = connected
        return value

    root = bone('ViewmodelRoot', (0, 0, -0.28), (0, 0, -0.18))
    tool = bone('Tool', (0.09, 0.43, -0.17), (0.09, 0.43, 0.56), root)
    left_upper = bone('LeftUpperArm', (-0.31, 0.04, -0.26), (-0.27, 0.23, -0.03), root)
    left_fore = bone('LeftForearm', left_upper.tail, (0.05, 0.40, 0.25), left_upper, True)
    left_hand = bone('LeftHand', left_fore.tail, (0.09, 0.43, 0.36), left_fore, True)
    right_upper = bone('RightUpperArm', (0.31, 0.04, -0.28), (0.29, 0.22, -0.10), root)
    right_fore = bone('RightForearm', right_upper.tail, (0.14, 0.40, -0.08), right_upper, True)
    right_hand = bone('RightHand', right_fore.tail, (0.09, 0.43, 0.03), right_fore, True)
    left_grip = bone('LeftGrip', (0.09, 0.43, 0.27), (0.09, 0.43, 0.38), tool)
    right_grip = bone('RightGrip', (0.09, 0.43, -0.10), (0.09, 0.43, 0.01), tool)
    left_grip.use_deform = False
    right_grip.use_deform = False
    bpy.ops.object.mode_set(mode='POSE')
    for hand_name, target in (('LeftHand', 'LeftGrip'), ('RightHand', 'RightGrip')):
        hand = armature.pose.bones[hand_name]
        ik = hand.constraints.new('IK')
        ik.name = hand_name + '_TwoHandGrip'
        ik.target = armature
        ik.subtarget = target
        ik.chain_count = 3
        ik.use_tail = True
        copy_rotation = hand.constraints.new('COPY_ROTATION')
        copy_rotation.target = armature
        copy_rotation.subtarget = target
        copy_rotation.mix_mode = 'AFTER'
    bpy.ops.object.mode_set(mode='OBJECT')
    return armature


def build_meshes(armature):
    skin = material('MAT_VM_Skin', (0.43, 0.22, 0.13), roughness=0.72)
    sleeve = material('MAT_VM_Sleeve', (0.035, 0.045, 0.052), roughness=0.83)
    glove = material('MAT_VM_Glove', (0.085, 0.072, 0.055), roughness=0.78)
    wood = material('MAT_VM_AxeWood', (0.24, 0.085, 0.025), roughness=0.64)
    steel = material('MAT_VM_AxeSteel', (0.12, 0.14, 0.16), metallic=0.76, roughness=0.30)

    # Layered sleeves, forearms and hands provide a coherent silhouette without third-party art.
    cylinder_between('LeftSleeve', (-0.31, 0.04, -0.26), (-0.27, 0.23, -0.03),
                     0.095, 16, sleeve, armature, 'LeftUpperArm', 0.075)
    cylinder_between('LeftForearm', (-0.27, 0.23, -0.03), (0.05, 0.40, 0.25),
                     0.072, 16, skin, armature, 'LeftForearm', 0.054)
    cylinder_between('LeftGlove', (0.05, 0.40, 0.25), (0.09, 0.43, 0.36),
                     0.060, 16, glove, armature, 'LeftHand', 0.050)
    cylinder_between('RightSleeve', (0.31, 0.04, -0.28), (0.29, 0.22, -0.10),
                     0.098, 16, sleeve, armature, 'RightUpperArm', 0.077)
    cylinder_between('RightForearm', (0.29, 0.22, -0.10), (0.14, 0.40, -0.08),
                     0.074, 16, skin, armature, 'RightForearm', 0.055)
    cylinder_between('RightGlove', (0.14, 0.40, -0.08), (0.09, 0.43, 0.03),
                     0.061, 16, glove, armature, 'RightHand', 0.050)

    cylinder_between('AxeHandle', (0.09, 0.43, -0.24), (0.09, 0.43, 0.56),
                     0.026, 20, wood, armature, 'Tool', 0.021)
    head = box('AxeHead', (0.09, 0.43, 0.52), (0.075, 0.055, 0.070),
               steel, armature, 'Tool', 0.012)
    head.rotation_euler.z = math.radians(2.0)
    blade = box('AxeBlade', (-0.015, 0.43, 0.52), (0.095, 0.018, 0.090),
                steel, armature, 'Tool', 0.006)
    # Scale the rear edge to make a wedge-shaped felling blade.
    for vertex in blade.data.vertices:
        if vertex.co.x < 0:
            vertex.co.z *= 1.30
    box('AxePoll', (0.19, 0.43, 0.52), (0.045, 0.048, 0.045),
        steel, armature, 'Tool', 0.008)


def keyframe_tool(armature, frame, location, rotation_degrees):
    tool = armature.pose.bones['Tool']
    tool.rotation_mode = 'XYZ'
    tool.location = location
    tool.rotation_euler = tuple(math.radians(value) for value in rotation_degrees)
    tool.keyframe_insert('location', frame=frame)
    tool.keyframe_insert('rotation_euler', frame=frame)


def make_action(armature, name, poses, loop=False):
    action = bpy.data.actions.new(name=name)
    action.use_fake_user = True
    armature.animation_data_create()
    armature.animation_data.action = action
    for frame, location, rotation in poses:
        keyframe_tool(armature, frame, location, rotation)
    action.frame_start = poses[0][0]
    action.frame_end = poses[-1][0]
    if loop:
        for curve in action.fcurves:
            curve.modifiers.new('CYCLES')
    return action


def build_actions(armature):
    idle = (0.0, 0.0, 0.0)
    make_action(armature, 'Axe_Idle', [
        (1, idle, (0, 0, 0)), (18, (0, 0.004, 0.006), (0.7, -0.5, 0.4)),
        (36, idle, (0, 0, 0))], True)
    make_action(armature, 'Axe_Equip', [
        (1, (0.25, -0.10, -0.46), (62, -28, -26)),
        (7, (0.10, -0.04, -0.20), (28, -12, -10)),
        (15, idle, (0, 0, 0))])
    make_action(armature, 'Axe_Unequip', [
        (1, idle, (0, 0, 0)), (8, (0.10, -0.04, -0.20), (28, -12, -10)),
        (14, (0.25, -0.10, -0.46), (62, -28, -26))])
    make_action(armature, 'Axe_Chop_Left', [
        (1, idle, (0, 0, 0)), (8, (-0.23, -0.03, 0.16), (-38, -35, 30)),
        (17, (0.17, 0.20, -0.06), (58, 42, -22)),
        (23, (0.10, 0.08, 0.01), (32, 20, -12)),
        (34, idle, (0, 0, 0))])
    make_action(armature, 'Axe_Chop_Right', [
        (1, idle, (0, 0, 0)), (8, (0.23, -0.03, 0.16), (-38, 35, -30)),
        (17, (-0.17, 0.20, -0.06), (58, -42, 22)),
        (23, (-0.10, 0.08, 0.01), (32, -20, 12)),
        (34, idle, (0, 0, 0))])
    make_action(armature, 'Axe_Chop_Heavy', [
        (1, idle, (0, 0, 0)), (11, (0.0, -0.12, 0.28), (-70, 0, 0)),
        (22, (0.0, 0.25, -0.10), (78, 0, 0)),
        (30, (0.0, 0.09, -0.01), (38, 0, 0)),
        (43, idle, (0, 0, 0))])
    make_action(armature, 'Axe_Recovery', [
        (1, (0.0, 0.09, -0.01), (38, 0, 0)),
        (8, (0.0, 0.02, 0.02), (12, 0, 0)), (16, idle, (0, 0, 0))])
    armature.animation_data.action = None


def export(output):
    os.makedirs(os.path.dirname(output), exist_ok=True)
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.export_scene.fbx(
        filepath=output, use_selection=True, apply_unit_scale=True,
        bake_space_transform=False, add_leaf_bones=False, path_mode='AUTO',
        embed_textures=False, bake_anim=True, bake_anim_use_all_actions=True,
        bake_anim_use_nla_strips=False, bake_anim_simplify_factor=0.0,
        mesh_smooth_type='FACE')


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--output', required=True)
    arguments = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    args = parser.parse_args(arguments)
    reset_scene()
    rig = create_rig()
    build_meshes(rig)
    build_actions(rig)
    export(os.path.abspath(args.output))
    print('R2_FOREST1F_VIEWMODEL_BUILT=' + os.path.abspath(args.output))


if __name__ == '__main__':
    main()
