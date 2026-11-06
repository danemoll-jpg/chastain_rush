"""Chastain Rush, Round 94 A (Dan, 2026-11-05: "I would love to have some nicer more realistic looking cars with some speed and handling. Would love
cars looking like 1961 Jaguar E-type, 1965 Shelby Cobra, lambos, porshes etc"): eight new player cars, generated in Blender from this script with the
shared kit (Tools/Blender/kit.py) and the car conventions of cars.py.

Run:  F:\\blender\\blender.exe --background --factory-startup --python Tools/Blender/cars94.py -- [Marlow Copperhead ...]

OUR OWN designs in the spirit of a type and an era, invented names, no make, badge, emblem or logo anywhere:
  Marlow      long-bonnet 1960s British coupe: dome bonnet with a centre bulge, oval mouth, faired-in lamps, fastback hatch, wire wheels (straight six)
  Copperhead  small 1960s V8 roadster: wide flared arches, big oval mouth, round lamps, side pipes along the sills, roll hoop, a low screen
  Stiletto    1970s wedge supercar, mid-engined: a flat knife-edge nose, a single slope to the screen, NACA ducts and side intakes, louvres (V12)
  Tempesta    modern angular V12 supercar: arrowhead lamps, big side intakes, a hexagon-ish engine cover, wide low stance, centre-lock wheels
  Falke       1960s / 70s rear-engined coupe: upright round lamps on the wings, sloping fastback over a louvred engine lid (flat six)
  Falke R     the modern one of the family: wider hips, oval lamps with light bars, a full-width tail light, a ducktail spoiler (flat six)
  Brawler     1960s muscle car: long hood with twin scoops, a hidden-lamp full-width grille, coke-bottle hips, a tunnel-back roof (V8)
  Meridian    modern grand tourer, front-engined: a long bonnet, a big grille, fender vents, a fastback roof (V8)
Conventions (cars.py): Unity vehicle space, wheel centres (+/-T, -0.20, +/-A), wheel outer radius 0.33, ground y = -0.53; the driver (rider.py pose
Car) at the seat point H, steering wheel at H + (0, .36, .36) tilted 25 degrees. The cabin clears the rider's head (H.y + .86) by 4 cm: low cars hide
the rider's hat (VehicleProfile.LowRoof). Groups Body, Steer, WheelFL / FR / RL / RR. Slots paint, metal, chrome, engine, rubber, lamp, tail, glass,
interior, caliper. Output SourceArt/Blender/<Car>.blend and Assets/Resources/VehicleModels/<Car>.fbx.
"""
import bpy, bmesh, math, sys, os
from mathutils import Vector, Matrix
sys.path.insert(0, os.path.dirname(__file__))
import kit
from kit import B, box, hull, cyl, blob, strip, made, place, at

kit.COLORS['caliper'] = (.75, .08, .05)
WY, RT = -.20, .33          # wheel centre height, tyre outer radius
G = 'Body'


# ---------------------------------------------------------------- smooth key curves
def curve(keys, z):
    """Catmull-Rom through (z, v) keys (any order), clamped at the ends."""
    k = sorted(keys)
    if z <= k[0][0]: return k[0][1]
    if z >= k[-1][0]: return k[-1][1]
    for i in range(len(k) - 1):
        if k[i][0] <= z <= k[i + 1][0]: break
    z0, v0 = k[max(i - 1, 0)]; z1, v1 = k[i]; z2, v2 = k[i + 1]; z3, v3 = k[min(i + 2, len(k) - 1)]
    t = (z - z1) / (z2 - z1)
    m1 = (v2 - v0) / (z2 - z0) * (z2 - z1) if z2 != z0 else 0
    m2 = (v3 - v1) / (z3 - z1) * (z2 - z1) if z3 != z1 else 0
    t2, t3 = t * t, t * t * t
    return (2 * t3 - 3 * t2 + 1) * v1 + (t3 - 2 * t2 + t) * m1 + (-2 * t3 + 3 * t2) * v2 + (t3 - t2) * m2


def loft_open(name, sections, group, slot, bevel=0.0):
    """A lofted surface through open point rows (no wrap, no caps)."""
    n = len(sections[0]); v = [B(*p) for s in sections for p in s]; f = []
    for i in range(len(sections) - 1):
        for j in range(n - 1): f.append((i * n + j, i * n + j + 1, (i + 1) * n + j + 1, (i + 1) * n + j))
    ob = kit.mesh_ob(name, v, f)
    return kit.finish(ob, group, slot, bevel, 1)


def lerp(a, b, t): return a + (b - a) * t


def ease(t): return math.sin(max(0, min(1, t)) * math.pi / 2)


