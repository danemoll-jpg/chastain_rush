"""0.106 Part C: the stunt buggy (Clay Hopper), generated in Blender from this script (same kit and standard as the other vehicles).

Run:  F:\\blender\\blender.exe --background --factory-startup --python Tools/Blender/stuntbuggy.py

Our own design, no make, badge or logo: a light two-seat-wide tube-frame dune buggy with one seat on the centre line, a full roll cage (painted
the body colour), long-travel suspension (A-arms and coil-overs at all four corners), big knobbly tyres, a short nose with two lamps, side pods,
the engine open at the back under the cage with two pipes, nerf bars between the wheels. Unity vehicle space as the other vehicles (x right,
y up, z forward); physics wheelbase 2.1 (axles at z = +/-1.05), track 0.68 (wheel centres at x = +/-0.68); the wheels are drawn touching the
ground at y = GROUND (its long suspension sits lower than the others). The rider uses the cars' pose (Tools/Blender/rider.py, pose Car) at the
seat point H, with the steering wheel at H + (0, 0.36, 0.36) tilted 25 degrees, as in the cars and the mower. Groups: Body, Steer,
WheelFL / WheelFR / WheelRL / WheelRR. Slots: paint, metal, chrome, engine, rubber, lamp, tail, interior.
Output: SourceArt/Blender/StuntBuggy.blend, Assets/Resources/VehicleModels/StuntBuggy.fbx
"""
import bpy, bmesh, math, sys, os
from mathutils import Vector
sys.path.insert(0, os.path.dirname(__file__))
import kit
from kit import B, box, hull, cyl, blob, strip, tube, made, place, at

GROUND = -.66
H = Vector((0, .06, -.28))                      # seat point (the Car pose's hip); VehicleProfile.StuntBuggy.Seat
AXLE = 1.05
RF, RR = .40, .43                               # front / rear wheel outer radius
TF, TR = .68, .68                               # wheel centres (half track)
WF, WR = .13, .15                               # tyre half width


def wheel(group, pos, left, R, half):
    s0 = len(kit.PARTS)
    ry = .11
    kit.tyre(group, R - ry, half, ry, 30, 8, knobs=24, squared=.9, knob=(.85, .03, .06))
    rim = (R - ry) * .78
    cyl(group + ' rim', (-half * .85, 0, 0), (half * .85, 0, 0), rim, group, 'metal', 24, cap=False)
    cyl(group + ' dish', (0, 0, 0), (half * .8, 0, 0), rim * .97, group, 'engine', 20, r1=rim * .45)
    for i in range(6):
        a = i * math.pi / 3
        box(group + ' spoke', (half * .55, math.cos(a) * rim * .55, math.sin(a) * rim * .55), (.03, .05, rim * .85), group, 'metal', .006, (math.degrees(a) - 90, 0, 0))
    cyl(group + ' hub', (half * .5, 0, 0), (half * .95, 0, 0), rim * .3, group, 'chrome', 12)
    place(made(s0), at(pos, left))


