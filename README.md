# MoshRunner

This is a simple wrapper to start the MSys2 version of Mosh without
requiring a complete MSys2 installation. It uses PLink to authenticate with
the Mosh server and does not support password authentication (i.e. it does
not query you for a password). Instead, it expects PAgeant to be running
with a valid certificate for the server you're connecting to.

To use this application, go to the releases tab. Download the ZIP file there
and extract it to your local drive. The Mosh.exe application is used to
start Mosh.