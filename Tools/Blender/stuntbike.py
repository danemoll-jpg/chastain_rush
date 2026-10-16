"""0.106 Part C: the stunt motorcycle (Skywhip 250), generated in Blender from this script on top of the game's own Ridge Scrambler (bikes.py).

Run:  F:\\blender\\blender.exe --background --factory-startup --python Tools/Blender/stuntbike.py

A freestyle dirt bike of our own (no make, badge or logo): the Scrambler's frame, forks, wheels and riding position (the rider's Dirt pose fits it
unchanged), with what freestyle riders fit for tricks: grab-handle cut-outs in the rear fender and a grab bar each side under the seat, a crossbar
pad, a bash guard, wide pegs, bright fork guards (the paint slot is the player's colour) and a big number 7 on the front plate and both side
plates. Groups: Body, Front, WheelFront, WheelRear. Slots: paint, metal, chrome, engine, rubber, lamp, tail.
Output: SourceArt/Blender/StuntBike.blend, Assets/Resources/VehicleModels/StuntBike.fbx
"""
import bpy, math, sys, os
from mathutils import Vector, Matrix
sys.path.insert(0, os.path.dirname(__file__))
import kit, bikes
from kit import B, box, hull, cyl, blob, strip, tube, made, place, at

SPEC = bikes.BIKES['Scrambler']


def lettering(text, centre, reading, up, size, group, slot='engine'):
    """The text as a thin mesh at a Unity point, reading along `reading`, upright along `up`."""
    cu = bpy.data.curves.new(text, 'FONT'); cu.body = text; cu.size = size; cu.extrude = .003; cu.align_x = 'CENTER'; cu.align_y = 'CENTER'
    ob = bpy.data.objects.new(text, cu); bpy.context.collection.objects.link(ob)
    bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True); bpy.context.view_layer.objects.active = ob
    bpy.ops.object.convert(target='MESH')
    r, u = B(*reading).normalized(), B(*up).normalized(); n = r.cross(u)
    ob.matrix_world = Matrix.Translation(B(*centre)) @ Matrix((r, u, n)).transposed().to_4x4()
    return kit.finish(ob, group, slot, 0, 1, smooth=False)


def extras():
    G = 'Body'; ax = bikes.axis_fn(SPEC)
    for s in (-1, 1):
        # grab bars under the seat's tail (the "flip grips"), and the cut-outs they sit in (dark panels on the fender's sides)
        tube('Grab bar', [(s * .08, .60, -.52), (s * .17, .62, -.60), (s * .18, .63, -.78), (s * .09, .66, -.86)], .014, G, 'metal', 6)
        hull('Grab cut-out', [(s * .095, .60, -.56), (s * .105, .60, -.56), (s * .105, .63, -.84), (s * .095, .63, -.84), (s * .095, .67, -.56), (s * .105, .67, -.56), (s * .105, .70, -.84), (s * .095, .70, -.84)], G, 'engine', .004, 1)
        # wide freestyle pegs
        box('Wide peg', (s * .24, .10, -.02), (.13, .028, .09), G, 'metal', .005)
        # side plate number
        lettering('7', (s * .14, .50, -.50), (0, 0, -s), (0, 1, 0), .16, G, 'engine')
    # bash guard over the skid plate's front, and a crossbar pad on the bars (Front)
    hull('Bash guard', [(-.14, -.14, .18), (.14, -.14, .18), (.14, -.02, .34), (-.14, -.02, .34), (-.14, -.10, .14), (.14, -.10, .14), (.14, .04, .30), (-.14, .04, .30)], G, 'engine', .02, 2)
    top = ax(1.25); by, bz = top[1] + .07, top[2] + .02
    cyl('Crossbar', (-.16, by + .07, bz), (.16, by + .07, bz), .012, 'Front', 'metal', 8)
    cyl('Crossbar pad', (-.10, by + .072, bz), (.10, by + .072, bz), .028, 'Front', 'paint', 10)
    for s in (-1, 1): cyl('Crossbar post', (s * .16, by, bz), (s * .16, by + .07, bz), .011, 'Front', 'metal', 6)
    # bright fork guards over the Scrambler's (paint), and the front plate's number
    fy, fz = SPEC['front']
    for s in (-1, 1):
        x = s * .09
        hull('Guard cover', [(x - .025, fy + .10, fz + .04), (x + .025, fy + .10, fz + .04), (x + .025, ax(.42)[1], ax(.42)[2] + .05), (x - .025, ax(.42)[1], ax(.42)[2] + .05),
                             (x - .025, fy + .10, fz + .085), (x + .025, fy + .10, fz + .085), (x + .025, ax(.42)[1], ax(.42)[2] + .095), (x - .025, ax(.42)[1], ax(.42)[2] + .095)], 'Front', 'paint', .01, 1)
    pc = Vector(ax(1.13)) + Vector((0, -.06, .10))
    lettering('7', (0, pc.y - .02, pc.z + .005), (1, 0, 0), (0, .93, -.36), .17, 'Front', 'engine')


def build():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    bikes.scrambler(SPEC); extras()
    fy, fz = SPEC['front']; ry_, rz = SPEC['rear']
    for g, (y, z), R, rx, front in (('WheelFront', (fy, fz), SPEC['Rf'], SPEC['wf'], True), ('WheelRear', (ry_, rz), SPEC['Rr'], SPEC['wr'], False)):
        s0 = len(kit.PARTS); bikes.wheel(g, R, rx, 26 if front else 24, 18, front, not front); place(made(s0), at((0, y, z)))
    out = kit.join_all()
    for ob in out:
        g = ob.name.split('__')[0]
        if g == 'WheelFront': kit.set_origin(ob, (0, fy, fz))
        if g == 'WheelRear': kit.set_origin(ob, (0, ry_, rz))
    kit.export('StuntBike')


if __name__ == '__main__':
    build()
