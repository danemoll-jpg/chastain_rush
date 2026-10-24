"""Chastain Rush, Round 63: the player's helicopter (Sky Survey), generated in Blender from this script (same kit and standard as the vehicles).

Run:  F:\\blender\\blender.exe --background --factory-startup --python Tools/Blender/helicopter.py

Our own design, no make, badge or logo: a light single-engine utility helicopter. A rounded cabin with a big glass bubble, two seats (the pilot on
the right, the game's rider in the cars' seated pose at Seat H), skids on two cross tubes, the engine cowl and mast behind the cabin, a two-blade
main rotor (10.4 m across), a tapered tail boom with a stabilizer, a swept fin and a two-blade tail rotor on its left side. Unity vehicle space as
the other vehicles (x right, y up, z forward), the origin at the physics box's middle (VehicleProfile.Helicopter: box 2.1 x 2.3 x 4.6 m centred at
y 0.05): the skids' bottom at y = GROUND. Groups: Body, Rotor (origin on the main rotor's hub, turns about y), TailRotor (origin on its hub, turns
about x), Disc (the main rotor's blur disc, shown at full speed). Slots: paint, white, glass, metal, engine, rubber, lamp, tail, interior.
Output: SourceArt/Blender/Helicopter.blend, Assets/Resources/VehicleModels/Helicopter.fbx
"""
import bpy, math, sys, os
from mathutils import Vector
sys.path.insert(0, os.path.dirname(__file__))
import kit
from kit import B, box, hull, cyl, blob, tube, loft, ring

kit.COLORS['white'] = (.93, .93, .91)
GROUND = -1.10
H = Vector((.36, -.32, .55))                     # the pilot's seat point (the Car pose's hip); VehicleProfile.Helicopter.Seat
HUB = (0, 1.56, -.30)                            # main rotor hub
TAIL_HUB = (-.24, 1.28, -6.38)                   # tail rotor hub (left side of the fin)
R_MAIN, R_TAIL = 5.2, .78