# ---------------------------------------------------------------- the body shell
class Car:
    def __init__(s, C): s.C = C; s.A = C['A']; s.T = C['T']; s.H = Vector(C['H'])

    def W(s, z): return curve(s.C['hw'], z)
    def TOP(s, z): return curve(s.C['top'], z)
    def BOT(s, z): return curve(s.C['bot'], z)
    def SH(s, z): return curve(s.C['sh'], z)
    def CR(s, z): return curve(s.C['crown'], z) if isinstance(s.C['crown'], list) else s.C['crown']

    def flare(s, z):
        f = s.C.get('flare', .03); out = 0
        for ax in (s.A, -s.A):
            d = abs(z - ax) / (RT + .22)
            if d < 1: out = max(out, f * (math.cos(d * math.pi) * .5 + .5))
        return out

    def door_top(s, z): return s.TOP(z) + s.CR(z) * .25

    def surf_y(s, x, z):
        """The body's upper surface height at (x, z) (a closed section's top half)."""
        pts = s.section(z, False); n = len(pts) // 2
        top = sorted({(abs(px), py) for px, py, _ in pts[1:n + 1]}, key=lambda p: p[0])
        x = abs(x); best = None
        hi = [p for p in pts[1:n + 1]]
        # walk the right half from the shoulder up to the centre (x falling): the first segment spanning x
        sh = max(range(len(hi)), key=lambda i: hi[i][0])
        seq = hi[sh:]
        for (x0, y0, _), (x1, y1, _) in zip(seq, seq[1:]):
            if min(x0, x1) <= x <= max(x0, x1) and x0 != x1: return y0 + (y1 - y0) * (x - x0) / (x1 - x0)
        return seq[-1][1]
    def floor(s): return s.H.y - .30

    def section(s, z, tub):
        hw = s.W(z) + s.flare(z); yt = s.TOP(z); yb = s.BOT(z); ysh = s.SH(z); cr = s.CR(z)
        ra = RT + s.C.get('arch_gap', .045)
        for ax in (s.A, -s.A):
            dz = z - ax
            if abs(dz) < ra: yb = max(yb, WY + math.sqrt(ra * ra - dz * dz))
        yb = min(yb, ysh - .035)
        lo = ysh - yb; hi = yt - ysh
        tuck = s.C.get('tuck', .84)
        half = [(hw * tuck, yb), (hw * lerp(tuck, 1, .7), yb + lo * .18), (hw * .993, yb + lo * .55), (hw, ysh),
                (hw * .99, ysh + hi * .40), (hw * .955, ysh + hi * .80), (hw * .895, yt + cr * .25)]
        if tub:
            xi = hw * .895 - .07; fl = s.floor()
            top = [(xi, yt + cr * .1), (xi - .01, fl + .05)]; centre = (0, fl)
            top.append((xi * .45, fl))
        else:
            top = [(hw * .70, yt + cr * .62), (hw * .40, yt + cr * .90), (hw * .14, yt + cr * .99)]; centre = (0, yt + cr)
        pts = [(0, yb - .004)] + half + top + [centre] + [(-x, y) for x, y in reversed(half + top)]
        return [(x, y, z) for x, y in pts]

    def shell(s):
        C = s.C; zf, zr = C['zf'], C['zr']; cz0, cz1 = C['tub']
        zs = set(); z = zr
        while z <= zf: zs.add(round(z, 3)); z += .05
        for ax in (s.A, -s.A):
            for i in range(-12, 13): zs.add(round(ax + i * .032, 3))
        for e in (zr + .015, zr + .04, zf - .015, zf - .04, cz0 - .005, cz0 + .005, cz1 - .005, cz1 + .005): zs.add(round(e, 3))
        zs = sorted(z for z in zs if zr <= z <= zf)
        secs = [s.section(z, cz1 < z < cz0) for z in reversed(zs)]
        kit.loft('Body shell', secs, G, 'paint', cap=True)
        # wheel-well liners: the arch's inside in black, so nothing is seen through the arch
        ra = RT + C.get('arch_gap', .045) + .005
        for ax in (s.A, -s.A):
            prof = [(ax + math.cos(a) * ra, WY + math.sin(a) * ra) for a in [math.radians(d) for d in range(0, 181, 12)]]
            w = (s.W(ax) + s.flare(ax)) * (C.get('tuck', .84) - .03) - (s.T - .16)
            for sg in (-1, 1): strip('Well liner', prof, w, .012, sg * (s.T - .16 + w / 2), G, 'engine', 0)
            for sg in (-1, 1): strip('Arch lip', prof, .03, .02, sg * (s.W(ax) + s.flare(ax) - .02), G, C.get('arch_trim', 'paint'), .004)

    # ---------------------------------------------------------------- the greenhouse (glass, roof, pillars)
    def gy(s, z):
        """Height of the glass's top along the car, 0 where the glass meets the belt."""
        g = s.C['cab']; zs, zw, zrr, zb, roof = g['zs'], g['zw'], g['zrr'], g['zb'], g['roof']
        if z >= zs or z <= zb: return None
        if z > zw:
            t = (zs - z) / (zs - zw)
            return lerp(s.door_top(zs), roof, (t * (2 - t) * .35 + t * .65) if g.get('screen') == 'lin' else ease(t) ** g.get('screen', 1.0))
        if z >= zrr: return roof + g.get('roofcrown', .012) * math.sin((z - zrr) / (zw - zrr) * math.pi)
        return lerp(roof, s.door_top(zb), (1 - ease((z - zb) / (zrr - zb))) ** g.get('back', 1.0))

    def gsec(s, z, out=0.0):
        g = s.C['cab']; roof = g['roof']; belt = s.door_top(z); yg = s.gy(z)
        hw = s.W(z) + s.flare(z); xb = hw * .895 - .012; xr = hw * g.get('rw', .78)
        sfr = max(0, min(1, (yg - belt) / max(.01, roof - belt)))
        xc = lerp(xb, xr, sfr)
        side = [(xb + out, belt - .002), (lerp(xb, xc, .5) + .015 * sfr + out, lerp(belt, yg, .5)), (xc + out * .7, yg - .035 * sfr + out * .5)]
        top = [(xc * .62, yg + .006 + out), (xc * .25, yg + .011 + out), (0, yg + .012 + out)]
        pts = side + top[:2] + [top[2]] + [(-x, y) for x, y in reversed(side + top[:2])]
        return [(x, y, z) for x, y in pts]

    def greenhouse(s):
        g = s.C['cab']; zs, zw, zrr, zb = g['zs'], g['zw'], g['zrr'], g['zb']
        zl = [lerp(zs - .001, zb + .001, i / 40) for i in range(41)]
        loft_open('Glass', [s.gsec(z) for z in zl], G, 'glass')
        # roof skin over the glass between the screen header and the rear header
        za, zc = zw - .03, (zrr + .06 if g.get('fastglass') else zrr - .02)
        rows = []
        for i in range(13):
            z = lerp(za, zc, i / 12); sec = s.gsec(z, .006); top = sec[2:-2]
            under = [(x * .98, y - .022, zz) for x, y, zz in reversed(top)]
            rows.append(top + under)
        kit.loft('Roof', rows, G, g.get('roofslot', 'paint'), cap=True)
        # pillars along the glass corners: A (screen to roof), C (roof to the belt behind)
        pil = g.get('pillar', 'paint'); pr = g.get('pr', .028)
        for sg in (-1, 1):
            a = [s.gsec(z, .004)[2 if sg > 0 else -3] for z in [lerp(zs - .01, zw - .03, i / 6) for i in range(7)]]
            kit.tube('A pillar', a, pr, G, pil)
            if not g.get('fastglass'):
                c = [s.gsec(z, .004)[2 if sg > 0 else -3] for z in [lerp(zrr + .02, zb + .01, i / 6) for i in range(7)]]
                kit.tube('C pillar', c, pr * 1.1, G, pil)
            # sail panels behind the side glass (fastbacks: the quarter between the last window and the rear glass)
            q = g.get('quarter')
            if q is not None:
                rows = []
                for i in range(9):
                    z = lerp(q, zb + .02, i / 8); sec = s.gsec(z, .008); side = sec[0:3] if sg > 0 else sec[-1:-4:-1]
                    inner = [(x - sg * .02, y, zz) for x, y, zz in reversed(side)]
                    rows.append(side + inner)
                kit.loft('Sail', rows, G, pil if pil == 'paint' else 'paint', cap=True)
            bz = g.get('bpost')
            if bz is not None:
                sec = s.gsec(bz, .006); p = [sec[0], sec[1], sec[2]] if sg > 0 else [sec[-1], sec[-2], sec[-3]]
                kit.tube('B pillar', p, .022, G, g.get('bslot', 'engine'))
            belt = [s.gsec(z, .004)[0 if sg > 0 else -1] for z in [lerp(zs - .02, zb + .02, i / 10) for i in range(11)]]
            kit.tube('Window sill', belt, .011, G, g.get('trim', 'chrome'))

    def windscreen_only(s):
        """An open car's screen: a low curved pane in a frame."""
        g = s.C['cab']; zs, h, rake = g['zs'], g['screen_h'], g['rake']
        rows = []
        for i in range(5):
            t = i / 4; z = zs - rake * t; y0 = s.door_top(zs) + .01 + h * t; hw = (s.W(zs) * .80) * (1 - .06 * t)
            rows.append([(hw * math.cos(a), y0 + .035 * math.sin(a) * 0 + (1 - abs(math.cos(a))) * .0, z - .06 * (1 - abs(math.cos(a)))) for a in [math.radians(d) for d in range(0, 181, 15)]])
        loft_open('Screen', rows, G, 'glass')
        top = rows[-1]; kit.tube('Screen frame', top, .016, G, 'chrome')
        for sg in (0, -1): kit.tube('Screen post', [rows[0][sg], rows[2][sg], rows[4][sg]], .018, G, 'chrome')

    # ---------------------------------------------------------------- the cockpit
    def interior(s):
        C = s.C; H = s.H; fl = s.floor(); cz0, cz1 = C['tub']; belt = s.door_top(H.z)
        for side in (1, -1):
            hx = H.x * side
            # bucket seat: cushion, back and side bolsters
            hull('Seat cushion', [(hx - .2, fl + .08, H.z - .22), (hx + .2, fl + .08, H.z - .22), (hx + .2, fl + .08, H.z + .24), (hx - .2, fl + .08, H.z + .24),
                                  (hx - .2, H.y - .03, H.z - .22), (hx + .2, H.y - .03, H.z - .22), (hx + .2, H.y - .05, H.z + .24), (hx - .2, H.y - .05, H.z + .24)], G, 'interior', .03, 2)
            hull('Seat back', [(hx - .21, H.y - .05, H.z - .32), (hx + .21, H.y - .05, H.z - .32), (hx + .21, H.y - .05, H.z - .18), (hx - .21, H.y - .05, H.z - .18),
                               (hx - .18, H.y + .62, H.z - .44), (hx + .18, H.y + .62, H.z - .44), (hx + .18, H.y + .62, H.z - .32), (hx - .18, H.y + .62, H.z - .32)], G, 'interior', .035, 2)
            for b in (-1, 1): box('Seat bolster', (hx + b * .19, H.y + .22, H.z - .31), (.05, .42, .12), G, 'interior', .02, (-10, 0, 0))
            # door card inside the tub wall
            xi = (s.W(H.z) * .895 - .07) - .015
            hull('Door card', [(side * xi, fl + .08, cz1 + .05), (side * (xi - .02), fl + .08, cz1 + .05), (side * (xi - .02), fl + .08, cz0 - .05), (side * xi, fl + .08, cz0 - .05),
                               (side * xi, belt - .03, cz1 + .05), (side * (xi - .02), belt - .03, cz1 + .05), (side * (xi - .02), belt - .03, cz0 - .05), (side * xi, belt - .03, cz0 - .05)], G, 'interior', .006, 1)
        # dashboard across the cockpit under the screen, gauges in front of the driver, centre console
        dz = H.z + .56; xw = s.W(dz) * .80
        # the dashboard stays inside the cabin: its front 6 cm behind the screen's foot, its top under the glass there
        zs = C['cab']['zs']; df = min(dz + .32, zs - .06, cz0 - .02); dz = min(dz, df - .12); dt = min(belt + .03, s.door_top(df) - .015)
        hull('Dashboard', [(-xw, dt - .20, dz), (xw, dt - .20, dz), (xw, dt - .17, df), (-xw, dt - .17, df),
                           (-xw, dt, dz + .03), (xw, dt, dz + .03), (xw, dt - .03, df), (-xw, dt - .03, df)], G, 'interior', .02, 1)
        belt = dt - .03
        box('Binnacle', (H.x, belt + .05, dz + .07), (.32, .07, .14), G, 'interior', .02)
        for i, dx in enumerate((-.075, .075)):
            cyl('Gauge', (H.x + dx, belt + .045, dz - .005), (H.x + dx, belt + .045, dz - .015), .055, G, 'metal', 16)
            cyl('Gauge ring', (H.x + dx, belt + .045, dz - .003), (H.x + dx, belt + .045, dz - .018), .06, G, 'chrome', 16, cap=False)
        for i in range(3): cyl('Gauge small', (-.05 + i * .05 if H.x < 0 else .05 - i * .05, belt - .06, dz - .005), (-.05 + i * .05, belt - .06, dz - .012), .022, G, 'metal', 10)
        box('Console', (0, fl + .14, H.z + .15), (.20, .26, .9), G, 'interior', .02)
        box('Shifter', (0, fl + .33, H.z + .25), (.03, .14, .03), G, 'chrome', .01)
        blob('Shift knob', (0, fl + .41, H.z + .25), (.025, .025, .025), G, 'interior', 10, 6)
        hull('Floor', [(-s.W(H.z) * .8, fl - .025, cz1 + .02), (s.W(H.z) * .8, fl - .025, cz1 + .02), (s.W(H.z) * .8, fl - .025, cz0 - .02), (-s.W(H.z) * .8, fl - .025, cz0 - .02),
                       (-s.W(H.z) * .8, fl + .015, cz1 + .02), (s.W(H.z) * .8, fl + .015, cz1 + .02), (s.W(H.z) * .8, fl + .015, cz0 - .02), (-s.W(H.z) * .8, fl + .015, cz0 - .02)], G, 'interior', 0, 1)
        # rear bulkhead behind the seats (closes the tub)
        xw = s.W(cz1) * .82
        hull('Bulkhead', [(-xw, fl, cz1 + .03), (xw, fl, cz1 + .03), (xw, fl, cz1 + .06), (-xw, fl, cz1 + .06),
                          (-xw, s.door_top(cz1) - .01, cz1 + .03), (xw, s.door_top(cz1) - .01, cz1 + .03), (xw, s.door_top(cz1) - .01, cz1 + .06), (-xw, s.door_top(cz1) - .01, cz1 + .06)], G, 'interior', .01, 1)
        return s.steering()

    def steering(s):
        H = s.H; t = math.radians(25); c = H + Vector((0, .36, .36)); u = Vector((0, math.cos(t), math.sin(t))); n = Vector((0, -math.sin(t), math.cos(t)))
        cyl('Steering column', tuple(c + n * .02), tuple(c + n * .30), .026, G, 'interior', 10)
        S = 'Steer'; bm = bmesh.new(); seg, side, R, r = 28, 6, .175, .017; vs = []
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
        kit.finish(ob, S, 'interior' if s.C.get('wood') is None else 'rubber')
        cyl('Steering hub', tuple(c - n * .005), tuple(c + n * .05), .045, S, 'metal', 12)
        spokes = (90, 210, 330) if not s.C.get('classic') else (0, 120, 180, 240, 300, 60)
        for ang in spokes:
            a = math.radians(ang); cyl('Steering spoke', tuple(c), tuple(c + Vector((1, 0, 0)) * math.cos(a) * R + u * math.sin(a) * R), .010, S, 'chrome' if s.C.get('classic') else 'metal', 6)
        return tuple(c), tuple(n)

    # ---------------------------------------------------------------- wheels
    def wheel(s, group, pos, left):
        st = s.C['wheel']; s0 = len(kit.PARTS); tw = s.C.get('tyre_w', .11) if pos[2] > 0 else s.C.get('tyre_wr', s.C.get('tyre_w', .11))
        rim = s.C.get('rim', .23)                       # rim radius: the tyre's sidewall is RT - rim
        side = (RT - rim) / 2
        kit.tyre(group, rim + side, tw, side, 32, 8, knobs=0, squared=.9)
        if s.C.get('whitewall'): pass
        cyl(group + ' barrel', (-tw * .9, 0, 0), (tw * .9, 0, 0), rim, group, 'metal', 28, cap=False)
        cyl(group + ' disc', (-.01, 0, 0), (.012, 0, 0), rim * .78, group, 'metal', 24)          # brake disc behind the spokes
        box(group + ' caliper', (-.005, rim * .55, -rim * .35), (.05, rim * .38, rim * .30), group, 'caliper' if s.C.get('red_calipers') else 'engine', .01, (35, 0, 0))
        f = tw * .85
        cyl(group + ' lip', (f - .006, 0, 0), (f + .006, 0, 0), rim + .004, group, 'chrome', 28, cap=False)
        if st == 'wire':
            for i in range(32):
                a = i * 2 * math.pi / 32; x0 = f - .07 if i % 2 else f - .005
                cyl(group + ' wire', (x0, math.cos(a) * .045, math.sin(a) * .045), (f - .02, math.cos(a + .35) * (rim - .01), math.sin(a + .35) * (rim - .01)), .0035, group, 'chrome', 4, smooth=False)
            cyl(group + ' hub', (f - .07, 0, 0), (f + .03, 0, 0), .05, group, 'chrome', 14)
            for k in range(2): box(group + ' spinner', (f + .035 + .004 * k, 0, 0), (.02, .15, .028), group, 'chrome', .006, (90 * k, 0, 0))
        else:
            n = {'five': 5, 'ten': 10, 'star': 5, 'cl': 10, 'slot': 5, 'fuchs': 5}[st]
            cyl(group + ' face', (f - .03, 0, 0), (f - .022, 0, 0), rim - .005, group, 'rubber', 24)     # the dark well behind the spokes
            for i in range(n):
                a = i * 2 * math.pi / n
                if st == 'slot':  # 1960s slotted steel / mag: a full face with slots
                    pass
                else:
                    wdt = {'five': .05, 'ten': .028, 'star': .06, 'cl': .026, 'fuchs': .07}[st]
                    for k, (r0, r1) in enumerate(((.06, rim * .62), (rim * .55, rim - .01))):
                        tw0 = wdt if k == 0 else wdt * (.8 if st != 'fuchs' else .55)
                        rm = (r0 + r1) / 2; L = r1 - r0
                        box(group + ' spoke', (f - .008 - .012 * k, math.cos(a) * rm, math.sin(a) * rm), (.022, tw0, L), group, 'metal' if st != 'fuchs' else 'chrome', .006, (math.degrees(a) - 90, 0, 0))
                    if st == 'five':  # split spokes: a second, thinner one beside each
                        b = a + .17; rm = rim * .64
                        box(group + ' spoke', (f - .01, math.cos(b) * rm, math.sin(b) * rm), (.02, .022, rim * .62), group, 'metal', .005, (math.degrees(b) - 90, 0, 0))
            if st == 'slot':
                cyl(group + ' face', (f - .02, 0, 0), (f - .004, 0, 0), rim - .01, group, 'metal', 28)
                for i in range(5):
                    a = i * 2 * math.pi / 5 + .6
                    box(group + ' slot', (f + .001, math.cos(a) * rim * .62, math.sin(a) * rim * .62), (.006, .04, .085), group, 'engine', .006, (math.degrees(a) - 90, 0, 0))
            hub = .055 if st != 'cl' else .045
            cyl(group + ' hub', (f - .03, 0, 0), (f + .012, 0, 0), hub, group, 'chrome' if st in ('slot', 'fuchs') else 'metal', 16, r1=hub * .8)
            if st == 'cl': cyl(group + ' nut', (f + .01, 0, 0), (f + .03, 0, 0), .03, group, 'caliper' if s.C.get('red_calipers') else 'chrome', 6)
            else:
                for i in range(5):
                    a = i * 2 * math.pi / 5
                    cyl(group + ' nut', (f + .005, math.cos(a) * .033, math.sin(a) * .033), (f + .016, math.cos(a) * .033, math.sin(a) * .033), .007, group, 'chrome', 6)
        place(made(s0), at(pos, left))

    def build(s, details):
        C = s.C; s.shell()
        if C['cab'].get('open'): s.windscreen_only()
        else: s.greenhouse()
        details(s)
        c, n = s.interior()
        T, A = s.T, s.A
        pos = {'WheelFL': (-T, WY, A), 'WheelFR': (T, WY, A), 'WheelRL': (-C.get('Tr', T), WY, -A), 'WheelRR': (C.get('Tr', T), WY, -A)}
        for g, p in pos.items(): s.wheel(g, p, p[0] < 0)
        return pos, c, n


