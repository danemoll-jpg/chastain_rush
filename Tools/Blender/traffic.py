"""0.81 Part C: the ambient traffic kit (visual only), generated in Blender from this script with the 0.75 kit.

Run:  blender --background --factory-startup --python Tools/Blender/traffic.py -- [TrafficSedan|TrafficWagon|TrafficPickup|TrafficVan]

Four plain everyday vehicles in the same stylized low-poly style as the player's: a sedan, a station wagon, a pickup truck
and a van. They replace the blocky traffic bodies with the same footprint as before (about 2.0 x 4.3 m, so the traffic
collider and behaviour are unchanged): wheel centres (+/-0.92, -0.22, +/-1.35), radius 0.33, ground at y = -0.55.
Each has headlamps and tail lamps (slots lamp / tail: they glow at night like the player's) and a simple driver
silhouette at the left seat (slot 'driver'). Groups: Body (fixed), WheelFL / FR / RL / RR (origin on the axle).
Slots: paint (the traffic colour), metal, chrome, engine, rubber, lamp, tail, glass, interior, driver.
"""
import bpy, math, sys, os
sys.path.insert(0, os.path.dirname(__file__))
import kit
from kit import box, hull, cyl, blob, made, place, at

kit.COLORS['driver'] = (.16, .15, .14)
TRACK, AXLE, WY = .92, 1.35, -.22


def slab(name, x, y0, y1, z0, z1, y1front=None, z1top=None, slot='paint', bevel=.06, taper=.04):
    """A body block: bottom y0, top y1 (y1front at the front end), front z1 (z1top at the top: a sloped face)."""
    yf = y1 if y1front is None else y1front; zt = z1 if z1top is None else z1top; t = taper
    return hull(name, [(-x, y0, z0), (x, y0, z0), (x, y0, z1), (-x, y0, z1),
                       (-x + t, y1, z0 + t), (x - t, y1, z0 + t), (x - t, yf, zt), (-x + t, yf, zt)], 'Body', slot, bevel, 2)


def glass_box(x, y0, y1, z0, z1, rake_f=.25, rake_r=.12):
    """Cabin: a glass block (windows) with a painted roof cap and pillars."""
    hull('Cabin glass', [(-x, y0, z0), (x, y0, z0), (x, y0, z1), (-x, y0, z1),
                         (-x + .1, y1, z0 + rake_r), (x - .1, y1, z0 + rake_r), (x - .1, y1, z1 - rake_f), (-x + .1, y1, z1 - rake_f)], 'Body', 'glass', .02, 1)
    hull('Roof', [(-x + .09, y1 - .01, z0 + rake_r - .02), (x - .09, y1 - .01, z0 + rake_r - .02), (x - .09, y1 - .01, z1 - rake_f + .02), (-x + .09, y1 - .01, z1 - rake_f + .02),
                  (-x + .12, y1 + .06, z0 + rake_r), (x - .12, y1 + .06, z0 + rake_r), (x - .12, y1 + .06, z1 - rake_f), (-x + .12, y1 + .06, z1 - rake_f)], 'Body', 'paint', .025, 2)
    for s in (-1, 1):
        for z, top in ((z1, z1 - rake_f), (z0, z0 + rake_r)):
            cyl('Pillar', (s * (x - .005), y0, z), (s * (x - .1), y1, top), .04, 'Body', 'paint', 8)


def driver(seat):
    """A simple driver silhouette (head, shoulders, arms to the wheel) at the left seat."""
    x, y, z = seat
    blob('Driver head', (x, y + .62, z), (.10, .12, .11), 'Body', 'driver', 12, 8)
    blob('Driver torso', (x, y + .30, z - .02), (.20, .26, .13), 'Body', 'driver', 12, 8)
    for s in (-1, 1): cyl('Driver arm', (x + s * .16, y + .40, z), (x + s * .12, y + .28, z + .36), .045, 'Body', 'driver', 8)
    cyl('Steering wheel', (x, y + .27, z + .40), (x, y + .30, z + .44), .17, 'Body', 'interior', 16)


def lamps(front, rear, yh, yt, x, round_=False):
    for s in (-1, 1):
        if round_: cyl('Headlamp', (s * x, yh, front - .02), (s * x, yh, front + .015), .085, 'Body', 'lamp', 16)
        else: box('Headlamp', (s * x, yh, front), (.30, .12, .04), 'Body', 'lamp', .01)
        box('Tail lamp', (s * x, yt, rear), (.22, .16, .04), 'Body', 'tail', .01)


def bumpers(front, rear, w=.98, slot='chrome'):
    box('Front bumper', (0, -.16, front + .02), (w * 2, .14, .12), 'Body', slot, .03)
    box('Rear bumper', (0, -.16, rear - .02), (w * 2, .14, .12), 'Body', slot, .03)


def common(front, rear):
    box('Grille', (0, .02, front + .005), (.9, .16, .04), 'Body', 'engine', .01)
    box('Rear plate', (0, -.02, rear - .03), (.34, .13, .02), 'Body', 'metal', .006)
    for s in (-1, 1):
        blob('Mirror', (s * 1.03, .52, .62), (.05, .045, .08), 'Body', 'paint', 10, 6)
        box('Arch shadow F', (s * .955, -.06, AXLE), (.06, .1, .82), 'Body', 'rubber', .02)  # 0.89: 5 mm proud of the body sides
        box('Arch shadow R', (s * .955, -.06, -AXLE), (.06, .1, .82), 'Body', 'rubber', .02)


