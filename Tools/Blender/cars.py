"""0.75 Part A: the two cars, Street Classic (coupe) and Longroof GT (wagon), generated in Blender from this script.

Run:  blender --background --factory-startup --python Tools/Blender/cars.py -- [StreetClassic|LongroofGT]

Fitted to the existing vehicles (not the other way round): wheel centres (+/-track, -0.20, +/-wheelbase/2), wheel
radius 0.33, ground at y = -0.53. The driver (Tools/Blender/rider.py, pose Car) sits at the seat point H, with the
steering wheel at H + (0, 0.36, 0.36) tilted 25 degrees, so the rider's hands are on its rim; the cabin is tall enough
for the cowboy hat. Output SourceArt/Blender/<Car>.blend and Assets/Resources/VehicleModels/<Car>.fbx.
Groups: Body (fixed), Steer (steering wheel: turns about its column, origin on the wheel centre), WheelFL / WheelFR /
WheelRL / WheelRR (origin on the axle, axle along x). Slots: paint (body colour), metal, chrome, engine, rubber, lamp,
tail, glass, interior.
"""
import bpy, bmesh, math, sys, os
from mathutils import Vector, Matrix
sys.path.insert(0, os.path.dirname(__file__))
import kit
from kit import B, box, hull, cyl, blob, strip, loft, made, place, at

CARS = {
    'StreetClassic': dict(track=.925, axle=1.30, H=(-.40, .04, -.15), length=1.88, hw=.965, belt=.43, roof=1.025,
                          wind=(.62, .12), rear=(-.52, -1.12), bpost=-.45, wagon=False,
                          keys=[(1.88, .86, .27, .02, -.10), (1.80, .93, .35, .04, -.18), (1.60, .955, .385, .05, -.26), (1.0, .965, .405, .05, -.30),
                                (.62, .965, .42, .04, -.30), (0, .965, .43, .03, -.30), (-.8, .97, .44, .03, -.30), (-1.62, .965, .445, .03, -.27),
                                (-1.82, .93, .41, .02, -.18), (-1.89, .86, .33, .01, -.10)]),
    'LongroofGT': dict(track=1.0, axle=1.50, H=(-.43, .08, -.10), length=2.26, hw=1.035, belt=.49, roof=1.085,
                       wind=(.95, .35), rear=(-1.95, -2.17), bpost=-.42, cpost=-1.18, wagon=True,
                       keys=[(2.26, .93, .34, .02, -.08), (2.17, 1.0, .42, .04, -.17), (1.95, 1.025, .45, .05, -.25), (1.2, 1.035, .47, .05, -.29),
                             (.95, 1.035, .48, .04, -.29), (0, 1.035, .49, .03, -.29), (-1.2, 1.035, .495, .03, -.29), (-1.98, 1.03, .50, .03, -.26),
                             (-2.18, .99, .48, .02, -.17), (-2.27, .93, .40, .01, -.08)]),
}
WY, R_ARCH = -.20, .405


def section(z, hw, yt, crown, yb, floor=None):
    """Body cross-section ring (Unity points), 18 points, counter-clockwise seen from the front. With floor, the ring is
    a tub (the cockpit between the doors, open down to the cabin floor); without, a closed top. Same point count."""
    xi = hw - .12
    if floor is None: inner = [(hw * .66, yt + crown * .9), (hw * .45, yt + crown * 1.0), (hw * .22, yt + crown * 1.04)]
    else: inner = [(xi, yt + crown * .55), (xi - .01, floor + .04), (xi * .5, floor)]
    half = [(hw * .80, yb), (hw * .97, yb + .05), (hw, yb + .13), (hw, yt - .10), (hw * .985, yt - .03), (hw * .9, yt + crown * .55)] + inner
    pts = [(0, yb - .005)] + half + [(0, floor if floor is not None else yt + crown * 1.05)] + [(-x, y) for x, y in reversed(half)]
    return [(x, y, z) for x, y in pts]


def interp(keys, z):
    for (z0, *a), (z1, *b) in zip(keys, keys[1:]):
        if z1 <= z <= z0:
            t = (z0 - z) / (z0 - z1); return [u + (v - u) * t for u, v in zip(a, b)]
    return keys[0][1:] if z > keys[0][0] else keys[-1][1:]


