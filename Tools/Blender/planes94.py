"""Chastain Rush, Round 94 B (Dan, 2026-11-05: "We could also hide some planes behind the eggs as well"): three more planes, generated in Blender
from this script with the kit and the standard of plane.py / plane_dart.py.

Run:  F:\\blender\\blender.exe --background --factory-startup --python Tools/Blender/planes94.py -- [Barnstormer Hornet KestrelJet]

Our own designs, no make, badge, roundel or logo:
  Barnstormer  a 1920s / 30s biplane: two wings on struts and wires, a round radial cowl with its cylinders, an open cockpit with a small screen
               (the pilot seen from the chase camera), a wooden two-blade propeller, a tail wheel
  Hornet       a 1930s air racer: a fat barrel fuselage behind a big cowl, short wings, the cockpit far back at the fin, huge wheel pants
  KestrelJet   a jet trainer: a slim fuselage with a pointed nose, a long bubble canopy over two seats (the pilot in front), side intakes, swept
               wings and fin, a jet pipe at the tail (no propeller: Prop / Disc groups absent)
Unity vehicle space as plane.py: the origin at the physics box's middle (box 1.3 x 1.5 x 7.6 centred at y 0.05), the wheels' bottom at
y = GROUND (PlaneFlight.WheelDrop). Groups: Body, Prop (origin on the hub, turns about z), Disc. Output SourceArt/Blender/<Name>.blend,
Assets/Resources/VehicleModels/<Name>.fbx. The pilot's seat point H is printed ("seat (x, y, z)") for VehicleProfile.Seat.
"""
import bpy, math, sys, os
from mathutils import Vector
sys.path.insert(0, os.path.dirname(__file__))
import kit
from kit import B, box, hull, cyl, blob, tube, loft, ring

kit.COLORS['white'] = (.94, .94, .92)
GROUND = -1.25
G = 'Body'


def fuselage(keys, slot='paint', n=20):
    loft('Fuselage', [ring((0, cy, z), rx, ry, n, 'z') for z, rx, ry, cy in keys], G, slot, cap=True)


def seat(H, back=.70):
    hull('Seat', [(-.22, H.y - .14, H.z - .25), (.22, H.y - .14, H.z - .25), (.22, H.y - .14, H.z + .22), (-.22, H.y - .14, H.z + .22),
                  (-.22, H.y + .02, H.z - .25), (.22, H.y + .02, H.z - .25), (.22, H.y, H.z + .22), (-.22, H.y, H.z + .22)], G, 'interior', .04, 2)
    hull('Seat back', [(-.23, H.y - .06, H.z - .34), (.23, H.y - .06, H.z - .34), (.23, H.y - .06, H.z - .20), (-.23, H.y - .06, H.z - .20),
                       (-.21, H.y + back, H.z - .48), (.21, H.y + back, H.z - .48), (.21, H.y + back, H.z - .34), (-.21, H.y + back, H.z - .34)], G, 'interior', .04, 2)
    box('Panel', (0, H.y + .55, H.z + .95), (.62, .24, .08), G, 'engine', .02, (-15, 0, 0))
    for dx in (-.14, 0, .14): cyl('Dial', (dx, H.y + .56, H.z + .905), (dx, H.y + .56, H.z + .9), .045, G, 'metal', 12)
    cyl('Stick', (0, H.y - .25, H.z + .55), (0, H.y + .12, H.z + .48), .025, G, 'engine', 8)
    hull('Cockpit floor', [(-.35, H.y - .30, H.z - .45), (.35, H.y - .30, H.z - .45), (.35, H.y - .30, H.z + 1.0), (-.35, H.y - .30, H.z + 1.0),
                           (-.35, H.y - .27, H.z - .45), (.35, H.y - .27, H.z - .45), (.35, H.y - .27, H.z + 1.0), (-.35, H.y - .27, H.z + 1.0)], G, 'interior', 0, 1)