# ---------------------------------------------------------------- shared details
def lamp_round(x, y, z, r, rim='chrome', face_z=1, cover=False):
    cyl('Headlamp bezel', (x, y, z - face_z * .04), (x, y, z + face_z * .015), r + .014, G, rim, 20)
    cyl('Headlamp', (x, y, z + face_z * .01), (x, y, z + face_z * .022), r, G, 'lamp', 20)
    blob('Headlamp lens', (x, y, z + face_z * .02), (r * .96, r * .96, r * .35), G, 'glass', 16, 8)


def lamp_tilt(x, y, z, r, pitch, rim='chrome', oval=1.0):
    """A round (or oval) lamp set into a sloping surface: its face looks forward and up by pitch degrees, the rim flush round it."""
    p = math.radians(pitch); d = Vector((0, math.sin(p), math.cos(p))); c = Vector((x, y, z))
    s0 = len(kit.PARTS)
    cyl('Headlamp bowl', tuple(c - d * .06), tuple(c + d * .005), r + .012, G, rim, 20)
    cyl('Headlamp', tuple(c), tuple(c + d * .012), r, G, 'lamp', 20)
    blob('Headlamp lens', tuple(c + d * .01), (r, r, r * .3), G, 'glass', 16, 8, rot=(-pitch, 0, 0))
    if oval != 1.0:
        for ob in made(s0):  # stretch across the car about the lamp's centre
            ob.matrix_world = Matrix.Translation(B(*c)) @ Matrix.Diagonal((oval, 1, 1, 1)) @ Matrix.Translation(-B(*c)) @ ob.matrix_world


