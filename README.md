# MoshRunner

This is a simple wrapper to start the MSys2 version of Mosh without
requiring a complete MSys2 installation. It uses PLink to authenticate with
the Mosh server. If you have a certificate and PAgeant running in the
background, that will be used to authenticate you. Otherwise, you'll
be queried for a password.

To use this application, go to the releases tab. Download the ZIP file there
and extract it to your local drive. The Mosh.exe application is used to
start Mosh.

The user name and host name can also be provided on the command line. To use this,
add the user and host name in the format <user>@<host>.