def sedan():
    slab('Lower body', .97, -.33, .28, -2.14, 2.14, y1front=.20, z1top=2.02)
    glass_box(.88, .28, .82, -1.25, .65, .45, .35)
    lamps(2.14, -2.14, .10, .13, .70); bumpers(2.14, -2.14); common(2.14, -2.14); driver((-.38, -.04, -.10))


def wagon():
    slab('Lower body', .97, -.33, .30, -2.14, 2.14, y1front=.22, z1top=2.02)
    glass_box(.88, .30, .86, -2.02, .62, .45, .06)
    for s in (-1, 1): cyl('Roof rail', (s * .62, .95, -1.85), (s * .62, .95, .05), .02, 'Body', 'chrome', 6)
    lamps(2.14, -2.14, .12, .20, .70); bumpers(2.14, -2.14, slot='engine'); common(2.14, -2.14); driver((-.38, -.02, -.10))


def pickup():
    slab('Hood and cab base', .98, -.33, .36, -.40, 2.14, y1front=.30, z1top=2.00)
    glass_box(.90, .36, .98, -.40, .62, .32, .02)
    # open bed: floor, sides and tailgate
    box('Bed floor', (0, .02, -1.30), (1.80, .06, 1.75), 'Body', 'engine', .01)
    slab('Bed base', .98, -.33, .02, -2.14, -.40, bevel=.03, taper=0)
    for s in (-1, 1): box('Bed side', (s * .92, .22, -1.27), (.12, .44, 1.78), 'Body', 'paint', .03)
    box('Tailgate', (0, .22, -2.10), (1.95, .42, .08), 'Body', 'paint', .02)  # 0.89: 1 cm inside the bed sides' top and bottom
    box('Cab back', (0, .22, -.43), (1.95, .42, .06), 'Body', 'paint', .02)
    lamps(2.14, -2.15, .14, .22, .78, round_=True); bumpers(2.14, -2.14); common(2.14, -2.14); driver((-.40, .06, .10))


def van():
    x, t = .99, .06
    hull('Body', [(-x, -.33, -2.14), (x, -.33, -2.14), (x, -.33, 1.95), (-x, -.33, 1.95),
                  (-x + t, 1.30, -2.14 + t), (x - t, 1.30, -2.14 + t), (x - t, 1.30, 1.30), (-x + t, 1.30, 1.30)], 'Body', 'paint', .10, 2)
    slab('Hood', .97, -.32, .40, 1.2, 2.14, y1front=.32, z1top=2.04)  # 0.89: underside 1 cm above the body's
    # windscreen on the sloped front (z = 1.95 - 0.399 (y + 0.33)), side windows along the top
    def zs(y): return 1.95 - .399 * (y + .33) + .012
    hull('Windscreen', [(-.84, .48, zs(.48)), (.84, .48, zs(.48)), (.80, 1.18, zs(1.18)), (-.80, 1.18, zs(1.18)),
                        (-.84, .48, zs(.48) + .02), (.84, .48, zs(.48) + .02), (.80, 1.18, zs(1.18) + .02), (-.80, 1.18, zs(1.18) + .02)], 'Body', 'glass', .005, 1)
    for s in (-1, 1):
        for z0, z1 in ((.55, 1.25), (-1.0, .40), (-2.0, -1.15)):
            hull('Side window', [(s * .985, .62, z0), (s * 1.0, .62, z0), (s * 1.0, .62, z1), (s * .985, .62, z1),
                                 (s * .985, 1.12, z0), (s * 1.0, 1.12, z0), (s * 1.0, 1.12, z1), (s * .985, 1.12, z1)], 'Body', 'glass', .004, 1)
        box('Side stripe', (s * .995, .30, -.20), (.012, .06, 3.9), 'Body', 'chrome', .002)
    box('Rear door seam', (0, .55, -2.155), (.02, 1.4, .02), 'Body', 'engine', .002)
    lamps(2.14, -2.14, .12, .30, .76); bumpers(2.14, -2.14, slot='engine'); common(2.14, -2.14); driver((-.40, .10, 1.05))


def wheel(group, pos, left):
    s0 = len(kit.PARTS)
    kit.tyre(group, .255, .10, .075, 24, 8, knobs=0, squared=.86)
    cyl(group + ' rim', (-.07, 0, 0), (.07, 0, 0), .19, group, 'metal', 20, cap=False)
    cyl(group + ' hub cap', (.06, 0, 0), (.085, 0, 0), .17, group, 'metal', 20)
    cyl(group + ' hub', (.085, 0, 0), (.10, 0, 0), .05, group, 'chrome', 12)
    place(made(s0), at(pos, left))


def build(name):
    bpy.ops.wm.read_factory_settings(use_empty=True)
    {'TrafficSedan': sedan, 'TrafficWagon': wagon, 'TrafficPickup': pickup, 'TrafficVan': van}[name]()
    pos = {'WheelFL': (-TRACK, WY, AXLE), 'WheelFR': (TRACK, WY, AXLE), 'WheelRL': (-TRACK, WY, -AXLE), 'WheelRR': (TRACK, WY, -AXLE)}
    for g, p in pos.items(): wheel(g, p, p[0] < 0)
    out = kit.join_all()
    for ob in out:
        g = ob.name.split('__')[0]
        if g in pos: kit.set_origin(ob, pos[g])
    kit.export(name)


if __name__ == '__main__':
    args = sys.argv[sys.argv.index('--') + 1:] if '--' in sys.argv else []
    for n in (args or ['TrafficSedan', 'TrafficWagon', 'TrafficPickup', 'TrafficVan']): build(n)
