"""0.81 Part B: two more motorcycles, generated in Blender from this script (same kit and standard as 0.75).

Run:  blender --background --factory-startup --python Tools/Blender/bikes.py -- [Scrambler|Drifter]

Original designs; only the type is borrowed (no make, model, badge or one real bike's shape).
  Scrambler  (dirt bike)  tall and lanky: long-travel forks with guards, a big 21-inch front wheel with knobbly tyres, a
             high beak-like front fender, slim tank between big radiator shrouds, a long flat seat and an upswept tail,
             side number plates, a skid plate; high, wide bars for an upright rider.
  Drifter    (cruiser)    long and low: raked-out chrome forks, a big round headlamp, wide pulled-back bars, a teardrop
             tank, a V-twin with finned cylinders and two long chrome pipes, deep valanced fenders, a fat rear tyre, a low
             stepped seat and forward foot controls for a relaxed, feet-forward rider.
The physics are the game's (wheel contact 0.53 m below the body origin); the wheels are drawn to touch the ground there.
Groups: Body (fixed), Front (turns about the steering axis through the front axle), WheelFront / WheelRear (origin on
the axle, axle along x). Slots: paint (body colour), metal, chrome, engine, rubber, lamp, tail.
Riders: pose Dirt / Cruiser in Tools/Blender/rider.py (hands on the grips below, feet on the pegs / forward controls).
"""
import bpy, math, sys, os
from mathutils import Vector
sys.path.insert(0, os.path.dirname(__file__))
import kit
from kit import box, hull, cyl, blob, strip, tube, loft, ring, made, place, at

GROUND = -.53

BIKES = {
    # axle positions (y, z), wheel outer radius, tyre section, steering axis direction (Unity), grips
    'Scrambler': dict(front=(-.16, .80), rear=(-.18, -.76), Rf=.37, Rr=.35, wf=.050, wr=.066, rake=(0, .80, -.33), grip=(.42, .905, .35)),
    'Drifter': dict(front=(-.20, .95), rear=(-.19, -.90), Rf=.33, Rr=.34, wf=.062, wr=.100, rake=(0, .70, -.42), grip=(.43, .76, .17)),
}


def axis_fn(B):
    a = Vector((0, B['front'][0], B['front'][1])); d = Vector(B['rake']).normalized()
    return lambda t: tuple(a + d * t)


def wheel(group, R, rx, knobs, spokes, disc, sprocket, fat=False):
    """At the origin, axle along x: tyre (outer radius R), rim, spokes (or a solid cast wheel for fat), hub, disc."""
    ry = rx * (1.0 if not fat else .75); core = R - ry
    kit.tyre(group, core, rx, ry, 30, 8, knobs=knobs, squared=.9 if knobs else .82, knob=(.85, .026, .045))
    rim = core - ry * .6
    cyl(group + ' rim', (-rx * .6, 0, 0), (rx * .6, 0, 0), rim, group, 'metal' if not fat else 'chrome', 28, cap=False)
    cyl(group + ' hub', (-.07, 0, 0), (.07, 0, 0), .05, group, 'metal', 12)
    if spokes:
        for i in range(spokes):
            a = i * 2 * math.pi / spokes; s = -1 if i % 2 else 1
            cyl(group + ' spoke', (s * .05, math.cos(a + .25) * .045, math.sin(a + .25) * .045), (s * .02, math.cos(a) * (rim - .01), math.sin(a) * (rim - .01)), .0045, group, 'metal', 4, smooth=False)
    else:
        for i in range(6):
            a = i * math.pi / 3
            box(group + ' cast spoke', (0, math.cos(a) * rim * .55, math.sin(a) * rim * .55), (.03, .05, rim * .9), group, 'chrome', .008, (math.degrees(a) - 90, 0, 0))
    if disc: cyl(group + ' brake disc', (.075, 0, 0), (.083, 0, 0), rim * .55, group, 'metal', 20)
    if sprocket: cyl(group + ' sprocket', (-.09, 0, 0), (-.08, 0, 0), rim * .5, group, 'engine', 20)