def wing(y, span, z0, z1, tipz0, tipz1, root=.55, t=.16, dihedral=.10, sweep=0.0, slot='paint', name='Wing'):
    """A tapered wing from the root (|x| = root) to the tip (|x| = span); chord z0..z1 at the root, tipz0..tipz1 at the tip."""
    for s in (-1, 1):
        x0, x1 = s * root, s * span; yt = y + dihedral
        hull(name, [(x0, y, z1), (x1, yt, tipz1 - sweep), (x1, yt, tipz0 - sweep), (x0, y, z0),
                    (x0, y + t, z1 - .05 * (z1 - z0)), (x1, yt + t * .55, tipz1 - sweep), (x1, yt + t * .55, tipz0 - sweep), (x0, y + t * .9, z0 + .1)], G, slot, .03, 2)
        blob(name + ' tip', (x1, yt + t * .3, (tipz0 + tipz1) / 2 - sweep), (.07, t * .4, (tipz1 - tipz0) / 2), G, slot, 10, 6)
    hull(name + ' centre', [(-root, y, z1), (root, y, z1), (root, y, z0), (-root, y, z0), (-root, y + t, z1 - .05 * (z1 - z0)), (root, y + t, z1 - .05 * (z1 - z0)), (root, y + t * .9, z0 + .1), (-root, y + t * .9, z0 + .1)], G, slot, .02, 1)


def nav_lights(span, y, z):
    for s in (-1, 1): blob('Nav light', (s * (span + .07), y, z), (.04, .04, .06), G, 'lamp' if s > 0 else 'tail', 8, 6)


def prop(hub, r, blades=2, slot='engine', tip='tail', width=.15):
    P = 'Prop'
    for i in range(blades):
        a = i * 2 * math.pi / blades; c, s_ = math.cos(a), math.sin(a)
        L = r - .12; m = .12 + L / 2
        box('Blade', (hub[0] + c * m, hub[1] + s_ * m, hub[2] + .05), (L, width, .05), P, slot, .01, (0, 0, math.degrees(a)))
        box('Blade tip', (hub[0] + c * (r - .06), hub[1] + s_ * (r - .06), hub[2] + .05), (.12, width + .002, .051), P, tip, .004, (0, 0, math.degrees(a)))
    cyl('Prop disc', (hub[0], hub[1], hub[2] + .04), (hub[0], hub[1], hub[2] + .05), r, 'Disc', 'glass', 40, smooth=False)


