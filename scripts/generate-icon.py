from pathlib import Path

from PIL import Image, ImageEnhance, ImageFilter


ROOT = Path(__file__).resolve().parents[1]
ASSET_DIR = ROOT / "src" / "ChronoOverlay" / "Assets"
MASTER_PATH = ASSET_DIR / "ChronoOverlay-master.png"
ICON_PATH = ASSET_DIR / "ChronoOverlay.ico"
PNG_PATH = ASSET_DIR / "ChronoOverlay.png"
FAVICON_PATH = ROOT / "site" / "public" / "favicon.png"
ICON_SIZES = (16, 20, 24, 32, 40, 48, 64, 128, 256)


def render_frame(master: Image.Image, size: int) -> Image.Image:
    frame = master.resize((size, size), Image.Resampling.LANCZOS)
    if size > 32:
        return frame

    # Small notification-area icons lose the paper texture and low-contrast edges.
    # A restrained contrast/color lift and sharpening preserve the selected artwork
    # while keeping its sun, floating planes, and horizon legible at 16-24 pixels.
    alpha = frame.getchannel("A")
    rgb = frame.convert("RGB")
    rgb = ImageEnhance.Color(rgb).enhance(1.12)
    rgb = ImageEnhance.Contrast(rgb).enhance(1.08)
    rgb = rgb.filter(ImageFilter.UnsharpMask(radius=0.7, percent=145, threshold=2))
    frame = rgb.convert("RGBA")
    frame.putalpha(alpha)
    return frame


def main() -> None:
    if not MASTER_PATH.exists():
        raise FileNotFoundError(f"Missing icon master: {MASTER_PATH}")

    ASSET_DIR.mkdir(parents=True, exist_ok=True)
    FAVICON_PATH.parent.mkdir(parents=True, exist_ok=True)

    with Image.open(MASTER_PATH) as source:
        master = source.convert("RGBA")

    frames = {size: render_frame(master, size) for size in ICON_SIZES}
    frames[256].save(
        ICON_PATH,
        format="ICO",
        sizes=[(size, size) for size in ICON_SIZES],
        append_images=[frames[size] for size in ICON_SIZES if size != 256],
    )
    frames[256].save(PNG_PATH, format="PNG", optimize=True)
    frames[256].save(FAVICON_PATH, format="PNG", optimize=True)


if __name__ == "__main__":
    main()
