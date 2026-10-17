"""0.94 Part C: the patrol car for Police Chase (the cop's vehicle only: not in the garage, the shop or races), generated in
Blender from this script with the 0.75 car kit (Tools/Blender/cars.py: body loft, cabin glass panes, interior and steering
wheel).

Run:  blender --background --factory-startup --python Tools/Blender/police.py

An original design: a generic late-1980s four-door sedan in black and white (black body, white doors and roof), a roof light
bar with a red half and a blue half, a push bar on the nose, a pillar spotlight, and the word POLICE on both front doors.
No real department's name, crest, badge, number or one real car's shape.
Same conventions as cars.py: wheel centres (+/-track, -0.20, +/-axle), radius 0.33, ground at y = -0.53, driver at the seat
point H (rider pose Car). Groups Body, Steer, WheelFL/FR/RL/RR. Slots as cars.py plus: white (the doors and roof),
sirenred / sirenblue (the light bar's lenses: the game flashes them, PoliceLights.cs).
"""
import bpy, math, sys, os
from mathutils import Vector, Matrix
sys.path.insert(0, os.path.dirname(__file__))
import kit, cars
from kit import box, hull, cyl, blob, made, place, at
from cars import interp, WY

kit.COLORS['white'] = (.94, .94, .92)
kit.COLORS['sirenred'] = (.85, .05, .05)
kit.COLORS['sirenblue'] = (.05, .2, .95)

C = dict(track=.95, axle=1.40, H=(-.42, .05, -.15), length=2.36, hw=1.0, belt=.48, roof=1.02,
         wind=(.80, .16), rear=(-.78, -1.36), bpost=-.30, wagon=False,
         keys=[(2.36, .92, .36, .02, -.10), (2.28, .98, .43, .04, -.18), (2.06, 1.0, .46, .05, -.26), (1.3, 1.0, .47, .05, -.30),
               (.80, 1.0, .48, .04, -.30), (0, 1.0, .49, .03, -.30), (-1.0, 1.0, .50, .03, -.30), (-1.9, 1.0, .51, .03, -.27),
               (-2.2, .97, .49, .02, -.18), (-2.37, .92, .43, .01, -.08)])


def lettering(text, centre, reading, up, size=.20):
    """The word as a thin mesh (Blender's built-in font) at a Unity point, reading along `reading`, upright along `up`; its
    face looks along reading x up (outwards)."""
    cu = bpy.data.curves.new(text, 'FONT'); cu.body = text; cu.size = size; cu.extrude = .004; cu.align_x = 'CENTER'; cu.align_y = 'CENTER'
    ob = bpy.data.objects.new(text, cu); bpy.context.collection.objects.link(ob)
    bpy.ops.object.select_all(action='DESELECT'); ob.select_set(True); bpy.context.view_layer.objects.active = ob
    bpy.ops.object.convert(target='MESH')
    r, u = kit.B(*reading).normalized(), kit.B(*up).normalized(); n = r.cross(u)
    ob.matrix_world = Matrix.Translation(kit.B(*centre)) @ Matrix((r, u, n)).transposed().to_4x4()
    return kit.finish(ob, 'Body', 'engine', 0, 1, smooth=False)