# ---------------------------------------------------------------- the biplane
def barnstormer():
    H = Vector((0, -.10, -.75)); HUB = (0, .05, 3.30)
    keys = [(3.10, .48, .48, .05), (2.85, .56, .56, .05), (2.2, .52, .58, .06), (1.0, .48, .56, .08), (.0, .44, .52, .10), (-.55, .42, .30, -.05),
            (-1.1, .40, .30, -.05), (-1.6, .36, .44, .10), (-2.6, .24, .34, .14), (-3.5, .12, .20, .20), (-3.95, .06, .10, .24)]
    fuselage(keys)
    # the radial: a cowl ring and nine cylinders showing round it
    cyl('Cowl ring', (0, .05, 2.75), (0, .05, 3.15), .62, G, 'metal', 28, r1=.55, cap=False)
    for i in range(9):
        a = i * 2 * math.pi / 9; c = (math.cos(a) * .50, .05 + math.sin(a) * .50, 3.0)
        cyl('Cylinder', (c[0] * .4, .05 + (c[1] - .05) * .4, 3.0), c, .085, G, 'engine', 10)
        for k in range(4): cyl('Fin', (c[0] * (.55 + k * .12), .05 + (c[1] - .05) * (.55 + k * .12), 2.98), (c[0] * (.55 + k * .12), .05 + (c[1] - .05) * (.55 + k * .12), 3.02), .11, G, 'engine', 10)
    blob('Hub cap', HUB, (.14, .14, .18), G, 'metal', 12, 8)
    # two wings, the top one ahead (stagger), interplane and cabane struts, flying wires
    wing(-.55, 4.3, -.35, .85, -.20, .70, root=.45, t=.14, dihedral=.12, name='Lower wing')
    wing(1.10, 4.5, -.15, 1.10, .0, .95, root=.0, t=.15, dihedral=.0, name='Upper wing')
    for s in (-1, 1):
        for z in (.55, -.10):
            cyl('Interplane strut', (s * 3.3, -.40 + .1, z + .05), (s * 3.3, 1.12, z + .25), .035, G, 'metal', 8)
        for z in (.75, .05): cyl('Cabane strut', (s * .35, .45, z), (s * .40, 1.12, z + .15), .03, G, 'metal', 8)
        tube('Flying wire', [(s * .45, -.38, .70), (s * 3.3, 1.12, .70)], .008, G, 'chrome', 2)
        tube('Flying wire', [(s * .45, -.38, -.05), (s * 3.3, 1.12, .05)], .008, G, 'chrome', 2)
        tube('Landing wire', [(s * .40, 1.12, .75), (s * 3.3, -.30, .60)], .008, G, 'chrome', 2)
    nav_lights(4.5, 1.20, .5)
    for s in (-1, 1): box('Wing stripe', (s * 3.7, 1.27, .45), (.45, .01, 1.0), G, 'white', .004)
    # the open cockpit: a padded rim, a small screen, the seat and panel
    tube('Cockpit rim', [(math.cos(a) * .38, .27, H.z + .05 + math.sin(a) * .55) for a in [i * 2 * math.pi / 20 for i in range(21)]], .04, G, 'interior', 4)
    kit.loft('Screen', [[(x * .28, .29 + .045 * i, H.z + .62 - .05 * i - .06 * (1 - abs(x))) for x in (-1, -.5, 0, .5, 1)] + [(x * .28, .29 + .045 * i, H.z + .632 - .05 * i - .06 * (1 - abs(x))) for x in (1, .5, 0, -.5, -1)] for i in range(4)], G, 'glass', cap=True)
    seat(H, .62)
    # the tail and the gear (two mains on a vee, a small tail wheel)
    hull('Stabilizer', [(-1.6, .28, -3.25), (1.6, .28, -3.25), (1.35, .28, -3.95), (-1.35, .28, -3.95), (-1.6, .33, -3.30), (1.6, .33, -3.30), (1.35, .32, -3.90), (-1.35, .32, -3.90)], G, 'paint', .02, 1)
    hull('Fin', [(-.05, .30, -3.0), (.05, .30, -3.0), (.05, .30, -4.05), (-.05, .30, -4.05), (-.03, 1.35, -3.75), (.03, 1.35, -3.75), (.03, 1.35, -4.20), (-.03, 1.35, -4.20)], G, 'paint', .02, 1)
    box('Rudder stripe', (0, .95, -4.05), (.11, .55, .25), G, 'white', .01, (-18, 0, 0))
    for s in (-1, 1):
        cyl('Gear leg', (s * .2, -.45, 1.4), (s * .95, GROUND + .33, 1.15), .04, G, 'metal', 8)
        cyl('Gear leg', (s * .2, -.45, .5), (s * .95, GROUND + .33, 1.15), .04, G, 'metal', 8)
        cyl('Main wheel', (s * .88, GROUND + .33, 1.15), (s * 1.05, GROUND + .33, 1.15), .33, G, 'rubber', 18)
        cyl('Wheel disc', (s * 1.051, GROUND + .33, 1.15), (s * 1.056, GROUND + .33, 1.15), .25, G, 'white', 18)
    cyl('Axle', (-.9, GROUND + .33, 1.15), (.9, GROUND + .33, 1.15), .035, G, 'metal', 8)
    cyl('Tail strut', (0, -.0, -3.6), (0, GROUND + .12, -3.75), .03, G, 'metal', 6)
    cyl('Tail wheel', (-.04, GROUND + .12, -3.75), (.04, GROUND + .12, -3.75), .12, G, 'rubber', 12)
    prop(HUB, .95, 2, 'interior', 'metal', .13)
    return H, HUB, 4.5


