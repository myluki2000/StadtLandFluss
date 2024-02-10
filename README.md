# Setup

## Server

The server requires 3 extra files in its executable directory (the same directory the SlfServer.exe file lies in):

* stadt.txt
* land.txt
* fluss.txt

These files contain the valid solutions for the three categories, separated by newlines.

Pre-made files containing valid answers for the continent of Europe (extracted from OpenStreetMap data) can be
downloaded at http://api.lutr.me/stadtlandfluss.zip

## Client

The client creates a config.txt which stores the client ID between sessions. If you copy the executable to a different computer to run it on multiple computers, make sure to 
delete the config.txt to make sure both machines have different client IDs, otherwise there will be ID conflicts on the server and stuff will break in unexpected ways.