def scrambler(B):
    ax = axis_fn(B); fy, fz = B['front']; ry_, rz = B['rear']; gx, gy, gz = B['grip']
    # --- frame ---
    cyl('Steering head', ax(1.02), ax(1.24), .036, 'Body', 'metal', 12)
    cyl('Main spine', ax(1.18), (0, .55, -.20), .032, 'Body', 'metal', 10)
    cyl('Down tube', ax(1.04), (0, .02, .22), .03, 'Body', 'metal', 10)
    cyl('Cradle', (0, .02, .22), (0, -.10, -.05), .028, 'Body', 'metal', 10)
    cyl('Cradle rear', (0, -.10, -.05), (0, .10, -.18), .028, 'Body', 'metal', 10)
    for s in (-1, 1):
        cyl('Seat rail', (s * .07, .55, -.20), (s * .075, .60, -.86), .018, 'Body', 'metal', 8)
        cyl('Rear strut', (s * .07, .12, -.18), (s * .075, .55, -.74), .016, 'Body', 'metal', 8)
        hull('Swing arm', [(s * .055 - .02, .04, -.17), (s * .055 + .02, .04, -.17), (s * .08 + .014, ry_ - .02, rz), (s * .08 - .014, ry_ - .02, rz),
                           (s * .055 - .02, .11, -.17), (s * .055 + .02, .11, -.17), (s * .08 + .014, ry_ + .03, rz), (s * .08 - .014, ry_ + .03, rz)], 'Body', 'metal', .008, 1)
        box('Foot peg', (s * .21, .10, -.02), (.11, .025, .06), 'Body', 'metal', .004)
        hull('Side number plate', [(s * .12, .40, -.30), (s * .135, .40, -.30), (s * .135, .45, -.74), (s * .12, .45, -.74),
                                   (s * .12, .555, -.26), (s * .135, .555, -.26), (s * .135, .58, -.72), (s * .12, .58, -.72)], 'Body', 'paint', .02, 2)
        hull('Shroud', [(s * .15, .38, .46), (s * .17, .38, .46), (s * .165, .44, .06), (s * .145, .44, .06),
                        (s * .155, .78, .50), (s * .175, .78, .50), (s * .16, .70, .06), (s * .14, .70, .06)], 'Body', 'paint', .014, 1)
        box('Radiator', (s * .11, .52, .28), (.035, .26, .14), 'Body', 'engine', .008)
    cyl('Axle rear', (-.10, ry_, rz), (.10, ry_, rz), .014, 'Body', 'metal', 8)
    cyl('Shock spring', (0, .55, -.22), (0, .16, -.42), .036, 'Body', 'metal', 12)
    cyl('Shock reservoir', (.06, .52, -.20), (.06, .40, -.26), .022, 'Body', 'chrome', 10)
    # --- engine and skid plate ---
    hull('Crankcase', [(-.12, -.06, -.12), (.12, -.06, -.12), (.12, -.04, .16), (-.12, -.04, .16), (-.13, .16, -.10), (.13, .16, -.10), (.13, .18, .18), (-.13, .18, .18)], 'Body', 'engine', .03)
    hull('Cylinder', [(-.09, .16, .02), (.09, .16, .02), (.09, .16, .20), (-.09, .16, .20), (-.08, .38, .08), (.08, .38, .08), (.08, .38, .25), (-.08, .38, .25)], 'Body', 'engine', .015, 1)
    for i in range(4):
        y = .21 + i * .04
        box('Cooling fin', (0, y, .12 + i * .015), (.23, .012, .24), 'Body', 'metal', 0)
    hull('Skid plate', [(-.13, -.12, -.16), (.13, -.12, -.16), (.13, -.10, .22), (-.13, -.10, .22), (-.14, -.08, -.18), (.14, -.08, -.18), (.14, .02, .30), (-.14, .02, .30)], 'Body', 'metal', .02, 2)
    cyl('Chain upper', (-.085, .12, -.07), (-.085, ry_ + .08, rz), .011, 'Body', 'engine', 6)
    cyl('Chain lower', (-.085, .02, -.07), (-.085, ry_ - .09, rz), .011, 'Body', 'engine', 6)
    # high pipe on the right, under the seat
    tube('Exhaust header', [(.05, .33, .26), (.12, .25, .34), (.17, .14, .26), (.18, .22, .02), (.19, .38, -.22), (.20, .48, -.42)], .026, 'Body', 'metal', 8)
    cyl('Silencer', (.20, .48, -.42), (.20, .58, -.82), .056, 'Body', 'metal', 14)
    cyl('Silencer cap', (.20, .58, -.82), (.20, .59, -.85), .04, 'Body', 'engine', 12)
    # --- bodywork ---
    hull('Fuel tank', [(-.12, .52, .02), (.12, .52, .02), (.13, .52, .42), (-.13, .52, .42), (-.08, .68, -.02), (.08, .68, -.02), (.09, .72, .36), (-.09, .72, .36)], 'Body', 'paint', .045, 3)
    box('Fuel cap', (0, .725, .22), (.05, .02, .05), 'Body', 'metal', .008)
    hull('Seat', [(-.075, .55, .18), (.075, .55, .18), (.12, .56, -.82), (-.12, .56, -.82), (-.07, .62, .12), (.07, .62, .12), (.115, .63, -.80), (-.115, .63, -.80)], 'Body', 'rubber', .03, 2)
    strip('Rear fender', [(-.36, .58), (-.56, .61), (-.80, .66), (-1.02, .72), (-1.16, .77)], .20, .016, 0, 'Body', 'paint')
    box('Tail lamp', (0, .735, -1.14), (.10, .045, .03), 'Body', 'tail', .008)
    # --- front end (steers): long forks with guards, clamps, tall wide bars, number plate, beak fender ---
    for s in (-1, 1):
        x = s * .09
        cyl('Fork slider', (x, fy, fz), (x, ax(.50)[1], ax(.50)[2]), .031, 'Front', 'engine', 12)
        cyl('Fork tube', (x, ax(.48)[1], ax(.48)[2]), (x, ax(1.26)[1], ax(1.26)[2]), .023, 'Front', 'chrome', 12)
        hull('Fork guard', [(x - .02, fy + .08, fz + .03), (x + .02, fy + .08, fz + .03), (x + .02, ax(.45)[1], ax(.45)[2] + .04), (x - .02, ax(.45)[1], ax(.45)[2] + .04),
                            (x - .02, fy + .08, fz + .07), (x + .02, fy + .08, fz + .07), (x + .02, ax(.45)[1], ax(.45)[2] + .08), (x - .02, ax(.45)[1], ax(.45)[2] + .08)], 'Front', 'rubber', .01, 1)
    cyl('Axle front', (-.11, fy, fz), (.11, fy, fz), .014, 'Front', 'metal', 8)
    for t, h in ((1.02, .035), (1.24, .04)):
        p = ax(t); box('Triple clamp', p, (.27, h, .09), 'Front', 'metal', .01, (-22, 0, 0))
    top = ax(1.25)
    for s in (-1, 1): cyl('Bar riser', (s * .035, top[1], top[2]), (s * .035, top[1] + .07, top[2] + .02), .016, 'Front', 'metal', 8)
    by, bz = top[1] + .07, top[2] + .02
    cyl('Handlebar', (-.24, by, bz), (.24, by, bz), .013, 'Front', 'metal', 10)
    cyl('Bar pad', (-.1, by + .006, bz), (.1, by + .006, bz), .027, 'Front', 'engine', 10)  # 0.89: wraps the bar (was flush below it)
    for s in (-1, 1):
        cyl('Handlebar bend', (s * .24, by, bz), (s * (gx - .1), gy - .005, gz - .005), .013, 'Front', 'metal', 10)
        cyl('Grip', (s * (gx - .1), gy - .005, gz - .005), (s * (gx + .04), gy, gz), .021, 'Front', 'rubber', 10)
        box('Lever', (s * (gx - .08), gy + .01, gz + .05), (.13, .012, .02), 'Front', 'metal', .004, (0, s * 18, 0))
    pc = Vector(ax(1.13)) + Vector((0, -.06, .10))
    hull('Number plate', [(-.13, pc.y - .17, pc.z + .03), (.13, pc.y - .17, pc.z + .03), (.13, pc.y - .17, pc.z + .06), (-.13, pc.y - .17, pc.z + .06),
                          (-.12, pc.y + .12, pc.z - .08), (.12, pc.y + .12, pc.z - .08), (.12, pc.y + .12, pc.z - .05), (-.12, pc.y + .12, pc.z - .05)], 'Front', 'paint', .02, 2)
    lc = pc + Vector((0, -.06, .055))
    cyl('Headlamp', (0, lc.y + .02, lc.z - .005), (0, lc.y - .006, lc.z + .03), .055, 'Front', 'lamp', 16)
    lo = ax(1.0)
    strip('Beak fender', [(lo[2] - .30, lo[1] - .06), (lo[2] - .12, lo[1] - .01), (lo[2] + .06, lo[1]), (lo[2] + .24, lo[1] - .03), (lo[2] + .40, lo[1] - .10), (lo[2] + .50, lo[1] - .18)], .16, .015, 0, 'Front', 'paint')