def body():
    G = 'Body'
    # ---- floor pan and the lower frame
    hull('Floor pan', [(-.42, -.30, -.95), (.42, -.30, -.95), (.42, -.30, .80), (-.42, -.30, .80), (-.42, -.25, -.95), (.42, -.25, -.95), (.42, -.25, .80), (-.42, -.25, .80)], G, 'engine', .02, 1)
    for s in (-1, 1):
        cyl('Lower rail', (s * .42, -.26, -1.30), (s * .42, -.26, .95), .035, G, 'paint', 10)
        cyl('Nose rail', (s * .42, -.26, .95), (s * .22, -.12, 1.42), .032, G, 'paint', 10)
        # roll cage: the main hoop behind the seat, the front hoop by the dash, the roof bars, the back stays to the engine frame
        tube('Main hoop side', [(s * .42, -.26, -.62), (s * .44, .40, -.66), (s * .40, .98, -.70), (s * .28, 1.10, -.70)], .035, G, 'paint', 6)
        tube('Front hoop side', [(s * .42, -.26, .55), (s * .40, .28, .38), (s * .36, .80, .05), (s * .26, .98, -.02)], .035, G, 'paint', 6)
        cyl('Roof bar', (s * .32, 1.06, -.68), (s * .30, .97, -.02), .032, G, 'paint', 10)
        cyl('Back stay', (s * .40, .98, -.70), (s * .40, .05, -1.30), .03, G, 'paint', 10)
        cyl('Engine rail', (s * .40, .05, -1.30), (s * .42, -.26, -1.30), .03, G, 'paint', 10)
        cyl('Side bar', (s * .44, .30, -.66), (s * .42, .30, .42), .028, G, 'paint', 10)
        cyl('Door brace', (s * .44, .30, -.66), (s * .42, -.24, .10), .024, G, 'paint', 8)
        # nerf bar between the wheels
        cyl('Nerf bar', (s * .62, -.18, -.62), (s * .62, -.18, .62), .03, G, 'metal', 10)
        cyl('Nerf mount', (s * .42, -.22, -.55), (s * .62, -.18, -.55), .022, G, 'metal', 8)
        cyl('Nerf mount', (s * .42, -.22, .55), (s * .62, -.18, .55), .022, G, 'metal', 8)
        # side pod
        hull('Side pod', [(s * .43, -.25, -.55), (s * .58, -.20, -.50), (s * .58, -.20, .45), (s * .43, -.25, .50), (s * .43, .10, -.52), (s * .54, .06, -.48), (s * .54, .06, .40), (s * .43, .10, .45)], G, 'paint', .03, 2)
    cyl('Main hoop top', (-.28, 1.10, -.70), (.28, 1.10, -.70), .035, G, 'paint', 10)
    cyl('Front hoop top', (-.26, .98, -.02), (.26, .98, -.02), .035, G, 'paint', 10)
    cyl('Roof cross', (-.31, 1.02, -.36), (.31, 1.02, -.36), .028, G, 'paint', 10)
    cyl('Harness bar', (-.42, .62, -.68), (.42, .62, -.68), .028, G, 'paint', 10)
    # ---- nose: a short panel over the front, two lamps, a bumper loop and a skid plate
    hull('Nose', [(-.40, -.20, .55), (.40, -.20, .55), (.24, -.16, 1.40), (-.24, -.16, 1.40), (-.36, .20, .42), (.36, .20, .42), (.20, .02, 1.30), (-.20, .02, 1.30)], G, 'paint', .05, 3)
    hull('Skid plate', [(-.26, -.34, .55), (.26, -.34, .55), (.20, -.26, 1.44), (-.20, -.26, 1.44), (-.26, -.30, .55), (.26, -.30, .55), (.20, -.20, 1.44), (-.20, -.20, 1.44)], G, 'metal', .015, 1)
    tube('Front bumper', [(-.30, -.20, 1.30), (-.34, .02, 1.52), (0, .10, 1.60), (.34, .02, 1.52), (.30, -.20, 1.30)], .03, G, 'metal', 8)
    for s in (-1, 1):
        cyl('Lamp pod', (s * .22, .80, .02), (s * .22, .80, .14), .07, G, 'engine', 14)
        cyl('Lamp', (s * .22, .80, .14), (s * .22, .80, .155), .058, G, 'lamp', 14)
    box('Dash', (0, .40, .30), (.62, .10, .18), G, 'interior', .03)
    # ---- the seat (a racing bucket on the centre line) and its harness
    hull('Seat base', [(-.24, H.y - .16, H.z - .22), (.24, H.y - .16, H.z - .22), (.24, H.y - .16, H.z + .26), (-.24, H.y - .16, H.z + .26),
                       (-.24, H.y - .02, H.z - .22), (.24, H.y - .02, H.z - .22), (.24, H.y - .04, H.z + .26), (-.24, H.y - .04, H.z + .26)], G, 'interior', .04, 2)
    hull('Seat back', [(-.25, H.y - .08, H.z - .30), (.25, H.y - .08, H.z - .30), (.25, H.y - .08, H.z - .16), (-.25, H.y - .08, H.z - .16),
                       (-.22, H.y + .62, H.z - .44), (.22, H.y + .62, H.z - .44), (.22, H.y + .62, H.z - .30), (-.22, H.y + .62, H.z - .30)], G, 'interior', .05, 2)
    for s in (-1, 1): cyl('Seat bolster', (s * .26, H.y - .04, H.z - .24), (s * .26, H.y + .48, H.z - .38), .045, G, 'interior', 10)
    box('Pedal floor', (0, -.24, .38), (.5, .02, .4), G, 'rubber', .005)
    # ---- the engine at the back, open under the cage, two pipes out of the top
    hull('Engine', [(-.30, -.28, -1.28), (.30, -.28, -1.28), (.30, -.28, -.78), (-.30, -.28, -.78), (-.26, .22, -1.22), (.26, .22, -1.22), (.26, .22, -.82), (-.26, .22, -.82)], G, 'engine', .04, 2)
    for i in range(4): box('Cooling fin', (0, .24 + i * .035, -1.02), (.46, .012, .32), G, 'metal', 0)
    blob('Air filter', (.20, .34, -.92), (.09, .09, .12), G, 'chrome', 14, 8)
    for s in (-1, 1):
        tube('Exhaust', [(s * .14, .10, -1.24), (s * .18, .30, -1.36), (s * .20, .52, -1.42)], .03, G, 'chrome', 8)
        cyl('Exhaust tip', (s * .20, .52, -1.42), (s * .21, .62, -1.46), .042, G, 'chrome', 12)
        box('Tail lamp', (s * .34, .02, -1.33), (.10, .06, .03), G, 'tail', .008)
    tube('Rear bumper', [(-.40, -.20, -1.30), (-.42, -.05, -1.48), (.42, -.05, -1.48), (.40, -.20, -1.30)], .03, G, 'metal', 8)
    # ---- long-travel suspension: an A-arm and a coil-over at each corner (to the wheel's hub; fixed to the body)
    for z0, R, T in ((AXLE, RF, TF), (-AXLE, RR, TR)):
        y = GROUND + R
        for s in (-1, 1):
            hub = (s * (T - .10), y, z0)
            for dz in (-.16, .16): cyl('A-arm', (s * .40, -.22, z0 + dz), hub, .022, G, 'metal', 8)
            cyl('Upright', (s * (T - .10), y - .10, z0), (s * (T - .10), y + .12, z0), .03, G, 'engine', 8)
            top = (s * .42, .48 if z0 > 0 else .40, z0 - .10 * (1 if z0 > 0 else -1))
            cyl('Coil-over body', (s * (T - .14), y + .05, z0), top, .036, G, 'engine', 12)
            cyl('Coil spring', (s * (T - .145), y + .10, z0), ((s * (T - .14) + top[0]) / 2, (y + .1 + top[1]) / 2, (z0 + top[2]) / 2), .052, G, 'chrome', 12)
    # ---- steering column (fixed) and wheel (Steer: origin on the wheel centre, turns about the column), as in the cars
    t = math.radians(25); c = H + Vector((0, .36, .36)); u = Vector((0, math.cos(t), math.sin(t))); n = Vector((0, -math.sin(t), math.cos(t)))
    cyl('Steering column', tuple(c + n * .02), tuple(c + n * .30), .024, G, 'interior', 10)
    S = 'Steer'; bm = bmesh.new(); seg, side, R, r = 28, 6, .17, .017; vs = []
    for i in range(seg):
        a = i * 2 * math.pi / seg; row = []
        for j in range(side):
            b = j * 2 * math.pi / side; rr = R + r * math.cos(b)
            p = c + Vector((1, 0, 0)) * math.cos(a) * rr + u * math.sin(a) * rr + n * r * math.sin(b)
            row.append(bm.verts.new(B(*p)))
        vs.append(row)
    for i in range(seg):
        for j in range(side): bm.faces.new((vs[i][j], vs[(i + 1) % seg][j], vs[(i + 1) % seg][(j + 1) % side], vs[i][(j + 1) % side]))
    me = bpy.data.meshes.new('Steering rim'); bm.to_mesh(me); bm.free(); ob = bpy.data.objects.new('Steering rim', me); bpy.context.collection.objects.link(ob); kit.outward(ob)
    kit.finish(ob, S, 'rubber')
    cyl('Steering hub', tuple(c - n * .005), tuple(c + n * .05), .045, S, 'engine', 12)
    for ang in (90, 210, 330):
        a = math.radians(ang); cyl('Steering spoke', tuple(c), tuple(c + Vector((1, 0, 0)) * math.cos(a) * R + u * math.sin(a) * R), .012, S, 'metal', 6)
    return tuple(c)


def build():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    c = body()
    pos = {'WheelFL': (-TF, GROUND + RF, AXLE), 'WheelFR': (TF, GROUND + RF, AXLE), 'WheelRL': (-TR, GROUND + RR, -AXLE), 'WheelRR': (TR, GROUND + RR, -AXLE)}
    for g, p in pos.items():
        front = g.startswith('WheelF')
        wheel(g, p, p[0] < 0, RF if front else RR, WF if front else WR)
    out = kit.join_all()
    for ob in out:
        g = ob.name.split('__')[0]
        if g in pos: kit.set_origin(ob, pos[g])
        if g == 'Steer': kit.set_origin(ob, c)
    print('StuntBuggy steering wheel centre', c, 'wheel bottoms at', GROUND)
    kit.export('StuntBuggy')


if __name__ == '__main__':
    build()
