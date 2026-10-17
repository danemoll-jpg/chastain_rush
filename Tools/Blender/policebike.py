"""0.99 Part E: the police motorcycle (Patrol Cycle), generated in Blender from this script on top of the Needle 600 (needle600.py).

Run:  F:\blender\blender.exe --background --factory-startup --python Tools/Blender/policebike.py

The Needle 600's frame, engine, wheels and handling family, with a police livery of our own: black body (the game paints the `paint` slot black),
white panels (slot `white`: the front cowl, the tank side panels, the top case, the helmet), red and blue light lenses front and rear (slots
`sirenred` / `sirenblue`, flashed by PoliceLights.cs), a whip antenna, and the word POLICE on the cowl and both tank panels. The rider's head gets
a white open-face police helmet (the rider is the game's parametric Rider.fbx; RiderLook.Police gives it a black jacket and no hair).
No real department's name, crest, badge or number.
Output:
  SourceArt/Blender/PoliceBike.blend, Assets/Resources/VehicleModels/PoliceBike.fbx
"""
import bpy, math, sys, os
from mathutils import Vector, Matrix
sys.path.insert(0, os.path.dirname(__file__))
import needle600 as N

N.COLORS['white'] = (.94, .94, .92); N.COLORS['sirenred'] = (.85, .05, .05); N.COLORS['sirenblue'] = (.05, .2, .95)
N.COLORS['paint'] = (.03, .03, .035)  # preview only: the game paints this slot black
B = N.B


def lettering(text, centre, reading, up, size, group='Body', slot='engine'):
    """The word as a thin mesh at a Unity point, reading along `reading`, upright along `up`; its face looks along reading x up."""
    cu = bpy.data.curves.new(text, 'FONT'); cu.body = text; cu.size = size; cu.extrude = .003; cu.align_x = 'CENTER'; cu.align_y = 'CENTER'
    ob = bpy.data.objects.new(text, cu); bpy.context.collection.objects.link(ob)
    bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True); bpy.context.view_layer.objects.active = ob
    bpy.ops.object.convert(target='MESH')
    r, u = B(*reading).normalized(), B(*up).normalized(); n = r.cross(u)
    ob.matrix_world = Matrix.Translation(B(*centre)) @ Matrix((r, u, n)).transposed().to_4x4()
    return N.finish(ob, group, slot, 0, 1, smooth=False)


def extras():
    """Replaces the Needle's own rider (the game's rider is a separate model): the police parts."""
    G, F = 'Body', 'Front'
    # helmet: an open-face white dome over the rider's head (Moto pose: head centre (0, 1.225, -.005)), a black visor peak, chin strap
    N.blob('Helmet dome', (0, 1.322, -.040), (.130, .088, .142), G, 'white', 20, 10)
    N.box('Helmet visor', (0, 1.338, .090), (.17, .012, .085), G, 'engine', .004, (-10, 0, 0))
    N.box('Helmet badge', (0, 1.372, .075), (.045, .03, .012), G, 'engine', .004)
    # front cowl on the steering head (steers with the bars): over the headlamp, red / blue lenses on its front corners
    pc = Vector(N.axis(1.0)) + Vector((0, -.05, .085))
    N.hull('Front cowl', [(-.15, pc.y + .075, pc.z - .06), (.15, pc.y + .075, pc.z - .06), (.13, pc.y + .075, pc.z + .13), (-.13, pc.y + .075, pc.z + .13),
                          (-.13, pc.y + .20, pc.z - .06), (.13, pc.y + .20, pc.z - .06), (.115, pc.y + .20, pc.z + .11), (-.115, pc.y + .20, pc.z + .11)], F, 'white', .03, 3)
    for s, slot in ((-1, 'sirenred'), (1, 'sirenblue')):
        N.box('Front lens', (s * .075, pc.y + .15, pc.z + .125), (.10, .045, .035), F, slot, .012)
    lettering('POLICE', (0, pc.y + .105, pc.z + .141), (1, 0, 0), (0, 1, 0), .055, F)
    # whip antenna on the cowl
    N.cyl('Antenna', (.10, pc.y + .20, pc.z - .03), (.12, pc.y + .62, pc.z - .12), .004, F, 'engine', 5)
    # white panels on the tank sides, POLICE on both
    for s in (-1, 1):
        # over the black radiator shroud (x 0.14-0.175 in needle600.py): the same outline, 1.2 cm outboard
        N.hull('Tank panel', [(s * .174, .17, .44), (s * .188, .17, .44), (s * .183, .24, .10), (s * .169, .24, .10),
                              (s * .179, .55, .52), (s * .193, .55, .52), (s * .178, .50, .10), (s * .164, .50, .10)], G, 'white', .004, 1)
        lettering('POLICE', (s * .197, .41, .30), (0, 0, s), (0, 1, 0), .06)
    # rear: a white top case on a rack, red / blue lenses on its back corners, a tail lamp between them, a siren horn under the seat tail
    N.box('Rack', (0, .50, -.68), (.26, .02, .36), G, 'engine', .006)
    N.hull('Top case', [(-.17, .515, -.50), (.17, .515, -.50), (.16, .515, -.88), (-.16, .515, -.88), (-.15, .77, -.52), (.15, .77, -.52), (.14, .76, -.86), (-.14, .76, -.86)], G, 'white', .03, 3)
    for s, slot in ((-1, 'sirenred'), (1, 'sirenblue')):
        N.box('Rear lens', (s * .105, .745, -.885), (.10, .045, .035), G, slot, .012)
    N.box('Case tail lamp', (0, .66, -.885), (.12, .05, .03), G, 'tail', .008)
    lettering('POLICE', (0, .64, -.505), (-1, 0, 0), (0, 1, 0), .06)
    N.box('Siren horn', (0, .42, -.62), (.12, .10, .10), G, 'engine', .02)


if __name__ == '__main__':
    N.rider = extras  # the Needle's build() calls bike(), rider(), the wheels and joins by group and slot
    objs = N.build()
    os.makedirs(os.path.join(N.ROOT, 'SourceArt', 'Blender'), exist_ok=True)
    bpy.ops.wm.save_as_mainfile(filepath=os.path.join(N.ROOT, 'SourceArt', 'Blender', 'PoliceBike.blend'))
    bpy.ops.export_scene.fbx(filepath=os.path.join(N.ROOT, 'Assets', 'Resources', 'VehicleModels', 'PoliceBike.fbx'), use_selection=False, object_types={'MESH'},
                             axis_forward='-Z', axis_up='Y', bake_space_transform=True, apply_unit_scale=True, apply_scale_options='FBX_SCALE_UNITS',
                             mesh_smooth_type='OFF', use_mesh_modifiers=True, add_leaf_bones=False, path_mode='STRIP', use_custom_props=False)
    counts = {ob.name: sum(len(p.vertices) - 2 for p in ob.data.polygons) for ob in bpy.data.objects if ob.type == 'MESH'}
    print('POLICEBIKE triangles', N.tris(), counts)