def body():
    G = 'Body'
    # ---- the lower tub: elliptical rings from the nose back to the boom's root (top at the belt line, y ~ 0)
    keys = [(2.28, .05, .05), (2.18, .32, .22), (1.95, .58, .32), (1.55, .74, .37), (.9, .80, .38), (0, .80, .38), (-.9, .78, .38), (-1.5, .62, .36), (-1.75, .40, .30)]
    secs = [ring((0, -.40, z), rx, ry, 16, 'z') for z, rx, ry in keys]
    loft('Lower tub', secs, G, 'paint', cap=True)
    loft('Belt stripe', [ring((0, -.26, z), rx * 1.01, .07, 16, 'z') for z, rx, ry in keys[1:-1]], G, 'white', cap=True)
    # ---- the glass bubble over the cabin, the frame's centre post and door posts
    blob('Canopy', (0, .02, .78), (.78, .86, 1.48), G, 'glass', 22, 14)
    tube('Canopy post', [(0, -.10, 2.12), (0, .55, 1.70), (0, .86, .90), (0, .88, .10)], .03, G, 'paint', 8)
    for s in (-1, 1):
        tube('Door post', [(s * .76, -.05, .00), (s * .74, .45, .02), (s * .55, .80, .05)], .035, G, 'paint', 8)
        tube('Door post', [(s * .72, -.05, -.62), (s * .70, .50, -.64), (s * .50, .82, -.66)], .04, G, 'paint', 8)
    # ---- the upper rear body (cabin roof behind the bubble, the engine deck) and the engine cowl, exhaust, mast
    hull('Roof', [(-.74, -.05, -.62), (.74, -.05, -.62), (.70, -.05, -1.72), (-.70, -.05, -1.72), (-.55, .92, -.40), (.55, .92, -.40), (.48, .82, -1.62), (-.48, .82, -1.62)], G, 'paint', .08, 3)
    blob('Engine cowl', (0, 1.00, -1.00), (.46, .30, .95), G, 'paint', 18, 10)
    blob('Cowl stripe', (0, .98, -1.00), (.47, .10, .96), G, 'white', 18, 6)
    for s in (-1, 1):
        box('Air intake', (s * .40, 1.02, -.62), (.06, .18, .34), G, 'engine', .02)
    cyl('Exhaust', (0, 1.02, -1.80), (0, 1.10, -2.20), .14, G, 'engine', 14, r1=.16)
    cyl('Mast', (HUB[0], .95, HUB[2]), (HUB[0], HUB[1] - .06, HUB[2]), .09, G, 'metal', 12)
    cyl('Swashplate', (HUB[0], 1.18, HUB[2]), (HUB[0], 1.26, HUB[2]), .20, G, 'metal', 16)
    # ---- tail boom, stabilizer, fin, tail rotor gearbox and the skid under the fin
    cyl('Tail boom', (0, .44, -1.55), (0, .80, -6.30), .30, G, 'paint', 18, r1=.12)
    hull('Stabilizer', [(-1.05, .70, -5.05), (1.05, .70, -5.05), (1.05, .70, -5.55), (-1.05, .70, -5.55), (-1.05, .76, -5.08), (1.05, .76, -5.08), (1.05, .76, -5.52), (-1.05, .76, -5.52)], G, 'paint', .015, 1)
    for s in (-1, 1): hull('Stabilizer tip', [(s * 1.03, .48, -5.02), (s * 1.09, .48, -5.02), (s * 1.09, .48, -5.58), (s * 1.03, .48, -5.58), (s * 1.03, 1.0, -5.10), (s * 1.09, 1.0, -5.10), (s * 1.09, 1.0, -5.52), (s * 1.03, 1.0, -5.52)], G, 'paint', .01, 1)
    hull('Fin', [(-.05, .66, -5.95), (.05, .66, -5.95), (.05, .70, -6.62), (-.05, .70, -6.62), (-.04, 1.95, -6.55), (.04, 1.95, -6.55), (.04, 1.95, -7.00), (-.04, 1.95, -7.00)], G, 'paint', .02, 1)
    hull('Lower fin', [(-.04, .30, -6.20), (.04, .30, -6.20), (.04, .30, -6.55), (-.04, .30, -6.55), (-.05, .72, -6.00), (.05, .72, -6.00), (.05, .72, -6.62), (-.05, .72, -6.62)], G, 'paint', .015, 1)
    tube('Tail skid', [(0, .40, -6.10), (0, .12, -6.45), (0, .10, -6.75)], .025, G, 'metal', 6)
    cyl('Tail gearbox', (-.06, TAIL_HUB[1], TAIL_HUB[2]), (TAIL_HUB[0] + .04, TAIL_HUB[1], TAIL_HUB[2]), .09, G, 'metal', 12)
    # ---- skids: two tubes with turned-up fronts on two arched cross tubes
    for s in (-1, 1):
        tube('Skid', [(s * 1.0, GROUND + .03, -1.45), (s * 1.0, GROUND + .03, .6), (s * 1.0, GROUND + .04, 1.45), (s * 1.0, GROUND + .14, 1.75), (s * 1.0, GROUND + .30, 1.86)], .045, G, 'metal', 8)
        for z in (.85, -.75):
            tube('Cross tube', [(s * 1.0, GROUND + .03, z), (s * .95, GROUND + .25, z), (s * .70, -.66, z)], .04, G, 'metal', 6)
        box('Step', (s * .92, -.80, .25), (.20, .03, .45), G, 'rubber', .005)
    # ---- inside: two seats, the panel and its hood, the pilot's cyclic and collective (the Car pose's hands come near them)
    for x in (H.x, -H.x):
        hull('Seat', [(x - .24, H.y - .14, H.z - .25), (x + .24, H.y - .14, H.z - .25), (x + .24, H.y - .14, H.z + .22), (x - .24, H.y - .14, H.z + .22),
                      (x - .24, H.y + .02, H.z - .25), (x + .24, H.y + .02, H.z - .25), (x + .24, H.y, H.z + .22), (x - .24, H.y, H.z + .22)], G, 'interior', .04, 2)
        hull('Seat back', [(x - .25, H.y - .06, H.z - .34), (x + .25, H.y - .06, H.z - .34), (x + .25, H.y - .06, H.z - .20), (x - .25, H.y - .06, H.z - .20),
                           (x - .23, H.y + .72, H.z - .44), (x + .23, H.y + .72, H.z - .44), (x + .23, H.y + .72, H.z - .30), (x - .23, H.y + .72, H.z - .30)], G, 'interior', .04, 2)
    hull('Panel', [(-.62, -.40, 1.35), (.62, -.40, 1.35), (.62, -.40, 1.62), (-.62, -.40, 1.62), (-.55, .06, 1.30), (.55, .06, 1.30), (.55, .02, 1.50), (-.55, .02, 1.50)], G, 'interior', .03, 2)
    box('Panel face', (0, -.06, 1.29), (1.0, .22, .02), G, 'engine', .004, (-15, 0, 0))
    c = H + Vector((0, .36, .36))
    cyl('Cyclic', (c.x, H.y - .10, c.z + .05), (c.x, c.y - .05, c.z), .02, G, 'engine', 8)
    blob('Cyclic grip', (c.x, c.y, c.z), (.035, .07, .035), G, 'rubber', 10, 6)
    cyl('Collective', (c.x - .30, H.y - .05, H.z + .05), (c.x - .28, H.y + .12, H.z + .40), .018, G, 'engine', 8)
    # ---- lamps: the landing light under the nose, navigation lights, the beacon on the fin and under the belly
    cyl('Landing light', (0, -.74, 1.85), (0, -.80, 1.95), .09, G, 'lamp', 14)
    blob('Nav light', (.80, -.20, .80), (.04, .04, .06), G, 'lamp', 8, 6)
    blob('Nav light', (-.80, -.20, .80), (.04, .04, .06), G, 'tail', 8, 6)
    blob('Beacon', (0, 1.98, -6.78), (.06, .05, .06), G, 'tail', 8, 6)
    blob('Belly beacon', (0, -.80, -.70), (.07, .04, .07), G, 'tail', 8, 6)