def strip_lamp(name, pts, r, slot='lamp'):
    kit.tube(name, pts, r, G, slot)


def bumper_bar(z, sgn, x0, y, r=.032, wrap=.12, slot='chrome'):
    """A slim classic bumper: a bent tube across the nose or tail, wrapping round the corners."""
    pts = [(-x0, y, z - sgn * wrap), (-x0 * .85, y, z - sgn * .015), (0, y, z + sgn * .01), (x0 * .85, y, z - sgn * .015), (x0, y, z - sgn * wrap)]
    kit.tube('Bumper', pts, r, G, slot, 6)


def exhaust(x, y, z, r=.035, L=.25, slot='chrome'):
    cyl('Exhaust tip', (x, y, z + L), (x, y, z - .02), r, G, slot, 14)
    cyl('Exhaust bore', (x, y, z - .015), (x, y, z - .022), r * .78, G, 'engine', 14)


def mirror(s, z, modern=False):
    for sg in (-1, 1):
        y = s.door_top(z) + .05; x = sg * (s.W(z) + .02)
        if modern:
            box('Mirror arm', (x, y - .02, z), (.07, .025, .06), G, 'engine', .006)
            blob('Mirror', (x + sg * .07, y + .015, z - .02), (.075, .045, .05), G, 'paint', 12, 6)
        else:
            cyl('Mirror stem', (x * .99, y - .04, z), (x * 1.01, y + .02, z), .008, G, 'chrome', 6)
            blob('Mirror', (x + sg * .01, y + .04, z - .01), (.045, .035, .03), G, 'chrome', 12, 6)


def handle(s, z, slot='chrome'):
    for sg in (-1, 1): box('Door handle', (sg * (s.W(z) + s.flare(z) * 0 + .004), s.door_top(z) - .07, z), (.018, .025, .13), G, slot, .006)


def plate(z, y, sgn):
    box('Number plate', (0, y, z + sgn * .005), (.34, .12, .015), G, 'metal', .005)


def intake(s, z0, z1, y0, y1, depth=.02, slot='engine'):
    """A dark recess on both sides of the body (side intakes, vents)."""
    for sg in (-1, 1):
        zc = (z0 + z1) / 2; x = sg * (s.W(zc) + s.flare(zc) + .003)
        hull('Intake', [(x, y0, z0), (x - sg * depth, y0 + .01, z0 - .02), (x - sg * depth, y0 + .01, z1 + .02), (x, y0, z1),
                        (x, y1, z0 - .03), (x - sg * depth, y1 - .01, z0 - .05), (x - sg * depth, y1 - .01, z1 + .02), (x, y1, z1)], G, slot, .004, 1)


def louvres(s, z0, z1, n, x, dy=.006, slot='engine'):
    for i in range(n):
        z = lerp(z0, z1, i / max(1, n - 1)); y = s.TOP(z) + s.CR(z) * .95 + dy
        box('Louvre', (0, y, z), (2 * x, .012, .022), G, slot, .003)


# ---------------------------------------------------------------- the eight cars
CARS = {}


def marlow(s):  # long-bonnet 1960s British coupe
    L, R = s.C['zf'], s.C['zr']
    # oval mouth with a thin chrome bar, faired-in lamps under clear covers at the front of the wings
    blob('Mouth', (0, -.13, L - .03), (.26, .085, .07), G, 'engine', 18, 10)
    kit.tube('Mouth bar', [(-.24, -.13, L + .015), (0, -.13, L + .045), (.24, -.13, L + .015)], .009, G, 'chrome')
    kit.tube('Mouth rim', [(math.cos(a) * .27, -.13 + math.sin(a) * .09, L - .005 + .03 * math.cos(a) ** 2 * 0) for a in [i * 2 * math.pi / 20 for i in range(21)]], .008, G, 'chrome')
    for sg in (-1, 1):
        x = sg * .50; z = L - .30; y = s.surf_y(x, z) - .035
        cyl('Headlamp bowl', (x, y, z - .14), (x, y, z - .02), .088, G, 'chrome', 18)
        cyl('Headlamp', (x, y, z - .03), (x, y, z - .015), .078, G, 'lamp', 18)
        blob('Lamp cover', (x, y + .012, z - .08), (.10, .08, .17), G, 'glass', 16, 8, rot=(-14, 0, 0))
        kit.tube('Lamp rim', [(x + math.cos(a) * .10, y + .012 + math.sin(a) * .08, z - .08 + .1 * math.cos(a) * 0) for a in [i * 2 * math.pi / 16 for i in range(17)]], .006, G, 'chrome') if False else None
        box('Indicator', (sg * .50, -.15, L - .12), (.07, .035, .04), G, 'lamp', .01)
        zt = R + .10; box('Tail lamp', (sg * .46, s.surf_y(.46, zt) - .05, zt - .02), (.13, .07, .06), G, 'tail', .02)
        box('Reverse lamp', (sg * .26, s.surf_y(.26, zt) - .12, zt - .04), (.06, .035, .05), G, 'lamp', .01)
        # small chrome blade bumpers in two pieces at each end
        kit.tube('Bumper', [(sg * .2, -.17, L - .005), (sg * .45, -.165, L - .10), (sg * .62, -.15, L - .30)], .022, G, 'chrome')
        kit.tube('Bumper', [(sg * .15, -.06, R + .03), (sg * .50, -.06, R + .09), (sg * .70, -.07, R + .30)], .022, G, 'chrome')
        exhaust(sg * .07, -.30, R + .05, .028)
        cyl('Fuel filler', (sg * .7 * 0, s.TOP(-1.75) + .05, -1.75), (0, s.TOP(-1.75) + .055, -1.75), .0, G, 'chrome', 4) if False else None
        # side vent louvres on the bonnet's flanks
        for k in range(6): box('Bonnet louvre', (sg * .40, s.TOP(1.0 - k * .05) + s.CR(1.0) * .55 + .004, 1.0 - k * .05), (.12, .01, .018), G, 'engine', .003)
    plate(R + .02, -.13, -1)
    # the bonnet's centre bulge, a raised spine
    s.C_bulge = kit.loft('Bonnet bulge', [[(x * w, s.TOP(z) + s.CR(z) + .01 + h * math.cos(x * math.pi / 2), z) for x in (-1, -.7, -.35, 0, .35, .7, 1)] + [(x * w, s.TOP(z) + s.CR(z) - .01, z) for x in (1, 0, -1)]
                                          for z, w, h in ((1.95, .05, .0), (1.7, .16, .025), (1.2, .19, .04), (.6, .17, .03), (.32, .12, .0))], G, 'paint', cap=True)
    mirror(s, .25); handle(s, -.20)
    box('Wiper', (-.25, s.door_top(.18) + .02, .16), (.38, .01, .02), G, 'chrome', .003, (0, 8, 0))
    box('Wiper', (.15, s.door_top(.18) + .02, .16), (.38, .01, .02), G, 'chrome', .003, (0, -8, 0))
    # rear hatch's hinge line and a bright strip down its centre
    kit.tube('Hatch trim', [(0, s.gy(-1.0) + .015, -1.0), (0, s.gy(-1.4) + .012, -1.4), (0, s.door_top(-1.85) + .01, -1.85)], .007, G, 'chrome')


CARS['Marlow'] = (dict(A=1.22, T=.71, H=(-.36, -.10, -.55), zf=2.20, zr=-2.27, tub=(.16, -1.02), classic=True, wheel='wire', rim=.21, tyre_w=.095, flare=.035, tuck=.80,
                       hw=[(2.20, .30), (2.10, .52), (1.85, .72), (1.4, .82), (.9, .83), (.3, .82), (-.5, .83), (-1.22, .87), (-1.8, .82), (-2.15, .66), (-2.27, .45)],
                       top=[(2.20, -.04), (2.08, .07), (1.85, .14), (1.4, .19), (.8, .23), (.30, .25), (-.5, .26), (-1.1, .28), (-1.6, .27), (-1.95, .20), (-2.2, .09), (-2.27, .02)],
                       bot=[(2.20, -.16), (2.0, -.33), (1.6, -.39), (0, -.40), (-1.8, -.38), (-2.15, -.30), (-2.27, -.17)],
                       sh=[(2.20, -.08), (1.9, .05), (1.22, .19), (.6, .15), (0, .13), (-.6, .14), (-1.22, .21), (-1.9, .15), (-2.27, -.02)],
                       crown=[(2.2, .04), (1.6, .07), (.4, .06), (0, .03), (-1.0, .04), (-2.27, .03)],
                       cab=dict(zs=.20, zw=-.20, zrr=-.88, zb=-1.85, roof=.79, rw=.70, screen=1.0, back=.75, bpost=-.66, quarter=-1.0, trim='chrome', pr=.024)), marlow)


