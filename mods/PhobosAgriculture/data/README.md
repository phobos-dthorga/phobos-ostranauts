# Native loader directory

Ostranauts requires this `data` directory to recognize the native mod as present.
Agriculture registers its definitions through its plugin; no duplicate JSON
definitions are needed here. Keep this file in source, packages and installations
so the required directory survives Git checkout and file-based installation.