def body(C):
    G = 'Body'; keys = C['keys']; A = C['axle']
    zs = sorted({k[0] for k in keys} | {round(A + s * d, 3) for s in (-1, 1) for d in [i * .045 for i in range(-10, 11)] if abs(d) <= R_ARCH + .02}
                | {round(-A + d, 3) for d in [i * .045 for i in range(-10, 11)]}, reverse=True)
    zs = sorted(set(z for z in zs if keys[-1][0] <= z <= keys[0][0]) | {round(C['wind'][0] - .031, 3), round(C['wind'][0] - .029, 3), round(C['rear'][1] + .029, 3), round(C['rear'][1] + .031, 3)}, reverse=True)
    secs = []
    for z in zs:
        hw, yt, crown, yb = interp(keys, z)
        for ax in (A, -A):  # wheel arches: the body's lower edge follows a circle round each wheel
            dz = z - ax
            if abs(dz) < R_ARCH: yb = max(yb, WY + math.sqrt(R_ARCH ** 2 - dz ** 2))
        if abs(abs(z) - A) < R_ARCH + .05: hw += .025 * max(0, 1 - abs(abs(z) - A) / (R_ARCH + .05))  # fender flare
        cockpit = C['rear'][1] + .03 < z < C['wind'][0] - .03
        secs.append(section(z, hw, yt, crown, yb, C['H'][1] - .30 if cockpit else None))
    kit.loft('Body shell', secs, G, 'paint', cap=True)
    # arch lips
    for ax in (A, -A):
        prof = [(ax + math.cos(a) * (R_ARCH + .01), WY + math.sin(a) * (R_ARCH + .01)) for a in [math.radians(d) for d in range(8, 173, 15)]]
        hw = interp(keys, ax)[0] + .025
        for s in (-1, 1): strip('Arch lip', prof, .05, .025, s * (hw - .012), G, 'engine')


def pane(name, quad, G, slot='glass', t=.012):
    """A thin panel through 4 Unity points (in order round its edge)."""
    q = [Vector(p) for p in quad]; n = (q[1] - q[0]).cross(q[3] - q[0]).normalized() * t
    pts = [tuple(p - n / 2) for p in q] + [tuple(p + n / 2) for p in q]
    return hull(name, pts, G, slot, .004 if slot != 'glass' else 0, 1)


def cabin(C):
    G = 'Body'; belt, roof = C['belt'], C['roof']; zf, zr0 = C['wind']; zr, zd = C['rear']
    hw = C['hw']; xb, xr = hw * .93, hw * .78  # glass x at the belt and at the roof edge
    # roof
    hull('Roof', [(-xr, roof - .045, zr - .02), (xr, roof - .045, zr - .02), (xr, roof - .045, zr0 + .02), (-xr, roof - .045, zr0 + .02),
                  (-xr + .03, roof, zr + .01), (xr - .03, roof, zr + .01), (xr - .03, roof, zr0), (-xr + .03, roof, zr0)], G, 'paint', .03, 2)
    # windscreen and rear glass (with frames)
    pane('Windscreen', [(-xb + .03, belt + .01, zf), (xb - .03, belt + .01, zf), (xr - .02, roof - .04, zr0 + .01), (-xr + .02, roof - .04, zr0 + .01)], G)
    pane('Rear glass', [(-xb + .03, belt + .02, zd), (-xr + .02, roof - .04, zr - .01), (xr - .02, roof - .04, zr - .01), (xb - .03, belt + .02, zd)], G)
    for s in (-1, 1):
        cyl('A pillar', (s * (xb - .01), belt, zf + .01), (s * (xr - .005), roof - .03, zr0), .035, G, 'paint', 8)
        cyl('Rear pillar', (s * (xb - .01), belt, zd - .01), (s * (xr - .005), roof - .03, zr), .04 if not C['wagon'] else .035, G, 'paint', 8)
        b = C['bpost']; cyl('B pillar', (s * xb, belt, b), (s * xr, roof - .03, b + .03), .04, G, 'engine', 8)
        posts = [zf, b, zd] if not C['wagon'] else [zf, b, C['cpost'], zd]
        for i, (z0, z1) in enumerate(zip(posts, posts[1:])):
            # side glass between posts: lower edge on the belt, upper edge under the roof
            f0 = 0 if i else (zr0 - zf); f1 = 0 if i < len(posts) - 2 else (zr - zd)
            q = [(s * xb, belt + .015, z0 - .03), (s * xr, roof - .05, z0 - .03 + f0 * .96), (s * xr, roof - .05, z1 + .03 + f1 * .96), (s * xb, belt + .015, z1 + .03)]
            pane('Side glass', q, G)
        cyl('Drip rail', (s * xr, roof - .045, zr - .02), (s * xr, roof - .045, zr0 + .02), .012, G, 'chrome', 6)
        cyl('Belt trim', (s * (hw + .002), belt - .005, zf + .05), (s * (hw + .002), belt - .005, zd - .02), .011, G, 'chrome', 6)
    if not C['wagon']:  # rear deck lid between the glass and the tail
        pass


