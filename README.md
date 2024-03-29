# paperWiz
![Preview](https://imgur.com/iNTDo3D.gif)

**Made for people with  HiDPI and / or Multiple Monitors.**

This script pulls a colour from your wallpaper and does 2 things with it,

 - Sets your second monitor as said colour.  -> You no longer have to look for a matching wallpaper / have the same one for both / tile the wallpaper across and have parts of it cut
 - Surrounds the wallpaper in said colour if the image is lower than your monitors resolution. -> You no longer have to worry about wallpapers being too low resolution for you monitor, and can also use vertical wallpapers.

**For vertical wallpapers you may want to downsize them with,**
 
`convert yourimage.png  -resize 600x1067\>  shrunk.png`

to ensure that they are centered rather than touching the top and bottom of your screen. Adjust the size to suit your display resolution.

## Installation

`git clone https://github.com/chrisJuresh/paperWiz.git`

`cd paperWiz`

`chmod +x install-paperWiz`

`./install-paperWiz`

`chmod +x paperWiz`

You can either run the program with ./paperWiz or put the paperWiz file in your $PATH

Remeber to change `resw` and `resh` to your main monitors resolutions.

Set your `smallwallres` to the vertical resolution you would like your wallpapers to be shrunk to (if you specify it). I recommend doing [ Vertical resolution of your main monitor / 9 x 7 ]

## Usage

Description:
  `paperWiz` is a script to set a wallpaper on two monitors. The first monitor is set with the chosen wallpaper and the second monitor is set with either the main color of the wallpaper or a color chosen from pyWal's cache.

  * `-w`: The path to the **w**allpaper you want to set. This option is required.
  
  * `-p`: Set the **p**osition of the wallpaper on the main monitor (c: center (default), s: south, n: north, w: west,e: east)
  
  * `-c`: Set the **c**olor for the second monitor. Options: 0-15 for color0-color15 from pywal, -1 for main color from wallpaper (default).

  * `-s`: **S**hrink the wallpaper.
  
  * `-h`: Display this **h**elp menu and exit.

Example:
  ./paperWiz -w /path/to/wallpaper.jpg -p s -c 0 -s

I recommending adding this script to a bind in sxiv as such;

```
#!/bin/sh
while read file
do
	case "$1" in
		"equal") paperWiz -w "$file" -c 4 & ;;
		"minus") paperWiz -w "$file" -c 4 -s & ;;
		"0") paperWiz -w "$file" & ;;
		"9") paperWiz -w "$file" -s & ;;
		"8") paperWiz -w "$file" -p s & ;;
	esac
done
```

## Dependencies

pywal (for pywal colors)

imagemagick 

feh 