def police(C):
    G = 'Body'; L = C['length']; hw = C['hw']; keys = C['keys']; belt, roof = C['belt'], C['roof']
    nose = interp(keys, L - .06); tail = interp(keys, -L + .06); zf, zr0 = C['wind']; zr, zd = C['rear']
    # bumpers (black rubber), grille, lamps
    for z, sgn in ((L + .03, 1), (-L - .03, -1)):
        hull('Bumper', [(-hw * .97, -.17, z - sgn * .10), (hw * .97, -.17, z - sgn * .10), (hw * .93, -.16, z + sgn * .05), (-hw * .93, -.16, z + sgn * .05),
                        (-hw * .97, -.01, z - sgn * .10), (hw * .97, -.01, z - sgn * .10), (hw * .93, -.02, z + sgn * .05), (-hw * .93, -.02, z + sgn * .05)], G, 'rubber', .02, 2)
    yh = nose[1] - .13
    box('Grille', (0, yh, L - .015), (hw * 1.0, .15, .06), G, 'engine', .01)
    for i in range(4): box('Grille bar', (0, yh - .05 + i * .033, L + .02), (hw * .85, .008, .012), G, 'chrome', 0)
    for s in (-1, 1):
        box('Headlamp', (s * hw * .70, yh, L + .005), (.30, .12, .04), G, 'lamp', .01)
        box('Headlamp rim', (s * hw * .70, yh, L - .02), (.33, .145, .03), G, 'chrome', .006)
        box('Indicator', (s * hw * .82, -.08, L + .04), (.12, .04, .02), G, 'lamp', .006)
        box('Tail lamp', (s * hw * .62, tail[1] - .12, -L - .01), (hw * .55, .10, .04), G, 'tail', .01)
        box('Reverse lamp', (s * hw * .22, tail[1] - .12, -L - .01), (.10, .06, .04), G, 'lamp', .008)
        cyl('Exhaust tip', (s * .55, -.22, -L + .25), (s * .55, -.22, -L - .08), .035, G, 'chrome', 10)
        zm = zf - .05
        box('Mirror arm', (s * (hw + .03), belt + .04, zm), (.08, .03, .05), G, 'engine', .006)
        blob('Mirror', (s * (hw + .09), belt + .06, zm), (.055, .045, .035), G, 'paint', 10, 6)
        for zh in (C['bpost'] + .12, C['bpost'] - .95): box('Door handle', (s * (hw + .012), belt - .08, zh), (.02, .03, .14), G, 'chrome', .006)
        # white doors: one panel over both doors, between the wheel arches, from the sill to just under the belt line
        z0, z1 = -C['axle'] + .46, C['axle'] - .46; y0, y1 = -.14, belt - .035; x0, x1 = s * (hw - .004), s * (hw + .008)
        hull('Door panel', [(x0, y0, z1), (x1, y0, z1), (x1, y0, z0), (x0, y0, z0), (x0, y1, z1), (x1, y1, z1), (x1, y1, z0), (x0, y1, z0)], G, 'white', .004, 1)
        # POLICE across the front door, reading from the front of the car to the back on the right side and back to front on
        # the left (left to right as seen standing beside it)
        lettering('POLICE', (s * (hw + .014), .14, .32), (0, 0, s), (0, 1, 0), .21)
    box('Number plate', (0, tail[1] - .26, -L - .02), (.36, .13, .02), G, 'metal', .006)
    # white roof skin (over the painted roof)
    xr = hw * .78
    hull('Roof skin', [(-xr + .01, roof - .006, zr - .01), (xr - .01, roof - .006, zr - .01), (xr - .01, roof - .006, zr0 + .01), (-xr + .01, roof - .006, zr0 + .01),
                       (-xr + .04, roof + .004, zr + .02), (xr - .04, roof + .004, zr + .02), (xr - .04, roof + .004, zr0 - .01), (-xr + .04, roof + .004, zr0 - .01)], G, 'white', .01, 1)
    # light bar: a black base on two feet, a red lens on the driver's side (left), a blue one on the right, a white centre
    zb = (zr0 + zr) * .5 + .15
    for s in (-1, 1): box('Light bar foot', (s * .45, roof + .03, zb), (.06, .05, .20), G, 'engine', .006)
    box('Light bar base', (0, roof + .07, zb), (1.30, .05, .30), G, 'engine', .01)
    for s, slot in ((-1, 'sirenred'), (1, 'sirenblue')):
        box('Light bar lens', (s * .36, roof + .145, zb), (.56, .10, .26), G, slot, .02)
    box('Light bar centre', (0, roof + .14, zb), (.14, .09, .24), G, 'lamp', .01)
    # push bar across the nose and a spotlight on the driver's A pillar, an antenna on the boot
    for s in (-1, 1):
        cyl('Push bar upright', (s * .42, -.18, L + .10), (s * .42, .26, L + .12), .03, G, 'engine', 10)
        cyl('Push bar foot', (s * .42, -.10, L + .10), (s * .42, -.10, L - .05), .025, G, 'engine', 8)
    for y in (.02, .20): cyl('Push bar rail', (-.46, y, L + .115), (.46, y, L + .115), .028, G, 'engine', 10)
    cyl('Spotlight stem', (-(hw - .02), belt + .10, zf - .02), (-(hw + .06), belt + .14, zf - .02), .015, G, 'chrome', 8)
    cyl('Spotlight', (-(hw + .08), belt + .15, zf - .10), (-(hw + .08), belt + .15, zf + .08), .07, G, 'chrome', 14, r1=.06)
    cyl('Antenna', (.5, tail[1] + .02, -L + .35), (.5, tail[1] + .80, -L + .40), .004, G, 'engine', 5)


def build():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    cars.body(C); cars.cabin(C); police(C); c, n = cars.interior(C)
    T, A = C['track'], C['axle']
    pos = {'WheelFL': (-T, WY, A), 'WheelFR': (T, WY, A), 'WheelRL': (-T, WY, -A), 'WheelRR': (T, WY, -A)}
    for g, p in pos.items(): cars.wheel(g, p, p[0] < 0, True)
    out = kit.join_all()
    for ob in out:
        g = ob.name.split('__')[0]
        if g in pos: kit.set_origin(ob, pos[g])
        if g == 'Steer': kit.set_origin(ob, c)
    print('PatrolCar steering wheel centre', c, 'column', n)
    kit.export('PatrolCar')


if __name__ == '__main__':
    build()
