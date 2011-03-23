LuaSharp
========

A pair of libraries which provide both low and high level interfaces to the Lua API from .NET/Mono.
Written in C# using MonoDevelop, should support Windows and Visual Studio out of the box.

Operating System Quirks
=======================
Some quirks in terms of some operating system behaving strangely with Lua.

Ubuntu
------
You may need to do this for Ubuntu 10.10 (Maverick).

    sudo apt-get install liblua5.1-0
    sudo ln -s /usr/bin/liblua5.1.so.0 /usr/bin/liblua5.1.so
    sudo ln -s /usr/bin/liblua5.1.so.0 /usr/bin/liblua51.so

Windows
-------
Make sure you have both the Lua DLLs in your System32 folder or in your application folder.