def copperhead(s):  # small 1960s V8 roadster
    L, R = s.C['zf'], s.C['zr']
    blob('Mouth', (0, -.12, L - .02), (.36, .12, .08), G, 'engine', 20, 10)
    kit.tube('Mouth rim', [(math.cos(a) * .37, -.12 + math.sin(a) * .125, L + .0) for a in [i * 2 * math.pi / 22 for i in range(23)]], .010, G, 'chrome')
    for i in range(3): box('Grille bar', (0, -.17 + i * .05, L + .01), (.62, .008, .012), G, 'chrome', 0)
    for sg in (-1, 1):
        z = L - .30; lamp_round(sg * .55, s.TOP(z) + s.CR(z) * .1 - .10, z + .05, .085)
        box('Indicator', (sg * .40, -.20, L - .05), (.06, .035, .03), G, 'lamp', .01)
        cyl('Tail lamp', (sg * .55, s.TOP(R + .2) - .07, R + .08), (sg * .55, s.TOP(R + .2) - .07, R + .02), .05, G, 'tail', 16)
        cyl('Tail lamp small', (sg * .67, s.TOP(R + .2) - .07, R + .10), (sg * .67, s.TOP(R + .2) - .07, R + .04), .035, G, 'lamp', 14)
        # side pipes under the doors: chrome with a perforated heat shield
        y = -.33; kit.tube('Side pipe', [(sg * (s.W(.9) + .03), y + .05, .75), (sg * (s.W(.3) + .06), y, .45), (sg * (s.W(-.5) + .07), y, -.55), (sg * (s.W(-.9) + .06), y + .02, -.80)], .045, G, 'chrome')
        box('Pipe shield', (sg * (s.W(-.1) + .09), y + .02, -.1), (.02, .07, .75), G, 'metal', .01)
        kit.tube('Bumper', [(sg * .15, -.19, L + .02), (sg * .35, -.19, L - .02)], .02, G, 'chrome')
        for zz in (L - .03, R + .05): box('Overrider', (sg * .30, -.15, zz), (.045, .12, .05), G, 'chrome', .015)
        box('Bonnet pin', (sg * .25, s.TOP(L - .5) + s.CR(L - .5) * .9 + .006, L - .50), (.03, .01, .03), G, 'chrome', .004)
    kit.tube('Rear bumper', [(-.55, -.18, R + .12), (-.4, -.18, R + .02), (.4, -.18, R + .02), (.55, -.18, R + .12)], .022, G, 'chrome')
    plate(R + .02, -.10, -1)
    hull('Bonnet scoop', [(-.18, s.TOP(.9) + s.CR(.9) - .01, .55), (.18, s.TOP(.9) + s.CR(.9) - .01, .55), (.18, s.TOP(1.2) + s.CR(1.2) - .01, 1.15), (-.18, s.TOP(1.2) + s.CR(1.2) - .01, 1.15),
                          (-.14, s.TOP(.9) + s.CR(.9) + .07, .62), (.14, s.TOP(.9) + s.CR(.9) + .07, .62), (.14, s.TOP(1.2) + s.CR(1.2) + .01, 1.10), (-.14, s.TOP(1.2) + s.CR(1.2) + .01, 1.10)], G, 'paint', .02, 2)
    box('Scoop mouth', (0, s.TOP(1.15) + s.CR(1.15) + .02, 1.13), (.26, .035, .02), G, 'engine', .005)
    # a roll hoop behind the driver and the fuel filler on the deck
    H = s.H; z = H.z - .45
    kit.tube('Roll hoop', [(H.x - .2, s.door_top(z), z), (H.x - .2, H.y + .78, z - .03), (H.x + .2, H.y + .78, z - .03), (H.x + .2, s.door_top(z), z)], .025, G, 'chrome')
    cyl('Fuel filler', (.3, s.TOP(R + .45) + s.CR(R + .45) * .6, R + .45), (.3, s.TOP(R + .45) + s.CR(R + .45) * .6 + .03, R + .45), .07, G, 'chrome', 16)
    for sg in (-1, 1): box('Racing stripe', (sg * .11, 0, 0), (0, 0, 0), G, 'paint', 0) if False else None
    handle(s, -.5)
    for sg in (-1, 1): mirror(s, .05) if sg > 0 else None


CARS['Copperhead'] = (dict(A=1.14, T=.72, H=(-.33, -.10, -.42), zf=1.98, zr=-2.02, tub=(.16, -.92), classic=True, wheel='star', rim=.22, tyre_w=.115, tyre_wr=.135, flare=.07, tuck=.82,
                           hw=[(1.98, .38), (1.88, .58), (1.6, .72), (1.14, .80), (.6, .74), (0, .73), (-.6, .75), (-1.14, .85), (-1.6, .80), (-1.9, .66), (-2.02, .48)],
                           top=[(1.98, -.06), (1.85, .05), (1.5, .13), (1.0, .17), (.4, .21), (0, .22), (-.9, .22), (-1.3, .24), (-1.7, .22), (-1.95, .12), (-2.02, .03)],
                           bot=[(1.98, -.18), (1.8, -.34), (1.4, -.40), (-1.6, -.40), (-1.95, -.30), (-2.02, -.20)],
                           sh=[(1.98, -.10), (1.6, .07), (1.14, .17), (.5, .10), (0, .09), (-.6, .10), (-1.14, .19), (-1.7, .13), (-2.02, -.04)],
                           crown=[(1.98, .03), (1.4, .06), (.3, .045), (0, .02), (-1.0, .03), (-2.02, .02)],
                           cab=dict(open=True, zs=.18, screen_h=.30, rake=.17)), copperhead)


def stiletto(s):  # 1970s wedge supercar, mid-engined
    L, R = s.C['zf'], s.C['zr']
    for sg in (-1, 1):
        # a pair of slim lamps in the knife-edge nose, amber indicators below, square tail lamps in a black panel
        box('Headlamp', (sg * .55, s.TOP(L - .08) - .03, L - .07), (.30, .045, .04), G, 'lamp', .006)
        box('Headlamp frame', (sg * .55, s.TOP(L - .08) - .03, L - .09), (.33, .06, .03), G, 'engine', .006)
        box('Indicator', (sg * .55, -.20, L - .03), (.22, .035, .03), G, 'lamp', .006)
        for k in range(3): box('Tail lamp', (sg * (.30 + k * .17), -.03, R - .012), (.15, .10, .02), G, 'tail' if k < 2 else 'lamp', .008)
        # NACA duct on the door, the big shoulder intake behind it
        hull('NACA duct', [(sg * (s.W(-.2) + .002), .07, .05), (sg * (s.W(-.2) - .01), .06, -.05), (sg * (s.W(-.2) - .01), .06, -.40), (sg * (s.W(-.2) + .002), .05, -.45),
                           (sg * (s.W(-.2) + .002), .09, .05), (sg * (s.W(-.2) - .01), .15, -.05), (sg * (s.W(-.2) - .01), .15, -.40), (sg * (s.W(-.2) + .002), .19, -.45)], G, 'engine', .004, 1)
        exhaust(sg * .25, -.25, R + .04, .038); exhaust(sg * .36, -.25, R + .04, .038)
        box('Engine cover edge', (sg * .55, s.TOP(-1.4) + .02, -1.4), (.025, .02, 1.2), G, 'engine', .004)
    intake(s, -.70, -1.25, .12, .30, .06)
    box('Tail panel', (0, -.03, R - .002), (1.45, .16, .012), G, 'engine', .004)
    plate(R + .02, -.17, -1)
    louvres(s, -1.25, -2.0, 9, .45)
    # a low wing over the tail on two struts
    hull('Wing', [(-.85, .34, R + .38), (.85, .34, R + .38), (.85, .34, R + .10), (-.85, .34, R + .10), (-.85, .37, R + .38), (.85, .37, R + .38), (.85, .40, R + .10), (-.85, .40, R + .10)], G, 'paint', .01, 1)
    for sg in (-1, 1): hull('Wing strut', [(sg * .45, s.TOP(R + .3), R + .32), (sg * .48, s.TOP(R + .3), R + .32), (sg * .48, s.TOP(R + .3), R + .18), (sg * .45, s.TOP(R + .3), R + .18),
                                           (sg * .45, .34, R + .30), (sg * .48, .34, R + .30), (sg * .48, .34, R + .16), (sg * .45, .34, R + .16)], G, 'engine', .004, 1)
    for sg in (-1, 1):
        for k in range(4): box('Bonnet vent', (sg * .25, s.TOP(1.0) + s.CR(1.0) * .7 + .003, .95 - k * .05), (.22, .008, .018), G, 'engine', .002)
    mirror(s, .40, True); handle(s, -.6, 'engine')
    box('Wiper', (-.15, s.door_top(.55) + .02, .52), (.5, .01, .02), G, 'engine', .003, (0, 5, 0))