def rotor():
    G = 'Rotor'
    blob('Hub', HUB, (.20, .14, .20), G, 'metal', 14, 8)
    for s in (-1, 1):
        # a blade: a long thin box with a slight twist look (two pieces), and its grip at the hub
        box('Blade grip', (s * .42, HUB[1], HUB[2]), (.55, .10, .22), G, 'metal', .02)
        box('Blade', (s * (.70 + (R_MAIN - .70) / 2), HUB[1] - .02, HUB[2]), (R_MAIN - .70, .05, .32), G, 'engine', .01)
        box('Blade tip', (s * (R_MAIN - .12), HUB[1] - .025, HUB[2]), (.24, .051, .321), G, 'white', .005)


def tail_rotor():
    G = 'TailRotor'
    x, y, z = TAIL_HUB
    blob('Tail hub', (x, y, z), (.08, .08, .08), G, 'metal', 10, 6)
    for s in (-1, 1):
        box('Tail blade', (x, y + s * R_TAIL / 2, z), (.03, R_TAIL, .13), G, 'engine', .005)
        box('Tail blade tip', (x - .002, y + s * (R_TAIL - .06), z), (.032, .12, .131), G, 'white', .003)


def disc():
    # the blur disc: a very thin cylinder (the game gives it a faint see-through material)
    cyl('Rotor disc', (HUB[0], HUB[1] - .03, HUB[2]), (HUB[0], HUB[1] - .02, HUB[2]), R_MAIN, 'Disc', 'glass', 48, smooth=False)


def build():
    bpy.ops.wm.read_factory_settings(use_empty=True)
    body(); rotor(); tail_rotor(); disc()
    out = kit.join_all()
    for ob in out:
        g = ob.name.split('__')[0]
        if g in ('Rotor', 'Disc'): kit.set_origin(ob, HUB)
        if g == 'TailRotor': kit.set_origin(ob, TAIL_HUB)
    print('Helicopter: hub', HUB, 'tail hub', TAIL_HUB, 'skids at', GROUND)
    kit.export('Helicopter')


if __name__ == '__main__':
    build()