def drifter(B):
    ax = axis_fn(B); fy, fz = B['front']; ry_, rz = B['rear']; gx, gy, gz = B['grip']
    # --- frame (low, long) ---
    cyl('Steering head', ax(.98), ax(1.20), .04, 'Body', 'metal', 12)
    cyl('Backbone', ax(1.10), (0, .40, -.30), .034, 'Body', 'engine', 10)
    cyl('Down tube', ax(1.0), (0, -.10, .30), .032, 'Body', 'engine', 10)
    cyl('Lower rail', (0, -.10, .30), (0, -.12, -.30), .03, 'Body', 'engine', 10)
    for s in (-1, 1):
        cyl('Seat rail', (s * .08, .30, -.25), (s * .09, .36, -.95), .02, 'Body', 'engine', 8)
        hull('Swing arm', [(s * .07 - .02, -.06, -.30), (s * .07 + .02, -.06, -.30), (s * .11 + .014, ry_ - .02, rz), (s * .11 - .014, ry_ - .02, rz),
                           (s * .07 - .02, .0, -.30), (s * .07 + .02, .0, -.30), (s * .11 + .014, ry_ + .03, rz), (s * .11 - .014, ry_ + .03, rz)], 'Body', 'chrome', .008, 1)
        cyl('Rear shock', (s * .12, ry_ + .05, rz + .08), (s * .10, .36, -.62), .03, 'Body', 'chrome', 10)
        # forward controls: pegs and the pedal mounts, well ahead of the engine
        cyl('Forward peg', (s * .24, .09, .36), (s * .33, .09, .36), .02, 'Body', 'rubber', 8)
        cyl('Control mount', (s * .12, .02, .30), (s * .24, .09, .36), .016, 'Body', 'chrome', 8)
    cyl('Axle rear', (-.13, ry_, rz), (.13, ry_, rz), .016, 'Body', 'metal', 8)
    # --- V-twin: crankcase, two finned cylinders in a V, air cleaner, primary cover ---
    hull('Crankcase', [(-.13, -.10, -.22), (.13, -.10, -.22), (.13, -.10, .14), (-.13, -.10, .14), (-.13, .10, -.20), (.13, .10, -.20), (.13, .10, .12), (-.13, .10, .12)], 'Body', 'engine', .035)
    for zc, lean in ((.10, 28), (-.16, -28)):
        d = Vector((0, math.cos(math.radians(lean)), math.sin(math.radians(lean)))); b0 = Vector((0, .08, zc))
        for i in range(6):
            c = b0 + d * (.06 + i * .035)
            cyl('Cylinder fin', tuple(c - d * .006), tuple(c + d * .006), .085 - i * .003, 'Body', 'chrome' if i % 2 == 0 else 'engine', 16)
        cyl('Cylinder', tuple(b0), tuple(b0 + d * .28), .062, 'Body', 'engine', 14)
        cyl('Rocker box', tuple(b0 + d * .27), tuple(b0 + d * .33), .07, 'Body', 'chrome', 14)
    blob('Air cleaner', (.15, .26, -.03), (.03, .10, .10), 'Body', 'chrome', 18, 8)
    blob('Primary cover', (-.15, -.02, -.20), (.035, .12, .20), 'Body', 'chrome', 18, 8)
    # two long chrome pipes along the right, low, ending in fishtails past the rear axle
    for k, (zc, h) in enumerate(((.10, .26), (-.16, .24))):
        tube('Header', [(.07, h, zc + .05), (.17, h - .05, zc + .04), (.21, .02 - k * .06, zc - .05), (.22, -.04 - k * .07, -.40)], .03, 'Body', 'chrome', 8)
        cyl('Pipe', (.22, -.04 - k * .07, -.40), (.23, .02 - k * .06, -1.18), .035, 'Body', 'chrome', 14)
    # --- bodywork: teardrop tank, valanced fenders, low stepped seat ---
    secs = []
    for z, w, y0, y1 in ((.48, .07, .48, .58), (.38, .14, .44, .66), (.18, .17, .42, .71), (-.02, .15, .40, .68), (-.12, .09, .40, .60)):
        c = (0, (y0 + y1) / 2, z); secs.append(ring(c, w, (y1 - y0) / 2, 14, 'z'))
    loft('Teardrop tank', secs, 'Body', 'paint')
    box('Tank badge strip', (0, .715, .18), (.03, .01, .3), 'Body', 'chrome', .004)
    blob('Fuel cap', (.06, .70, .26), (.03, .012, .03), 'Body', 'chrome', 10, 5)
    hull('Seat', [(-.13, .26, -.08), (.13, .26, -.08), (.16, .26, -.52), (-.16, .26, -.52), (-.10, .33, -.10), (.10, .33, -.10), (.15, .32, -.46), (-.15, .32, -.46)], 'Body', 'rubber', .04, 3)
    hull('Pillion', [(-.12, .32, -.50), (.12, .32, -.50), (.12, .36, -.82), (-.12, .36, -.82), (-.10, .40, -.52), (.10, .40, -.52), (.10, .43, -.80), (-.10, .43, -.80)], 'Body', 'rubber', .04, 3)
    Rr = B['Rr']; prof = [(rz + math.cos(a) * (Rr + .05), ry_ + math.sin(a) * (Rr + .05)) for a in [math.radians(d) for d in range(10, 171, 10)]]
    strip('Rear fender', prof, .30, .02, 0, 'Body', 'paint')
    for s in (-1, 1): strip('Rear valance', [(p[0], p[1]) for p in prof[3:-3]], .03, .02, s * .155, 'Body', 'paint')
    box('Tail lamp', (0, ry_ + Rr + .08, rz - .30), (.12, .05, .05), 'Body', 'tail', .01)
    box('Tail lamp chrome', (0, ry_ + Rr + .06, rz - .28), (.14, .07, .04), 'Body', 'chrome', .01)
    # --- front end: raked chrome forks, big round headlamp, wide pulled-back bars, valanced front fender ---
    for s in (-1, 1):
        x = s * .11
        cyl('Fork slider', (x, fy, fz), (x, ax(.50)[1], ax(.50)[2]), .036, 'Front', 'chrome', 12)
        cyl('Fork tube', (x, ax(.48)[1], ax(.48)[2]), (x, ax(1.22)[1], ax(1.22)[2]), .027, 'Front', 'chrome', 12)
    cyl('Axle front', (-.13, fy, fz), (.13, fy, fz), .016, 'Front', 'metal', 8)
    for t, h in ((.98, .04), (1.20, .045)):
        p = ax(t); box('Triple clamp', p, (.30, h, .10), 'Front', 'chrome', .012, (-31, 0, 0))
    hc = Vector(ax(1.08)) + Vector((0, 0, .17))
    cyl('Headlamp bucket', tuple(hc + Vector((0, 0, -.10))), tuple(hc + Vector((0, 0, .04))), .11, 'Front', 'chrome', 20)
    cyl('Headlamp', tuple(hc + Vector((0, 0, .035))), tuple(hc + Vector((0, 0, .05))), .095, 'Front', 'lamp', 20)
    for s in (-1, 1): blob('Passing lamp', (s * .19, hc.y - .12, hc.z - .02), (.045, .045, .04), 'Front', 'lamp', 12, 6)
    top = ax(1.21)
    for s in (-1, 1): cyl('Riser', (s * .05, top[1], top[2]), (s * .05, top[1] + .08, top[2]), .018, 'Front', 'chrome', 8)
    by, bz = top[1] + .08, top[2]
    tube('Handlebar', [(-gx - .04, gy, gz), (-.30, gy - .01, gz + .06), (-.12, by, bz + .02), (.12, by, bz + .02), (.30, gy - .01, gz + .06), (gx + .04, gy, gz)], .014, 'Front', 'chrome', 8)
    for s in (-1, 1):
        cyl('Grip', (s * (gx - .09), gy, gz + .02), (s * (gx + .05), gy, gz - .01), .022, 'Front', 'rubber', 10)
        cyl('Mirror stem', (s * .30, gy, gz + .06), (s * .34, gy + .22, gz + .02), .007, 'Front', 'chrome', 6)
        blob('Mirror', (s * .345, gy + .24, gz + .02), (.05, .035, .012), 'Front', 'chrome', 12, 6)
    Rf = B['Rf']; fprof = [(fz + math.cos(a) * (Rf + .045), fy + math.sin(a) * (Rf + .045)) for a in [math.radians(d) for d in range(20, 161, 10)]]
    strip('Front fender', fprof, .22, .018, 0, 'Front', 'paint')
    for s in (-1, 1): strip('Front valance', fprof[2:-2], .03, .018, s * .115, 'Front', 'paint')


def build(name):
    Bk = BIKES[name]; bpy.ops.wm.read_factory_settings(use_empty=True)
    (scrambler if name == 'Scrambler' else drifter)(Bk)
    fy, fz = Bk['front']; ry_, rz = Bk['rear']
    for g, (y, z), R, rx, front in (('WheelFront', (fy, fz), Bk['Rf'], Bk['wf'], True), ('WheelRear', (ry_, rz), Bk['Rr'], Bk['wr'], False)):
        s0 = len(kit.PARTS)
        if name == 'Scrambler': wheel(g, R, rx, 26 if front else 24, 18, front, not front)
        else: wheel(g, R, rx, 0, 0, front, False, fat=True)
        place(made(s0), at((0, y, z)))
    print(name, 'wheel bottoms', fy - Bk['Rf'], ry_ - Bk['Rr'], 'ground', GROUND)
    out = kit.join_all()
    for ob in out:
        g = ob.name.split('__')[0]
        if g == 'WheelFront': kit.set_origin(ob, (0, fy, fz))
        if g == 'WheelRear': kit.set_origin(ob, (0, ry_, rz))
    kit.export(name)


if __name__ == '__main__':
    args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    for n in (args or list(BIKES)): build(n)