CARS['Stiletto'] = (dict(A=1.25, T=.82, Tr=.84, H=(-.38, -.13, -.15), zf=2.07, zr=-2.10, tub=(.60, -.65), wheel='star', rim=.24, tyre_w=.13, tyre_wr=.16, flare=.06, tuck=.86, red_calipers=False,
                         hw=[(2.07, .78), (1.95, .86), (1.6, .90), (1.25, .93), (.5, .93), (0, .94), (-.7, .96), (-1.25, .99), (-1.8, .97), (-2.10, .94)],
                         top=[(2.07, -.08), (1.95, .0), (1.5, .09), (1.05, .15), (.6, .20), (0, .24), (-.65, .27), (-1.2, .30), (-1.8, .31), (-2.05, .28), (-2.10, .20)],
                         bot=[(2.07, -.26), (1.9, -.36), (1.5, -.41), (-1.8, -.41), (-2.10, -.30)],
                         sh=[(2.07, -.16), (1.5, .03), (1.25, .11), (.5, .09), (0, .10), (-.6, .13), (-1.25, .19), (-1.8, .20), (-2.10, .13)],
                         crown=[(2.07, .005), (1.0, .02), (0, .015), (-2.1, .012)],
                         cab=dict(zs=1.05, zw=.0, zrr=-.58, zb=-1.15, roof=.72, rw=.74, screen='lin', back=.6, quarter=-.62, pillar='paint', trim='engine', pr=.03, bpost=None)), stiletto)


def tempesta(s):  # modern angular V12 supercar
    L, R = s.C['zf'], s.C['zr']
    for sg in (-1, 1):
        # arrowhead lamps: a dark slit with a Y-shaped light blade, the big lower intakes, a thin tail light blade
        z = L - .25; y = s.TOP(z) + s.CR(z) * .2 - .02
        hull('Lamp slot', [(sg * .45, y - .04, L - .10), (sg * .82, y - .03, L - .42), (sg * .82, y - .01, L - .42), (sg * .45, y, L - .10),
                           (sg * .45, y - .04 + .012, L - .12), (sg * .80, y - .03 + .012, L - .44), (sg * .80, y, L - .44), (sg * .45, y + .01, L - .12)], G, 'engine', .004, 1)
        kit.tube('Lamp blade', [(sg * .48, y - .012, L - .12), (sg * .66, y - .010, L - .27), (sg * .80, y - .008, L - .41)], .009, G, 'lamp')
        kit.tube('Lamp blade', [(sg * .62, y - .012, L - .24), (sg * .66, y - .025, L - .33)], .007, G, 'lamp')
        hull('Front intake', [(sg * .30, -.32, L - .02), (sg * .78, -.30, L - .18), (sg * .78, -.12, L - .22), (sg * .40, -.10, L - .05),
                              (sg * .30, -.32, L - .10), (sg * .76, -.30, L - .26), (sg * .76, -.12, L - .30), (sg * .40, -.10, L - .13)], G, 'engine', .004, 1)
        kit.tube('Tail blade', [(sg * .15, s.TOP(R) - .05, R - .006), (sg * .50, s.TOP(R) - .05, R - .006), (sg * .78, s.TOP(R) - .08, R - .004)], .018, G, 'tail')
        hull('Rear vent', [(sg * .45, -.10, R), (sg * .88, -.10, R + .10), (sg * .88, .06, R + .12), (sg * .45, .05, R), (sg * .45, -.10, R + .06), (sg * .86, -.10, R + .16), (sg * .86, .06, R + .18), (sg * .45, .05, R + .06)], G, 'engine', .004, 1)
        exhaust(sg * .11, -.16, R + .03, .045, .2, 'metal')
        # front splitter tips, side skirt blades
        box('Side skirt', (sg * (s.W(0) + .015), -.38, 0), (.03, .04, 1.6), G, 'engine', .006)
    hull('Splitter', [(-.85, -.43, L - .25), (.85, -.43, L - .25), (.80, -.43, L + .02), (-.80, -.43, L + .02), (-.85, -.40, L - .25), (.85, -.40, L - .25), (.80, -.40, L + .02), (-.80, -.40, L + .02)], G, 'engine', .004, 1)
    intake(s, -.55, -1.25, -.10, .25, .09)
    intake(s, -1.35, -1.75, .14, .25, .04)
    hull('Diffuser', [(-.70, -.42, R + .35), (.70, -.42, R + .35), (.70, -.42, R + .02), (-.70, -.42, R + .02), (-.70, -.38, R + .35), (.70, -.38, R + .35), (.70, -.18, R + .02), (-.70, -.18, R + .02)], G, 'engine', .004, 1)
    for k in range(5): box('Diffuser fin', (-.5 + k * .25, -.32, R + .12), (.012, .14, .26), G, 'engine', .002, (-30, 0, 0))
    # the engine cover: hexagon louvres in a black frame between the roof and the tail; an active spoiler blade at the tail
    for i in range(4):
        for sg in (-1, 1):
            z = -.95 - i * .22; y = s.TOP(z) + s.CR(z) + .004
            hull('Engine glass', [(sg * .05, y, z), (sg * .32, y - .01, z + .05), (sg * .32, y - .01, z - .13), (sg * .05, y, z - .18),
                                  (sg * .05, y + .012, z), (sg * .32, y + .002, z + .05), (sg * .32, y + .002, z - .13), (sg * .05, y + .012, z - .18)], G, 'engine', .004, 1)
    hull('Spoiler', [(-.75, s.TOP(R + .2) + .02, R + .25), (.75, s.TOP(R + .2) + .02, R + .25), (.75, s.TOP(R + .2) + .02, R + .05), (-.75, s.TOP(R + .2) + .02, R + .05),
                     (-.75, s.TOP(R + .2) + .04, R + .25), (.75, s.TOP(R + .2) + .04, R + .25), (.75, s.TOP(R + .2) + .07, R + .05), (-.75, s.TOP(R + .2) + .07, R + .05)], G, 'engine', .006, 1)
    plate(R + .02, -.25, -1)
    mirror(s, .45, True)
    for k in range(3): box('Bonnet vent', (0, s.TOP(1.15 - k * .06) + s.CR(1.15) + .003, 1.15 - k * .06), (.46, .008, .02), G, 'engine', .002)


CARS['Tempesta'] = (dict(A=1.36, T=.86, Tr=.87, H=(-.38, -.14, -.12), zf=2.30, zr=-2.30, tub=(.66, -.62), wheel='cl', rim=.255, tyre_w=.13, tyre_wr=.17, flare=.07, tuck=.86, red_calipers=True, arch_gap=.035,
                         hw=[(2.30, .72), (2.2, .86), (1.9, .94), (1.36, .99), (.6, .96), (0, .97), (-.7, 1.0), (-1.36, 1.03), (-1.9, 1.01), (-2.2, .96), (-2.30, .90)],
                         top=[(2.30, -.14), (2.2, -.03), (1.8, .07), (1.15, .15), (.66, .20), (0, .24), (-.6, .27), (-1.3, .29), (-1.9, .29), (-2.2, .25), (-2.30, .16)],
                         bot=[(2.30, -.33), (2.0, -.40), (1.5, -.43), (-1.9, -.43), (-2.30, -.30)],
                         sh=[(2.30, -.20), (1.9, .04), (1.36, .14), (.6, .07), (0, .08), (-.6, .11), (-1.36, .20), (-1.9, .20), (-2.30, .08)],
                         crown=[(2.30, .004), (1.2, .02), (0, .015), (-2.3, .01)],
                         cab=dict(zs=1.15, zw=.0, zrr=-.60, zb=-1.05, roof=.71, rw=.72, screen='lin', back=.55, quarter=-.58, pillar='engine', trim='engine', pr=.03)), tempesta)


