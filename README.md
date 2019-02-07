# FF6 Battle Background Palettes Helper

## Binaries

To get the binaries, backgrounds and palettes, download from one of 
these two sources:

[FF6hacking wiki](https://www.ff6hacking.com/wiki/doku.php?id=sprite:bgs)  
[RHDN entry](http://www.romhacking.net/utilities/1321/)

## Palette Exporter

Located in the "palette exporter" folder. This utility simply convert all 
backgrounds palettes into Microsoft RIFF palette format that you can import 
in Gimp. This is however not what you will manually do. Those palette files 
are later neccesary for the image converter to work. To export all the 
palettes, simply place a headerless FF3us ROM in the same folder as the 
exporter and double click on the exporter executable. A folder with your ROM 
name will be created with all the palettes files inside.

#2 Image Converter
------------------
Located in the "image converter" folder. In order for the background image to 
have the same palette order than the ROM, we need to convert existing BBGs rips 
from non-8bpp-indexed PNG format to 8bpp indexed 256 format with correct palette 
order. To do this you need at least one BBG rip named "BG_XX.png" where XX is 
the BBG FF3us ID in hexadecimal. The second thing need is the BBG palette file 
named "BG_XX_PAL_YY.bin". YY is the FF3us BBG palette index. This correct filename 
is generated when exporting palettes (see #1) and palette ID for the BBG is fetched 
from BBG data. You need one palette file and one BBG PNG rip matching for a single 
succesfull conversion. You therefore can have the 54 palettes files and 54 BBG rips 
in the same folder as the converter and launch all images conversions at once by 
clicking on the converter. 

#3 Palette Importer
-------------------
Put you exported Gimp .txt palette file(s) in the "palette importer folder". 
Your palette file must be named "BG_XX.txt" where XX is the FF3us ID of the BBG you 
just edited in gimp. You also need your ROM in the same folder as the importer. 
Click on the importer and it will fetch the correct palette ID for the BBG(s) and 
import the palette file(s) in the ROM. Quick and simple!