def interior(C):
    G = 'Body'; H = Vector(C['H']); belt = C['belt']
    for side in (1, -1):
        hx = H.x if side == 1 else -H.x
        hull('Seat cushion', [(hx - .2, H.y - .16, H.z - .22), (hx + .2, H.y - .16, H.z - .22), (hx + .2, H.y - .16, H.z + .26), (hx - .2, H.y - .16, H.z + .26),
                              (hx - .2, H.y - .03, H.z - .22), (hx + .2, H.y - .03, H.z - .22), (hx + .2, H.y - .04, H.z + .26), (hx - .2, H.y - .04, H.z + .26)], G, 'interior', .03, 2)
        hull('Seat back', [(hx - .2, H.y - .05, H.z - .30), (hx + .2, H.y - .05, H.z - .30), (hx + .2, H.y - .05, H.z - .17), (hx - .2, H.y - .05, H.z - .17),
                           (hx - .19, H.y + .56, H.z - .40), (hx + .19, H.y + .56, H.z - .40), (hx + .19, H.y + .56, H.z - .29), (hx - .19, H.y + .56, H.z - .29)], G, 'interior', .03, 2)
    # rear bench
    rz = H.z - .85
    hull('Rear seat', [(-.75, H.y - .1, rz - .2), (.75, H.y - .1, rz - .2), (.75, H.y - .1, rz + .25), (-.75, H.y - .1, rz + .25),
                       (-.75, H.y + .45, rz - .3), (.75, H.y + .45, rz - .3), (.75, H.y + .02, rz + .25), (-.75, H.y + .02, rz + .25)], G, 'interior', .03, 2)
    # dashboard with a binnacle in front of the driver
    dz = H.z + .56
    hull('Dashboard', [(-C['hw'] * .9, belt - .15, dz), (C['hw'] * .9, belt - .15, dz), (C['hw'] * .9, belt - .12, dz + .30), (-C['hw'] * .9, belt - .12, dz + .30),
                       (-C['hw'] * .9, belt + .05, dz + .03), (C['hw'] * .9, belt + .05, dz + .03), (C['hw'] * .9, belt + .02, dz + .30), (-C['hw'] * .9, belt + .02, dz + .30)], G, 'interior', .02, 1)
    box('Binnacle', (H.x, belt + .07, dz + .06), (.30, .06, .14), G, 'interior', .02)
    box('Dials', (H.x, belt + .07, dz - .012), (.24, .045, .01), G, 'metal', .004)
    # 0.88 (BUG-004): the carpet's top was at H.y - .30, the same height as the body shell's cabin floor (section(), floor
    # H.y - .30), and the two fought for the same pixels (flashing floorboards). The carpet now straddles the shell floor:
    # its top 1.5 cm above it, its bottom below it, so it is the one floor seen from inside.
    hull('Floor', [(-C['hw'] * .9, H.y - .325, H.z - 1.2), (C['hw'] * .9, H.y - .325, H.z - 1.2), (C['hw'] * .9, H.y - .325, dz + .25), (-C['hw'] * .9, H.y - .325, dz + .25),
                   (-C['hw'] * .9, H.y - .285, H.z - 1.2), (C['hw'] * .9, H.y - .285, H.z - 1.2), (C['hw'] * .9, H.y - .285, dz + .25), (-C['hw'] * .9, H.y - .285, dz + .25)], G, 'interior', 0, 1)
    # steering column (fixed) and wheel (Steer: origin on the wheel centre, turns about the column)
    t = math.radians(25); c = H + Vector((0, .36, .36)); u = Vector((0, math.cos(t), math.sin(t))); n = Vector((0, -math.sin(t), math.cos(t)))
    cyl('Steering column', tuple(c + n * .02), tuple(c + n * .30), .026, G, 'interior', 10)
    S = 'Steer'; bm = bmesh.new()
    seg, side, R, r = 28, 6, .175, .016
    vs = []
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
    cyl('Steering hub', tuple(c - n * .005), tuple(c + n * .05), .045, S, 'metal', 12)
    for ang in (90, 210, 330):
        a = math.radians(ang); cyl('Steering spoke', tuple(c), tuple(c + Vector((1, 0, 0)) * math.cos(a) * R + u * math.sin(a) * R), .012, S, 'metal', 6)
    return tuple(c), tuple(n)


