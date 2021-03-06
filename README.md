# paperWiz
![Preview](https://imgur.com/iNTDo3D.gif)

**Made for people with  HiDPI and / or Multiple Monitors.**

This script pulls a colour from you wallpaper and does 2 things with it,

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

`paperWiz /your/wallpaper 1` to use color4 from pywal

`paperWiz /your/wallpaper 2` to use the most common colour in the image (extracted from imagemagick)

`paperWiz /your/wallpaper 1 2` use color4 and push the image to the bottom of the screen (replace 1 with 2 to use the most common colour)

`paperWiz /your/wallpaper 1 1 1` to use color 4 from pywal and shrink the image to a vertical resolution of 1120 (replace the first two 1's to use the most common color and to send the image to the bottom of the screen respectively)

I recommending adding this script to a bind in sxiv as such;

```
#!/bin/sh
while read file
do
	case "$1" in
		"equal") paperWiz "$file" 1 & ;;
		"minus") paperWiz "$file" 1 1 1 & ;;
		"0") paperWiz "$file" 2 & ;;
		"9") paperWiz "$file" 2 1 1 & ;;
		"8") paperWiz "$file" 2 2 & ;;
	esac
done
```


## Dependencies

pywal (for color4)

imagemagick (for both)
