from pathlib import Path
from PIL import Image, ImageDraw, ImageFont


ROOT = Path(__file__).resolve().parents[1]
ASSET_DIR = ROOT / "src" / "ChronoOverlay" / "Assets"
ASSET_DIR.mkdir(parents=True, exist_ok=True)

canvas = Image.new("RGBA", (256, 256), (0, 0, 0, 0))
draw = ImageDraw.Draw(canvas)
draw.rounded_rectangle((20, 20, 236, 236), radius=50, fill=(22, 26, 31, 255), outline=(91, 104, 120, 255), width=5)

font_candidates = [
    Path("C:/Windows/Fonts/seguisb.ttf"),
    Path("C:/Windows/Fonts/segoeuib.ttf"),
    Path("C:/Windows/Fonts/arialbd.ttf"),
]
font_path = next(path for path in font_candidates if path.exists())
font = ImageFont.truetype(str(font_path), 156)
bounds = draw.textbbox((0, 0), "C", font=font)
x = (256 - (bounds[2] - bounds[0])) / 2 - bounds[0]
y = (256 - (bounds[3] - bounds[1])) / 2 - bounds[1] - 3
draw.text((x, y), "C", font=font, fill=(239, 243, 247, 255))

canvas.save(
    ASSET_DIR / "ChronoOverlay.ico",
    format="ICO",
    sizes=[(16, 16), (20, 20), (24, 24), (32, 32), (40, 40), (48, 48), (64, 64), (128, 128), (256, 256)],
)
canvas.save(ASSET_DIR / "ChronoOverlay.png", format="PNG")
