# paperWiz
**Made for people with  HiDPI and / or Multiple Monitors.**
This script pulls a colour from you wallpaper and does 2 things with it,

 - Sets your second monitor as said colour.  -> You no longer have to look for a matching wallpaper / have the same one for both / tile the wallpaper across and have parts of it cut
 - Surrounds the wallpaper in said colour if the image is lower than your monitors resolution. -> You no longer have to worry about wallpapers being too low resolution for you monitor, and can also use vertical wallpapers.



## Installation

`git clone https://github.com/chrisJuresh/paperWiz.git`

`cd paperWiz`

`chmod +x install-paperWiz`

`./install-paperWiz`

`chmod +x paperWiz`

You can either run the program with ./paperWiz or put the paperWiz file in your $PATH

Remeber to change resw and resh to your main monitors resolutions 

## Usage

`paperWiz /your/wallpaper` to use color4 from pywal

`paperWiz /your/wallpaper 2` to use the most common colour in the image (extracted from imagemagick)

I recommending adding this script to a bind in sxiv as such;

```
#!/bin/sh
while read file
do
	case "$1" in
		"Return") paperWiz "$file" & ;;
		"backslash") paperWiz "$file" 2 & ;;
	esac
done
```


## Dependencies

pywal (for color4)

imagemagick (for both)