def details(C):
    G = 'Body'; L = C['length']; hw = C['hw']; keys = C['keys']; nose = interp(keys, L - .06); tail = interp(keys, -L + .06)
    # bumpers (chrome), front and rear
    for zf, sgn in ((L + .03, 1), (-L - .03, -1)):
        hull('Bumper', [(-hw * .97, -.16, zf - sgn * .10), (hw * .97, -.16, zf - sgn * .10), (hw * .93, -.15, zf + sgn * .04), (-hw * .93, -.15, zf + sgn * .04),
                        (-hw * .97, -.02, zf - sgn * .10), (hw * .97, -.02, zf - sgn * .10), (hw * .93, -.03, zf + sgn * .04), (-hw * .93, -.03, zf + sgn * .04)], G, 'chrome', .025, 2)
        for s in (-1, 1): box('Bumper guard', (s * hw * .45, -.09, zf + sgn * .055), (.06, .16, .06), G, 'rubber', .015)
    # grille and lamps on the nose
    yh = nose[1] - .13
    # 0.89 Part F: the grille face 1 cm behind the bezel fronts and the bars (it was on their plane: flicker), and the bars
    # end short of the inner bezels instead of running across their faces.
    box('Grille', (0, yh, L - .015), (hw * 1.1, .17, .06), G, 'engine', .01)
    for i in range(5): box('Grille bar', (0, yh - .06 + i * .03, L + .02), (2 * (hw * .52 - .07), .008, .012), G, 'chrome', 0)
    for s in (-1, 1):
        for k, x in enumerate((.70, .52)):
            cyl('Headlamp bezel', (s * hw * x, yh, L - .03), (s * hw * x, yh, L + .025), .075 if k == 0 else .062, G, 'chrome', 16)
            cyl('Headlamp', (s * hw * x, yh, L + .02), (s * hw * x, yh, L + .032), .062 if k == 0 else .05, G, 'lamp', 16)
        box('Indicator', (s * hw * .80, -.075, L + .035), (.12, .04, .02), G, 'lamp', .006)
        # tail lamps: wide strips across the tail
        box('Tail lamp', (s * hw * .62, tail[1] - .12, -L - .01), (hw * .55, .09, .04), G, 'tail', .01)
        box('Reverse lamp', (s * hw * .22, tail[1] - .12, -L - .01), (.10, .06, .04), G, 'lamp', .008)
        cyl('Exhaust tip', (s * .55, -.22, -L + .25), (s * .55, -.22, -L - .08), .035, G, 'chrome', 10)
        # mirrors, door handles
        zm = C['wind'][0] - .05
        box('Mirror arm', (s * (hw + .03), C['belt'] + .04, zm), (.08, .03, .05), G, 'chrome', .006)
        blob('Mirror', (s * (hw + .09), C['belt'] + .06, zm), (.055, .045, .035), G, 'paint', 10, 6)
        box('Door handle', (s * (hw + .006), C['belt'] - .08, C['bpost'] + .12), (.02, .03, .14), G, 'chrome', .006)
    box('Number plate', (0, tail[1] - .26, -L - .02), (.36, .13, .02), G, 'metal', .006)
    box('Front plate', (0, -.10, L + .085), (.32, .10, .015), G, 'metal', .006)
    # hood bulge (paint), for the coupe a scoop; for the wagon a roof rack
    zc = C['wind'][0]
    if not C['wagon']:
        hull('Hood bulge', [(-.32, nose[1] + .01, zc + .25), (.32, nose[1] + .01, zc + .25), (.30, nose[1] - .02, L - .2), (-.30, nose[1] - .02, L - .2),
                            (-.26, nose[1] + .055, zc + .28), (.26, nose[1] + .055, zc + .28), (.24, nose[1] + .02, L - .25), (-.24, nose[1] + .02, L - .25)], G, 'paint', .02, 2)
        box('Hood scoop', (0, nose[1] + .075, zc + .55), (.34, .05, .03), G, 'engine', .01)
        # trunk lip spoiler
        hull('Spoiler', [(-.8, tail[1] + .02, -L + .12), (.8, tail[1] + .02, -L + .12), (.8, tail[1] + .02, -L + .02), (-.8, tail[1] + .02, -L + .02),
                         (-.8, tail[1] + .06, -L + .10), (.8, tail[1] + .06, -L + .10), (.8, tail[1] + .065, -L + .0), (-.8, tail[1] + .065, -L + .0)], G, 'paint', .01, 1)
    else:
        zr0, zr = C['wind'][1], C['rear'][0]
        for s in (-1, 1): cyl('Roof rail', (s * .62, C['roof'] + .05, zr0 - .15), (s * .62, C['roof'] + .05, zr + .1), .02, G, 'chrome', 8)
        for z in (zr0 - .5, zr + .45):
            cyl('Rack bar', (-.62, C['roof'] + .05, z), (.62, C['roof'] + .05, z), .018, G, 'chrome', 6)
            for s in (-1, 1): box('Rail foot', (s * .62, C['roof'] + .02, z), (.05, .05, .08), G, 'engine', .008)
        # bright side trim between the wheel arches
        for s in (-1, 1):
            box('Side trim', (s * (hw + .004), C['belt'] - .17, -.48), (.012, .05, 2.9), G, 'chrome', .004)