def falke(s, modern=False):  # rear-engined coupe, the 1960s / 70s one and (modern) its descendant
    L, R = s.C['zf'], s.C['zr']
    for sg in (-1, 1):
        # upright lamps on the front wings (round; the modern one oval, with a light bar inside)
        z = L - .40; x = sg * .56; y = s.surf_y(x, z)
        if modern:
            lamp_tilt(x, y - .035, z, .085, 30, 'engine', oval=1.35)
            kit.tube('Lamp ring', [(x + math.cos(a) * .065, y - .03 + math.sin(a) * .045 * .9, z + .02 + math.sin(a) * .045 * .45) for a in [i * 2 * math.pi / 16 for i in range(17)]], .007, G, 'lamp')
            box('Intake', (sg * .48, -.25, L - .03), (.35, .12, .04), G, 'engine', .008)
            kit.tube('Tail blade', [(-.74, s.TOP(R) - .05, R - .004), (-.4, s.TOP(R) - .04, R - .007), (0, s.TOP(R) - .04, R - .008), (.4, s.TOP(R) - .04, R - .007), (.74, s.TOP(R) - .05, R - .004)], .02, G, 'tail') if sg > 0 else None
            exhaust(sg * .18, -.30, R + .06, .045, .2, 'metal')
            box('Side intake', (sg * (s.W(-1.4) + s.flare(-1.4)), .10, -1.45), (.02, .08, .25), G, 'engine', .006)
        else:
            lamp_tilt(x, y - .03, z, .08, 22, 'chrome')
            box('Indicator', (sg * .52, -.15, L - .03), (.10, .04, .03), G, 'lamp', .008)
            box('Tail lamp', (sg * .60, .04, R + .06), (.18, .07, .05), G, 'tail', .015)
            kit.tube('Bumper', [(sg * .05, -.18, L + .005), (sg * .45, -.18, L - .05), (sg * .72, -.17, L - .30)], .022, G, 'chrome')
            kit.tube('Bumper', [(sg * .05, -.17, R - .005), (sg * .45, -.17, R + .05), (sg * .72, -.16, R + .30)], .022, G, 'chrome')
            box('Horn grille', (sg * .45, -.11, L - .01), (.10, .04, .02), G, 'chrome', .004)
            exhaust(sg * .40, -.30, R + .08, .03)
    if not modern:
        louvres(s, -1.55, -1.95, 8, .28)
        kit.tube('Rain gutter', [(0, s.gy(-.9) + .02, -.9)], .0, G, 'chrome') if False else None
        handle(s, -.35)
        mirror(s, .20)
        box('Engine grille', (0, s.TOP(-1.75) + s.CR(-1.75) + .01, -1.75), (.5, .01, .40), G, 'engine', .004)
    else:
        hull('Ducktail', [(-.70, s.TOP(R + .40) + s.CR(R + .4) - .01, R + .55), (.70, s.TOP(R + .40) + s.CR(R + .4) - .01, R + .55), (.72, s.TOP(R + .2) - .01, R + .12), (-.72, s.TOP(R + .2) - .01, R + .12),
                          (-.70, s.TOP(R + .40) + s.CR(R + .4) + .005, R + .55), (.70, s.TOP(R + .40) + s.CR(R + .4) + .005, R + .55), (.72, s.TOP(R + .2) + .09, R + .10), (-.72, s.TOP(R + .2) + .09, R + .10)], G, 'paint', .01, 1)
        louvres(s, -1.55, -1.85, 6, .30)
        mirror(s, .25, True)
        box('Side blade', (0, 0, 0), (0, 0, 0), G, 'paint', 0) if False else None
        hull('Splitter', [(-.75, -.40, L - .2), (.75, -.40, L - .2), (.72, -.40, L), (-.72, -.40, L), (-.75, -.37, L - .2), (.75, -.37, L - .2), (.72, -.37, L), (-.72, -.37, L)], G, 'engine', .004, 1)
        hull('Diffuser', [(-.65, -.40, R + .3), (.65, -.40, R + .3), (.65, -.40, R + .02), (-.65, -.40, R + .02), (-.65, -.37, R + .3), (.65, -.37, R + .3), (.65, -.22, R + .02), (-.65, -.22, R + .02)], G, 'engine', .004, 1)
    plate(R + .02, -.08 if not modern else -.10, -1)
    plate(L + .02, -.20, 1) if not modern else None


def falke_r(s): falke(s, True)


CARS['Falke'] = (dict(A=1.11, T=.68, Tr=.70, H=(-.34, -.08, -.18), zf=1.85, zr=-2.10, tub=(.45, -.75), classic=True, wheel='fuchs', rim=.20, tyre_w=.10, tyre_wr=.11, flare=.035, tuck=.84,
                      hw=[(1.85, .55), (1.75, .68), (1.45, .77), (1.11, .80), (.5, .79), (0, .79), (-.6, .80), (-1.11, .84), (-1.6, .82), (-1.95, .74), (-2.10, .62)],
                      top=[(1.85, -.10), (1.75, .0), (1.45, .09), (1.0, .14), (.5, .18), (0, .22), (-.8, .24), (-1.3, .22), (-1.7, .16), (-2.0, .06), (-2.10, -.02)],
                      bot=[(1.85, -.22), (1.65, -.36), (1.3, -.39), (-1.7, -.38), (-2.10, -.24)],
                      sh=[(1.85, -.12), (1.5, .07), (1.11, .17), (.5, .10), (0, .09), (-.6, .11), (-1.11, .18), (-1.7, .11), (-2.10, -.06)],
                      crown=[(1.85, -.03), (1.2, -.05), (.65, -.03), (.45, .02), (0, .03), (-2.1, .05)],
                      cab=dict(zs=.48, zw=.0, zrr=-.62, zb=-1.55, roof=.81, rw=.74, screen=1.0, back=.62, bpost=-.30, quarter=-.95, trim='chrome', pr=.022)), falke)

CARS['FalkeR'] = (dict(A=1.22, T=.80, Tr=.84, H=(-.36, -.10, -.18), zf=2.10, zr=-2.30, tub=(.50, -.78), wheel='five', rim=.25, tyre_w=.12, tyre_wr=.15, flare=.06, tuck=.86, red_calipers=True, arch_gap=.035,
                       hw=[(2.10, .62), (2.0, .80), (1.7, .89), (1.22, .92), (.5, .90), (0, .90), (-.6, .93), (-1.22, 1.0), (-1.8, .98), (-2.15, .90), (-2.30, .80)],
                       top=[(2.10, -.12), (2.0, -.01), (1.7, .08), (1.2, .13), (.5, .17), (0, .21), (-.8, .23), (-1.4, .21), (-1.9, .15), (-2.2, .08), (-2.30, .0)],
                       bot=[(2.10, -.30), (1.9, -.39), (1.5, -.41), (-1.9, -.40), (-2.30, -.28)],
                       sh=[(2.10, -.16), (1.7, .07), (1.22, .16), (.5, .09), (0, .09), (-.6, .11), (-1.22, .20), (-1.8, .14), (-2.30, -.02)],
                       crown=[(2.1, -.03), (1.3, -.05), (.7, -.03), (.5, .02), (0, .03), (-2.3, .04)],
                       cab=dict(zs=.52, zw=.0, zrr=-.66, zb=-1.62, roof=.79, rw=.72, screen=1.1, back=.6, quarter=-1.0, pillar='paint', trim='engine', bslot='engine', bpost=-.36, pr=.025)), falke_r)


def brawler(s):  # 1960s muscle car
    L, R = s.C['zf'], s.C['zr']
    # the full-width grille with hidden lamps behind its bars, the split chrome bumper, the tail's long lamp panel
    hull('Grille', [(-.86, -.25, L - .02), (.86, -.25, L - .02), (.86, -.25, L + .0), (-.86, -.25, L + .0), (-.86, s.TOP(L) - .02, L - .04), (.86, s.TOP(L) - .02, L - .04), (.86, s.TOP(L) - .02, L - .02), (-.86, s.TOP(L) - .02, L - .02)], G, 'engine', .006, 1)
    for i in range(7): box('Grille bar', (0, -.23 + i * .045, L + .005), (1.70, .008, .01), G, 'metal', 0)
    for sg in (-1, 1):
        for xx in (.55, .74): cyl('Lamp door', (sg * xx, -.10, L + .002), (sg * xx, -.10, L + .012), .08, G, 'engine', 16)
        box('Parking lamp', (sg * .62, -.30, L + .01), (.14, .03, .02), G, 'lamp', .006)
        exhaust(sg * .55, -.38, R + .10, .035)
        box('Side marker', (sg * (s.W(L - .25) + .004), -.05, L - .25), (.01, .03, .08), G, 'lamp', .003)
        box('Side marker', (sg * (s.W(R + .25) + .004), -.02, R + .25), (.01, .03, .08), G, 'tail', .003)
        # twin bonnet scoops
        z0 = .95; yb = s.TOP(z0) + s.CR(z0) * .7
        hull('Bonnet scoop', [(sg * .14, yb - .01, .70), (sg * .36, yb - .015, .70), (sg * .36, yb - .015, 1.25), (sg * .14, yb - .01, 1.25),
                              (sg * .16, yb + .065, .78), (sg * .34, yb + .06, .78), (sg * .34, yb + .005, 1.22), (sg * .16, yb + .01, 1.22)], G, 'paint', .015, 2)
        box('Scoop mouth', (sg * .25, yb + .03, 1.215), (.16, .045, .015), G, 'engine', .004)
    kit.tube('Bumper', [(-.92, -.30, L - .22), (-.85, -.31, L + .02), (0, -.31, L + .05), (.85, -.31, L + .02), (.92, -.30, L - .22)], .045, G, 'chrome', 6)
    hull('Tail panel', [(-.84, -.18, R + .005), (.84, -.18, R + .005), (.84, -.18, R + .025), (-.84, -.18, R + .025), (-.84, .05, R + .02), (.84, .05, R + .02), (.84, .05, R + .04), (-.84, .05, R + .04)], G, 'engine', .004, 1)
    for k in (-1, 1):
        for xx in (.55, .74): cyl('Tail lamp', (k * xx, -.06, R + .01), (k * xx, -.06, R - .005), .075, G, 'tail', 18)
    kit.tube('Rear bumper', [(-.92, -.29, R + .22), (-.85, -.30, R - .02), (0, -.30, R - .05), (.85, -.30, R - .02), (.92, -.29, R + .22)], .045, G, 'chrome', 6)
    plate(R - .04, -.20, -1)
    # bright trim: the drip rail, the wheel-arch lips, a rocker strip; the fuel cap on the rear wing
    for sg in (-1, 1): box('Rocker trim', (sg * (s.W(0) + .004), -.30, -.05), (.012, .035, 1.6), G, 'chrome', .004)
    cyl('Fuel cap', (-(s.W(-1.75) + .005), .10, -1.75), (-(s.W(-1.75) + .03), .10, -1.75), .065, G, 'chrome', 16)
    mirror(s, .35); handle(s, -.45)
    hull('Ducktail', [(-.80, s.TOP(R + .2) + .0, R + .25), (.80, s.TOP(R + .2) + .0, R + .25), (.82, s.TOP(R + .1) - .01, R + .03), (-.82, s.TOP(R + .1) - .01, R + .03),
                      (-.80, s.TOP(R + .2) + .03, R + .24), (.80, s.TOP(R + .2) + .03, R + .24), (.82, s.TOP(R + .1) + .06, R + .02), (-.82, s.TOP(R + .1) + .06, R + .02)], G, 'paint', .01, 1)


