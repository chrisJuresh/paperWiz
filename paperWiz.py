#!/usr/bin/env python3

import argparse
import os
import subprocess
import shutil
from PIL import Image

from line_profiler import LineProfiler

# Set your desired resolution here
resW = 2560
resH = 1440
smallwallres = 1080

###################################

cache = os.path.join(os.environ['HOME'], '.cache', 'paperWiz')


def print_help():
    print(f"""Usage: {os.path.basename(__file__)} [OPTIONS]
Options:
  -w WALLPAPER_PATH   Path to the wallpaper you want to set
  -p POSITION         Set the position of the wallpaper on the main monitor (c: center (default), s: south, n: north, w: west,e: east)
  -c [WALCOLOR]       Set the color for the second monitor (0-15: color0-color15 from pywal, -1: main color from wallpaper (default))
  -s                  Shrink the wallpaper's vertical resolution to {smallwallres}
  -h, --help          Display this help menu and exit
Example:
  python {os.path.basename(__file__)} -w /path/to/wallpaper.jpg -p s -c 0 -s
""")


def main():
    parser = argparse.ArgumentParser(add_help=False)
    parser.add_argument("-w", "--wallpaper", required=True)
    parser.add_argument("-p", "--position", default="c")
    parser.add_argument("-c", "--walColor", default="-1")
    parser.add_argument("-s", "--shrink_res", action="store_true")
    parser.add_argument("-h", "--help", action="store_true")

    args = parser.parse_args()

    if args.help:
        print_help()
        exit(0)

    wallpaper = args.wallpaper
    position = args.position
    walColor = args.walColor
    shrink_res = args.shrink_res

    # Check if the color specified is in the right range
    if int(walColor) < -1 or int(walColor) > 15:
        print("Error: The color specified must be between -1 and 15")
        exit(1)

    # Set and check the position variable
    position = position.lower()
    if position not in ["c", "n", "s", "e", "w"]:
        print("Error: The position specified must be c, s, n, w, or e")
        exit(1)

    subprocess.run(["wpg", "-a", wallpaper],
                   stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    subprocess.run(["wpg", "-s", os.path.basename(wallpaper), "-n"],
                   stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    grey = os.path.join(cache, "grey.png")
    subprocess.run(["wal", "-i", wallpaper, "-n"],
                   stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    subprocess.run(["xdotool", "key", "Alt_L+F5"],
                   stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    subprocess.run(["telegram-palette-gen", "--wal"],
                   stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    subprocess.run(["waldunst"], stdout=subprocess.DEVNULL,
                   stderr=subprocess.DEVNULL)
    subprocess.Popen(["dunst"], stdout=subprocess.DEVNULL,
                     stderr=subprocess.DEVNULL)

    wall = os.path.join(cache, "wall.png")
    wallImg = Image.open(wallpaper)
    if shrink_res:
        wallImg.thumbnail((2560, smallwallres))

    wallHex = None

    if int(args.walColor) != -1:
        # Fetch color
        with open(os.path.join(os.environ['HOME'], '.cache', 'wal', 'colors.sh'), 'r') as f:
            lines = f.readlines()

        for line in lines:
            if f"color{args.walColor}" in line:
                wallHex = line.split("'")[1]
                break

        smallColorImg = Image.new("RGB", (2, 2), wallHex)
    else:
        # Fetch the main color in the wallpaper

        histogramImg = wallImg.copy()
        # Adjust the size (100, 100) as needed
        histogramImg.thumbnail((70, 70))

        # Quantize the image to reduce the color space

        # Find the most frequent color in the quantized image
        wallHex = sorted(histogramImg.getcolors(
            2 ** 24), reverse=True)[0][1]

        smallColorImg = Image.new("RGB", (2, 2), wallHex)

    # Check if the wallpaper is larger than the monitor
    wallresW, wallresH = wallImg.size

    smallColorImgPath = os.path.join(cache, "smallColorImg.png")
    if wallresW >= resW and wallresH >= resH:
        # Use default wallpaper
        subprocess.run(["feh", "--bg-fill", wall, smallColorImgPath],
                       stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)
    else:
        # Create a color image matching the size of the main monitor
        largeColorImg = Image.new("RGB", (resW, resH), wallHex)

        # Apply the wallpaper on top of the color image in the position specified
        img1 = wallImg.convert('RGBA')
        img2 = largeColorImg.convert('RGBA')
        img3 = smallColorImg.convert('RGBA')
        img1_copy = img1.copy()
        pos_map = {
            "c": (int((resW - img1.width) / 2), int((resH - img1.height) / 2)),
            "n": (int((resW - img1.width) / 2), 0),
            "s": (int((resW - img1.width) / 2), resH - img1.height),
            "e": (resW - img1.width, int((resH - img1.height) / 2)),
            "w": (0, int((resH - img1.height) / 2))
        }
        pos = pos_map[args.position]
        img2.paste(img1_copy, pos, img1_copy)
        img3.save(smallColorImgPath, "PNG")
        overlayedImg = os.path.join(cache, "overlayedImg.png")
        img2.save(overlayedImg, "PNG", compress_level=0)
        subprocess.run(["feh", "--bg-fill", overlayedImg, smallColorImgPath],
                       stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)

    # Send notification
    subprocess.run(["notify-send", "paperWiz", "Wallpaper set"],
                   stdout=subprocess.DEVNULL, stderr=subprocess.DEVNULL)


if __name__ == "__main__":
    lp = LineProfiler()
    lp_wrapper = lp(main)
    lp_wrapper()
    lp.print_stats()
