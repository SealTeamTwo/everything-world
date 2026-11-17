"""Keep artist areas where they are when the playlist changes.

The game remembers where every artist stood the last time the world was settled
(Game/EverythingWorld/Assets/_Project/Resources/ArtistPlaces.txt). Lands put artists who are in that
file back in the same place, and only find spots for new artists and for ones whose area changed size.

After a playlist change and a rebuild of the data:

    python build/take_shots.py places     (Unity closed) builds the world and writes Logs/Places.txt
    python build/artist_places.py check   lists every artist who is new or moved since the settled places
    python build/artist_places.py save    makes the world as built the new settled places

Check before you save: a long list of moved artists means a land rearranged itself and needs a look.
"""
import collections
import json
import os
import shutil
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PROJECT = os.path.join(ROOT, 'Game', 'EverythingWorld')
BUILT = os.path.join(PROJECT, 'Logs', 'Places.txt')
SETTLED = os.path.join(PROJECT, 'Assets', '_Project', 'Resources', 'ArtistPlaces.txt')
sys.stdout.reconfigure(encoding='utf-8')

def load(path):
    out = {}
    for line in open(path, encoding='utf-8'):
        f = line.rstrip('\n').split('\t')
        if len(f) == 6: out[f[0]] = (int(f[1]), float(f[2]), float(f[3]), float(f[4]), int(f[5]))
    return out

mode = sys.argv[1] if len(sys.argv) > 1 else 'check'
if mode not in ('check', 'save'): sys.exit(__doc__)
if not os.path.exists(BUILT): sys.exit('No Logs/Places.txt yet: run "python build/take_shots.py places" first (Unity closed).')
new = load(BUILT)
old = load(SETTLED) if os.path.exists(SETTLED) else {}
names = {s['id']: s['name'] for s in json.load(open(os.path.join(PROJECT, 'Assets', '_Project', 'Data', 'scenes.json'), encoding='utf-8'))['scenes']}
by = collections.defaultdict(list); moved = 0
for a, (sid, x, z, r, songs) in new.items():
    if a not in old: by[sid].append(f'new: {a} ({songs} song{"s" if songs != 1 else ""})'); continue
    osid, ox, oz, _, osongs = old[a]
    if osid != sid: by[sid].append(f'arrived from {names.get(osid, osid)}: {a}'); continue
    d = ((x - ox) ** 2 + (z - oz) ** 2) ** .5
    if d > 1.5: by[sid].append(f'moved {d:.0f} m: {a}' + (f' (songs {osongs} -> {songs})' if osongs != songs else '')); moved += 1
gone = [a for a in old if a not in new]
for sid in sorted(by, key=lambda s: names.get(s, '')):
    print(names.get(sid, sid)); [print('   ', line) for line in sorted(by[sid])]
if gone: print('No longer in the playlist:', ', '.join(sorted(gone)))
print(f'{len(new)} artists; {moved} moved, {sum(len(v) for v in by.values()) - moved} new or arrived, {len(gone)} gone.')
if mode == 'save':
    shutil.copyfile(BUILT, SETTLED)
    print('Saved as the settled places. Commit Resources/ArtistPlaces.txt (and its .meta) with the data.')