CARS['Brawler'] = (dict(A=1.42, T=.80, H=(-.40, -.04, -.20), zf=2.55, zr=-2.55, tub=(.40, -1.05), classic=True, wheel='slot', rim=.22, tyre_w=.13, tyre_wr=.14, flare=.04, tuck=.86, arch_trim='chrome',
                        hw=[(2.55, .88), (2.4, .92), (1.8, .94), (1.42, .96), (.6, .94), (0, .93), (-.6, .96), (-1.42, 1.0), (-2.0, .97), (-2.4, .92), (-2.55, .88)],
                        top=[(2.55, .12), (2.4, .17), (1.8, .21), (1.0, .24), (.4, .27), (0, .29), (-1.0, .31), (-1.8, .31), (-2.3, .30), (-2.55, .26)],
                        bot=[(2.55, -.24), (2.3, -.36), (1.8, -.38), (-1.9, -.38), (-2.4, -.34), (-2.55, -.22)],
                        sh=[(2.55, .02), (1.8, .10), (1.42, .14), (.6, .12), (0, .12), (-.8, .14), (-1.42, .20), (-2.0, .17), (-2.55, .12)],
                        crown=[(2.55, .02), (1.4, .04), (0, .03), (-2.55, .03)],
                        cab=dict(zs=.42, zw=-.08, zrr=-.92, zb=-1.55, roof=.90, rw=.80, screen=1.0, back=.8, bpost=None, trim='chrome', pillar='paint', roofslot='engine', pr=.03)), brawler)


def meridian(s):  # modern front-engined grand tourer
    L, R = s.C['zf'], s.C['zr']
    hull('Grille', [(-.42, -.30, L + .006), (.42, -.30, L + .006), (.48, -.04, L - .02), (-.48, -.04, L - .02), (-.40, -.30, L - .06), (.40, -.30, L - .06), (.46, -.04, L - .08), (-.46, -.04, L - .08)], G, 'engine', .02, 2)
    kit.tube('Grille surround', [(-.42, -.31, L + .012), (-.49, -.17, L + .0), (-.49, -.035, L - .018), (0, -.025, L - .012), (.49, -.035, L - .018), (.49, -.17, L + .0), (.42, -.31, L + .012), (0, -.32, L + .016), (-.42, -.31, L + .012)], .012, G, 'chrome')
    for i in range(5): box('Grille vane', (0, -.27 + i * .055, L + .004 - i * .006), (.82, .008, .02), G, 'metal', 0)
    for sg in (-1, 1):
        z = L - .32; y = s.TOP(z) + s.CR(z) * .2 - .04
        hull('Headlamp', [(sg * .55, y - .06, L - .16), (sg * .85, y - .04, L - .52), (sg * .85, y + .005, L - .52), (sg * .55, y - .01, L - .16),
                          (sg * .55, y - .06 + .015, L - .18), (sg * .83, y - .04 + .015, L - .54), (sg * .83, y + .02, L - .54), (sg * .55, y + .005, L - .18)], G, 'glass', .006, 1)
        kit.tube('Lamp blade', [(sg * .57, y - .05, L - .18), (sg * .72, y - .04, L - .35), (sg * .83, y - .02, L - .50)], .009, G, 'lamp')
        cyl('Lamp eye', (sg * .70, y - .02, L - .30), (sg * .70, y - .025, L - .27), .045, G, 'lamp', 14)
        hull('Fender vent', [(sg * (s.W(.95) + s.flare(.95) + .002), .02, .80), (sg * (s.W(.95) + s.flare(.95) - .01), .03, .78), (sg * (s.W(.95) + s.flare(.95) - .01), .03, .40), (sg * (s.W(.95) + s.flare(.95) + .002), .02, .38),
                             (sg * (s.W(.95) + s.flare(.95) + .002), .12, .74), (sg * (s.W(.95) + s.flare(.95) - .01), .11, .72), (sg * (s.W(.95) + s.flare(.95) - .01), .07, .40), (sg * (s.W(.95) + s.flare(.95) + .002), .06, .38)], G, 'engine', .004, 1)
        kit.tube('Vent bar', [(sg * (s.W(.6) + s.flare(.6) + .006), .07, .74), (sg * (s.W(.6) + s.flare(.6) + .006), .05, .42)], .008, G, 'chrome')
        kit.tube('Tail lamp', [(sg * .20, s.TOP(R) - .05, R - .006), (sg * .50, s.TOP(R) - .05, R - .006), (sg * .70, s.TOP(R) - .07, R - .004)], .025, G, 'tail')
        exhaust(sg * .32, -.32, R + .05, .045, .2, 'chrome'); exhaust(sg * .45, -.32, R + .07, .045, .2, 'chrome')
        box('Side sill', (sg * (s.W(0) + .006), -.33, -.05), (.02, .035, 1.8), G, 'engine', .004)
    hull('Diffuser', [(-.62, -.40, R + .3), (.62, -.40, R + .3), (.62, -.40, R + .02), (-.62, -.40, R + .02), (-.62, -.37, R + .3), (.62, -.37, R + .3), (.62, -.24, R + .02), (-.62, -.24, R + .02)], G, 'engine', .004, 1)
    hull('Lip spoiler', [(-.70, s.TOP(R + .15) - .005, R + .20), (.70, s.TOP(R + .15) - .005, R + .20), (.72, s.TOP(R + .05), R + .02), (-.72, s.TOP(R + .05), R + .02),
                         (-.70, s.TOP(R + .15) + .01, R + .20), (.70, s.TOP(R + .15) + .01, R + .20), (.72, s.TOP(R + .05) + .05, R + .01), (-.72, s.TOP(R + .05) + .05, R + .01)], G, 'paint', .008, 1)
    plate(R + .02, -.15, -1)
    for k in range(2):
        for sg in (-1, 1): box('Bonnet vent', (sg * .28, s.TOP(1.3 - k * .08) + s.CR(1.3) * .75 + .003, 1.3 - k * .08), (.20, .008, .025), G, 'engine', .002)
    mirror(s, .30, True)
    for sg in (-1, 1): box('Door handle', (sg * (s.W(-.3) + .004), s.door_top(-.3) - .06, -.3), (.01, .02, .14), G, 'chrome', .004)


CARS['Meridian'] = (dict(A=1.38, T=.84, Tr=.86, H=(-.38, -.10, -.42), zf=2.42, zr=-2.32, tub=(.30, -.98), wheel='ten', rim=.255, tyre_w=.125, tyre_wr=.15, flare=.05, tuck=.86, red_calipers=False, arch_gap=.035,
                         hw=[(2.42, .62), (2.32, .82), (2.0, .92), (1.38, .97), (.6, .94), (0, .94), (-.6, .96), (-1.38, 1.0), (-1.9, .97), (-2.2, .90), (-2.32, .80)],
                         top=[(2.42, -.12), (2.32, .01), (2.0, .10), (1.4, .17), (.8, .22), (.3, .25), (-.5, .27), (-1.2, .28), (-1.8, .26), (-2.15, .21), (-2.32, .13)],
                         bot=[(2.42, -.30), (2.2, -.39), (1.8, -.41), (-1.9, -.41), (-2.32, -.28)],
                         sh=[(2.42, -.16), (2.0, .06), (1.38, .17), (.6, .10), (0, .10), (-.6, .12), (-1.38, .21), (-1.9, .17), (-2.32, .05)],
                         crown=[(2.42, .01), (1.6, .05), (.4, .04), (0, .03), (-2.32, .02)],
                         cab=dict(zs=.33, zw=-.15, zrr=-.95, zb=-1.85, roof=.82, rw=.70, screen=1.1, back=.62, quarter=-1.15, pillar='paint', trim='chrome', bslot='engine', bpost=-.62, pr=.026)), meridian)


def build(name):
    C, details = CARS[name]
    bpy.ops.wm.read_factory_settings(use_empty=True)
    car = Car(C); pos, c, n = car.build(details)
    out = kit.join_all()
    for ob in out:
        g = ob.name.split('__')[0]
        if g in pos: kit.set_origin(ob, pos[g])
        if g == 'Steer': kit.set_origin(ob, c)
    print(name, 'steering wheel centre', c, 'column', n, 'seat', C['H'])
    kit.export(name)


if __name__ == '__main__':
    args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    for nm in (args or list(CARS)): build(nm)
