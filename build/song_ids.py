"""Refresh every land kit's song table (Kits/*SongIds.cs) from the game's scenes.json.

A land's kit gives songs their own lights and pieces through a table of its artists' songs and track ids.
When the playlist changes, run this after export_unity.py so new songs get their lights and removed ones go:

    python build/song_ids.py           rewrite the tables
    python build/song_ids.py --check   only say what would change

Each table keeps its own top lines; only the entries between the braces are rewritten. The land a table
belongs to is worked out from the artists already in it.
"""
import collections
import glob
import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
PROJECT = os.path.join(ROOT, 'Game', 'EverythingWorld', 'Assets', '_Project')
CHECK = '--check' in sys.argv
sys.stdout.reconfigure(encoding='utf-8')

scenes = json.load(open(os.path.join(PROJECT, 'Data', 'scenes.json'), encoding='utf-8'))['scenes']
by_scene = {}
scene_of = collections.defaultdict(collections.Counter)
for s in scenes:
    if s['id'] >= 1000: continue                      # the hub and the artist stations repeat other scenes' songs
    table = collections.defaultdict(list)
    for t in s['tracks']: table[t['artist']].append((t['name'], t['id']))
    by_scene[s['id']] = (s['name'], table)
    for a, songs in table.items(): scene_of[a][s['id']] += len(songs)

def cs(text): return text.replace('\\', '\\\\').replace('"', '\\"')
ENTRY = re.compile(r'^\s*\{ "((?:[^"\\]|\\.)*)", new\[\] \{')

changed = 0
for path in sorted(glob.glob(os.path.join(PROJECT, 'Scripts', 'World', 'Kits', '*SongIds.cs'))):
    text = open(path, encoding='utf-8', newline='').read()
    nl = '\r\n' if '\r\n' in text else '\n'
    lines = text.split(nl)
    rows = [i for i, l in enumerate(lines) if ENTRY.match(l)]
    if not rows: print('no entries found in', os.path.basename(path)); continue
    old_artists = [ENTRY.match(lines[i]).group(1).replace('\\"', '"').replace('\\\\', '\\') for i in rows]
    votes = collections.Counter()
    for a in old_artists:
        for sid, n in scene_of.get(a, {}).items(): votes[sid] += 1
    sid = votes.most_common(1)[0][0]
    name, table = by_scene[sid]
    indent = lines[rows[0]][:len(lines[rows[0]]) - len(lines[rows[0]].lstrip())]
    # artists and songs already in the table keep their order (so a rerun changes only what the playlist changed);
    # new artists go in by name, new songs at the end of their artist's row
    order = [a for a in old_artists if a in table]
    for a in sorted(set(table) - set(order), key=str.casefold):
        at = next((k for k, o in enumerate(order) if o.casefold() > a.casefold()), len(order)); order.insert(at, a)
    old_ids = {a: re.findall(r'", "([A-Za-z0-9]+)"\)', lines[i]) for a, i in zip(old_artists, rows)}
    new_rows = []
    for a in order:
        have = {i: t for t, i in table[a]}
        ids = [i for i in old_ids.get(a, []) if i in have] + [i for t, i in sorted(table[a]) if i not in old_ids.get(a, [])]
        songs = ', '.join(f'("{cs(have[i])}", "{i}")' for i in ids)
        new_rows.append(f'{indent}{{ "{cs(a)}", new[] {{ {songs} }} }},')
    new_text = nl.join(lines[:rows[0]] + new_rows + lines[rows[-1] + 1:])
    if new_text != text:
        changed += 1
        old_set = {l.strip() for l in lines[rows[0]:rows[-1] + 1]}; new_set = {l.strip() for l in new_rows}
        gone = sorted(set(old_artists) - set(table)); came = sorted(set(table) - set(old_artists))
        print(f'{os.path.basename(path)} ({name}): {len(new_set - old_set)} rows changed'
              + (f', new artists: {", ".join(came)}' if came else '') + (f', left: {", ".join(gone)}' if gone else ''))
        if not CHECK: open(path, 'w', encoding='utf-8', newline='').write(new_text)
print(f'{changed} tables', 'would change' if CHECK else 'rewritten')