def wheel(group, pos, left, wagon):
    s0 = len(kit.PARTS)
    kit.tyre(group, .255, .105, .075, 28, 8, knobs=0, squared=.86)
    cyl(group + ' rim', (-.07, 0, 0), (.07, 0, 0), .19, group, 'metal', 24, cap=False)
    cyl(group + ' rim face', (.06, 0, 0), (.075, 0, 0), .195, group, 'chrome' if not wagon else 'metal', 24)
    cyl(group + ' hub', (.07, 0, 0), (.095, 0, 0), .06, group, 'chrome', 14)
    n = 5
    for i in range(n):
        a = i * 2 * math.pi / n
        if wagon:  # five-spoke alloy
            box(group + ' spoke', (.082, math.cos(a) * .11, math.sin(a) * .11), (.016, .055, .14), group, 'metal', .006, (math.degrees(a) - 90, 0, 0))  # 0.89: clear of both hub faces (4-5 mm)
        else:  # rally wheel: slots between the hub and the rim
            box(group + ' slot', (.077, math.cos(a + .6) * .13, math.sin(a + .6) * .13), (.012, .04, .07), group, 'engine', .006, (math.degrees(a + .6) - 90, 0, 0))
        cyl(group + ' nut', (.093, math.cos(a) * .035, math.sin(a) * .035), (.104, math.cos(a) * .035, math.sin(a) * .035), .008, group, 'metal', 6)
    place(made(s0), at(pos, left))


def build(name):
    C = CARS[name]
    bpy.ops.wm.read_factory_settings(use_empty=True)
    body(C); cabin(C); details(C); c, n = interior(C)
    T, A = C['track'], C['axle']
    pos = {'WheelFL': (-T, WY, A), 'WheelFR': (T, WY, A), 'WheelRL': (-T, WY, -A), 'WheelRR': (T, WY, -A)}
    for g, p in pos.items(): wheel(g, p, p[0] < 0, C['wagon'])
    out = kit.join_all()
    for ob in out:
        g = ob.name.split('__')[0]
        if g in pos: kit.set_origin(ob, pos[g])
        if g == 'Steer': kit.set_origin(ob, c)
    print(name, 'steering wheel centre', c, 'column (towards the dash)', n)
    kit.export(name)


if __name__ == '__main__':
    args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    for name in (args or list(CARS)): build(name)
