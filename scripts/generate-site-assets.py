from pathlib import Path
import colorsys

from PIL import Image


ROOT = Path(__file__).resolve().parents[1]
ASSET_DIR = ROOT / "site" / "src" / "assets"
ASSET_DIR.mkdir(parents=True, exist_ok=True)

width, height = 720, 32
image = Image.new("RGB", (width, height))
pixels = image.load()
for x in range(width):
    red, green, blue = colorsys.hsv_to_rgb(x / (width - 1), 0.88, 1.0)
    color = (round(red * 255), round(green * 255), round(blue * 255))
    for y in range(height):
        pixels[x, y] = color

image.save(ASSET_DIR / "hue-spectrum.png", optimize=True)
