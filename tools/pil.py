
from PIL import Image
import os

img = Image.open("/mnt/Nvme/fm/fm_g/fm/Assets/database.png").convert("RGB")
W, H = img.size
px = img.load()

def is_red(r, g, b):
    return r > 180 and g < 80 and b < 80

def find_separators_vertical():
    cols = []
    for x in range(W):
        red_pixels = sum(is_red(*px[x, y]) for y in range(H))
        if red_pixels > H * 0.15:   # ← thin lines, not full height
            cols.append(x)
    return cols

def find_separators_horizontal():
    rows = []
    for y in range(H):
        red_pixels = sum(is_red(*px[x, y]) for x in range(W))
        if red_pixels > W * 0.15:
            rows.append(y)
    return rows

def compress(lines, gap=3):
    groups = []
    for v in lines:
        if not groups or v - groups[-1][-1] > gap:
            groups.append([v, v])
        else:
            groups[-1][1] = v
    return [(a + b) // 2 for a, b in groups]

x_sep = compress(find_separators_vertical())
y_sep = compress(find_separators_horizontal())

# Add image borders
x_edges = [0] + x_sep + [W]
y_edges = [0] + y_sep + [H]

os.makedirs("out", exist_ok=True)

card_id = 1
for r in range(len(y_edges) - 1):
    for c in range(len(x_edges) - 1):
        left = x_edges[c] + 1
        right = x_edges[c + 1] - 1
        top = y_edges[r] + 1
        bottom = y_edges[r + 1] - 1

        if right - left > 40 and bottom - top > 40:
            img.crop((left, top, right, bottom)) \
               .save(f"out/card_{card_id:03}.png")
            card_id += 1

print(f"Extracted {card_id - 1} cards")