# ---------------------------------------------------------------- the air racer
def hornet():
    H = Vector((0, .02, -1.95)); HUB = (0, .02, 3.05)
    keys = [(2.85, .50, .50, .02), (2.55, .66, .66, .02), (2.0, .76, .76, .02), (1.2, .78, .78, .02), (.3, .72, .70, .03), (-.6, .60, .60, .05),
            (-1.4, .46, .40, .0), (-2.2, .32, .32, .04), (-3.0, .20, .30, .24), (-3.7, .08, .14, .28), (-3.95, .04, .06, .30)]
    fuselage(keys, n=22)
    cyl('Cowl', (0, .02, 2.40), (0, .02, 2.95), .70, G, 'white', 28, r1=.58, cap=False)
    cyl('Cowl face', (0, .02, 2.93), (0, .02, 2.95), .58, G, 'engine', 28)
    blob('Spinner', HUB, (.20, .20, .32), G, 'white', 14, 8)
    for s in (-1, 1):
        for k in range(4): box('Exhaust stub', (s * .62, -.35 + k * .02, 2.2 - k * .16), (.06, .05, .10), G, 'engine', .01)
    # the checker band round the cowl's back (paint / white squares)
    for i in range(16):
        a = i * 2 * math.pi / 16; box('Checker', (math.cos(a) * .775, .02 + math.sin(a) * .775, 2.25), (.16, .02, .12), G, 'white' if i % 2 else 'engine', .004, (0, 0, math.degrees(a) + 90))
    # short wings low on the barrel, the cockpit far back with a small bubble, the fin with its rounded top
    wing(-.55, 3.3, -.40, .75, -.15, .45, root=.6, t=.18, dihedral=.10, name='Wing')
    nav_lights(3.3, -.40, .2)
    for s in (-1, 1): box('Wing stripe', (s * 2.6, -.36, .2), (.35, .01, .70), G, 'white', .004)
    blob('Canopy', (0, H.y + .66, H.z + .05), (.30, .34, .60), G, 'glass', 18, 10)
    hull('Turtle deck', [(-.18, .30, H.z - .40), (.18, .30, H.z - .40), (-.06, .30, -3.6), (.06, .30, -3.6), (-.14, H.y + .70, H.z - .40), (.14, H.y + .70, H.z - .40), (-.04, .40, -3.6), (.04, .40, -3.6)], G, 'paint', .03, 2)
    seat(H, .60)
    hull('Stabilizer', [(-1.2, .25, -3.35), (1.2, .25, -3.35), (1.0, .25, -3.95), (-1.0, .25, -3.95), (-1.2, .30, -3.40), (1.2, .30, -3.40), (1.0, .29, -3.90), (-1.0, .29, -3.90)], G, 'paint', .02, 1)
    hull('Fin', [(-.05, .28, -2.95), (.05, .28, -2.95), (.05, .28, -4.0), (-.05, .28, -4.0), (-.03, 1.15, -3.55), (.03, 1.15, -3.55), (.03, 1.15, -4.05), (-.03, 1.15, -4.05)], G, 'paint', .02, 1)
    blob('Fin top', (0, 1.12, -3.80), (.04, .14, .28), G, 'paint', 10, 6)
    box('Race number disc', (0, .55, -1.0), (0, 0, 0), G, 'white', 0) if False else None
    for s in (-1, 1):
        cyl('Race disc', (s * .73, .05, -.2), (s * .76, .05, -.2), .32, G, 'white', 20)
        cyl('Main leg', (s * .85, -.55, .35), (s * 1.0, GROUND + .30, .35), .05, G, 'metal', 8)
        blob('Wheel pant', (s * 1.0, GROUND + .40, .35), (.17, .36, .72), G, 'paint', 14, 8)
        cyl('Main wheel', (s * .92, GROUND + .30, .35), (s * 1.08, GROUND + .30, .35), .30, G, 'rubber', 18)
    cyl('Tail wheel leg', (0, .0, -3.5), (0, GROUND + .10, -3.62), .03, G, 'metal', 6)
    cyl('Tail wheel', (-.04, GROUND + .10, -3.62), (.04, GROUND + .10, -3.62), .10, G, 'rubber', 12)
    prop(HUB, 1.0, 2, 'engine', 'tail', .16)
    return H, HUB, 3.3


