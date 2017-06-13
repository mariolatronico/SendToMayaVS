# Send to Maya plugin
This Visual Studio 2017 plugin sends the active text editor content to Maya via a socket opened on an IP address.
By default the IP address is localhost and the port is 7720.

In maya be sure to run this code (usually via userSetup.py in C:\Users\THEUSER\Documents\maya\scripts\userSetup.py):
```python
    if cmds.commandPort(':7720', q=True) !=1:
        cmds.commandPort(n=':7720', eo = False, nr = True, stp='python')
```

