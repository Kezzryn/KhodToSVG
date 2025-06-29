# Daggerheart Kohd to Scalable Vector Graphic

This is a command line tool to generate Kohd word glyphs in .svg format.

## Description

Kohd is a written language for the Daggerheart RPG. It is featured in the Motherboard campaign frame.

This is a tool for Game Masters to help them create Kohd word-glyphs for their players.

This software is currently an early release and feature incomplete.

Kohd was craeted by [u/Admire_the_Cipher](https://www.reddit.com/user/Admire_the_Cipher/)

## Features and Limitations

### Rolling With Hope
I've proven the core concept and can create most glyphs. More complicated ones may timeout. A Kohd-glyph will always be generated, but some complicated words may have traces that cross over other paths.

All generated Kohd-glyphs are saved as SVG files that can be edited in a graphics editor.

### Rolling With Fear
There are pathfinding annoyances that need to be smoothed out. 
Complicated Kohd-glyphs may not fully path properly.

## Future adventures
This software is NOT feature complete. This is an initial proof of concept that's now in a state I'm comfortable to share.

Things to do in no particular order:
* Null nodes
* Charge and ground trace re-positioning and scaling
* Ground trace look.
* Better HTML and SVG generation
* CSS support
* .config file to control internal program variables and switches
* Remapping which letters go to which node
* Sentence support including: 
* * punctuation
* * Sentence trace lines
* * Bus bar
* Better error handling 
* Better error and debug logging
* Performance and pathfinding optimizations
* Better (or rather any) pathfinding loop detection and timeouts

## Installing
Note: The program is built for Windows with an x64 CPU.

Extract the zip file to a folder. The zip files contains a single executable file.

## Executing program

Double click the executable to run.
-or- 
Kohdtosvg can be invoked from the command line.

Command line example: 
`'c:\temp\>Kohdtosvg -f "output_file.html" -t "text to translate"`

### Command line Switches 
```
Filename to output to: -f "output_file.html" 
Text to translate: -t "text to translate"
Help: -h
Stopwatch: -s
```

### Notes
The program will create a `cache` folder and output individual `.svg` files into that folder. It will use the contents of this folder to avoid re-generating Kohd-gylphs.


## Authors
    Reddit: [u/RaveBomb](https://www.reddit.com/user/RaveBomb/)

## Version History
* 0.0.1.0
    * Initial Release

## License
This project is licensed under the [Creative Comms Attribution Share Alike 4.0 International] License - see the [LICENSE.md](LICENSE.md) file for details.