# ---------------------------------------------------------------- the jet trainer
def kestrel():
    H = Vector((0, -.12, .35))
    keys = [(3.85, .02, .02, -.02), (3.5, .16, .16, -.03), (3.0, .32, .34, -.02), (2.3, .44, .48, .0), (1.4, .50, .50, .0), (.9, .50, .42, -.06), (.3, .52, .40, -.08),
            (-.8, .56, .40, -.06), (-1.5, .54, .46, .0), (-1.8, .52, .48, .04), (-2.8, .42, .40, .08), (-3.6, .34, .34, .10), (-3.85, .32, .32, .10)]
    fuselage(keys, n=22)
    blob('Nose cone', (0, -.02, 3.5), (.16, .16, .40), G, 'engine', 14, 8)
    # the long bubble over two seats (the pilot in front, an empty rear seat), its frame
    blob('Canopy', (0, .52, -.15), (.42, .40, 1.45), G, 'glass', 22, 12)
    tube('Canopy frame', [(math.cos(a) * .40, .42 + math.sin(a) * .36, -.25) for a in [i * math.pi / 10 for i in range(11)]], .025, G, 'engine', 4)
    seat(H, .72)
    Hr = H + Vector((0, .12, -1.05)); seat(Hr, .72)
    # side intakes, swept low wings, the swept fin and tailplane, the jet pipe
    for s in (-1, 1):
        hull('Intake', [(s * .40, -.30, 1.05), (s * .78, -.32, .55), (s * .78, -.32, -.6), (s * .48, -.30, -.6),
                        (s * .40, .22, 1.05), (s * .74, .18, .55), (s * .74, .18, -.6), (s * .48, .20, -.6)], G, 'paint', .03, 2)
        hull('Intake mouth', [(s * .44, -.24, 1.07), (s * .76, -.26, .62), (s * .76, -.26, .60), (s * .44, -.24, 1.05),
                              (s * .44, .16, 1.07), (s * .72, .12, .62), (s * .72, .12, .60), (s * .44, .16, 1.05)], G, 'engine', .01, 1)
    wing(-.45, 4.0, -1.3, .95, -1.30, -.55, root=.6, t=.16, dihedral=-.04, sweep=.25, name='Wing')
    nav_lights(4.0, -.45, -1.15)
    for s in (-1, 1):
        box('Wing stripe', (s * 3.2, -.27, -1.15), (.35, .01, .62), G, 'white', .004)
        blob('Tip tank', (s * 4.05, -.42, -.9), (.12, .12, .70), G, 'white', 12, 8)
    hull('Stabilizer', [(-1.6, .05, -2.7), (1.6, .05, -3.35), (1.2, .05, -3.75), (-1.2, .05, -3.75), (-1.6, .10, -2.75), (1.6, .10, -3.40), (1.2, .10, -3.72), (-1.2, .10, -3.72)], G, 'paint', .02, 1) if False else None
    for s in (-1, 1):
        hull('Stabilizer', [(s * .25, .05, -2.75), (s * 1.6, .05, -3.40), (s * 1.6, .05, -3.70), (s * .25, .05, -3.75),
                            (s * .25, .11, -2.80), (s * 1.6, .09, -3.43), (s * 1.6, .09, -3.67), (s * .25, .11, -3.72)], G, 'paint', .02, 1)
    hull('Fin', [(-.06, .40, -2.2), (.06, .40, -2.2), (.06, .40, -3.6), (-.06, .40, -3.6), (-.03, 1.55, -3.25), (.03, 1.55, -3.25), (.03, 1.55, -3.85), (-.03, 1.55, -3.85)], G, 'paint', .02, 1)
    box('Fin stripe', (0, 1.25, -3.55), (.13, .16, .70), G, 'white', .01, (-30, 0, 0))
    blob('Beacon', (0, 1.58, -3.75), (.05, .05, .06), G, 'tail', 8, 6)
    cyl('Jet pipe', (0, .10, -3.70), (0, .10, -3.95), .28, G, 'metal', 22, r1=.25, cap=False)
    cyl('Jet pipe inside', (0, .10, -3.72), (0, .10, -3.73), .26, G, 'engine', 22)
    # tricycle gear
    cyl('Nose leg', (0, -.45, 2.4), (0, GROUND + .22, 2.45), .05, G, 'metal', 8)
    cyl('Nose wheel', (-.06, GROUND + .22, 2.45), (.06, GROUND + .22, 2.45), .22, G, 'rubber', 16)
    for s in (-1, 1):
        cyl('Main leg', (s * 1.2, -.42, -.75), (s * 1.25, GROUND + .28, -.80), .055, G, 'metal', 8)
        cyl('Main wheel', (s * 1.18, GROUND + .28, -.80), (s * 1.32, GROUND + .28, -.80), .28, G, 'rubber', 18)
    return H, None, 4.0


PLANES = {'Barnstormer': barnstormer, 'Hornet': hornet, 'KestrelJet': kestrel}


def build(name):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    H, HUB, span = PLANES[name]()
    out = kit.join_all()
    for ob in out:
        g = ob.name.split('__')[0]
        if g in ('Prop', 'Disc') and HUB: kit.set_origin(ob, HUB)
    print(name, 'hub', HUB, 'wheels at', GROUND, 'span', span, 'seat (%.2f, %.2f, %.2f)' % (H.x, H.y, H.z))
    kit.export(name)


if __name__ == '__main__':
    args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    for nm in (args or list(PLANES)): build(nm)
