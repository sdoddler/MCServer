# MCServer
### Why?
I began this project after some friends and I started playing minecraft again. I looked for some basic server wrappers for vanilla minecraft. 
Most are either out of date (version specific), paid web interfaces, or based on linux systems. 
This is something to run for the home user in the Windows environment.

## Features
This is a basic Minecraft Server Wrapper for **Windows** with the following features:
- Arguement options (Ram Min/Max & nogui)
- Direct command input
- Direct output reader (run nogui, and still know whats going on)
- Server messages (with options for how frequent & colour)
- Auto Start server on run
- Server Restarts 
  - Restart at Time of day
  - Restart after specific uptime
  - Restart PC as well
  - Chat warnings (60,30,15,10,5,2,1 Minutes)


## How to use
1. Go to the [Releases](https://github.com/sdoddler/MCServer/releases) page and download the latest .zip.
2. Extract latest zip wherever you like (I find it easier to pop it in the same location as Server.jar)
3. Run the **.exe** and browse for your **server.jar**
4. Set your options how you like
   1. If you setup to restart PC put a shortcut to the .exe in: C:\ProgramData\Microsoft\Windows\Start Menu\Programs\StartUp so that MCServer starts after the restart
5. ????
6. Mine for diamonds

### Potential Upcoming Features
I mostly made this as our friends have been playing again, if people are interested i can continue this project and add some of the following:

- Backups (either timed, or when the server restarts)
- Batch commands to run at press of a button or at time of day
- Give command interface (Give yourself ALL the diamonds)
- More user friendly Server messages section with better color options
- Better reading of server output
- Cleaned up interface

Open to other ideas as well provided they're possible
